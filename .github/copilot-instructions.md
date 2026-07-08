# GitHub Copilot Instructions

Use `docs/context/AI_ONBOARDING.md` as the first source of truth. Prefer small,
layer-aligned changes and avoid broad refactors.

## Read First

- `docs/context/AI_ONBOARDING.md`
- `docs/context/PROJECT_IDENTITY.md`
- `docs/context/ACTIVE_DECISIONS.md`
- `docs/context/CURRENT_STATE.md`
- The specific module doc for the file being edited.

## Architecture

- `src/DeValpo.MediaIntelligence.Api`: controllers, auth, middleware, health and hosted workers.
- `src/DeValpo.MediaIntelligence.Application`: use cases, orchestration, provider-neutral interfaces.
- `src/DeValpo.MediaIntelligence.Domain`: normalized entities and job state.
- `src/DeValpo.MediaIntelligence.Contracts`: public DTOs and API contracts.
- `src/DeValpo.MediaIntelligence.Infrastructure`: EF Core, repositories, storage, provider SDKs and webhooks.
- `tests/DeValpo.MediaIntelligence.Tests`: unit/API tests.

## Conventions

- Keep provider SDK models out of `Domain` and `Contracts`.
- Keep public contract names stable; `targetKey` is canonical.
- Keep multipart endpoints compatible while production uses cloud references.
- Use resource files for application-facing text when applicable.
- Prefer focused tests around changed behavior.

## Security

- Do not read, suggest or print files from `secrets/`.
- Do not inspect generated `site/`.
- Do not hardcode credentials, bucket names, collection IDs, provider values or local paths.
- Do not accept public URLs or signed URLs as productive input.
- Do not delete staged cloud files at job completion; lifecycle handles staging cleanup.
- Delete temporary biometric `FaceId` values after processing.

## Build And Test

```powershell
dotnet build DeValpo.MediaIntelligence.sln
dotnet test DeValpo.MediaIntelligence.sln --no-build
python -m mkdocs build --strict
powershell -ExecutionPolicy Bypass -File scripts\ci\secret-scan.ps1 -Root .
```
