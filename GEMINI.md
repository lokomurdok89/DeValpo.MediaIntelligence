# MediaIntelligence Generic AI Guide

Generic adapter for Gemini or other coding agents. The main source of truth is
`docs/context/AI_ONBOARDING.md`.

## Read First

- `docs/context/AI_ONBOARDING.md`
- `docs/context/PROJECT_IDENTITY.md`
- `docs/context/ACTIVE_DECISIONS.md`
- `docs/context/CURRENT_STATE.md`
- Relevant module docs under `docs/modules/`

## Do Not Read By Default

- `secrets/`
- `site/`
- `docs/archive/`
- `docs/reference/` unless needed for the task.
- Build outputs such as `bin/`, `obj/`, `TestResults/`.

## Rules

- Respect the .NET layered architecture.
- Keep credentials and provider values out of code and docs.
- Keep docs short and link to the canonical context instead of duplicating it.
- Preserve multipart compatibility and production cloud-reference behavior.
- Do not commit unless the user asks.

## Cheap Validation

```powershell
dotnet build DeValpo.MediaIntelligence.sln
dotnet test DeValpo.MediaIntelligence.sln --no-build
python -m mkdocs build --strict
```
