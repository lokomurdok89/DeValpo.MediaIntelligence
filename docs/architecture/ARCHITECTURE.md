# Architecture

## Solution Structure

`DeValpo.MediaIntelligence.sln` contains five production projects and one test
project. Dependencies flow inward:

```text
Domain              Contracts
  ^                    ^
  |                    |
Application ----------+
  ^
  |
Infrastructure
  ^
  |
Api
```

| Project | Implemented responsibility |
| --- | --- |
| `Api` | HTTP controllers, JWT/auth, middleware, health endpoints, hosted workers and composition root. |
| `Application` | Use cases, orchestration, profile resolution, cloud-selector validation and provider-neutral interfaces. |
| `Domain` | Persisted normalized entities and job state. |
| `Contracts` | Public request/response DTOs and API-facing enums. |
| `Infrastructure` | EF Core repositories, PostgreSQL mappings, local storage, provider SDKs, cloud browsers and webhook delivery. |
| `Tests` | Unit tests and API-host tests. |

## Composition

`Api/Program.cs` composes `AddApplication()` and `AddInfrastructure()`, binds
options, configures JWT, health checks, Serilog and feature-gated controllers/
workers. Application registers use cases and abstractions; Infrastructure binds
repositories, EF Core/Npgsql, local storage, cloud providers and webhooks.

## Boundary Rules

- Controllers translate HTTP requests and enforce access before calling use cases.
- Application may depend on Domain and Contracts, but not provider SDK models.
- Infrastructure implements Application abstractions and owns SDK/EF mappings.
- Domain stores normalized state, not provider responses or credentials.
- Contracts do not expose EF or cloud-provider models.

## Persisted Model

`MediaIntelligenceDbContext` persists clients, API-key hashes, profiles,
analysis jobs/assets/results, human-review tasks, audit events, retention,
webhook targets/deliveries, video search entities and image-batch entities.
EF Core migrations live under `Infrastructure/Persistence/Migrations`.

## External Systems

- PostgreSQL stores metadata and job state; binary media is not stored in it.
- Local media storage stores uploaded files under the configured root.
- Google Document AI, Video Intelligence and Vision are provider integrations.
- AWS Rekognition and S3 support face and image workflows.
- GCS and S3 references are resolved only through configured allow-lists.

See [processing flows](PROCESSING_FLOWS.md) for runtime behavior and
[configuration](../development/CONFIGURATION_GUIDE.md) for runtime options.
