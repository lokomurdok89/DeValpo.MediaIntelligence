# Saved Video Results — Sanitized Evidence

## Executed S3 Facial Pilot — 2026-11-09

The user authorized the existing API to search the 1:12 S3 video with its
existing reference photo. Two separate real API responses were retained:

| Input | API status | Match rows | Issues | Interpretation |
| --- | --- | ---: | ---: | --- |
| Original MP4 / HEVC, 72.040 s | `Completed` | 0 | 1 | `AWS_REKOGNITION_FACE_SEARCH_FAILED`: `Unsupported codec/format`; not a successful empty search |
| Authorized MP4 / H.264 copy, 72.072 s | `Completed` | 39 | 0 | Completed facial search; 32 distinct timestamps |

The compatible copy was created without changing the original, uploaded to the
same private S3 bucket with AES256 server-side encryption, and submitted through
`POST /api/v1/face-search-jobs`. Both source selectors used `ExplicitAssets`;
the reference photo was reused from S3, with an opaque `targetKey` and
`minSimilarity: 80`. No source media bytes were uploaded through the API.

The successful response contains point records from **6.506 s to 71.871 s**,
with similarity **81.23–99.99 / 100**. It was created at **19:44:03 UTC** and
completed at **19:45:05 UTC**. These timestamps describe this one execution,
not a performance benchmark. Repeated timestamps remain in the original JSON
and viewer; no deduplication or identity verification is claimed.

The collection was checked after each run and reported **FaceCount: 0**.
No S3 objects were deleted; lifecycle remains responsible for file retention.
The existing backend was built and run with isolated local PostgreSQL storage;
no backend code, contracts or permissions were changed. Google and historical
jobs were not rerun.

Exact API response bytes, job receipts, conversion/upload evidence and the
compatible video are stored locally under ignored `outputs/private/`, also
excluded from Docker build contexts. They are intentionally not published.
The gallery uses the compatible copy and its own JSON, alongside the two short
videos and their existing results. Browser verification confirmed seeking to
**32.966 s** and **71.871 s**, and playback from the selected occurrence.
The viewer itself makes no provider calls.

Codec requirement: [AWS stored-video documentation](https://docs.aws.amazon.com/rekognition/latest/dg/video.html).

## What Was Found

The historical QA review records an existing Google Video Intelligence video job with `Completed` status, **60 match segments for the reported target**, **2,819 generic face occurrences** and **0 issues**.

Historical source (removed from the working tree; retained in Git): `docs/qa-review.md`, section **AWS Rekognition Disabled-Mode QA → Runtime Evidence**, review date **2026-07-07**. Inspected repository revision: `2c27a288e459378514b52f676c16080c95be250e`. This summary was prepared on 2026-11-09. The source is a written QA record, not the original provider response. Its date is the review date, not an independently verified processing timestamp.

## How To Read It

| Observation | Meaning | Does not establish |
| --- | --- | --- |
| `Completed` | A recorded terminal job state | That every current job succeeds |
| 60 matching segments | Returned intervals for the reported target | 60 videos, people or manually verified correct matches |
| 2,819 face occurrences | Generic detections over time | Unique people or reference-face identification |
| 0 issues | No issues reported for that result | Measured precision/recall or absence of all defects |

The practical output is a set of moments an external player can seek to. No timestamps, confidence values, clip durations or playback links are reconstructed: the raw result is unavailable in the inspected files.

## Scope And Limitations

The following points describe the earlier historical-file recovery, before
the explicitly authorized live pilot above.

- The initial repository-only search did not locate the media. A subsequent user-authorized filename search under the parent workspace found AWS JSON and a local test-media folder outside this repository. Only relevant result files were parsed; secrets and unrelated project contents were not read. No services, databases or cloud storage were accessed. The original Google result JSON remains unlocated.
- No end-to-end test, provider call, upload or historical-job replay was performed.
- The same QA section records a separate long-running provider-error case. This selected completed result is not a universal reliability claim.
- The record does not establish whether its source was multipart, S3 or GCS. The README's S3 diagram explains the cloud-reference implementation separately.
- This is Google video OCR/generic face detection evidence, **not AWS facial recognition evidence** or certification of the configurable photo pilot.
- No measured accuracy, cost, throughput or current readiness is inferred.

## Publication-Safe Extraction

Only the review date, provider, status and aggregate counts are reproduced. Target identifiers/numbers, job identifiers, filenames, cloud identifiers/paths, credentials and original media are excluded. This is a summary, not fabricated API JSON. The AWS section below uses aggregate values and temporal bounds from saved files. Original-media publication still requires explicit permission; finding a file is not permission to publish its contents.

## S3 Integration Evidence

These code-inspection facts support the documented integration independently of the historical result:

- `VideoSearchJobsController`: accepts form `assetSelectorJson`, checks client/profile access, returns an accepted job response and exposes owner-scoped status/result reads.
- `CloudAssetSelectorParser`: accepts S3 selectors and rejects URL input fields.
- `AssetReferenceResolver`: enforces configured bucket allowlists; not evidence of complete per-client/prefix isolation.
- `GoogleVideoIntelligenceSearchProvider`: reads cloud references and stages non-GCS video in GCS before Google Video Intelligence.
- `AwsRekognitionFaceSearchProvider`: separate reference-face video workflow using S3.

This is code inspection, not a newly executed S3 demonstration.

## Saved AWS Artifacts

The expanded local inspection on 2026-11-09 found these files outside the repository:

| Local source basename | Verified content |
| --- | --- |
| `face-search-api-result-threshold-80.json` | API response: `Completed`, 18 facial matches, no issues, provider `AwsRekognition`; completion date 2026-07-08 |
| `face-search-threshold-summary.json` | Five historical thresholds (80, 70, 60, 50, 40), each recording 18 matches; threshold 80 shares the API response's job ID |
| `aws-face-search-result.json` | Raw provider response: `SUCCEEDED`, S3 object reference, 86 person entries, none containing face matches |
| `face-search-video-compare.json` | Two comparison rows: one with 0 matches and a nonempty issue field, one with 18 and an empty issue field |

The API response's video filename matches a file in the local test-media folder.
This is a filename association, not a content-hash verification against S3.
The source JSON were decoded locally (including UTF-16 exports), not copied here.

For the API response, provider similarities span **98.63–99.94** (0–100 scale).
There are **18 point occurrences** from **0.000 s** through **8.508 s**; each has
equal start/end times. These are not continuous clips, distinct people, manually
verified true positives or a measured 98% accuracy result.

The raw AWS provider job ID and application job ID are different namespaces;
no mapping was recovered. The comparison-file job IDs also do not match this
API response. Do not merge these artifacts into one asserted execution or infer
that provider success implies face matches. The raw S3 reference supports the
presence of S3-backed provider input, not the entire API submission trace.

The old Google QA record remains documentary evidence, while AWS has local
JSON evidence. Historical-file recovery alone did not recheck cloud status or
provider behavior; the separate live pilot above records what was subsequently
executed. Private bucket names, object keys, job/client/
consent identifiers, reference-image keys, face IDs and original images/videos
are excluded from these pages and both README files.

## Additional Historical Google Setup Record

The retired video setup guide records one Google Video Intelligence execution
on **2026-07-07**: **774.23 MB**, **1 video**, reachable GCS bucket, `Completed`,
**1 attempt**, approximately **24 minutes 50 seconds**. This is documentary
history, not a new run, a current readiness check or a benchmark. No recovered
job ID/raw JSON establishes that it is the same execution as the 60-segment
record above. The original export remains private and unlocated.

Source retained in Git: `docs/reference/video-mvp-real-route.md` and its English
translation at revision `2c27a288e459378514b52f676c16080c95be250e`.
Operational setup now lives in [Google video search](../video-target-search.en.md).
