# DeValpo.MediaIntelligence

Reusable multi-client document and media intelligence engine.

The product exposes a secure API for clients to upload real documents, run configured analysis profiles, persist normalized results, manage human review and notify external systems through webhooks.

## Documentation

The full documentation is built as a MkDocs Material site with navigation tabs, search, Mermaid diagrams and bilingual pages:

- Local preview: `python -m mkdocs serve`
- GitHub Pages URL when enabled: [DeValpo MediaIntelligence Docs](https://lokomurdok89.github.io/DeValpo.MediaIntelligence/)
- Local documentation source: [docs/index.md](docs/index.md)
- Endpoint sequence diagrams: [docs/api-endpoint-flows.md](docs/api-endpoint-flows.md)

## Quick Start

```powershell
dotnet restore DeValpo.MediaIntelligence.sln
dotnet build DeValpo.MediaIntelligence.sln
dotnet test DeValpo.MediaIntelligence.sln
docker compose -f deploy\docker-compose.yml up --build
```

The local API listens on `http://localhost:8085`.

## Main Local Flow

1. Configure `deploy/.env` from `deploy/.env.example`.
2. Configure Google ADC outside the repository.
3. Start Docker Compose.
4. Generate a local Development token.
5. Upload a real PDF through `POST /api/v1/analyze`.
6. Read the result from `/api/v1/analysis-jobs/{jobId}/result`.

Use [docs/getting-started.md](docs/getting-started.md) for the complete step-by-step guide.

## Repository Layout

| Path | Purpose |
| --- | --- |
| `src/DeValpo.MediaIntelligence.Api` | ASP.NET Core API, Swagger, auth and health endpoints. |
| `src/DeValpo.MediaIntelligence.Application` | Use cases, orchestration and application abstractions. |
| `src/DeValpo.MediaIntelligence.Infrastructure` | EF Core, PostgreSQL, storage, Google Document AI and workers. |
| `src/DeValpo.MediaIntelligence.Contracts` | Public DTOs and API contracts. |
| `src/DeValpo.MediaIntelligence.Domain` | Domain entities and persistence model. |
| `deploy` | Docker Compose and environment examples. |
| `docs` | MkDocs documentation source. |
| `tests` | Unit and API tests. |

## Documentation Development

```powershell
pip install -r requirements-docs.txt
mkdocs serve
mkdocs build --strict
```

`.github/workflows/docs.yml` builds the documentation on every push and pull request. GitHub Pages deploy is gated by the repository variable `DEPLOY_GITHUB_PAGES=true`, because private repositories require a GitHub plan that supports Pages.

## Safety

Do not commit:

- `deploy/.env`
- `secrets/*.json`
- Google credential files
- raw API keys
- JWT signing keys
- webhook signing secrets

Run the secret scan before pushing:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\ci\secret-scan.ps1 -Root .
```
