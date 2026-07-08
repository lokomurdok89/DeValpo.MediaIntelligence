# AI Onboarding

This is the main onboarding document for Codex, Claude, Copilot, Gemini or any
other AI agent working in `DeValpo.MediaIntelligence`.

## Read First

1. `docs/context/PROJECT_IDENTITY.md`
2. `docs/context/ACTIVE_DECISIONS.md`
3. `docs/context/CURRENT_STATE.md`
4. `README.md` for user-facing repo entry points.
5. Only the task-specific module or reference docs.

## Do Not Read

- `secrets/`
- `site/`
- `bin/`, `obj/`, `TestResults/`
- `docs/archive/` unless auditing historical planning.
- `docs/reference/` unless provider setup, endpoint diagrams or long evidence are needed.

## Short Repo Map

- `src/DeValpo.MediaIntelligence.Api`: HTTP API, auth, middleware, health and workers.
- `src/DeValpo.MediaIntelligence.Application`: use cases, orchestration and abstractions.
- `src/DeValpo.MediaIntelligence.Domain`: entities and normalized job state.
- `src/DeValpo.MediaIntelligence.Contracts`: public DTOs and contracts.
- `src/DeValpo.MediaIntelligence.Infrastructure`: EF Core, repositories, storage and providers.
- `tests/DeValpo.MediaIntelligence.Tests`: test suite.
- `docs/modules/`: short module documentation.
- `docs/reference/`: long task-specific references.
- `docs/specs/`: feature specs.
- `docs/adr/`: architecture decision records.

## Choose Documentation By Task

- General orientation: use this file plus `PROJECT_IDENTITY`, `ACTIVE_DECISIONS`
  and `CURRENT_STATE`.
- Feature work: create or update a spec in `docs/specs/` first.
- API work: read `docs/api.md` and the matching contract/controller files.
- Provider work: read the relevant module doc and only the required provider reference.
- Cloud references or staging: read `docs/cloud-asset-references-and-staging.md`
  and `docs/modules/cloud-assets.md`.
- QA/release: read `docs/qa.md`, `docs/operations.md` and CI scripts.

## Recommended Flow

1. Discovery: inspect only the files needed to understand the task.
2. Spec: write or update a concise spec for important features.
3. Implementation: keep changes scoped to the existing layers.
4. Tests: run the smallest meaningful validation set.
5. PR: prepare concise PR notes and risk/test coverage.
6. State: update `docs/context/CURRENT_STATE.md` when the repo state changes.

## Conversation Rules

- Use one conversation per feature or PR.
- Do not mix unrelated features in one run.
- Do not read `docs/reference/` or `docs/archive/` unless needed.
- Do not commit unless the user explicitly asks.
- For documentation-only requests, do not modify source code.
