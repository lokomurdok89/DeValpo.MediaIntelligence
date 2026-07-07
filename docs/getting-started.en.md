# Getting Started

This is the short path to run the product locally and test it with a real document.

## Requirements

- .NET SDK 10.0.x.
- Docker Desktop.
- Google Cloud CLI.
- Access to the Google Document AI project and processor.
- AWS CLI only if you will validate real Rekognition.

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

AWS is disabled by default. To test real Rekognition, also configure:

```text
AwsRekognition__Enabled=true
AwsRekognition__FaceSearchEnabled=true
AwsRekognition__Region=<aws-region>
AwsRekognition__BucketName=<private-s3-bucket>
AwsRekognition__CollectionId=<rekognition-collection-id>
AwsRekognition__CredentialsPath=/run/secrets/aws-credentials
AwsRekognition__ProfileName=default
AwsRekognition__AllowUserCredentials=true
AwsRekognition__UsePolling=true
```

## 2. Prepare Google ADC

```powershell
gcloud auth application-default login
gcloud auth application-default set-quota-project <project-id>
Copy-Item "$env:APPDATA\gcloud\application_default_credentials.json" secrets\google-application-default-credentials.json
```

More detail: [local tokens and Google secrets](local-tokens-and-google-secrets.md).

That document explains the full path from installing Google Cloud CLI to obtaining the ADC credential this API uses to call Document AI.

## 2.1. Prepare Optional AWS Credentials

Only if you will test real face search:

```powershell
aws configure --profile <profile-name>
Copy-Item "$env:USERPROFILE\.aws\credentials" secrets\aws-credentials
```

Docker mounts that file at `/run/secrets/aws-credentials`. Do not paste access keys into `deploy/.env` or commit them.

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

## 6.1. Validate Optional AWS

```powershell
curl.exe "http://localhost:8085/api/v1/providers/aws-rekognition/status?checkRemote=true" `
  -H "Authorization: Bearer $token"
```

Expected result when AWS is configured correctly: `AWS Rekognition S3 bucket and collection are reachable.`

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
