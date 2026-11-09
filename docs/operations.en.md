# Operations

Local and release operations rely on Docker Compose, health checks, readiness checks, runbooks and CI.

## Health And Readiness

| Endpoint | Purpose | Security |
| --- | --- | --- |
| `/health` | Lightweight API process liveness. | Public |
| `/api/v1/health` | Versioned health endpoint. | Public |
| `/ready` | Required dependencies: DB, storage and optionally Google. | Public |
| `/api/v1/operations/metrics` | Operational counters. | Operator/Admin |

## Local Docker

```powershell
docker compose -f deploy\docker-compose.yml config --quiet
docker compose -f deploy\docker-compose.yml up --build
docker compose -f deploy\docker-compose.yml ps
```

Components:

- `postgres`: PostgreSQL 17.
- `media-intelligence-api`: ASP.NET Core API.
- Media asset volume.
- Local Google secret mount.

## Runbooks

- [Google Document AI](runbooks/google-document-ai.md)
- [PostgreSQL](runbooks/postgres.md)
- [Storage](runbooks/storage.md)
- [Release and migrations](runbooks/release-and-migrations.md)
- [Security, governance and retention](runbooks/security-governance-retention.md)

## CI/CD And Pages

CI validates build, tests, migrations, secret scan, Docker and Trivy. The documentation site is built with `mkdocs build --strict` and published through GitHub Pages from `main`.

More detail: [GitHub CI/CD](github-ci-cd.md).

## Diagnosis

Use `docker compose -f deploy/docker-compose.yml logs media-intelligence-api`
locally without sharing secrets. `Pending` can indicate a stopped worker;
`Failed` requires checking normalized issues and provider logs. For `401`, `403`,
invalid uploads, credentials or webhook failures, use the existing
[error reference](reference/api-endpoint-flows.en.md).
[AWS setup and diagnosis](aws-rekognition-face-search.en.md) is now the single
provider guide, alongside [production credentials](reference/production-cloud-credentials.md).
