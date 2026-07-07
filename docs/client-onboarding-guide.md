# Client Onboarding Guide

This guide describes the first controlled client onboarding flow for DeValpo MediaIntelligence.

## Prerequisites

The operator must provide:

- Client key, for example `first-client`.
- Display name and contact email.
- Allowed profile keys. For the first release, use `generic-document-ocr-v1` unless compatibility requires `invoice-extraction-v1`.
- Webhook URL and signing secret if the client wants callbacks.
- Retention period in days.

The platform must already have:

- PostgreSQL reachable.
- Google Document AI enabled and ready.
- JWT signing key supplied through deployment secrets.
- Swagger disabled or protected in release environments.
- Migrations applied through the release runbook.

## Create Client

Use an admin-scoped token:

```powershell
curl.exe -X POST "https://<host>/api/v1/admin/clients" `
  -H "Authorization: Bearer <adminAccessToken>" `
  -H "Content-Type: application/json" `
  -d "{\"clientKey\":\"<clientKey>\",\"displayName\":\"<displayName>\",\"contactEmail\":\"<email>\",\"allowedProfileKeys\":[\"generic-document-ocr-v1\"],\"keyName\":\"initial\",\"scope\":\"media-intelligence:client\",\"expiresAt\":null}"
```

The response returns the raw API key once. Store it in the client's secret manager. The platform stores only the hash.

## Exchange API Key For JWT

```powershell
curl.exe -X POST "https://<host>/api/v1/auth/token" `
  -H "Content-Type: application/json" `
  -d "{\"clientKey\":\"<clientKey>\",\"apiKey\":\"<rawApiKey>\"}"
```

Use the returned `accessToken` as `Authorization: Bearer <token>`.

## Submit A Document

```powershell
curl.exe -X POST "https://<host>/api/v1/analyze" `
  -H "Authorization: Bearer <clientAccessToken>" `
  -F "clientKey=<clientKey>" `
  -F "profileKey=generic-document-ocr-v1" `
  -F "assetType=Document" `
  -F "file=@C:\path\document.pdf"
```

The API returns a job id. Poll job status:

```powershell
curl.exe "https://<host>/api/v1/analysis-jobs/<jobId>" `
  -H "Authorization: Bearer <clientAccessToken>"
```

Read the result:

```powershell
curl.exe "https://<host>/api/v1/analysis-jobs/<jobId>/result" `
  -H "Authorization: Bearer <clientAccessToken>"
```

## Configure Webhook

```powershell
curl.exe -X POST "https://<host>/api/v1/admin/clients/<clientKey>/webhook-targets" `
  -H "Authorization: Bearer <adminAccessToken>" `
  -H "Content-Type: application/json" `
  -d "{\"targetKey\":\"primary\",\"url\":\"https://client.example/webhooks/media-intelligence\",\"signingSecret\":\"<clientWebhookSecret>\",\"isActive\":true}"
```

The webhook payload excludes OCR text, raw provider output and secrets.

## Configure Retention

```powershell
curl.exe -X PUT "https://<host>/api/v1/admin/clients/<clientKey>/retention-policy" `
  -H "Authorization: Bearer <adminAccessToken>" `
  -H "Content-Type: application/json" `
  -d "{\"enabled\":true,\"retentionDays\":30}"
```

Retention should be executed by an operator according to the client's data policy.

## Operational Smoke Test

For each onboarded client:

- API key can be exchanged for JWT.
- Client can submit one real PDF with its allowed profile.
- Client can read only its own job and result.
- `/ready` is healthy.
- Audit events do not include raw API keys or OCR text.
- Webhook signature can be verified if webhooks are enabled.

