# Project Identity

## Name

`DeValpo.MediaIntelligence`

## Product Objective

Reusable backend service for multi-client document and media intelligence. It
accepts analysis and search jobs, processes them asynchronously through cloud
providers, persists auditable results and exposes operational diagnostics.

## Technical Stack

- .NET backend with ASP.NET Core.
- EF Core with PostgreSQL.
- Docker Compose for local runtime.
- MkDocs Material for documentation.
- GitHub Actions for CI and docs builds.

## Layered Architecture

- `Api`: HTTP endpoints, auth, middleware, health checks and hosted workers.
- `Application`: use cases, orchestration, business validation and interfaces.
- `Domain`: entities and normalized job state.
- `Contracts`: public DTOs and API contracts.
- `Infrastructure`: EF Core, repositories, storage, provider SDK integrations and webhooks.

## Main Modules

- Document analysis.
- Video target search.
- Face search.
- Image batch search.
- Cloud asset references and staging.
- Client access, audit, retention, webhooks and operations.

## AI Integrations

- Google Document AI for document extraction.
- Google Video Intelligence for video detection.
- Google Vision for image OCR and visible-number image search.
- AWS Rekognition for face search and image OCR or face matching where supported.

## Cloud Providers

- Google Cloud for Document AI, Video Intelligence, Vision and GCS.
- AWS for Rekognition and S3.

## Database

PostgreSQL managed through EF Core migrations in
`src/DeValpo.MediaIntelligence.Infrastructure/Persistence/Migrations`.

## Design Principles

- Provider-neutral contracts at the API boundary.
- Provider-specific details isolated in `Infrastructure`.
- Asynchronous job workflows with persisted snapshots, issues and audit state.
- Secure productive inputs through cloud references, not public URLs.
- Short, task-oriented docs with long references loaded only when needed.

## Acceptable Changes

- Small feature, bugfix, documentation and test changes aligned to existing layers.
- Provider additions behind application abstractions.
- New specs, ADRs, module docs or onboarding updates when state changes.

## Not Acceptable Without Explicit Approval

- Reading or editing credentials.
- Hardcoding provider values.
- Refactoring across layers without a task-specific reason.
- Breaking multipart compatibility.
- Changing public contracts casually.
- Deleting generated or staged cloud assets as part of job completion.
