# Current Architecture

## Backend

ASP.NET Core API in `src/DeValpo.MediaIntelligence.Api` exposes job endpoints, auth, audit, health/status and workers.

## Application

`src/DeValpo.MediaIntelligence.Application` contains use cases:

- `MediaAnalysis`
- `VideoSearch`
- `FaceSearch`
- `ImageBatchSearch`
- `CloudAssets`
- `Webhooks`
- `Audit`
- `Profiles`

## Domain

`src/DeValpo.MediaIntelligence.Domain` contains entities for jobs, assets, matches, issues and operational state.

## Contracts

`src/DeValpo.MediaIntelligence.Contracts` contains API DTOs grouped by capability.

## Infrastructure

`src/DeValpo.MediaIntelligence.Infrastructure` contains EF Core, repositories, storage, provider SDK integrations and webhook delivery.

## Integrations

- Google Document AI for document extraction.
- Google Video Intelligence for video detection.
- Google Vision for image OCR.
- AWS Rekognition for face search and image OCR/face matching.

## Database

PostgreSQL with EF Core migrations.

## Testing

Tests live in `tests/DeValpo.MediaIntelligence.Tests`.

## Deploy

Docker Compose lives in `deploy/`; CI helpers live in `scripts/ci/`.
