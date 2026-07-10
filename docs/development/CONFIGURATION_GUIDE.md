# Configuration Guide

## Configuration Sources

ASP.NET Core loads `appsettings.json`, environment-specific appsettings and
environment variables. Docker Compose reads `deploy/.env.example` and may load
an untracked `deploy/.env` for a local developer override.

`deploy/.env.example` is intentionally non-sensitive: it is a safe local
baseline, not a ready-to-run cloud configuration. Copy its keys into a local,
ignored override and supply actual values only through approved secret handling.

## Key Groups

| Group | Purpose |
| --- | --- |
| `ConnectionStrings__MediaIntelligence`, `Database__*` | PostgreSQL connectivity and migration behavior. |
| `Storage__*` | Local media storage provider and root. |
| `Jwt__*` | Issuer, audience, signing key and token policy. |
| `GoogleDocumentAi__*`, `GoogleVideoIntelligence__*`, `GoogleVision__*` | Google provider enablement and credential source. |
| `AwsRekognition__*` | AWS provider enablement, feature gating and provider options. |
| `CloudAssets__*` | S3/GCS allow-lists, staging limits and concurrency limits. |
| `Skills__*`, `AnalysisProfiles__*` | Mock registration and profile behavior. |
| `AnalysisWorker__*`, `WebhookDelivery__*`, `Readiness__*` | Worker polling, webhook retries and readiness checks. |

## Local, Mock And Real Providers

The base appsettings keep cloud providers and mock profiles disabled. Enable a
provider only with the required non-secret identifiers, permissions and a
credential source mounted outside the repository. Mocks are for development and
tests; staging and production reject mock skills/profiles at startup.

## Secret Rules

Never commit or print credential JSON, connection strings, raw API keys, JWT
signing keys, webhook signing secrets, provider identifiers tied to production,
or bucket/collection identifiers. `secrets/` is mounted read-only for local
containers and is not documentation input. Use placeholders such as
`<managed-secret>` and `<provider-identifier>` in examples.

## Release Guardrails

`appsettings.Staging.json` and `appsettings.Production.json` disable automatic
migrations, Swagger, user credentials and mocks, and require HTTPS metadata.
Validate those invariants with `scripts/ci/validate-release-config.ps1`.
