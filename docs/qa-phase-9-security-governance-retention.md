# QA Phase 9: Security, Governance And Retention

## Scope

Validate the Phase 9 controls before moving to Phase 10:

- Admin-only access to governance endpoints.
- Sanitized audit events for critical actions.
- Per-client retention policy and execution.
- No secrets or document content in logs or audit metadata.
- Product documentation for operators.

Baseline:

- Branch: `codex/phase-9-security-governance-retention`.
- Base commit before Phase 9 changes: `7ddcea3`.
- Environment: Docker local, Development mode.
- Google Document AI: configured with project `primeval-wind-499515-k6`, location `us`, processor `44b3274ce1e84552`; credential contents were not inspected or recorded.

## QA Gates

| Gate | Expected Result | Status |
| --- | --- | --- |
| Build | `dotnet build` succeeds with 0 warnings | Passed |
| Tests | `dotnet test` succeeds | Passed |
| Migration drift | EF reports no pending model changes after migration | Passed |
| Compose config | Docker Compose config is valid | Passed |
| Admin authorization | Anonymous requests return 401 and client tokens return 403 for admin governance endpoints | Passed |
| Audit trail | Critical actions generate sanitized audit events | Passed |
| Retention | Enabled policy deletes eligible stored media and marks rows with `DeletedAt` | Passed |
| Secret safety | Logs and audit metadata do not include API keys, webhook secrets, JWT signing key, Google credential values or OCR text | Passed |

## Automated Coverage

- `ApiSecurityTests` covers anonymous and client-only rejection for admin audit and retention endpoints.
- `AuditApplicationServiceTests` verifies audit metadata is sanitized.
- `RetentionApplicationServiceTests` verifies retention validation and deletion behavior.

## Real QA Evidence

- `docker compose -f deploy\docker-compose.yml up --build -d` rebuilt and started API plus PostgreSQL.
- `/health` returned `200`.
- `/ready` returned `Healthy`, including PostgreSQL, local storage and Google Document AI.
- Anonymous `GET /api/v1/admin/audit-events` returned `401`.
- Client-scoped token calling `GET /api/v1/admin/audit-events` returned `403`.
- Admin token created a real QA API client and one-time API key.
- Real API key was exchanged through `POST /api/v1/auth/token`.
- Real PDF analysis was submitted through `POST /api/v1/analyze` for `invoice-extraction-v1`; worker completed the job with confidence `0.8900`, `3` extracted fields and `0` issues.
- `GET /api/v1/analysis-jobs/{jobId}/result` generated `analysis_result.read`.
- Retention policy was set to enabled with `retentionDays=1`.
- The QA asset was aged in the local QA database to validate deletion without waiting a real day.
- `POST /api/v1/admin/clients/{clientKey}/retention-policy/execute` returned `scannedAssets=1` and `deletedAssets=1`.
- PostgreSQL showed the media asset row marked with `DeletedAt`.
- API key rotation and revocation generated `api_key.rotated` and `api_key.revoked`.
- Human review start and approve were executed through API endpoints and generated `human_review.started` plus `human_review.completed`.
- Audit metadata scan showed no raw API key and no OCR text.
- Docker log scan showed no raw API key, OCR name, private key or refresh token.

## Findings

- Major fixed before final validation: retention initially considered `RequiresReview` eligible for deletion. It now only includes `Completed`, `Failed` and `Cancelled` jobs so pending human review evidence is preserved.
- Minor command issue during QA: PowerShell plus `curl.exe` JSON quoting produced invalid dev-token payloads. QA switched to `Invoke-RestMethod` for JSON bodies on Windows.

QA result: no open Blocker, Critical or Major findings for Phase 9.
