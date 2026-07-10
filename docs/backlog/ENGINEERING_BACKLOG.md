# Engineering Backlog

This backlog records verified gaps. It is not evidence that a remediation has
been implemented or validated.

## ENG-001: Add Real Upload Malware Scanning

- Category: Security
- Priority: P0
- Problem: Upload validation delegates threat scanning to a no-op implementation.
- Risk: Malicious files can reach local storage and downstream providers.
- Evidence: `Api/Validation/NoOpUploadedFileThreatScanner.cs`.
- Proposal: Integrate an approved scanner behind `IUploadedFileThreatScanner`.
- Acceptance criteria: infected fixtures are rejected, clean fixtures pass, and failures are observable.
- Dependencies: scanner runtime and operational ownership.
- Status: Open

## ENG-002: Protect Webhook Delivery From SSRF

- Category: Security
- Priority: P0
- Problem: webhook targets accept any absolute HTTP/HTTPS URL.
- Risk: an authorized or compromised admin can direct workers to internal services.
- Evidence: `Application/Webhooks/WebhookDeliveryApplicationService.cs`.
- Proposal: enforce HTTPS and an outbound-host policy that rejects loopback, link-local and private ranges.
- Acceptance criteria: unsafe target cases are rejected and approved targets still deliver.
- Dependencies: approved customer-host policy.
- Status: Open

## ENG-003: Protect Persisted Webhook Signing Secrets

- Category: Security
- Priority: P0
- Problem: webhook HMAC signing secret is modeled as a persisted string.
- Risk: database disclosure exposes outbound webhook credentials.
- Evidence: `Domain/Entities/ClientWebhookTarget.cs`; `Persistence/Configurations/ClientConfiguration.cs`.
- Proposal: use managed secret storage or application-level encryption with rotation.
- Acceptance criteria: raw secrets are not readable from ordinary database access and rotation is supported.
- Dependencies: secret-management design and migration plan.
- Status: Open

## ENG-004: Add API Rate Limiting

- Category: Security and resilience
- Priority: P1
- Problem: no rate-limiting middleware or policy is configured.
- Risk: token, upload, status and job endpoints are susceptible to abuse.
- Evidence: `Api/Program.cs`; no rate-limiting configuration in the solution.
- Proposal: define per-client and endpoint policies with safe rejection behavior.
- Acceptance criteria: limits are tested and operationally observable.
- Dependencies: client traffic assumptions.
- Status: Open

## ENG-005: Add Telemetry And Distributed Tracing

- Category: Observability
- Priority: P1
- Problem: Serilog correlation exists, but no OpenTelemetry/tracing export is configured.
- Risk: cross-worker and provider failures are difficult to diagnose.
- Evidence: `Api/Program.cs`; current CI/configuration files.
- Proposal: define traces, metrics, dashboards and retention without sensitive payloads.
- Acceptance criteria: job, worker, provider and webhook paths can be correlated end-to-end.
- Dependencies: observability platform and data-classification policy.
- Status: Open

## ENG-006: Add Real PostgreSQL Persistence Tests

- Category: Testing
- Priority: P1
- Problem: tests rely on fakes, configuration in memory and API host tests.
- Risk: EF mappings, migrations and query behavior can regress undetected.
- Evidence: `tests/DeValpo.MediaIntelligence.Tests`; no Testcontainers/SQLite persistence suite.
- Proposal: add isolated PostgreSQL integration tests, preferably with Testcontainers.
- Acceptance criteria: migrations and representative repositories run against PostgreSQL in CI.
- Dependencies: CI container support.
- Status: Open

## ENG-007: Automate Approved Cloud Smoke Tests

- Category: Testing
- Priority: P1
- Problem: real provider validation is environment-dependent and manual.
- Risk: credentials, permissions and cloud routing drift after unit tests pass.
- Evidence: `docs/testing/TESTING_STRATEGY.md`; provider status endpoints.
- Proposal: create an opt-in protected smoke workflow using sanitized assets and isolated cloud resources.
- Acceptance criteria: each enabled provider has recorded pass/fail evidence without exposing secrets.
- Dependencies: cloud test accounts, budgets and retention controls.
- Status: Open

## ENG-008: Publish And Enforce Coverage In CI

- Category: Quality
- Priority: P2
- Problem: Coverlet is referenced but CI does not collect, publish or gate coverage.
- Risk: quality changes cannot be measured consistently.
- Evidence: test project and `.github/workflows/ci.yml`.
- Proposal: collect coverage, publish artifacts and agree risk-based thresholds.
- Acceptance criteria: CI reports coverage and policy is documented.
- Dependencies: baseline measurement.
- Status: Open

## ENG-009: Make Trivy Blocking For Defined Severities

- Category: Supply chain
- Priority: P2
- Problem: Trivy uses `--exit-code 0`.
- Risk: high/critical findings do not block CI.
- Evidence: `.github/workflows/ci.yml`.
- Proposal: define remediation/exception policy and enforce it in CI.
- Acceptance criteria: policy violations fail CI with a reviewable exception path.
- Dependencies: vulnerability ownership and exception process.
- Status: Open

## ENG-010: Cover Relevant Branches In CI

- Category: CI/CD
- Priority: P2
- Problem: push CI covers `main`, `develop` and `feature/**`, not current chore/agent/codex conventions.
- Risk: direct pushes on active branch prefixes can miss CI.
- Evidence: `.github/workflows/ci.yml`; local branch conventions.
- Proposal: align triggers with the branch policy or require pull-request-only validation.
- Acceptance criteria: every supported development branch receives required checks.
- Dependencies: branch-protection policy.
- Status: Open

## ENG-011: Establish Formatting And Analyzer Policy

- Category: Maintainability
- Priority: P2
- Problem: no `.editorconfig`, analyzer policy or format check is configured.
- Risk: style and basic static-analysis expectations drift.
- Evidence: repository root and project configuration.
- Proposal: add a minimal shared policy and CI validation after team agreement.
- Acceptance criteria: local and CI formatting/analyzer behavior is documented and repeatable.
- Dependencies: team conventions.
- Status: Open

## ENG-012: Define Code Ownership And Review Routing

- Category: Governance
- Priority: P2
- Problem: no CODEOWNERS or documented ownership matrix exists.
- Risk: sensitive provider, persistence and security changes lack predictable review.
- Evidence: `.github/` contents and current documentation.
- Proposal: define owners by architectural area and escalation backup.
- Acceptance criteria: review routing is visible and maintained.
- Dependencies: named maintainers.
- Status: Open

## ENG-013: Define Data Classification And Retention Controls

- Category: Governance
- Priority: P1
- Problem: media, OCR, biometric references and audit data lack a unified classification policy.
- Risk: inconsistent logging, storage and retention decisions.
- Evidence: `docs/security.md`; `docs/runbooks/security-governance-retention.md`.
- Proposal: classify data, define allowed locations, retention and deletion evidence.
- Acceptance criteria: policy maps each persisted/processed data class to controls and owners.
- Dependencies: legal/privacy decision.
- Status: Open

## ENG-014: Define SLO, RPO And RTO

- Category: Operations
- Priority: P2
- Problem: no service objectives or recovery targets are documented.
- Risk: readiness and incident priorities cannot be measured against an agreed target.
- Evidence: `docs/operations.md`; `docs/runbooks/`.
- Proposal: establish service, backup and recovery objectives by workflow.
- Acceptance criteria: objectives, monitoring and recovery verification are documented.
- Dependencies: product and infrastructure ownership.
- Status: Open

## ENG-015: Publish A Cloud Permission Matrix

- Category: Security and operations
- Priority: P1
- Problem: provider documentation describes setup but not a single least-privilege matrix.
- Risk: overprivileged identities and unclear operational responsibility.
- Evidence: `docs/reference/cloud-identities-and-provider-profiles.md`; provider integrations.
- Proposal: map each provider operation to identity, permission, resource scope and rotation owner.
- Acceptance criteria: matrix is reviewed and referenced by runbooks.
- Dependencies: cloud account ownership.
- Status: Open

## ENG-016: Maintain A Dependency Inventory

- Category: Supply chain
- Priority: P2
- Problem: package versions live in project files without a maintained inventory or review cadence.
- Risk: upgrade, license and vulnerability ownership is unclear.
- Evidence: production and test `.csproj` files; CI workflow.
- Proposal: publish an inventory/SBOM process with ownership and update cadence.
- Acceptance criteria: inventory is generated or maintained and reviewed in releases.
- Dependencies: supply-chain tooling decision.
- Status: Open

## ADR Candidates

Write retrospective ADRs only after discussion for: provider-routing and
cross-cloud staging, cloud-asset lifecycle/cleanup, webhook-secret management,
and the target deployment/observability model.
