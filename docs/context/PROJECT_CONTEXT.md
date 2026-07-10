# Project Context

`DeValpo.MediaIntelligence` is a standalone backend service for multi-client
document and media intelligence. It accepts authenticated jobs, processes them
asynchronously, persists auditable state and exposes operational diagnostics.
It is not part of `Contador` or a sibling repository; keep changes inside this
repository unless a task explicitly broadens the scope.

## Verified Product Scope

- Document analysis uses Google Document AI.
- Video target search uses Google Video Intelligence.
- Face search uses AWS Rekognition.
- Image batch search supports visible-number search and face-reference flows.
- Productive media can be selected from approved S3 or GCS references; multipart
  remains available for development, compatibility and smoke tests.

## Technical Shape

- ASP.NET Core API and hosted workers.
- Application use cases, provider-neutral abstractions and orchestration.
- Domain entities for normalized job, result, audit and delivery state.
- Contracts for public DTOs.
- Infrastructure for EF Core/PostgreSQL, local media storage, cloud providers,
  repositories and webhook delivery.

## Canonical Documentation

- `ACTIVE_DECISIONS.md`: decisions that constrain future work.
- `CURRENT_STATE.md`: durable operating baseline and known gaps; verify live Git
  state with commands instead of copying branch names here.
- `../architecture/ARCHITECTURE.md`: implemented components and boundaries.
- `../architecture/PROCESSING_FLOWS.md`: implemented runtime flows.
- `../development/`, `../testing/`, `../standards/` and `../backlog/`: working
  practices, quality expectations and pending engineering work.

## Context Boundaries

Do not treat documentation as proof of a real cloud validation. Credentials,
provider identifiers, raw API keys, JWT signing keys, webhook signing secrets,
connection strings, OCR content and biometric data are not documentation data.
