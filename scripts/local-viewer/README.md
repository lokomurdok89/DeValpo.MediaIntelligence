# Local Video Result Gallery

Select an existing local video, inspect its saved findings and click a timestamp
to seek. Python 3.10+ standard library only; no cloud calls, packages, database,
backend changes or transcoding. Original media/results remain read-only.

## Start

For the bundled privacy-safe demo, no credentials or private files are needed:

```powershell
python scripts/local-viewer/serve.py --video docs/assets/demo/public-demo-media-withheld.mp4 --result docs/assets/demo/public-result.json
```

The 39 match rows retain real times/scores from the saved 2026-11-09 AWS result.
The paired video is a labeled generated replacement with a clock, not original
footage. The JSON is an allowlisted derivative, not a raw API export. No source
frames, audio, original filenames, cloud paths or biometric IDs are included.
This demonstrates navigation, not visual identity verification.

For your own authorized media:

```powershell
python scripts/local-viewer/serve.py --videos-dir "<local-video-folder>" --result "<saved-api-result.json>" --result "<saved-video-comparison.json>" --result "<72-second-video-api-result.json>"
```

The folder's MP4, MOV and WebM files become selectable cards (no recursion).
Repeat `--result` for each export. Omit it to play videos with no associated result.
Open the complete loopback URL printed by the command. Stop with Ctrl+C;
`--port <number>` optionally chooses a port. Keep the session local, never tunnel it.

Single-video mode remains compatible and can explicitly associate an empty API
result that contains no video filenames:

```powershell
python scripts/local-viewer/serve.py --video "<local-video.mp4>" --result "<saved-api-result.json>"
```

## Supported Saved Results

- AWS API `faceMatches`: times and similarity, on a 0–100 scale.
- Google API `targetMatches[].matches` and `faceOccurrences`: times and confidence
  converted from 0–1 to 0–100. Generic face detection is not identity recognition.
- Comparison summary array: `ObjectKey`, `Status`, `Matches`, `Issues`. Shows
  count/status and an issue indicator only, without fabricated timestamps.
- UTF-8 and UTF-16 JSON exports are accepted. Raw AWS `Persons` responses and
  threshold-only summaries are not video result exports.

Detailed API rows associate by exact `videoFileName` basename; comparison rows
use the basename of `ObjectKey`. This is not a hash/provenance check. Do not rename
videos or assume original/transcoded files share results. Detailed API rows take
precedence over a summary; their executions are not merged. Multiple detailed
exports for one video are rejected. Unnamed empty API results are not assigned
across a folder: use the explicit single-video mode for those.

## Display States

- Detailed findings: clickable rows, previous/next and play from selection.
- Completed empty result: zero findings, with issues displayed separately.
- Summary only: saved count and status; no timeline if timestamps are absent.
- Result unavailable: playable video, but no associated export, not a zero result.

Switching videos clears the previous result and position. MP4/H.264 is recommended;
other codecs depend on the browser. A playback error does not change saved results.

The prepared three-video collection now includes the 1:12 H.264 copy with
39 real AWS match rows from the authorized 2026-11-09 run, the short H.264 video
with 18 saved points, and the other short video with a zero-match summary and
issues. The new copy has its own API JSON; do not attach it to the original
HEVC file, whose separate run returned a codec error. Repeated timestamps are
preserved (39 rows, 32 distinct times). Seeking and playback were checked in
the browser using the real compatible copy.

Keep media/results outside Git; ignored `outputs/private/` is available for
local-only copies and exports. Use a gallery folder with the compatible copy
in place of the 1:12 original and add its JSON as another `--result`. No original
Google JSON was found; Google parsing is supported but its real timeline is
not part of this demo. Private files are intentionally not bundled for distribution.

The UI receives filenames, times, scores, finding types, status and issue
indicators only, not raw JSON, cloud paths, issue messages or biometric IDs.
The server exposes only explicit catalog media and fixed UI routes; no directory
listing. No screenshots of private media should be added to the repository.

## Focused Offline Checks

```powershell
python -B -m unittest discover -s scripts/local-viewer -p test_viewer.py
```

Uses synthetic fixtures and a loopback HTTP server, not real media or providers.
