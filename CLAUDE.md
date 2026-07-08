# MediaIntelligence Claude Guide

Claude Code adapter. The canonical onboarding document is
`docs/context/AI_ONBOARDING.md`; keep this file brief.

## Read First

1. `docs/context/AI_ONBOARDING.md`
2. `docs/context/PROJECT_IDENTITY.md`
3. `docs/context/ACTIVE_DECISIONS.md`
4. `docs/context/CURRENT_STATE.md`
5. Task-specific docs only.

## Skip By Default

- `secrets/`
- `site/`
- `bin/`
- `obj/`
- `TestResults/`
- `docs/archive/`
- `docs/reference/` unless the request requires it.

## Project Rules

- Preserve layers: `Api`, `Application`, `Domain`, `Contracts`, `Infrastructure`.
- Keep provider SDK details inside `Infrastructure`.
- Keep public request/response contracts inside `Contracts`.
- Keep business orchestration inside `Application`.
- Do not expose provider objects through domain or API contracts.
- Do not change source code for documentation-only tasks.

## Security Rules

- Never read or print `secrets/`.
- Never hardcode provider values or credentials.
- Keep multipart compatibility.
- Use cloud references for production asset input.
- Do not delete staged cloud files after jobs.
- Delete temporary AWS Rekognition `FaceId` values after processing.
- Treat `targetKey` as the canonical technical target name.

## Validation

```powershell
dotnet build DeValpo.MediaIntelligence.sln
dotnet test DeValpo.MediaIntelligence.sln --no-build
python -m mkdocs build --strict
powershell -ExecutionPolicy Bypass -File scripts\ci\secret-scan.ps1 -Root .
docker compose -f deploy\docker-compose.yml config --quiet
```

## Working Agreement

- One feature or PR per conversation.
- Use specs for important features.
- Update `docs/context/CURRENT_STATE.md` when implementation state changes.
- Do not commit unless explicitly asked.
