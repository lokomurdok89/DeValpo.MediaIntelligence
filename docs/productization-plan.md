# Productization Plan

This plan tracks the work required to move DeValpo MediaIntelligence from local MVP to a production-ready product.

## Current Baseline

Already working:

- API + PostgreSQL start through one Docker Compose file.
- JWT Bearer protects functional endpoints.
- Development token endpoint exists only for local Development.
- Swagger is enabled and includes endpoint descriptions.
- Google Document AI can process a real PDF through ADC credentials mounted outside the repository.
- The real product document OCR profile is `generic-document-ocr-v1`.
- `invoice-extraction-v1` remains as a temporary compatibility alias.
- Mock skills and mock profiles are gated by explicit configuration and are disabled by default.
- API clients can be created by an admin-scoped token.
- API keys are returned once and stored only as hashes.
- Clients can exchange API keys for short-lived JWTs.
- API keys can be rotated and revoked.
- Analysis jobs, media assets, skill executions, results, issues and human review tasks are persisted.
- Initial EF Core migration exists.
- Basic unit and API security tests exist.
- Interfaces have been split from several application implementations.

Known product gaps:

- User-facing messages must continue to be audited so new runtime text remains resource-backed.
- Rate limiting and abuse controls are not implemented yet.
- Auth/audit events are not recorded yet.
- Human review is persisted but has no review workflow.
- Webhook delivery is modeled but not implemented.
- There is no production observability, audit trail or rate limiting.
- There is no real QA matrix using real documents and real provider calls.

## Product Definition

The product should provide a secure API for clients to submit media/document assets, run configured intelligence profiles, persist normalized results, expose operational status, and support review/retry/integration flows.

Initial product scope:

- Real document/PDF ingestion.
- Real Google Document AI OCR provider.
- Client-scoped API access.
- Persisted jobs and results.
- Human review queue for uncertain results.
- Operator-visible diagnostics.
- Docker/local and production deployment paths.

Out of scope until later:

- Live video production processing.
- Face recognition or biometric identification.
- Fully autonomous document business decisions.
- Unbounded bulk ingestion.

## Phase 1: Foundation Hardening

Goal: remove MVP shortcuts that can leak into product behavior.

Work:

1. Move all user-facing and operational messages to resource files.
   - Create resource files for API, Application and Infrastructure messages.
   - Keep technical error codes and claim names as constants, not resources.
   - Cover Google status messages, validation errors, orchestration issues, health check messages and storage errors.

2. Refactor Google Document AI credential handling.
   - Done in Phase 1: `IGoogleCredentialProvider` is split from `GoogleDocumentAiClientFactory`.
   - Done in Phase 1: obsolete credential loading pragmas are removed.
   - Done in Phase 1: ADC and service account JSON file credentials are explicit.
   - Done in Phase 1: credential content is not logged or exposed.

3. Clean profile naming.
   - Done in Phase 1: `generic-document-ocr-v1` is the real OCR profile.
   - Done in Phase 1: `invoice-extraction-v1` remains only as a compatibility alias.
   - Done in Phase 1: docs and examples use the generic profile.

4. Remove product dependency on mock skills.
   - Done in Phase 1: mock skills are registered only when explicitly enabled.
   - Done in Phase 1: production profiles reference only real skills by default.
   - Done in Phase 1: startup validation fails when enabled profiles reference missing/mock skills while mocks are disabled.

5. Keep interface segregation clean.
   - No public interface should share a file with its implementation.
   - Large interfaces should be split when consumers need only part of the contract.

Exit criteria:

- `dotnet build` has zero warnings.
- `dotnet test` passes.
- `rg "Mock" src/DeValpo.MediaIntelligence.Infrastructure/DependencyInjection.cs` shows mocks gated by environment/config.
- `rg "\"[A-Z][^\"]*(failed|required|not found|invalid|disabled|configured)" src` does not find user-facing hardcoded messages outside resource helpers/tests.
- Real Document AI status and one real PDF analysis work locally.

## Phase 2: Customer Access And Security

Goal: replace local JWT assumptions with a product access model.

Work:

1. Introduce API clients.
   - Done in Phase 2: store client records with status, display name, contact metadata and allowed profiles.
   - Done in Phase 2: add client key lifecycle for create, rotate, revoke and expire.
   - Done in Phase 2: never store raw API keys. Store hashes only.

2. Add token issuance workflow.
   - Done in Phase 2: clients authenticate with API key.
   - Done in Phase 2: API issues short-lived JWTs with `sub`, `client_key`, `scope`, `jti`, `iat`, `exp`.
   - Done in Phase 2: Development token endpoint remains Development-only.

3. Enforce tenant/profile authorization.
   - Done in Phase 3: `clientKey` from request must match authenticated client.
   - Done in Phase 3: client can only execute assigned profiles.
   - Done in Phase 3: results/jobs are readable by the owning client or operator/admin role.

4. Add authorization policies.
   - Done in Phase 2: admin scope protects API client management.
   - Done in Phase 3: client, operator and admin scopes are enforced for submission, resource access and Google remote diagnostics.

5. Add rate limiting and abuse controls.
   - Per-client request limits.
   - Max file size by plan/profile.
   - Optional idempotency key for repeated submissions.

Exit criteria:

- Unauthorized clients cannot access other clients' jobs/results.
- API key rotation can be done without downtime.
- Raw secrets never appear in database, logs, Swagger examples or test output.
- Swagger documents auth requirements clearly.

## Phase 3: Real Analysis Workflow

Goal: make analysis reliable for real documents and operationally safe.

Work:

1. Convert analysis execution to asynchronous jobs.
   - Done in Phase 4: `/api/v1/analyze` enqueues and returns `202 Accepted`.
   - Done in Phase 4: background worker processes queued jobs.
   - Done in Phase 4: retry policy handles transient provider errors up to the configured attempt count.
   - Done in Phase 4: tracks `Pending`, `Running`, `Completed`, `Failed`, `Cancelled`, `RequiresReview`.

2. Improve provider output normalization.
   - Store raw provider metadata safely when useful.
   - Extract normalized text and structured fields.
   - Add confidence and page/region references where available.
   - Preserve provider issues in a machine-readable way.

3. Add document taxonomy.
   - Generic OCR profile.
   - Optional profile-specific extraction later.
   - Avoid hardcoding invoice assumptions into the base engine.

4. Add validation for uploaded files.
   - Done in Phase 5: extension allowlist by asset type.
   - Done in Phase 5: declared content type check with binary signature sniffing.
   - Done in Phase 5: corrupt/incomplete PDF handling.
   - Done in Phase 5: empty and oversized document handling.
   - Done in Phase 5: malware scanning hook with no-op local implementation.

5. Add real provider health and readiness.
   - `/health` should remain lightweight.
   - `/ready` should validate database and configured providers.
   - Provider remote checks must have timeout and cache.

Exit criteria:

- A real PDF can be submitted and later retrieved by job id.
- Provider transient failures are retried or classified.
- Corrupt, empty and unsupported files return stable errors.
- Job state transitions are testable and auditable.

## Phase 4: Human Review Product Surface

Goal: turn `HumanReviewTask` from a persisted flag into a usable workflow.

Work:

1. Add review endpoints.
   - List pending review tasks.
   - Get review task detail.
   - Approve/correct/reject extracted result.
   - Complete review with operator identity and timestamp.

2. Add corrected result model.
   - Store original provider result.
   - Store human-corrected fields.
   - Preserve audit trail.

3. Add review assignment and status.
   - `Pending`, `InReview`, `Completed`, `Rejected`.
   - Optional assignee.

4. Add review metrics.
   - Review rate by profile.
   - Most common issues.
   - Average review time.

Exit criteria:

- Low-confidence real analysis opens a review task.
- An operator can complete review through API.
- Original and corrected values remain auditable.

## Phase 5: Integrations And Delivery

Goal: deliver results to downstream systems reliably.

Work:

1. Implement webhook delivery.
   - Register webhook targets per client.
   - Sign webhook payloads.
   - Retry with backoff.
   - Store attempts and last response.

2. Add result export.
   - JSON export.
   - Optional text export.
   - Optional provider raw document export gated by security policy.

3. Add idempotent callbacks.
   - Include job id and event id.
   - Let clients safely retry processing.

Exit criteria:

- Completed jobs can notify a test webhook receiver.
- Failed webhook attempts are visible and retryable.
- Webhook payloads are signed and documented.

## Phase 6: Observability And Operations

Goal: make production behavior visible and supportable.

Work:

1. Structured logging.
   - Include correlation id, job id, client key, profile key and provider.
   - Exclude secrets and raw document content.

2. Metrics.
   - Request counts/status.
   - Job duration.
   - Provider latency/error rate.
   - Review rate.
   - Queue depth.

3. Tracing.
   - API request to job execution to provider call.
   - Propagate correlation id.

4. Operational endpoints.
   - `/health` liveness.
   - `/ready` readiness.
   - Provider status with cached remote result.

5. Runbooks.
   - Google auth failure.
   - Processor not found.
   - Quota/billing failure.
   - Database unavailable.
   - Storage unavailable.

Exit criteria:

- A production incident can be diagnosed from logs/metrics without reading source code.
- Provider errors are classified consistently.
- Health/readiness behavior is documented and tested.

## Phase 7: Data Governance And Compliance

Goal: handle client documents as sensitive data.

Work:

1. Data retention.
   - Per-client retention policy.
   - Delete or archive media assets after configured period.
   - Delete provider raw output if not needed.

2. Encryption.
   - Encrypt secrets at rest.
   - Use managed secrets in production.
   - Ensure database/storage encryption in target infrastructure.

3. Audit logging.
   - Auth events.
   - Job reads.
   - Result reads.
   - Review changes.
   - Key rotation/revocation.

4. Privacy controls.
   - Redaction strategy for logs.
   - PII handling policy.
   - Data access roles.

Exit criteria:

- Sensitive content is not logged.
- Retention can be executed and verified.
- Audit trail covers critical product actions.

## Phase 8: Deployment And Release

Goal: move beyond local Docker safely.

Work:

1. Production configuration.
   - Remove dummy defaults for production.
   - Enforce strong JWT/API key settings.
   - Disable Development token endpoint.
   - Disable Swagger or protect it in production.

2. CI/CD.
   - Build and test.
   - Docker image build.
   - Compose config validation.
   - Migration validation.
   - Secret scanning.
   - Container vulnerability scan.

3. Database migration process.
   - Decide automatic vs controlled migrations.
   - Add rollback/runbook.
   - Test migrations against a copy of production-like data.

4. Environment strategy.
   - Local.
   - QA/staging.
   - Production.

Exit criteria:

- One tagged release can be deployed to staging.
- Secrets are supplied by environment/secret manager, not repository files.
- Production starts only with valid secure configuration.

## QA Strategy

Real QA should avoid synthetic-only confidence. Use real documents, real Docker, real PostgreSQL and real Google Document AI when provider behavior is under test.

Required QA suites:

- API contract QA.
- Auth and tenant isolation QA.
- Google provider QA.
- File ingestion QA.
- Persistence and job lifecycle QA.
- Human review QA.
- Webhook QA.
- Observability QA.
- Deployment QA.
- Security regression QA.

Test data:

- Small valid PDF.
- Multi-page PDF.
- Scanned PDF.
- Image document.
- Empty file.
- Corrupt PDF.
- Oversized file.
- Unsupported extension.
- Document with accents/special characters.
- Document with low OCR quality.

## Priority Order

Recommended execution order:

1. Resource files and Google factory refactor.
2. Remove/gate mocks from production path.
3. Rename/genericize real document OCR profile.
4. Client API key and JWT issuance workflow.
5. Tenant/profile authorization.
6. Async job execution.
7. Human review endpoints.
8. Webhook delivery.
9. Observability and readiness.
10. Retention/audit/security hardening.
11. Staging deployment path.
12. Product QA certification using `docs/devalpo-mediaintelligence-qa`.

## Product Readiness Gate

Do not call the system product-ready until all are true:

- Real client authentication exists.
- Client isolation is enforced and tested.
- Real Google Document AI flow works in Docker and staging.
- Production profile does not depend on mock skills.
- All user-facing messages are resource-backed.
- Secrets never appear in repo, logs or Swagger examples.
- Analysis jobs are recoverable, auditable and observable.
- Human review and webhook behavior are implemented or explicitly disabled in product configuration.
- Real-document QA has passed with documented evidence.
