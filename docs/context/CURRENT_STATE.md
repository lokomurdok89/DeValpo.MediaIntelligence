# Current State

## Live Git State

This document intentionally does not store a branch name, commit SHA, pull
request count or working-tree status. Those values are temporary: verify them
with `git branch --show-current`, `git status --short --branch` and `git log`.

## Operating State

- Standalone repo root: `C:\DeValpo\MediaIntelligence`.
- Backend-only service; no frontend lives in this repo.
- Main solution: `DeValpo.MediaIntelligence.sln`.
- Docs use MkDocs Material through `mkdocs.yml`.
- Active modules include document analysis, video search, face search, image
  batch search and cloud assets.

## Recommended Next Work

1. Keep one conversation per feature or PR.
2. Start important work by creating or updating a spec under `docs/specs/`.
3. Prefer module-level changes with focused tests.
4. Update this file after implementation, validation or PR state changes.

## Open Risks

- Provider credentials and cloud profiles remain external to the repo.
- Real provider smoke tests require configured cloud accounts and mounted
  credentials outside `secrets/`.
- Cross-cloud staging must preserve lifecycle-owned cleanup semantics.
- Public contract terms must stay stable, especially `targetKey`.
- Production readiness gaps are tracked in `../backlog/ENGINEERING_BACKLOG.md`.

## Known Validations

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
