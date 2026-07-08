# QA Phase 11: Product Certification

## Scope

Certify the product for controlled onboarding of the first client.

## Release Candidate

- Target tag: `v0.1.0-rc.1`.
- Branch: `codex/phase-11-product-certification`.
- Base commit before Phase 11 changes: `eb40d94`.

## Quality Gates

| Area | Expected Result | Status |
| --- | --- | --- |
| Auth | API key exchange issues JWT and protected endpoints reject missing tokens | Passed |
| Tenant isolation | Client B cannot access Client A job/result | Passed |
| Google provider | Remote status and real PDF processing pass with configured processor | Passed |
| File ingestion | Valid real PDF is accepted; invalid file behavior remains covered by automated tests | Passed |
| Async lifecycle | Job moves from pending/running to terminal state | Passed |
| Human review | Operator workflow remains available and audited | Passed |
| Webhooks | Signed delivery reaches local receiver and history is visible | Passed |
| Observability | `/health`, `/ready`, metrics and logs are usable without leaking OCR/secrets | Passed |
| Security | Audit, retention and release safety gates pass | Passed |
| CI/CD | GitHub Actions release gates pass on PR | Passed |
| Docs | Release scope, client onboarding and runbooks are complete | Passed |
| Tag | `v0.1.0-rc.1` exists after merge | Passed |

## Real Document Matrix

| Asset | Type | Purpose | Result |
| --- | --- | --- | --- |
| `DTE Daniel.pdf` | PDF | Real Google Document AI OCR and async processing | Passed: job `01891cc6-3e61-4177-bb2e-0cbfe98b2bd1`, confidence `0.8900`, `3` extracted fields, `requiresHumanReview=false` |

## Real QA Evidence

- `dotnet build` passed with `0` warnings.
- `dotnet test` passed with `72/72`.
- `dotnet ef migrations has-pending-model-changes --project src\DeValpo.MediaIntelligence.Infrastructure --startup-project src\DeValpo.MediaIntelligence.Api` reported no model drift.
- `powershell -NoProfile -File scripts\ci\secret-scan.ps1 -Root .` passed.
- `powershell -NoProfile -File scripts\ci\validate-release-config.ps1 -Root .` passed.
- `docker compose -f deploy\docker-compose.yml config --quiet` passed.
- Docker image build for `devalpo-media-intelligence-api:ci` passed.
- Trivy `aquasec/trivy:0.69.3` scan against `devalpo-media-intelligence-api:ci` reported `0` HIGH/CRITICAL vulnerabilities.
- GitHub Actions `build-test` passed on PR #18.
- Tag `v0.1.0-rc.1` was created and pushed after PR #18 merged.
- Docker Compose release candidate rebuilt and started API plus PostgreSQL.
- `/health` returned `200`.
- `/ready` returned `Healthy` with PostgreSQL, local storage and Google Document AI.
- Google Document AI remote status returned configured and reachable for project `primeval-wind-499515-k6`, location `us`, processor `44b3274ce1e84552`; credential contents were not inspected or recorded.
- Created two real QA clients:
  - Client A: `cert-20260706215734-a`.
  - Client B: `cert-20260706215734-b`.
- Both clients exchanged one-time API keys for JWTs through `/api/v1/auth/token`.
- Client A submitted real PDF `DTE Daniel.pdf` with profile `generic-document-ocr-v1`.
- Client A read its result successfully.
- Client B attempted to read Client A result and received `404`.
- Local webhook receiver on port `8099` received one signed delivery from the container through `host.docker.internal`.
- Webhook delivery history returned one `Delivered` record.
- Webhook payload contained signature headers and did not contain OCR text.
- Human review task `9fb73927-1f61-4331-a56a-ca296c7e2f30` was started and approved through API endpoints.
- Human review audit events persisted: `human_review.started`, `human_review.completed`.
- Operational metrics endpoint returned counters successfully.
- Retention policy for Client A was configured with `enabled=true`, `retentionDays=30`.
- Audit event metadata did not include raw API keys or OCR text.
- Docker logs did not include raw API keys or OCR text.
- Local Docker deployment is the release-candidate staging-equivalent for this repository. External cloud deployment remains outside `v0.1.0-rc.1` scope and is listed in `docs/release-scope-v0.1.0-rc.1.md`.

## Findings

- No Blocker, Critical or Major findings found in local product certification.
- No open Blocker, Critical, Major or Minor findings remain for `v0.1.0-rc.1`.
