# Getting Started

For the offline demo, follow the repository README. It needs
neither cloud credentials nor the API. This page is for a new backend installation;
provider checks and job submissions require separate authorization and may incur costs.

## Requirements

- Docker Desktop / Compose for the API and PostgreSQL 17.
- .NET SDK 10.x only for building or running outside Docker.
- A configured provider only for the workflow you intend to use; Google is not a prerequisite for AWS facial search.
- An external JWT signing secret, never committed.

## 1. Configure The Selected Workflow

Copy the example only if the local file does not already exist:

```powershell
if (-not (Test-Path deploy/.env)) { Copy-Item deploy/.env.example deploy/.env }
```

Set `Jwt__SigningKey` to a private value of at least 32 characters. Keep the
existing local Development issuer/audience configuration consistent with the API.
`Jwt__RequireHttpsMetadata=false` is for local development, not a production recommendation.
`Database__ApplyMigrationsOnStartup=true` applies schema changes at local startup;
for production use the [migration runbook](runbooks/release-and-migrations.md).

| Workflow | Canonical setup |
| --- | --- |
| Facial search in S3 video | [AWS configuration and permissions](aws-rekognition-face-search.en.md) |
| Video numbers / generic faces | [Google Video Intelligence](video-target-search.en.md) |
| Document OCR | [Document AI runbook](runbooks/google-document-ai.md) and [ADC setup](reference/local-tokens-and-google-secrets.md) |
| Production identities | [Managed roles and external secrets](reference/production-cloud-credentials.md) |

Configure only the providers needed. Keep credential files out of version control;
local Compose mounts `secrets/` read-only at `/run/secrets`. A mount is not permission
to print credentials or publish the directory.

## 2. Start Locally

```powershell
docker compose -f deploy/docker-compose.yml config --quiet
docker compose -f deploy/docker-compose.yml up --build
```

API: `http://localhost:8085`; Development Swagger: `/swagger`;
PostgreSQL host port: `5435`. `/health` checks process liveness; `/ready`
checks configured dependencies. Do not paste full Compose config or logs containing secrets.

## 3. Onboard A Client And Obtain A Token

Only in local `Development`, obtain the admin token needed for client setup:

```powershell
# Development only; never use this helper in a production environment.
$baseUrl = "http://localhost:8085"
$body = @{ subject = "local-admin"; clientKey = "admin"; scope = "media-intelligence:admin" } | ConvertTo-Json
$adminToken = (Invoke-RestMethod -Method Post -Uri "$baseUrl/api/v1/dev/token" -ContentType "application/json" -Body $body).accessToken
```

Then follow [client onboarding](client-onboarding-guide.md): create a client with
only the intended profile, securely retain its one-time API key and exchange it
at `POST /api/v1/auth/token`. Use that client JWT for submissions and result reads,
not the admin token. Production needs an authorized admin identity; the development
helper is unavailable there.

The catalog endpoints `/api/v1/capabilities`, `/api/v1/profiles` and `/api/v1/skills`
accept a bearer token. Remote provider checks require operator/admin scope;
a default development token is not a substitute for those permissions.

## 4. Submit And Read Results When Authorized

[API contracts](api.en.md) link the workflows. Source media for productive video/
image jobs uses cloud selectors; multipart is retained for development.
Poll the corresponding job route, read `/result` and inspect `issues` as well as
status. Document jobs can require human review; see the API reference for states,
review endpoints and webhook headers.

[Operations and diagnosis](operations.en.md) covers logs and failures.
This guide does not require repeating historical cloud jobs to show the demo.
