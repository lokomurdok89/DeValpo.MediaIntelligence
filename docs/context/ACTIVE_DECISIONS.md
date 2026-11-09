# Active Decisions

## First Product Delivery — Approved 2026-11-09

- First Minimum Awesome Product: facial search in private S3 videos using the
  existing AWS Rekognition video workflow; bib-photo search follows later.
- Keep the demo simple: existing video and saved result, with clickable
  occurrences. Earlier proposed workload limits and release gates are not
  prerequisites for this demo.
- Reuse saved results; do not repeat historical E2E jobs or threshold sweeps.
- No general refactor, generic search engine or contract rewrite. Only concrete
  blockers to this journey may be corrected after implementation approval.
- The user subsequently authorized a simple local viewer of the existing video
  and saved AWS API JSON: click an occurrence to seek, without refactoring or
  new AWS calls. Implemented independently in `scripts/local-viewer/` using only
  Python standard library and static HTML/CSS/JS; originals remain read-only.
- The user also authorized the local three-video gallery with existing result
  association and seeking. Missing exports remain explicitly unavailable; saved
  summaries do not generate artificial timestamps or merge different jobs.
- This viewer authorization does not authorize backend/config changes, cloud
  execution or publication/deployment.
- A subsequent, explicit exception on 2026-11-09 authorizes starting the local
  API and searching the existing 1:12 S3 video with its reference image. After
  AWS rejected the original HEVC codec, the user also authorized one H.264 copy,
  upload to the same private bucket and a search of that copy. Preserve both
  real responses privately; publish only sanitized evidence in both README files.
  This is not permission for historical replays, threshold sweeps or Google calls.
  No backend refactor, commit or deployment is authorized.
- Canonical product scope: [S3 facial-video MAP](../specs/s3-video-face-search-map.md).
- The public-facing demo reuses an allowlisted derivative of the 39-row saved
  result and a generated replacement video, never original frames or audio.
  It must be labeled as a privacy placeholder, not footage analyzed by AWS.
  README EN/ES lead with this offline demo and the implemented cloud-first flow.
  No further cloud runs, refactor, publication or commit are authorized.
- Documentation cleanup proceeds in small batches: generic ADR/spec templates
  and duplicated review/done checklists were removed with their navigation links.
  Integration contracts, operational context, useful diagrams and presentation
  remain. Do not delete modules, mocks or migrations without evidence and approval.
- The next approved documentation batch consolidates the bilingual implementation
  and video-MVP guides, AWS runbook and parallel architecture overview into the
  existing setup, onboarding, API, provider and bilingual architecture references.
  Keep useful sequences in `docs/architecture/PROCESSING_FLOWS.md` and historical
  measurements with explicit provenance in `docs/examples/`; no new standalone
  replacement guide or runtime changes are needed.
- Repository presentation targets reader clarity, visual appeal and technical
  exploration, not sales plans, pricing or commercial offers. Keep the working
  example and offline demo ahead of internal process/AI documentation.
- Treat GitHub as a technical portfolio: visitors should quickly understand the
  implemented result, try the privacy-safe demo and inspect the architecture.
  Keep current diagrams; remove superseded phase plans and bib-only sequences.

## Architecture

- Keep layered architecture: `Api`, `Application`, `Domain`, `Contracts`, `Infrastructure`.
- `Api` owns HTTP, auth, audit entry points, middleware, health and workers.
- `Application` owns orchestration, business validation and provider-neutral interfaces.
- `Domain` owns normalized entities and job state.
- `Contracts` owns public DTOs and must not expose EF or provider SDK models.
- `Infrastructure` owns EF Core, repositories, storage, provider SDKs and webhooks.

## Contracts And Provider Values

- Do not hardcode credentials, provider values, bucket names, collection IDs or paths.
- `targetKey` is the canonical technical target name in contracts.
- Keep multipart compatibility for development, smoke tests and existing clients.
- Production input uses cloud references through `assetSelectorJson`.
- Public URLs and signed URLs are not productive inputs.
- `dataKey` is only a business synonym and must not replace `targetKey` in API
  contracts.
- `consentReferenceId` is optional. The service stores only an external
  reference; legal consent records belong to the external platform.

## Cloud Asset Lifecycle

- Production uses cloud references.
- Do not delete staged cloud files when jobs finish.
- S3/GCS lifecycle policies own normal staging cleanup.
- A monthly sweep may be used only as a secondary operational safety net.

## Provider Routing

- AWS Rekognition is the provider for identity face recognition.
- Google Vision supports OCR and visible-number image search, not identity face
  recognition.
- Google Document AI is the provider for document analysis.
- Cross-cloud processing may stage an asset in a provider-compatible bucket.
- The external platform owns events, users and the live asset catalog;
  MediaIntelligence stores technical job snapshots, resolved assets, matches,
  issues, audit state and webhook state.

## Biometric Cleanup

- Delete temporary AWS Rekognition `FaceId` values after processing.
- Treat `FaceId` deletion as privacy cleanup, separate from file staging cleanup.

## Context Boundaries

- Do not read `secrets/` by default.
- Do not read generated `site/` by default.
- Do not read `docs/reference/` unless the task requires it.
