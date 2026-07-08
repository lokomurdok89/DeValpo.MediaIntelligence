# QA Phase 8: Observability And Readiness

## Scope

Phase 8 makes the system diagnosable without reading source code.

Included:

- Split lightweight liveness from dependency readiness.
- Add `/ready` with PostgreSQL, storage and Google Document AI readiness checks.
- Cache remote Google Document AI readiness checks with a bounded timeout.
- Add structured request/job logging context with correlation id, client key, job id, profile key and provider where available.
- Add operator/admin operational metrics endpoint.
- Add runbooks for Google Document AI, PostgreSQL and storage failures.

Out of scope:

- External metrics backend.
- Distributed tracing backend.
- Alerting rules.
- Production audit log table.
- Rate limiting.

## Automated QA

Executed on branch `codex/phase-8-observability-readiness`:

- `dotnet build`: passed, 0 warnings.
- `dotnet test`: passed, 56/56.
- `dotnet ef migrations has-pending-model-changes --project src\DeValpo.MediaIntelligence.Infrastructure --startup-project src\DeValpo.MediaIntelligence.Api`: passed, no pending model changes.
- `docker compose -f deploy\docker-compose.yml config --quiet`: passed.

Expected coverage:

- `/health` remains anonymous and lightweight.
- `/api/v1/health` remains anonymous.
- `/api/v1/operations/metrics` requires authentication.
- Client-only token cannot read operational metrics.
- Google readiness check returns healthy when disabled and not required.
- Google readiness check returns unhealthy when required and not configured.

## Real Docker QA Matrix

Environment:

- Docker Compose: `deploy/docker-compose.yml`.
- API: `http://localhost:8085`.
- Database: PostgreSQL container.
- Provider: Google Document AI with local ADC mounted through Docker secrets.
- Auth: Development admin/operator token.
- Test document: real PDF processed through Google Document AI.

| Scenario | Expected Result | Status |
| --- | --- | --- |
| `/health` liveness | HTTP `200`, body `Healthy`, no DB/provider dependency | Passed |
| `/ready` readiness healthy path | HTTP `200`, JSON includes `postgres`, `local-storage`, `google-document-ai` healthy | Passed: all three checks `Healthy` |
| Metrics without token | HTTP `401` | Passed |
| Metrics with client token | HTTP `403` | Passed |
| Metrics with operator/admin token | HTTP `200`, counters only, no secrets/document text | Passed |
| Real document analysis logs | Logs include correlation/job/client/profile/provider metadata and no OCR text | Passed: job `4a5d2923-0138-4894-beb3-41c2f7d508dc` |
| DB unavailable readiness | `/ready` returns non-healthy with `postgres` failed | Passed: `/health` stayed `200`, `/ready` returned `503`, `postgres:Unhealthy` |
| Google remote readiness cache | Repeated `/ready` calls do not require repeated remote calls within TTL | Passed: consecutive cached calls returned `200` in about `0.006s` and `0.005s` |

## QA Result

Passed after remediation.

## Findings

### Major: Metrics Query Failed Against PostgreSQL

Initial `GET /api/v1/operations/metrics` with an admin token returned `400` because EF Core/Npgsql could not translate projection from `GroupBy` directly into `MetricBucketResponse`.

Root cause:

- The query projected grouped rows directly into a contract record type.

Remediation:

- Changed the repository to project grouped rows into an anonymous type inside SQL and map to `MetricBucketResponse` in memory.
- Rebuilt Docker and re-ran metrics QA.

Status: fixed.

## Residual Risks

- Phase 8 exposes basic persisted counters, not a production metrics backend.
- Distributed tracing and alerting remain Phase 10/product hardening work.
