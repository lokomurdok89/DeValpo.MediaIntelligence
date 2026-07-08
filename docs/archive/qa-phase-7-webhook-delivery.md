# QA Phase 7: Webhook Delivery

## Scope

Phase 7 delivers completed analysis results to downstream systems through signed webhook callbacks.

Included:

- Register webhook targets per client.
- Store target metadata and signing secret for outbound HMAC signing.
- Enqueue `analysis.completed` deliveries when jobs complete.
- Send signed JSON payloads from a background worker.
- Store attempts, status, last HTTP status code, last error and retry schedule.
- Retry failed attempts with backoff.
- Expose delivery history by client and job.
- Document payload and signature headers.

Out of scope:

- Secret manager integration for webhook signing secrets.
- Webhook target update/delete lifecycle.
- Webhook event replay endpoint.
- Per-client webhook rate limiting.
- Webhook UI.

## Automated QA

Executed on branch `codex/phase-7-webhook-delivery`:

- `dotnet build`: passed, 0 warnings.
- `dotnet test`: passed, 51/51.
- `dotnet ef migrations has-pending-model-changes --project src\DeValpo.MediaIntelligence.Infrastructure --startup-project src\DeValpo.MediaIntelligence.Api`: passed, no pending model changes.
- `docker compose -f deploy\docker-compose.yml config --quiet`: passed.

Expected unit coverage:

- HMAC signature uses `timestamp.payloadJson` and returns `sha256=<hex>`.
- Enqueued payload includes event/job/client/profile/status/result URL.
- Enqueued payload does not include signing secret or raw OCR text.
- Created webhook target response does not return signing secret.
- Successful send marks delivery `Delivered`.
- Failed send before max attempts marks delivery `RetryPending`.

## Real Docker QA Matrix

Environment:

- Docker Compose: `deploy/docker-compose.yml`.
- API: `http://localhost:8085`.
- Database: PostgreSQL container.
- Auth: local Development JWT with admin/client scopes.
- Provider: Google Document AI with local ADC mounted through Docker secrets.
- Test document: real PDF processed through Google Document AI.
- Receiver: local HTTP receiver reachable from Docker through `host.docker.internal`.

| Scenario | Expected Result | Status |
| --- | --- | --- |
| Register webhook target | `201`, response excludes signing secret | Passed: target `qa-local`, response did not include signing secret |
| Complete real analysis job | Job reaches terminal success using Google Document AI | Passed: job `0cf8cd09-0103-432d-92ea-fe963d5677fa` |
| Callback to local receiver | Receiver gets one signed `analysis.completed` request | Passed: local receiver received `POST /webhook` |
| Validate signature | HMAC matches `timestamp.rawBody` | Passed |
| Inspect delivery history | Delivery becomes `Delivered` with attempt count | Passed: `Delivered`, attempt count `1` |
| Simulate receiver down | Delivery becomes `RetryPending` and schedules retry | Passed: job `f7edaf99-5056-4325-b1e9-3ab3bbd58bef`, `RetryPending`, attempt count `1`, `NextAttemptAt` populated |
| Payload privacy | Payload does not include signing secret or raw OCR text | Passed |

## QA Result

Passed after remediation.

## Findings

### Major: Docker Could Not Reach Local Host Receiver

Initial callback attempts to `host.docker.internal:8099` failed from the API container with `Network is unreachable`.

Root cause:

- The Compose service did not explicitly map `host.docker.internal` to Docker's host gateway, so the name was not routable in this local engine.

Remediation:

- Added `extra_hosts: ["host.docker.internal:host-gateway"]` to the API service in `deploy/docker-compose.yml`.
- Recreated the API container.
- Re-ran a real PDF analysis with a local receiver.

Status: fixed.

### Major: Migration Required New Target FK On Existing Delivery Rows

The first generated migration made `WebhookDeliveries.ClientWebhookTargetId` non-nullable. That could fail on databases with historical delivery rows.

Root cause:

- EF generated a required FK because the domain property was `Guid`.

Remediation:

- Changed `ClientWebhookTargetId` to nullable.
- Configured `OnDelete(DeleteBehavior.SetNull)`.
- Regenerated the migration.

Status: fixed.

## Residual Risks

- Webhook signing secrets are stored for local MVP delivery. Production must move them to managed secret storage or encrypted-at-rest secret handling before onboarding external clients.
