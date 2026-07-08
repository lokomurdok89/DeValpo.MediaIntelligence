---
name: mediaintelligence-qa-release
description: Use for QA, release readiness, CI/CD, docs validation, secret scanning, Docker Compose validation and PR-readiness checks in DeValpo.MediaIntelligence.
---

# MediaIntelligence QA Release

## Use When

- Preparing a PR or release.
- Checking docs, tests, Docker Compose, EF migrations or secret hygiene.

## Read

- `docs/context/AI_ONBOARDING.md`
- `docs/context/CURRENT_STATE.md`
- `docs/qa.md`
- `docs/operations.md`
- `.github/workflows/ci.yml`
- `.github/workflows/docs.yml`

## Do Not Read

- `secrets/`
- `site/`
- Raw credentials or mounted provider files.

## Validate

```powershell
dotnet build DeValpo.MediaIntelligence.sln
dotnet test DeValpo.MediaIntelligence.sln --no-build
python -m mkdocs build --strict
powershell -ExecutionPolicy Bypass -File scripts\ci\secret-scan.ps1 -Root .
powershell -ExecutionPolicy Bypass -File scripts\ci\validate-release-config.ps1
docker compose -f deploy\docker-compose.yml config --quiet
```
