# Minimum Awesome Product — Facial Search In S3 Videos

## Product Scope

Find moments in a video using an authorized reference face, show the returned
occurrences and jump to a selected moment. Facial video search is the first
product experience; bib search and configurable word/code search follow later.

**Private S3 video + reference image → AWS facial search → saved occurrences →
click and seek.** Similarity is a provider score, not verified identity.

## Available Today

- The existing backend submits facial jobs, processes asynchronously through
  AWS Rekognition, persists state/results and exposes status and result routes.
- The local viewer in `scripts/local-viewer/` opens an existing MP4 and its saved
  AWS API JSON. It supports clickable occurrences, previous/next and playback.
- The viewer runs on loopback using Python's standard library and static assets.
  It reads originals without changing them and makes no new AWS calls. It is a
  saved-result consumer, not a deployed frontend or live S3 integration.
- A saved AWS API result contains 18 point occurrences, no issues and similarity
  scores of 98.63–99.94. [Evidence and provenance](../examples/video-result-evidence.md).
- The explicitly authorized 2026-11-09 S3 run adds the 1:12 H.264 copy with
  39 real match rows (32 timestamps), similarity 81.23–99.99, threshold 80 and
  no issues. The viewer loads its saved API response without additional AWS calls.
- A completed search can legitimately return an empty match collection. A job
  failure or an issue remains distinguishable from a successful empty result.

Opening instructions are in the repository's English/Spanish README and
`scripts/local-viewer/README.md`.

## Existing Cloud Flow

1. The external platform owns the media catalog, private storage and upload access.
2. Submit structured references through `assetSelectorJson` and
   `referenceAssetSelectorJson`; public/signed URLs are not job inputs.
3. `POST /api/v1/face-search-jobs` accepts the existing manifest and profile.
   `targetKey` remains canonical; `minSimilarity` is configurable.
4. The hosted worker indexes reference faces, searches the S3 video and maps
   occurrences to the target. This differs from image-batch source-face indexing.
5. Read `GET /api/v1/face-search-jobs/{jobId}` and its `/result` endpoint.
6. A consumer displays the corresponding video and seeks to returned timestamps.

The backend attempts temporary `FaceId` deletion; S3/GCS lifecycle owns staged
file cleanup. Playback access is the consumer's responsibility. Do not publish
original media, biometric identifiers, credentials or private cloud references.
See [AWS integration](../aws-rekognition-face-search.md) and
[cloud asset references](../cloud-asset-references-and-staging.md).

## Local Gallery — Implemented

The gallery in `scripts/local-viewer/` selects the three existing local videos.
Associate saved results by exact basename, never by filename similarity or by
assuming an original and a transcoded video share a processing result.

- Detailed AWS API matches provide timestamps and similarity for clickable seeking.
- Saved comparison summaries provide counts/status/issues, not invented timestamps.
- A video without an associated export shows “Resultado no disponible”.
- Completed zero matches, unavailable results and provider issues remain distinct.
- Support existing Google API `targetMatches` and `faceOccurrences` exports when
  supplied. No original Google JSON was recovered, so no Google timeline is claimed.
- Original files remain read-only. Private copies/results may use ignored
  `outputs/private/`, excluded from Git and Docker build contexts. Preserve the
  single-video launch option; add a folder gallery with explicit result paths.

Keep the existing layers and contracts. No general refactor, historical E2E
replay, threshold sweep or publication is part of this work. The user explicitly
authorized the 2026-11-09 AWS pilot and, after a codec rejection, one H.264
copy/upload and search. Both responses were retained separately; this does not
authorize additional cloud jobs. The original HEVC error is not a valid empty
search. The successful copy is the one displayed in the three-video gallery.
Earlier phase plans, release gates and proposed workload limits are retired;
they are not prerequisites for demonstrating the existing product.

## Related Work

- [Configurable search and later bib pilot](bib-photo-search-pilot.md)
- [Product direction](../roadmap.md)
- Existing Archify image/OCR sequences remain design references for that later
  slice, not recordings of the current facial-video execution.
