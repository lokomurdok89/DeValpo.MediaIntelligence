# Architecture

DeValpo.MediaIntelligence is a standalone API for analyzing documents, images, videos and future live-video buffers.

## Layers

- `Contracts`: public DTOs and API-facing enums.
- `Domain`: persistence entities and core data model.
- `Application`: use cases, orchestration, profile resolution and provider/storage abstractions.
- `Infrastructure`: EF Core, PostgreSQL, local storage, Google Document AI, Google Video Intelligence, AWS Rekognition and skills.
- `Api`: controllers, middleware, Swagger, health checks and composition.

Dependency direction:

```text
Domain
Contracts
Application -> Domain + Contracts
Infrastructure -> Application + Domain + Contracts
Api -> Application + Infrastructure + Contracts
```

## End-To-End Flow

```mermaid
flowchart TD
    A["Client app"] --> B["JWT Bearer request"]
    B --> C["API controller"]
    C --> D["Application service"]
    D --> E["Media storage"]
    D --> F["PostgreSQL"]
    D --> G["Analysis orchestrator"]
    G --> H["Profile resolver"]
    G --> I["Skills"]
    I --> J["Google Document AI"]
    D --> L["Video/Face search services"]
    L --> M["Google Video Intelligence"]
    L --> N["AWS Rekognition + S3"]
    G --> K["Normalized result"]
    K --> F
    L --> F
```

## Runtime Components

```mermaid
flowchart LR
    API["ASP.NET Core API"] --> App["Application services"]
    Worker["Background workers"] --> App
    App --> Storage["Local storage"]
    App --> DB["PostgreSQL"]
    App --> Orchestrator["MediaAnalysisOrchestrator"]
    Orchestrator --> Skills["Skills"]
    Skills --> Google["Google Document AI"]
    App --> VideoSearch["VideoSearchApplicationService"]
    App --> FaceSearch["FaceSearchApplicationService"]
    VideoSearch --> GVI["Google Video Intelligence"]
    FaceSearch --> AWS["AWS Rekognition + S3"]
```

## Profiles, Skills And Capabilities

- A capability describes what the platform can do.
- A skill is an executable unit that produces fields, labels, entities, clips or issues.
- A profile selects skills, order, thresholds and review rules for a use case.

## Cloud Providers

- Google Document AI processes documents.
- Google Video Intelligence processes visible-number OCR and generic face occurrences in videos.
- AWS Rekognition processes consented face comparison against reference images.
- Each provider lives in its own Infrastructure namespace and uses separate options and credentials.

## Model And Mapper Convention

- Canonical use-case models live under `Application/<Feature>/Models`.
- External providers do not project directly to public DTOs; they first convert SDK responses to canonical Application models.
- Mappers that know external SDKs live under `Infrastructure/Providers/<Provider>/<Service>/Mappers`.
- Use `ToDomain...` names when converting SDK/provider data to internal models and `ToDto...` only when the result is a `Contracts` DTO.
