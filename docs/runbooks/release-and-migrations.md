# Release And Migration Runbook

This runbook covers the release path for staging and production.

Cloud runtime identities, key rotation and minimum AWS/GCP permissions are covered in [Production cloud credentials](../reference/production-cloud-credentials.md).

## Release Safety Rules

`Staging` and `Production` must start only when these rules are true:

- `Jwt__SigningKey` is supplied by the deployment secret store and is not a local, dummy, test or change-me value.
- `Jwt__RequireHttpsMetadata=true`.
- `Swagger__Enabled=false`.
- `Database__ApplyMigrationsOnStartup=false`.
- `GoogleDocumentAi__AllowUserCredentials=false`.
- `Skills__EnableMockSkills=false`.
- `AnalysisProfiles__EnableMockProfiles=false`.

The API validates these rules at startup and fails before serving traffic when a release environment is unsafe.

## Staging Configuration

Use the same image that will be promoted to production. Supply environment values through the deployment platform or secret manager, not repository files:

```text
ASPNETCORE_ENVIRONMENT=Staging
ConnectionStrings__MediaIntelligence=<staging-postgres-connection-string>
Jwt__Issuer=DeValpo.MediaIntelligence
Jwt__Audience=DeValpo.MediaIntelligence.Api
Jwt__SigningKey=<secret-manager-value-min-32-bytes>
Jwt__RequireHttpsMetadata=true
Swagger__Enabled=false
Database__ApplyMigrationsOnStartup=false
GoogleDocumentAi__Enabled=true
GoogleDocumentAi__ProjectId=<project-id>
GoogleDocumentAi__Location=<location>
GoogleDocumentAi__ProcessorId=<processor-id>
GoogleDocumentAi__CredentialsPath=<managed-credential-path-if-needed>
GoogleDocumentAi__AllowUserCredentials=false
```

The repository includes `deploy/.env.staging.example` as a safe template. Do not copy real secrets into the repository.

## Migration Process

Automatic migration on application startup is allowed only for local Docker development. For staging and production:

1. Build and test the release candidate.
2. Review generated migrations in `src/DeValpo.MediaIntelligence.Infrastructure/Persistence/Migrations`.
3. Validate model drift:

```powershell
dotnet tool restore --tool-manifest dotnet-tools.json
dotnet ef migrations has-pending-model-changes `
  --project src\DeValpo.MediaIntelligence.Infrastructure `
  --startup-project src\DeValpo.MediaIntelligence.Api
```

4. Generate the SQL script for review:

```powershell
dotnet ef migrations script `
  --project src\DeValpo.MediaIntelligence.Infrastructure `
  --startup-project src\DeValpo.MediaIntelligence.Api `
  --idempotent `
  --output artifacts\migration.sql
```

5. Apply the reviewed SQL through the database release channel.
6. Deploy the application image after the database migration is complete.
7. Validate `/health`, `/ready`, one authenticated token exchange and one real document analysis in staging.

## Rollback

Rollback is operational, not automatic:

- Keep the previous application image tag.
- Keep a backup or point-in-time recovery window for PostgreSQL before applying migration SQL.
- If the application deployment fails before migration use, roll back the image.
- If the migration has changed data shape, use the reviewed rollback notes for that migration or restore from backup according to the database runbook.

## CI Gates

The release branch must pass:

- Restore, build and tests.
- EF migration drift validation.
- Docker image build.
- Docker Compose config validation.
- Secret scan.
- Release configuration validation.
- Container vulnerability scan in report mode.
