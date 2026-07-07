# Primeros Pasos

Esta es la ruta corta para levantar el producto localmente y probar con un documento real.

## Requisitos

- .NET SDK 10.0.x.
- Docker Desktop.
- Google Cloud CLI.
- Acceso al proyecto y processor de Google Document AI.

## 1. Preparar Variables Locales

```powershell
Copy-Item deploy\.env.example deploy\.env
```

Edita `deploy/.env` y configura:

```text
GoogleDocumentAi__Enabled=true
GoogleDocumentAi__ProjectId=<project-id>
GoogleDocumentAi__Location=<location>
GoogleDocumentAi__ProcessorId=<processor-id>
GoogleDocumentAi__CredentialsPath=/run/secrets/google-application-default-credentials.json
GoogleDocumentAi__AllowUserCredentials=true
Jwt__SigningKey=<local-dev-secret-min-32-chars>
```

## 2. Preparar Google ADC

```powershell
gcloud auth application-default login
gcloud auth application-default set-quota-project <project-id>
Copy-Item "$env:APPDATA\gcloud\application_default_credentials.json" secrets\google-application-default-credentials.json
```

Mas detalle: [tokens y secretos Google](local-tokens-and-google-secrets.md).

## 3. Levantar Docker

```powershell
docker compose -f deploy\docker-compose.yml config
docker compose -f deploy\docker-compose.yml up --build
```

Servicios esperados:

- API: `http://localhost:8085`.
- Swagger: `http://localhost:8085/swagger`.
- PostgreSQL host port: `5435`.

## 4. Probar Salud

```powershell
curl.exe http://localhost:8085/health
curl.exe http://localhost:8085/ready
```

## 5. Generar Token Local

```powershell
$tokenJson = curl.exe -s -X POST http://localhost:8085/api/v1/dev/token `
  -H "Content-Type: application/json" `
  -d "{}"

$token = ($tokenJson | ConvertFrom-Json).accessToken
```

## 6. Validar Google

```powershell
curl.exe "http://localhost:8085/api/v1/providers/google-document-ai/status?checkRemote=true" `
  -H "Authorization: Bearer $token"
```

## 7. Subir Documento Real

```powershell
curl.exe -X POST http://localhost:8085/api/v1/analyze `
  -H "Authorization: Bearer $token" `
  -F "clientKey=generic" `
  -F "profileKey=generic-document-ocr-v1" `
  -F "assetType=Document" `
  -F "file=@<ruta-local-al-documento.pdf>"
```

Lee el `jobId` y consulta el resultado:

```powershell
curl.exe "http://localhost:8085/api/v1/analysis-jobs/<jobId>/result" `
  -H "Authorization: Bearer $token"
```
