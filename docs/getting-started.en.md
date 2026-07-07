# Getting Started

This is the short path to run the product locally and test it with a real document.

## Requirements

- .NET SDK 10.0.x.
- Docker Desktop.
- Google Cloud CLI.
- Access to the Google Document AI project and processor.

## 1. Prepare Local Variables

```powershell
Copy-Item deploy\.env.example deploy\.env
```

Edit `deploy/.env` and configure:

```text
GoogleDocumentAi__Enabled=true
GoogleDocumentAi__ProjectId=<project-id>
GoogleDocumentAi__Location=<location>
GoogleDocumentAi__ProcessorId=<processor-id>
GoogleDocumentAi__CredentialsPath=/run/secrets/google-application-default-credentials.json
GoogleDocumentAi__AllowUserCredentials=true
Jwt__SigningKey=<local-dev-secret-min-32-chars>
```

## 2. Prepare Google ADC

```powershell
gcloud auth application-default login
gcloud auth application-default set-quota-project <project-id>
Copy-Item "$env:APPDATA\gcloud\application_default_credentials.json" secrets\google-application-default-credentials.json
```

More detail: [local tokens and Google secrets](local-tokens-and-google-secrets.md).

## 3. Start Docker

```powershell
docker compose -f deploy\docker-compose.yml config
docker compose -f deploy\docker-compose.yml up --build
```

Expected services:

- API: `http://localhost:8085`.
- Swagger: `http://localhost:8085/swagger`.
- PostgreSQL host port: `5435`.

## 4. Check Health

```powershell
curl.exe http://localhost:8085/health
curl.exe http://localhost:8085/ready
```

## 5. Generate Local Token

```powershell
$tokenJson = curl.exe -s -X POST http://localhost:8085/api/v1/dev/token `
  -H "Content-Type: application/json" `
  -d "{}"

$token = ($tokenJson | ConvertFrom-Json).accessToken
```

## 6. Validate Google

```powershell
curl.exe "http://localhost:8085/api/v1/providers/google-document-ai/status?checkRemote=true" `
  -H "Authorization: Bearer $token"
```

## 7. Upload A Real Document

```powershell
curl.exe -X POST http://localhost:8085/api/v1/analyze `
  -H "Authorization: Bearer $token" `
  -F "clientKey=generic" `
  -F "profileKey=generic-document-ocr-v1" `
  -F "assetType=Document" `
  -F "file=@<local-path-to-document.pdf>"
```

Read the `jobId` and query the result:

```powershell
curl.exe "http://localhost:8085/api/v1/analysis-jobs/<jobId>/result" `
  -H "Authorization: Bearer $token"
```
