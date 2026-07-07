# Release Scope v0.1.0-rc.1

This release candidate is the first controlled-onboarding version of DeValpo MediaIntelligence.

## Included

- Docker local runtime for API and PostgreSQL.
- Google Document AI OCR through the product profile `generic-document-ocr-v1`.
- Compatibility alias `invoice-extraction-v1` for current local validation flows.
- Client API key creation, rotation, revocation and JWT issuance.
- JWT-protected functional endpoints.
- Tenant isolation for jobs, results, profiles, webhooks and review tasks.
- Async analysis jobs with persisted status and retry attempts.
- File ingestion validation for empty, unsupported, mismatched, corrupt and oversized files.
- Local media asset storage outside PostgreSQL.
- Persisted normalized analysis result fields, labels, entities, clips and issues.
- Human review queue and review completion endpoints.
- Webhook target configuration, signed delivery, retry and delivery history.
- Health, readiness, operational metrics and structured log context.
- Audit events for critical auth, result-read, review, retention and key lifecycle actions.
- Per-client retention policy and execution.
- CI gates for build, tests, EF migration drift, secret scan, release config, Docker build, Compose config and container vulnerability scan.
- Staging/Production startup safety checks for unsafe local configuration.

## Excluded

- Public self-service UI.
- Multi-region or autoscaling deployment.
- Managed production secret manager integration.
- Object storage provider beyond local filesystem storage.
- Production identity provider or customer SSO.
- Antivirus engine integration beyond the current scanner abstraction.
- Paid SLA, support process and formal incident management.
- Video/live-video production flows.
- Automated deployment to an external cloud environment.

## Release Readiness Position

The release is ready for controlled onboarding of one first client in a supervised environment after:

- The deployment operator supplies managed secrets and production database/storage configuration.
- Database migrations are applied through the release runbook.
- A real document smoke test passes in the target environment.
- Operational owners accept the runbooks and QA evidence.

## Version Tag

Target tag after merge: `v0.1.0-rc.1`.

