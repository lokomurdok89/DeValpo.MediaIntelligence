# Active Decisions

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
- Do not read `docs/reference/` or `docs/archive/` unless the task requires it.
