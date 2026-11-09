"""Read-only, loopback-only gallery for existing videos and saved results."""

import argparse
import json
import math
import mimetypes
import re
import secrets
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path, PureWindowsPath
from urllib.parse import urlsplit


def load_matches(result_path, video_path):
    """Expose only occurrence times and similarity, never the original JSON."""
    data = json.loads(result_path.read_bytes())  # Supports UTF-8 and UTF-16 exports.
    if not isinstance(data, dict):
        raise ValueError("Usa un JSON de respuesta API de AWS o Google.")
    if isinstance(data.get("faceMatches"), list):
        items = [(item, "face") for item in data["faceMatches"]]
    elif isinstance(data.get("targetMatches"), list) and isinstance(data.get("faceOccurrences"), list):
        items = []
        for target in data["targetMatches"]:
            if not isinstance(target, dict) or not isinstance(target.get("matches"), list):
                raise ValueError("El resultado Google contiene un target no válido.")
            items.extend((item, "number") for item in target["matches"])
        items.extend((item, "detection") for item in data["faceOccurrences"])
    else:
        raise ValueError("Usa el JSON API con faceMatches o targetMatches/faceOccurrences.")
    issues = data.get("issues", [])
    if not isinstance(issues, list):
        raise ValueError("El campo issues debe ser una lista.")
    rows = []
    for item, kind in items:
        if not isinstance(item, dict):
            raise ValueError("El resultado contiene una coincidencia no válida.")
        filename = item.get("videoFileName", "")
        if not isinstance(filename, str):
            raise ValueError("La coincidencia no identifica un video válido.")
        if PureWindowsPath(filename).name.casefold() != video_path.name.casefold():
            continue
        score_key = "similarity" if kind == "face" else "confidence"
        start, end, similarity = (item.get(key) for key in
                                  ("startTimeSeconds", "endTimeSeconds", score_key))
        if any(isinstance(v, bool) or not isinstance(v, (int, float)) or
               not math.isfinite(v) for v in (start, end, similarity)):
            raise ValueError("Los tiempos y la similitud deben ser números finitos.")
        if start < 0 or end < start or not 0 <= similarity <= (100 if kind == "face" else 1):
            raise ValueError("Hay tiempos o similitudes fuera de rango.")
        expected_provider = "AwsRekognition" if kind == "face" else "GoogleVideoIntelligence"
        if item.get("sourceProvider") != expected_provider:
            raise ValueError("El proveedor del resultado no corresponde a su formato.")
        row = {"start": start, "end": end, "similarity": similarity}
        if kind != "face":
            row.update(kind=kind, similarity=similarity * 100)
        rows.append(row)
    if items and not rows:
        raise ValueError("El nombre del video no coincide con el resultado guardado.")
    rows.sort(key=lambda row: row["start"])
    status = data.get("status")
    return {"status": status if isinstance(status, str) and status in {"Completed", "Failed", "Pending", "Running"}
            else "Unknown", "issuesCount": len(issues), "matches": rows}


def load_catalog(videos, result_paths):
    """Associate explicit exports by basename; summaries never invent a timeline."""
    sources = [(path, json.loads(path.read_bytes())) for path in result_paths]
    catalog = []
    for index, video in enumerate(videos):
        detailed, summaries = [], []
        for path, data in sources:
            if isinstance(data, list):
                for row in data:
                    if not isinstance(row, dict) or not isinstance(row.get("ObjectKey"), str):
                        continue
                    if PureWindowsPath(row["ObjectKey"]).name.casefold() == video.name.casefold():
                        count = row.get("Matches")
                        if isinstance(count, bool) or not isinstance(count, int) or count < 0:
                            raise ValueError("El resumen contiene un conteo no válido.")
                        issues = row.get("Issues")
                        if not isinstance(issues, (str, list)):
                            raise ValueError("El resumen no especifica sus incidencias.")
                        status = row.get("Status")
                        summaries.append({"availability": "summary", "provider": "AWS Rekognition",
                                          "status": status if isinstance(status, str) and status in {"Completed", "Failed", "Pending", "Running"} else "Unknown",
                                          "issuesCount": None, "hasIssues": bool(issues),
                                          "totalMatches": count, "matches": []})
                continue
            if not isinstance(data, dict):
                raise ValueError("Formato de resultado no reconocido.")
            if isinstance(data.get("faceMatches"), list):
                rows = data["faceMatches"]
                provider = "AWS Rekognition"
            elif isinstance(data.get("targetMatches"), list) and isinstance(data.get("faceOccurrences"), list):
                rows = list(data["faceOccurrences"])
                for target in data["targetMatches"]:
                    if not isinstance(target, dict) or not isinstance(target.get("matches"), list):
                        raise ValueError("El resultado Google contiene un target no válido.")
                    rows.extend(target["matches"])
                provider = "Google Video Intelligence"
            else:
                raise ValueError("Usa resultados API o el resumen comparativo, no exportaciones AWS brutas.")
            names = {PureWindowsPath(row["videoFileName"]).name.casefold() for row in rows
                     if isinstance(row, dict) and isinstance(row.get("videoFileName"), str)}
            # An empty API result has no filename: never attach it to every video.
            if video.name.casefold() not in names:
                continue
            result = load_matches(path, video)
            result.update(availability="available", provider=provider,
                          totalMatches=len(result["matches"]), hasIssues=bool(result["issuesCount"]))
            detailed.append(result)
        if len(detailed) > 1 or (not detailed and len(summaries) > 1):
            raise ValueError("Hay varios resultados para un video. Selecciona una ejecución por video.")
        result = (detailed or summaries or [{"availability": "unavailable", "provider": None,
                  "status": None, "issuesCount": None, "hasIssues": False,
                  "totalMatches": None, "matches": []}])[0]
        catalog.append({"id": str(index), "label": video.name, **result})
    return catalog


def byte_range(header, size):
    """Resolve a single HTTP range, including suffix ranges, for video seeking."""
    if not header:
        return 0, size - 1
    match = re.fullmatch(r"bytes=(\d*)-(\d*)", header.strip())
    if not match or not any(match.groups()):
        raise ValueError("Invalid range")
    left, right = match.groups()
    if left:
        start, end = int(left), min(int(right), size - 1) if right else size - 1
    else:
        length = int(right)
        if length <= 0:
            raise ValueError("Invalid suffix")
        start, end = max(0, size - length), size - 1
    if start >= size or start > end:
        raise ValueError("Unsatisfiable range")
    return start, end


def make_handler(video_path, result, prefix, catalog=None, videos=None):
    assets = Path(__file__).parent
    videos = videos or [video_path]
    catalog = catalog or [{"id": "0", "label": video_path.name, "availability": "available",
                           "provider": "AWS Rekognition", "totalMatches": len(result["matches"]),
                           "hasIssues": bool(result["issuesCount"]), **result}]
    catalog_payload = json.dumps({"videos": catalog}, allow_nan=False).encode("utf-8")
    payload = json.dumps(result, allow_nan=False).encode("utf-8")
    routes = {"": "index.html", "viewer.css": "viewer.css", "viewer.js": "viewer.js"}

    class Handler(BaseHTTPRequestHandler):
        def log_message(self, *_):
            pass  # Do not record media paths or the local session URL.

        def headers_response(self, status, content_type, length, extra=None):
            self.send_response(status)
            self.send_header("Content-Type", content_type)
            self.send_header("Content-Length", str(length))
            self.send_header("Cache-Control", "no-store")
            self.send_header("X-Content-Type-Options", "nosniff")
            self.send_header("Referrer-Policy", "no-referrer")
            self.send_header("Content-Security-Policy",
                             "default-src 'self'; media-src 'self'; object-src 'none'; "
                             "frame-ancestors 'none'; base-uri 'none'")
            for key, value in (extra or {}).items():
                self.send_header(key, value)
            self.end_headers()

        def do_HEAD(self):
            self.serve(head=True)

        def do_GET(self):
            try:
                self.serve()
            except (BrokenPipeError, ConnectionResetError, ConnectionAbortedError):
                pass  # Normal when a player changes its requested time/range.

        def serve(self, head=False):
            path = urlsplit(self.path).path
            if not path.startswith(prefix):
                self.send_error(404)
                return
            route = path[len(prefix):]
            media = re.fullmatch(r"media/(0|[1-9]\d*)", route)
            if route == "media" or media:
                index = int(media.group(1)) if media else 0
                if index >= len(videos):
                    self.send_error(404)
                    return
                selected_video = videos[index]
                size = selected_video.stat().st_size
                header = self.headers.get("Range")
                try:
                    start, end = byte_range(header, size)
                except ValueError:
                    self.headers_response(416, "text/plain", 0,
                                          {"Content-Range": f"bytes */{size}"})
                    return
                extra = {"Accept-Ranges": "bytes"}
                if header:
                    extra["Content-Range"] = f"bytes {start}-{end}/{size}"
                self.headers_response(206 if header else 200,
                                      mimetypes.guess_type(selected_video.name)[0] or "video/mp4",
                                      end - start + 1, extra)
                if not head:
                    with selected_video.open("rb") as stream:
                        stream.seek(start)
                        remaining = end - start + 1
                        while remaining:
                            block = stream.read(min(256 * 1024, remaining))
                            if not block:
                                break
                            self.wfile.write(block)
                            remaining -= len(block)
                return
            if route == "catalog":
                body, content_type = catalog_payload, "application/json; charset=utf-8"
            elif route == "matches":
                body, content_type = payload, "application/json; charset=utf-8"
            elif route in routes:
                file = assets / routes[route]
                body = file.read_bytes()
                content_type = {".html": "text/html", ".css": "text/css",
                                ".js": "text/javascript"}[file.suffix] + "; charset=utf-8"
            else:
                self.send_error(404)
                return
            self.headers_response(200, content_type, len(body))
            if not head:
                self.wfile.write(body)

    return Handler


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    source = parser.add_mutually_exclusive_group(required=True)
    source.add_argument("--video", type=Path)
    source.add_argument("--videos-dir", type=Path, help="Folder with local MP4/MOV/WebM videos")
    parser.add_argument("--result", action="append", default=[], type=Path,
                        help="Existing API export or comparison summary; repeat for multiple files")
    parser.add_argument("--port", type=int, default=0, help="0 chooses a free local port")
    args = parser.parse_args()
    try:
        if args.video:
            videos = [args.video.resolve()]
        else:
            if not args.videos_dir.is_dir():
                raise ValueError("Selecciona una carpeta de videos existente.")
            videos = sorted((p.resolve() for p in args.videos_dir.iterdir()
                             if p.is_file() and p.suffix.lower() in {".mp4", ".mov", ".webm"}),
                            key=lambda p: p.name.casefold())
        if not videos or any(not p.is_file() or p.stat().st_size == 0 for p in videos):
            raise ValueError("Selecciona videos existentes y no vacíos.")
        catalog = load_catalog(videos, args.result)
        # A single explicit pair can associate an empty result without guessing.
        if args.video and len(args.result) == 1:
            data = json.loads(args.result[0].read_bytes())
            if isinstance(data, dict):
                result = load_matches(args.result[0], videos[0])
                catalog[0].update(result, availability="available",
                                  provider="AWS Rekognition" if "faceMatches" in data else "Google Video Intelligence",
                                  totalMatches=len(result["matches"]), hasIssues=bool(result["issuesCount"]))
        result = catalog[0]
    except (ValueError, OSError) as error:
        parser.error(str(error) if isinstance(error, ValueError) and
                     not isinstance(error, json.JSONDecodeError) else "No se pudo leer el JSON.")
    prefix = "/" + secrets.token_urlsafe(18) + "/"
    with ThreadingHTTPServer(("127.0.0.1", args.port),
                             make_handler(videos[0], result, prefix, catalog, videos)) as server:
        print(f"Visor local: http://127.0.0.1:{server.server_port}{prefix}", flush=True)
        print(f"{len(videos)} videos. Sin llamadas cloud. Archivos en solo lectura. Ctrl+C para cerrar.", flush=True)
        try:
            server.serve_forever()
        except KeyboardInterrupt:
            pass


if __name__ == "__main__":
    main()
