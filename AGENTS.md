# MediaIntelligence Agent Guide

Codex adapter. Keep this file short and use
`docs/context/AI_ONBOARDING.md` as the main source of truth.

## Read First

1. `docs/context/AI_ONBOARDING.md`
2. `docs/context/PROJECT_IDENTITY.md`
3. `docs/context/ACTIVE_DECISIONS.md`
4. `docs/context/CURRENT_STATE.md`
5. `docs/context/codex-minimal.md`
6. `docs/context/active-decisions.md`
7. `docs/context/architecture-current.md`
8. Only the module or reference docs needed for the task.

## Do Not Read By Default

- `secrets/`
- `site/`
- `bin/`
- `obj/`
- `TestResults/`
- `docs/archive/`
- `docs/reference/` unless the task needs a specific long reference.

## Architecture Rules

- Keep the layer boundaries: `Api`, `Application`, `Domain`, `Contracts`, `Infrastructure`.
- API owns HTTP/auth/audit entry points and request shape.
- Application owns use cases, orchestration, provider-neutral validation and interfaces.
- Domain owns entities and normalized job state.
- Contracts owns public DTOs and must not expose provider SDK or EF models.
- Infrastructure owns EF Core, repositories, storage, provider SDKs, webhooks and workers.
- User-facing application text belongs in resource files.

## Security Rules

- Do not read, print or edit credentials or files under `secrets/`.
- Do not hardcode credentials, provider values, bucket names, collection IDs or local paths.
- Productive inputs use cloud references; multipart stays compatible for development and smoke tests.
- Never accept public URLs or signed URLs as productive input.
- `targetKey` is the canonical technical contract name.
- `consentReferenceId` is optional and only an external reference.
- Do not delete staged cloud files after jobs; S3/GCS lifecycle owns staging cleanup.
- Delete temporary biometric `FaceId` values after processing.

## Validation Commands

Use the smallest relevant set:

```powershell
dotnet build DeValpo.MediaIntelligence.sln
dotnet test DeValpo.MediaIntelligence.sln --no-build
dotnet ef migrations has-pending-model-changes --project src\DeValpo.MediaIntelligence.Infrastructure --startup-project src\DeValpo.MediaIntelligence.Api
python -m mkdocs build --strict
powershell -ExecutionPolicy Bypass -File scripts\ci\secret-scan.ps1 -Root .
powershell -ExecutionPolicy Bypass -File scripts\ci\validate-release-config.ps1
docker compose -f deploy\docker-compose.yml config --quiet
```

## Workflow

- One conversation per feature or PR.
- Start with discovery, then write/update a spec, implement, test, prepare PR notes and update `docs/context/CURRENT_STATE.md`.
- Do not commit unless the user explicitly asks.
