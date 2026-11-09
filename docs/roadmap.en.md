# Product Direction

## What We Have

A cloud-first backend with private S3/GCS references, asynchronous processing,
persisted results and Google/AWS adapters. The local viewer opens a video and
its saved AWS result, then seeks to a selected occurrence without calling AWS.

See the [existing results](examples/video-result-evidence.md) and
[current scope](specs/s3-video-face-search-map.md).

## Next Visible Value

- The three-video gallery is implemented. The specifically authorized S3 run
  on 2026-11-09 added 39 clickable AWS records for the 1:12 H.264 copy, alongside
  the short videos' saved results. No further reprocessing is needed to show it.
- Recover the missing original Google export if available; do not attribute
  another video's JSON to it or invent a timeline from the historical summary.
- Keep facial recognition as the main experience and also show Google's
  findings, distinguishing generic face detection from facial identity search.
- Afterwards, develop the bib pilot and extend configurable search to words and
  codes. These extensions are not presented as implemented.

The architecture supports provider and consumer extensions. Workers currently
run inside the API; distributed execution would be future work, not a capability
already demonstrated.

No general refactor, further cloud executions or historical-result replays are
needed to view the saved demo. There is no phase plan or certification prerequisite
to showing what already works.
