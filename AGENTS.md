# MediaIntelligence Agent Guide

## Read first

- `README.md`
- `docs/context/codex-minimal.md`
- `docs/context/active-decisions.md`
- `docs/context/architecture-current.md`
- `docs/api.md`

Then read only the module document that matches the task under `docs/modules/`.

## Do not read by default

- `secrets/`
- `site/`
- `bin/`
- `obj/`
- `TestResults/`
- `docs/archive/`
- Long files under `docs/reference/` unless the task needs that specific evidence.
- Provider credential guides unless the task is about setup, production readiness or smoke tests.

## Architecture rules

- Keep the layer boundaries: `Api`, `Application`, `Domain`, `Contracts`, `Infrastructure`.
- API controllers handle auth, request shape and audit entry points.
- Application services orchestrate use cases and own business validation.
- Infrastructure owns provider SDKs, EF Core repositories, storage and webhooks.
- Domain entities must not depend on provider SDK models.
- Contracts must not expose AWS, Google or EF models.
- User-facing text belongs in resource files when it is part of the application surface.

## Security rules

- Do not hardcode credentials, bucket names, collection IDs, paths or secrets.
- Do not read or print files from `secrets/`.
- Do not accept public URLs or signed URLs as productive input.
- Use `targetKey` as the canonical opaque target identifier.
- `consentReferenceId` is optional and is only an external reference.
- Jobs must not delete staged cloud files at completion; bucket lifecycle owns staging cleanup.
- Temporary biometric `FaceId` values must still be deleted after processing.

## Validation commands

Use the smallest relevant set while developing:

```powershell
dotnet build DeValpo.MediaIntelligence.sln
dotnet test DeValpo.MediaIntelligence.sln --no-build
dotnet ef migrations has-pending-model-changes --project src\DeValpo.MediaIntelligence.Infrastructure --startup-project src\DeValpo.MediaIntelligence.Api
python -m mkdocs build --strict
powershell -ExecutionPolicy Bypass -File scripts\ci\secret-scan.ps1
powershell -ExecutionPolicy Bypass -File scripts\ci\validate-release-config.ps1
docker compose -f deploy\docker-compose.yml config --quiet
```
