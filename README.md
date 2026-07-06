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

Create a local environment file and keep it out of git:

```bash
cp deploy/.env.example deploy/.env
```

Google Document AI supports Application Default Credentials (ADC). For Docker local, create ADC with `gcloud` and copy the generated local credential into the ignored `secrets` folder:

```bash
gcloud auth application-default login
gcloud auth application-default set-quota-project primeval-wind-499515-k6
```

On Windows:

```powershell
Copy-Item "$env:APPDATA\gcloud\application_default_credentials.json" secrets\google-application-default-credentials.json
```

Then set these values in `deploy/.env` when you want to use the real provider in Docker:

```text
GoogleDocumentAi__Enabled=true
GoogleDocumentAi__ProjectId=primeval-wind-499515-k6
GoogleDocumentAi__Location=us
GoogleDocumentAi__ProcessorId=44b3274ce1e84552
GoogleDocumentAi__CredentialsPath=/run/secrets/google-application-default-credentials.json
Jwt__SigningKey=replace-with-local-dev-secret-min-32-chars
```

```bash
docker compose -f deploy/docker-compose.yml config
docker compose -f deploy/docker-compose.yml up --build
```

This single Compose file starts both components:

- `postgres`: PostgreSQL database on host port `5435`.
- `media-intelligence-api`: API on host port `8085`.

In Docker, migrations run automatically on API startup because `deploy/.env.example` sets `Database__ApplyMigrationsOnStartup=true`.

The API listens on `http://localhost:8085`, Swagger is available at `http://localhost:8085/swagger`, and health is exposed at `/health`.

Functional endpoints require JWT Bearer. In Development/Docker local, generate a token with:

```bash
curl -X POST http://localhost:8085/api/v1/dev/token \
  -H "Content-Type: application/json" \
  -d "{\"subject\":\"local-dev-user\",\"clientKey\":\"local-dev-client\",\"scope\":\"media-intelligence:mvp\"}"
```

Use the returned `accessToken` in Swagger Authorize or with:

```bash
curl http://localhost:8085/api/v1/capabilities \
  -H "Authorization: Bearer <accessToken>"
```

## Database migrations

Apply migrations from the repository root:

```bash
dotnet tool restore
dotnet dotnet-ef database update --project src/DeValpo.MediaIntelligence.Infrastructure --startup-project src/DeValpo.MediaIntelligence.Api
```

Automatic migration execution is controlled by `Database:ApplyMigrationsOnStartup`. It defaults to `false` in local appsettings and is enabled in Docker through `deploy/.env.example`.

## Google Document AI

For the complete local setup, including Google ADC, Docker secrets and JWT tokens, follow:

- [Local tokens and Google secrets setup](docs/local-tokens-and-google-secrets.md)

Set these values through environment variables or appsettings:

```text
GoogleDocumentAi__Enabled=true
GoogleDocumentAi__ProjectId=your-project
GoogleDocumentAi__Location=us
GoogleDocumentAi__ProcessorId=your-processor
GoogleDocumentAi__CredentialsPath=
```

If `GoogleDocumentAi__CredentialsPath` is empty, the Google SDK uses Application Default Credentials. If you run inside Docker, set it to the mounted ADC file under `/run/secrets`. Do not commit Google credential JSON files.

Check provider readiness:

```bash
curl http://localhost:8085/api/v1/providers/google-document-ai/status
curl "http://localhost:8085/api/v1/providers/google-document-ai/status?checkRemote=true" \
  -H "Authorization: Bearer <accessToken>"
```

The basic status endpoint checks local configuration and credential file presence. `checkRemote=true` validates the configured processor against Google and requires a Bearer token.

## Analyze an asset

```bash
curl -X POST http://localhost:8085/api/v1/analyze \
  -H "Authorization: Bearer <accessToken>" \
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
