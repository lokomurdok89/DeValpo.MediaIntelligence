# Architecture

DeValpo.MediaIntelligence is a standalone API for analyzing documents, images, videos and future live-video buffers.

## Layers

- `Contracts`: public DTOs and API-facing enums.
- `Domain`: persistence entities and core data model.
- `Application`: use cases, orchestration, profile resolution and provider/storage abstractions.
- `Infrastructure`: EF Core, PostgreSQL, local storage, Google Document AI and skills.
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
    G --> K["Normalized result"]
    K --> F
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
```

## Profiles, Skills And Capabilities

- A capability describes what the platform can do.
- A skill is an executable unit that produces fields, labels, entities, clips or issues.
- A profile selects skills, order, thresholds and review rules for a use case.
