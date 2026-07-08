---
name: mediaintelligence-provider-google
description: Use for Google Document AI, Google Video Intelligence or Google Vision work in DeValpo.MediaIntelligence, including readiness, provider status and Google-specific validation.
---

# MediaIntelligence Google Provider

## Use When

- Working on Google Document AI, Video Intelligence or Vision.
- Checking provider status, readiness, polling, OCR or visible-number search.

## Read

- `docs/context/AI_ONBOARDING.md`
- `docs/context/ACTIVE_DECISIONS.md`
- `docs/modules/document-analysis.md` for Document AI tasks.
- `docs/modules/video-search.md` for video tasks.
- `docs/modules/image-batch-search.md` for Vision image tasks.
- Relevant files under `src/...Infrastructure/Providers/Google`.

## Do Not Read

- `secrets/`
- Google credential JSON files.
- `site/`
- Long local token docs unless the task is credential setup.

## Validate

```powershell
dotnet test DeValpo.MediaIntelligence.sln --filter Google
python -m mkdocs build --strict
```
