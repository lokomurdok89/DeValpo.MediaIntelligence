# Operacion

La operacion local y de release se apoya en Docker Compose, health checks, readiness checks, runbooks y CI.

## Health Y Readiness

| Endpoint | Uso | Seguridad |
| --- | --- | --- |
| `/health` | Liveness liviano del proceso API. | Publico |
| `/api/v1/health` | Health versionado. | Publico |
| `/ready` | Dependencias requeridas: DB, storage y opcionalmente Google. | Publico |
| `/api/v1/operations/metrics` | Contadores operativos. | Operator/Admin |

## Docker Local

```powershell
docker compose -f deploy\docker-compose.yml config --quiet
docker compose -f deploy\docker-compose.yml up --build
docker compose -f deploy\docker-compose.yml ps
```

Componentes:

- `postgres`: PostgreSQL 17.
- `media-intelligence-api`: API ASP.NET Core.
- Volumen de media assets.
- Montaje local de secretos Google.

## Runbooks

- [Credenciales, usuarios y perfiles cloud](reference/cloud-identities-and-provider-profiles.md)
- [Google Document AI](runbooks/google-document-ai.md)
- [PostgreSQL](runbooks/postgres.md)
- [Storage](runbooks/storage.md)
- [Release y migraciones](runbooks/release-and-migrations.md)
- [Seguridad, gobierno y retencion](runbooks/security-governance-retention.md)

## CI/CD Y Pages

La CI valida build, tests, migrations, secret scan, Docker y Trivy. El sitio documental se construye con `mkdocs build --strict` y se publica por GitHub Pages desde `main`.

Mas detalle: [GitHub CI/CD](github-ci-cd.md).

## Diagnóstico

Usa `docker compose -f deploy/docker-compose.yml logs media-intelligence-api`
localmente, sin compartir secretos. `Pending` puede indicar un worker detenido;
`Failed` requiere revisar incidencias normalizadas y logs del proveedor.
Para `401`, `403`, uploads inválidos, credenciales o webhooks, consulta la
[tabla de errores](reference/api-endpoint-flows.md#checklist-de-error-rapido).
[Configuración y diagnóstico AWS](aws-rekognition-face-search.md) concentra el
runbook del proveedor; [credenciales productivas](reference/production-cloud-credentials.md)
conserva identidades y permisos.
