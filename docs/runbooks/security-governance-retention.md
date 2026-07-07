# Security, Governance And Retention Runbook

This runbook covers the local product controls added in Phase 9: audit trail, per-client retention, PII/log handling and the preparation path for managed secrets.

## Access Model

Functional API endpoints require a Bearer token. Administrative governance endpoints require `media-intelligence:admin` scope:

- `GET /api/v1/admin/audit-events`
- `GET /api/v1/admin/clients/{clientKey}/retention-policy`
- `PUT /api/v1/admin/clients/{clientKey}/retention-policy`
- `POST /api/v1/admin/clients/{clientKey}/retention-policy/execute`

Client tokens must receive `403 Forbidden` on these endpoints. Anonymous requests must receive `401 Unauthorized`.

## Audit Events

The platform records sanitized audit events for critical actions:

- API token issued or rejected.
- API client created.
- API key rotated or revoked.
- Analysis result read.
- Human review started or completed.
- Webhook target created.
- Retention policy updated.
- Retention execution.

Audit metadata must contain identifiers and counters only. It must not contain raw API keys, webhook signing secrets, OCR text, document content or Google credential values.

Read recent events:

```powershell
curl.exe "http://localhost:8085/api/v1/admin/audit-events?limit=50" `
  -H "Authorization: Bearer <adminAccessToken>"
```

Filter by client:

```powershell
curl.exe "http://localhost:8085/api/v1/admin/audit-events?clientKey=<clientKey>&limit=50" `
  -H "Authorization: Bearer <adminAccessToken>"
```

## Retention Policy

Retention is configured per client and removes stored media assets for completed, failed or cancelled jobs older than the configured retention period. Jobs requiring human review are not eligible until they reach a terminal state.

Create or update a policy:

```powershell
curl.exe -X PUT "http://localhost:8085/api/v1/admin/clients/<clientKey>/retention-policy" `
  -H "Authorization: Bearer <adminAccessToken>" `
  -H "Content-Type: application/json" `
  -d "{\"enabled\":true,\"retentionDays\":30}"
```

Read the policy:

```powershell
curl.exe "http://localhost:8085/api/v1/admin/clients/<clientKey>/retention-policy" `
  -H "Authorization: Bearer <adminAccessToken>"
```

Execute retention:

```powershell
curl.exe -X POST "http://localhost:8085/api/v1/admin/clients/<clientKey>/retention-policy/execute" `
  -H "Authorization: Bearer <adminAccessToken>"
```

The execution response returns scanned and deleted asset counters. The database row is marked with `DeletedAt` so the deletion remains auditable.

## PII And Logs

Logs and audit events must be treated as operational metadata, not document storage.

Allowed:

- Correlation id.
- Job id.
- Client key.
- Profile key.
- Provider name.
- Status and counters.
- Sanitized error category.

Not allowed:

- Raw OCR text.
- Document body, image bytes or provider raw output.
- API keys.
- JWT signing keys.
- Webhook signing secrets.
- Google credential JSON or access tokens.
- Personal identifiers extracted from customer documents.

When adding new logs, prefer structured identifiers and counts. If a value came from a document, do not log it.

## Secret Manager Preparation

Local Docker still uses environment variables and mounted files. Production must move these values to a managed secret source:

- `Jwt__SigningKey`
- API key hashes remain in the database, but raw API keys are shown only once at creation.
- Webhook signing secrets.
- Google Document AI credentials or workload identity settings.
- Database password.

Before production:

1. Replace local `.env` values with secret-manager references in deployment configuration.
2. Disable service-account JSON files where workload identity is available.
3. Restrict secret read access to the API runtime identity only.
4. Enable secret rotation procedures for JWT signing keys and webhook signing secrets.
5. Keep Swagger and logs free from secret examples.

