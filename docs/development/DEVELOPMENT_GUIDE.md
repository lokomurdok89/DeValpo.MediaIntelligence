# Development Guide

## Prerequisites

- .NET SDK 10.x and Docker Compose.
- Python with the packages in `requirements-docs.txt` for documentation work.
- PostgreSQL is supplied by Docker Compose for local runtime.
- Cloud credentials are optional for local code/test work and must stay outside
  the repository.

## Common Commands

```powershell
dotnet restore DeValpo.MediaIntelligence.sln
dotnet build DeValpo.MediaIntelligence.sln
dotnet test DeValpo.MediaIntelligence.sln --no-build
dotnet tool restore --tool-manifest dotnet-tools.json
docker compose -f deploy\docker-compose.yml up --build
python -m mkdocs build --strict
```

Run `docker compose -f deploy\docker-compose.yml config --quiet` before a
Docker change. The local API port is documented in `deploy/docker-compose.yml`.
Do not claim a command passed unless it was executed successfully.

## Migrations

Production and staging configuration disable automatic migration at startup.
After a model change, review migration drift with:

```powershell
dotnet ef migrations has-pending-model-changes --project src\DeValpo.MediaIntelligence.Infrastructure --startup-project src\DeValpo.MediaIntelligence.Api
```

Creating or applying migrations requires a task-specific authorization.

## Working Practices

- Start material features with a concise spec in `docs/specs/`.
- Keep source changes inside the owning layer and add focused tests.
- Update module, architecture or configuration documentation when behavior,
  contracts or runtime configuration changes.
- Use `docs/testing/TESTING_STRATEGY.md` to choose validation. Cloud smoke tests
  are required only when provider behavior, credentials or cloud routing changes.

## Troubleshooting

- Startup option failures: check the relevant environment variables and example
  file, never paste secrets into source or tickets.
- Docker readiness failures: distinguish PostgreSQL, local storage and provider
  readiness before changing code.
- Provider failures: use status endpoints with authorized remote checks only in
  an approved, configured environment.
