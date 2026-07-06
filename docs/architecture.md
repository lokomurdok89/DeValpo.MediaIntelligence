# Architecture

DeValpo.MediaIntelligence is a standalone API for analyzing documents, images, videos and future live-video buffers.

## Layers

- `Contracts`: public DTOs and API-facing enums.
- `Domain`: persistence entities and core data model.
- `Application`: use cases, orchestration, profile resolution and provider/storage abstractions.
- `Infrastructure`: EF Core, PostgreSQL, local storage, Google Document AI and mock skills.
- `Api`: controllers, middleware, Swagger, health checks and composition.

Dependency direction:

```text
Domain
Contracts
Application -> Domain + Contracts
Infrastructure -> Application + Domain + Contracts
Api -> Application + Infrastructure + Contracts
```

## End-to-end flow

```text
Controller
  -> Application Service
    -> MediaAnalysisOrchestrator
      -> IMediaAnalysisSkill
        -> Provider or mock implementation
    -> Repository + UnitOfWork
```

The API stores file metadata and local storage paths. It does not store binary files in PostgreSQL.

## Separation from Contador

This repository does not depend on `Contador` entities, DTOs, endpoints, database or frontend. Future Contador integration should happen through HTTP/API consumption of public contracts.

## Capabilities, skills and profiles

- A capability describes a technical ability such as OCR or stamp detection.
- A skill is an executable analysis unit.
- A profile selects skills, order, thresholds and review rules for a client or use case.
