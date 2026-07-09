# Google Document AI Runbook

For production credential rules, service account handling and key rotation, see [Production cloud credentials](../reference/production-cloud-credentials.md).

## Symptoms

- `/ready` reports `google-document-ai` as `Unhealthy`.
- `GET /api/v1/providers/google-document-ai/status?checkRemote=true` returns `configured=false`.
- Analysis jobs fail or require review with provider-related issues.

## First Checks

```bash
curl http://localhost:8085/ready
curl "http://localhost:8085/api/v1/providers/google-document-ai/status?checkRemote=true" \
  -H "Authorization: Bearer <operatorOrAdminAccessToken>"
```

Check Docker configuration:

```bash
docker compose -f deploy/docker-compose.yml config
docker compose -f deploy/docker-compose.yml ps
```

## Common Causes

- `GoogleDocumentAi__Enabled` is false when readiness requires Google.
- Missing `ProjectId`, `Location` or `ProcessorId`.
- Credential file is not mounted under `/run/secrets`.
- ADC user credential is mounted but `GoogleDocumentAi__AllowUserCredentials` is false.
- Service account or ADC identity lacks Document AI processor permissions.
- Processor id, location or project id points to the wrong processor.
- API/billing/quota is unavailable in Google Cloud.

## Remediation

1. Confirm the processor exists in the configured project/location.
2. Confirm `documentai.googleapis.com` is enabled.
3. Confirm Docker can see the mounted credential file.
4. Re-run `/ready` after config changes.
5. Review logs using the `CorrelationId` returned in failed API responses.

## Safety

- Do not paste or commit credential JSON.
- Do not log raw OCR text or provider payloads.
- Rotate credentials if a secret was exposed outside the local machine.
