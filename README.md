# DeValpo.MediaIntelligence

Reusable multi-client media and document intelligence engine.

This repository is intentionally separate from `C:\DeValpo\Contador`. It exposes public DTOs through `DeValpo.MediaIntelligence.Contracts` and keeps providers, storage and persistence behind Application abstractions.

## Requirements

- .NET SDK 10.0.x
- Docker Desktop
- PostgreSQL 17 when running outside Docker

## Run locally

```bash
dotnet restore DeValpo.MediaIntelligence.sln
dotnet tool restore
dotnet build DeValpo.MediaIntelligence.sln
dotnet test DeValpo.MediaIntelligence.sln
dotnet run --project src/DeValpo.MediaIntelligence.Api
```

Swagger is enabled by `Swagger:Enabled` and is available at `/swagger`.

## Run with Docker

```bash
docker compose -f deploy/docker-compose.yml config
docker compose -f deploy/docker-compose.yml up --build
```

The API listens on `http://localhost:8085` and health is exposed at `/health`.

## Database migrations

Apply migrations from the repository root:

```bash
dotnet tool restore
dotnet dotnet-ef database update --project src/DeValpo.MediaIntelligence.Infrastructure --startup-project src/DeValpo.MediaIntelligence.Api
```

Automatic migration execution is controlled by `Database:ApplyMigrationsOnStartup` and defaults to `false`.

## Google Document AI

Set these values through environment variables or appsettings:

```text
GoogleDocumentAi__Enabled=true
GoogleDocumentAi__ProjectId=your-project
GoogleDocumentAi__Location=us
GoogleDocumentAi__ProcessorId=your-processor
GoogleDocumentAi__CredentialsPath=/secure/path/service-account.json
```

Do not commit service account JSON files.

## Analyze an asset

```bash
curl -X POST http://localhost:8085/api/v1/analyze \
  -F "clientKey=arenysmed" \
  -F "profileKey=arenysmed-delivery-evidence-v1" \
  -F "assetType=Image" \
  -F "metadataJson={\"source\":\"manual-test\"}" \
  -F "file=@sample.png"
```

## Add a skill

1. Implement `IMediaAnalysisSkill`.
2. Register it in `Infrastructure/DependencyInjection.cs`.
3. Add its name to a profile definition.
4. Add focused tests for the skill or orchestrator behavior.

## Add a profile

Profiles are resolved through `IAnalysisProfileResolver`. The initial implementation is static and can be replaced by a database-backed resolver without changing controllers or skills.

## CI/CD status

GitHub Actions validates restore, build, tests, Docker build and Docker Compose config. There is no automatic deployment in this phase.
