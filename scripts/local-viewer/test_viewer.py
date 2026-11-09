"""Small offline checks; no media files, database or provider calls."""

import json
import unittest
import tempfile
import threading
from http.client import HTTPConnection
from http.server import ThreadingHTTPServer
from pathlib import Path
from types import SimpleNamespace

from serve import byte_range, load_matches, load_catalog, make_handler


class ViewerTests(unittest.TestCase):
    def result(self, **overrides):
        match = {"videoFileName": "sample.mp4", "startTimeSeconds": 1.5,
                 "endTimeSeconds": 1.5, "similarity": 98.6,
                 "sourceProvider": "AwsRekognition", "targetKey": "private-target"}
        match.update(overrides)
        payload = {"status": "Completed", "jobId": "private-job", "issues": [],
                   "faceMatches": [match]}
        return SimpleNamespace(read_bytes=lambda: json.dumps(payload).encode("utf-16"))

    def test_utf16_result_exposes_only_viewer_fields(self):
        result = load_matches(self.result(), Path("sample.mp4"))
        self.assertEqual(result, {"status": "Completed", "issuesCount": 0,
                                  "matches": [{"start": 1.5, "end": 1.5,
                                               "similarity": 98.6}]})

    def test_wrong_video_and_invalid_timestamps_are_rejected(self):
        with self.assertRaises(ValueError):
            load_matches(self.result(), Path("different.mp4"))
        with self.assertRaises(ValueError):
            load_matches(self.result(startTimeSeconds=-1), Path("sample.mp4"))

    def test_ranges_allow_seeking(self):
        self.assertEqual(byte_range(None, 100), (0, 99))
        self.assertEqual(byte_range("bytes=20-29", 100), (20, 29))
        self.assertEqual(byte_range("bytes=90-", 100), (90, 99))
        self.assertEqual(byte_range("bytes=-10", 100), (90, 99))
        for header in ["bytes=100-", "bytes=40-20", "bytes=-0", "bytes=-"]:
            with self.assertRaises(ValueError):
                byte_range(header, 100)

    def test_gallery_keeps_missing_summary_and_detailed_results_separate(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            api, summary = root / "api.json", root / "summary.json"
            api.write_bytes(self.result().read_bytes())
            summary.write_text(json.dumps([
                {"ObjectKey": "private/source.mp4", "Matches": 0, "Status": "Completed",
                 "Issues": "private message", "JobId": "private-job"},
                {"ObjectKey": "private/sample.mp4", "Matches": 17, "Status": "Completed", "Issues": ""}
            ]))
            catalog = load_catalog([Path("unknown.mp4"), Path("source.mp4"), Path("sample.mp4")], [api, summary])
            self.assertEqual([r["availability"] for r in catalog], ["unavailable", "summary", "available"])
            self.assertEqual([r["totalMatches"] for r in catalog], [None, 0, 1])
            self.assertTrue(catalog[1]["hasIssues"])
            self.assertEqual(catalog[1]["matches"], [])
            self.assertNotIn("private", json.dumps(catalog))

    def test_empty_api_is_not_assigned_to_every_video(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "empty.json"
            path.write_text(json.dumps({"status": "Completed", "faceMatches": [], "issues": []}))
            result = load_catalog([Path("a.mp4"), Path("b.mp4")], [path])
            self.assertTrue(all(r["availability"] == "unavailable" for r in result))
            self.assertEqual(load_matches(path, Path("a.mp4"))["matches"], [])

    def test_google_api_maps_number_and_face_times_without_identity_data(self):
        row = {"videoFileName": "sample.mp4", "startTimeSeconds": 2, "endTimeSeconds": 3,
               "confidence": 0.85, "sourceProvider": "GoogleVideoIntelligence", "visibleNumber": "private-text"}
        data = {"status": "Completed", "issues": [], "targetMatches": [{"matches": [row]}],
                "faceOccurrences": [{**row, "startTimeSeconds": 1}]}
        result = load_matches(SimpleNamespace(read_bytes=lambda: json.dumps(data).encode()), Path("sample.mp4"))
        self.assertEqual([r["kind"] for r in result["matches"]], ["detection", "number"])
        self.assertEqual(result["matches"][0]["similarity"], 85)
        self.assertNotIn("private-text", json.dumps(result))
        row["confidence"] = 85
        with self.assertRaises(ValueError):
            load_matches(SimpleNamespace(read_bytes=lambda: json.dumps(data).encode()), Path("sample.mp4"))

    def test_multiple_detailed_runs_are_not_silently_merged(self):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / "api.json"
            path.write_bytes(self.result().read_bytes())
            with self.assertRaises(ValueError):
                load_catalog([Path("sample.mp4")], [path, path])

    def test_http_serves_only_selected_media_ranges_and_sanitized_catalog(self):
        with tempfile.TemporaryDirectory() as folder:
            videos = [Path(folder) / name for name in ["a.mp4", "b.mp4"]]
            videos[0].write_bytes(b"first-video")
            videos[1].write_bytes(b"second-video")
            catalog = load_catalog(videos, [])
            server = ThreadingHTTPServer(("127.0.0.1", 0), make_handler(videos[0], catalog[0], "/session/", catalog, videos))
            thread = threading.Thread(target=server.serve_forever, daemon=True)
            thread.start()
            connection = HTTPConnection("127.0.0.1", server.server_port)
            try:
                connection.request("GET", "/session/media/1", headers={"Range": "bytes=0-5"})
                response = connection.getresponse()
                self.assertEqual(response.status, 206)
                self.assertEqual(response.getheader("Content-Range"), "bytes 0-5/12")
                self.assertEqual(response.read(), b"second")
                for path in ["/session/media/2", "/session/media/../a.mp4", "/catalog", "/session/api.json"]:
                    connection.request("GET", path)
                    response = connection.getresponse()
                    self.assertEqual(response.status, 404)
                    response.read()
                connection.request("GET", "/session/catalog")
                response = connection.getresponse()
                self.assertEqual(response.status, 200)
                body = response.read().decode()
                self.assertNotIn(folder, body)
                self.assertEqual(len(json.loads(body)["videos"]), 2)
            finally:
                connection.close()
                server.shutdown()
                server.server_close()
                thread.join()


if __name__ == "__main__":
    unittest.main()
