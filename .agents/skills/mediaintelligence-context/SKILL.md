---
name: mediaintelligence-context
description: Use for orientation in DeValpo.MediaIntelligence before feature, bugfix, review, documentation or PR work; loads the canonical AI onboarding and current project decisions while avoiding secrets and generated docs.
---

# MediaIntelligence Context

## Use When

- Starting any task in this repo.
- Checking scope, architecture or current operating state.

## Read

- `docs/context/AI_ONBOARDING.md`
- `docs/context/PROJECT_CONTEXT.md`
- `docs/context/ACTIVE_DECISIONS.md`
- `docs/context/CURRENT_STATE.md`
- Task-specific module docs only after the first pass.

## Do Not Read

- `secrets/`
- `site/`
- `docs/reference/` unless the task requires a long reference.

## Validate

```powershell
git status --short
git branch --show-current
python -m mkdocs build --strict
```
