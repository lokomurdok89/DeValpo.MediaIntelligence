# Local Tokens And Google Secrets Setup

This guide explains how to generate and use the local secrets and tokens needed to run `DeValpo.MediaIntelligence` with Google Document AI in Docker.

Follow the steps in order.

## What You Need

- Windows PowerShell.
- Docker Desktop running.
- Google Cloud CLI installed.
- Access to the Google Cloud project:

```text
primeval-wind-499515-k6
```

- Existing Document AI processor:

```text
ProjectId: primeval-wind-499515-k6
Location: us
ProcessorId: 44b3274ce1e84552
Type: OCR_PROCESSOR
```

## Important Files

These files are safe to commit:

```text
deploy/.env.example
secrets/.gitkeep
docs/local-tokens-and-google-secrets.md
```

These files are local secrets. Never commit them:

```text
deploy/.env
secrets/google-application-default-credentials.json
secrets/google-document-ai.json
```

The repo already ignores these files with `.gitignore`.

## Token And Secret Types

There are two different things. Do not mix them.

### 1. Google ADC Credential

This lets the API call Google Document AI.

Local file:

```text
secrets/google-application-default-credentials.json
```

Docker path:

```text
/run/secrets/google-application-default-credentials.json
```

### 2. Local Admin JWT Token

This lets an operator create API clients in local Docker Development.

You generate it from:

```text
POST http://localhost:8085/api/v1/dev/token
```

This endpoint is only for Development. Do not use it as the customer auth flow.

### 3. Client API Key And JWT Token

This is the real client access flow.

1. An operator creates a client through `POST /api/v1/admin/clients`.
2. The API returns the raw API key once.
3. The platform stores only the API key hash.
4. The client exchanges the API key through `POST /api/v1/auth/token`.
5. The API returns a short-lived JWT.

Use it as:

```text
Authorization: Bearer <accessToken>
```

## Step 1: Confirm Google CLI Works

Run:

```powershell
gcloud --version
```

Then:

```powershell
gcloud config list
```

Expected project:

```text
primeval-wind-499515-k6
```

If the project is different, run:

```powershell
gcloud config set project primeval-wind-499515-k6
```

## Step 2: Confirm Document AI API Is Enabled

Run:

```powershell
gcloud services list --enabled --project primeval-wind-499515-k6 --filter="documentai.googleapis.com"
```

Expected output:

```text
NAME: documentai.googleapis.com
TITLE: Cloud Document AI API
```

If it is missing, enable it:

```powershell
gcloud services enable documentai.googleapis.com --project primeval-wind-499515-k6
```

## Step 3: Confirm The Processor Exists

The installed `gcloud` may not include the `documentai` command group. Use REST with your `gcloud` token.

Run:

```powershell
$googleToken = gcloud auth print-access-token
$headers = @{ Authorization = "Bearer $googleToken" }

Invoke-RestMethod `
  -Method Get `
  -Headers $headers `
  -Uri "https://us-documentai.googleapis.com/v1/projects/primeval-wind-499515-k6/locations/us/processors" |
  ConvertTo-Json -Depth 10
```

Look for this processor:

```text
name: projects/474013945360/locations/us/processors/44b3274ce1e84552
displayName: entel-security-intake-ocr
type: OCR_PROCESSOR
state: ENABLED
```

For this project, use:

```text
GoogleDocumentAi__ProjectId=primeval-wind-499515-k6
GoogleDocumentAi__Location=us
GoogleDocumentAi__ProcessorId=44b3274ce1e84552
```

## Step 4: Create Google ADC Credentials

Service account key creation is blocked in this project by policy:

```text
constraints/iam.disableServiceAccountKeyCreation
```

That is expected. Use Application Default Credentials instead.

Stop Docker first so the local login callback is not blocked:

```powershell
docker compose -f deploy\docker-compose.yml down
```

Start Google ADC login:

```powershell
gcloud auth application-default login --no-launch-browser
```

If `gcloud` looks frozen, wait at least 45 seconds. This local CLI can be slow.

When it prints a Google URL:

1. Copy the URL.
2. Open it in your browser.
3. Sign in.
4. Allow the requested permissions.
5. Google will show a verification code.
6. Copy that code.
7. Paste it back into PowerShell.
8. Press Enter.

Expected final message:

```text
Credentials saved to file: [<application-default-credentials-path>]
```

## Step 5: Set The Quota Project

Run:

```powershell
gcloud auth application-default set-quota-project primeval-wind-499515-k6
```

Expected result:

```text
Quota project "primeval-wind-499515-k6" was added to ADC
```

## Step 6: Validate The ADC File Exists

Run:

```powershell
Test-Path "$env:APPDATA\gcloud\application_default_credentials.json"
```

Expected output:

```text
True
```

## Step 7: Copy ADC Into The Docker Secrets Folder

Run from the repo root:

```powershell
Copy-Item "$env:APPDATA\gcloud\application_default_credentials.json" `
  secrets\google-application-default-credentials.json `
  -Force
```

Validate:

```powershell
Test-Path "secrets\google-application-default-credentials.json"
```

Expected output:

```text
True
```

Do not print this file. Do not paste its contents into chat. Do not commit it.

## Step 8: Create deploy/.env

If `deploy\.env` does not exist:

```powershell
Copy-Item deploy\.env.example deploy\.env
```

Open `deploy\.env` and set:

```text
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8080
ConnectionStrings__MediaIntelligence=Host=postgres;Port=5432;Database=media_intelligence;Username=postgres;Password=postgres
Database__ApplyMigrationsOnStartup=true
Storage__Provider=Local
Storage__LocalRootPath=/app/storage/media-assets

GoogleDocumentAi__Enabled=true
GoogleDocumentAi__ProjectId=primeval-wind-499515-k6
GoogleDocumentAi__Location=us
GoogleDocumentAi__ProcessorId=44b3274ce1e84552
GoogleDocumentAi__CredentialsPath=/run/secrets/google-application-default-credentials.json
GoogleDocumentAi__AllowUserCredentials=true

Jwt__Issuer=DeValpo.MediaIntelligence
Jwt__Audience=DeValpo.MediaIntelligence.Api
Jwt__SigningKey=local-dev-secret-change-me-1234567890
Jwt__RequireHttpsMetadata=false

MediaAnalysis__MaxFileSizeBytes=104857600
Swagger__Enabled=true
POSTGRES_DB=media_intelligence
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
```

The JWT signing key must have at least 32 characters.

## Step 9: Start Docker

Run:

```powershell
docker compose -f deploy\docker-compose.yml up --build -d
```

Check containers:

```powershell
docker compose -f deploy\docker-compose.yml ps
```

Expected:

```text
media-intelligence-api        Up
media-intelligence-postgres   Up (healthy)
```

## Step 10: Check Health

Run:

```powershell
curl.exe http://localhost:8085/health
```

Expected:

```json
{"status":"Healthy"}
```

The exact response can include extra health details.

## Step 11: Generate A Local Admin Token

Run:

```powershell
$adminTokenResponse = Invoke-RestMethod `
  -Method Post `
  -Uri http://localhost:8085/api/v1/dev/token `
  -ContentType "application/json" `
  -Body (@{
    subject = "local-admin"
    clientKey = "admin"
    scope = "admin media-intelligence:admin"
  } | ConvertTo-Json -Compress)

$adminToken = $adminTokenResponse.accessToken
```

Validate:

```powershell
$adminToken.Length
```

Expected: a number greater than `100`.

## Step 12: Create A Client API Key And JWT

Create a client:

```powershell
$clientKey = "local-client-" + (Get-Date -Format "yyyyMMddHHmmss")

$clientResponse = Invoke-RestMethod `
  -Method Post `
  -Uri http://localhost:8085/api/v1/admin/clients `
  -ContentType "application/json" `
  -Headers @{ Authorization = "Bearer $adminToken" } `
  -Body (@{
    clientKey = $clientKey
    displayName = "Local Client"
    contactEmail = "ops@example.com"
    allowedProfileKeys = @("generic-document-ocr-v1")
    keyName = "primary"
    scope = "media-intelligence:client"
    expiresAt = $null
  } | ConvertTo-Json -Compress)
```

Important:

- `$clientResponse.apiKey` is shown once.
- Store it in a secure client-side secret store.
- The database stores only a hash, not this raw value.

Exchange the API key for a client JWT:

```powershell
$tokenResponse = Invoke-RestMethod `
  -Method Post `
  -Uri http://localhost:8085/api/v1/auth/token `
  -ContentType "application/json" `
  -Body (@{
    clientKey = $clientKey
    apiKey = $clientResponse.apiKey
  } | ConvertTo-Json -Compress)

$token = $tokenResponse.accessToken
```

Validate:

```powershell
$token.Length
```

Expected: a number greater than `100`.

## Step 13: Confirm Protected Endpoints Require JWT

Without token:

```powershell
curl.exe -s -o NUL -w "%{http_code}" http://localhost:8085/api/v1/capabilities
```

Expected:

```text
401
```

With token:

```powershell
curl.exe -s -o NUL -w "%{http_code}" `
  http://localhost:8085/api/v1/capabilities `
  -H "Authorization: Bearer $token"
```

Expected:

```text
200
```

## Step 14: Validate Google Document AI Local Status

Run:

```powershell
curl.exe http://localhost:8085/api/v1/providers/google-document-ai/status
```

Expected:

```json
{
  "enabled": true,
  "configured": true,
  "credentialFilePresent": true,
  "projectId": "primeval-wind-499515-k6",
  "location": "us",
  "processorId": "44b3274ce1e84552",
  "missingFields": []
}
```

## Step 15: Validate Google Document AI Remote Status

Run:

```powershell
curl.exe "http://localhost:8085/api/v1/providers/google-document-ai/status?checkRemote=true" `
  -H "Authorization: Bearer $adminToken"
```

Expected:

```json
{
  "enabled": true,
  "configured": true,
  "credentialFilePresent": true,
  "projectId": "primeval-wind-499515-k6",
  "location": "us",
  "processorId": "44b3274ce1e84552",
  "missingFields": [],
  "message": "Google Document AI processor is reachable."
}
```

## Step 16: Analyze A Real PDF

Example:

```powershell
$pdfPath = "<path-to-your-document.pdf>"

$analyzeJson = curl.exe -s -X POST http://localhost:8085/api/v1/analyze `
  -H "Authorization: Bearer $token" `
  -F "clientKey=$clientKey" `
  -F "profileKey=generic-document-ocr-v1" `
  -F "assetType=Document" `
  -F "file=@$pdfPath"

$queuedJob = $analyzeJson | ConvertFrom-Json
$jobId = $queuedJob.jobId
```

Expected:

```json
{
  "jobId": "...",
  "status": "Pending",
  "processingAttempts": 0,
  "createdAt": "..."
}
```

Poll until the job reaches `Completed` or `RequiresReview`:

```powershell
curl.exe "http://localhost:8085/api/v1/analysis-jobs/$jobId" `
  -H "Authorization: Bearer $token"
```

Then read the result:

```powershell
curl.exe "http://localhost:8085/api/v1/analysis-jobs/$jobId/result" `
  -H "Authorization: Bearer $token"
```

Expected result shape:

```json
{
  "jobId": "...",
  "confidence": 0.89,
  "requiresHumanReview": false,
  "extractedFields": [
    {
      "name": "provider",
      "value": "google-document-ai"
    },
    {
      "name": "processor_id",
      "value": "44b3274ce1e84552"
    },
    {
      "name": "text",
      "value": "..."
    }
  ],
  "issues": []
}
```

## Troubleshooting

### gcloud login looks frozen

Wait 45 seconds.

This CLI has been observed taking around 40 seconds to print output.

### Google shows a verification code

That is expected with:

```powershell
gcloud auth application-default login --no-launch-browser
```

Copy the code from the browser and paste it into PowerShell.

### Key creation is blocked

If this command fails:

```powershell
gcloud iam service-accounts keys create ...
```

with:

```text
constraints/iam.disableServiceAccountKeyCreation
```

do not fight it. Use ADC instead. This guide already uses ADC.

### credentialFilePresent is false

Check that `deploy\.env` has:

```text
GoogleDocumentAi__CredentialsPath=/run/secrets/google-application-default-credentials.json
GoogleDocumentAi__AllowUserCredentials=true
```

Then check the local file exists:

```powershell
Test-Path "secrets\google-application-default-credentials.json"
```

### Google credentials are invalid

Regenerate ADC:

```powershell
gcloud auth application-default login --no-launch-browser
gcloud auth application-default set-quota-project primeval-wind-499515-k6
Copy-Item "$env:APPDATA\gcloud\application_default_credentials.json" `
  secrets\google-application-default-credentials.json `
  -Force
docker compose -f deploy\docker-compose.yml up --build -d
```

### Google processor is not found

Confirm these values:

```text
GoogleDocumentAi__ProjectId=primeval-wind-499515-k6
GoogleDocumentAi__Location=us
GoogleDocumentAi__ProcessorId=44b3274ce1e84552
```

### curl cannot open the PDF

This means the local file path is wrong.

Validate first:

```powershell
Test-Path $pdfPath
```

Expected:

```text
True
```

Then run the `curl` command again.

## Final Checklist

Before saying the local MVP base works, all checks must pass:

```text
[ ] deploy/.env exists
[ ] secrets/google-application-default-credentials.json exists
[ ] docker compose ps shows API up and PostgreSQL healthy
[ ] /health returns 200
[ ] /api/v1/capabilities returns 401 without token
[ ] /api/v1/dev/token returns local admin accessToken in Development
[ ] /api/v1/admin/clients returns one-time apiKey with admin token
[ ] /api/v1/auth/token exchanges apiKey for client accessToken
[ ] /api/v1/capabilities returns 200 with client token
[ ] /api/v1/providers/google-document-ai/status?checkRemote=true says processor is reachable with admin token
[ ] /api/v1/analyze returns 202 and jobId from a real PDF
[ ] /api/v1/analysis-jobs/{jobId} reaches Completed or RequiresReview
[ ] /api/v1/analysis-jobs/{jobId}/result returns extracted text from a real PDF
```
