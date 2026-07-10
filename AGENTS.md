# MediaIntelligence Agent Guide

Keep this file operational. The canonical context is `docs/context/`.

## Required Reading

1. Always read `docs/context/AI_ONBOARDING.md`, `PROJECT_CONTEXT.md`,
   `ACTIVE_DECISIONS.md` and `CURRENT_STATE.md`.
2. Feature or contract work: read the matching module/API document and create or
   update a spec under `docs/specs/` when the change is material.
3. Provider, cloud asset, QA/release, architecture or configuration work: read
   the linked guide under `docs/` before editing.

## Engineering Rules

- Preserve `Api`, `Application`, `Domain`, `Contracts` and `Infrastructure`
  boundaries. Provider SDK and EF types stay out of public contracts.
- Keep public contracts stable: `targetKey` is canonical; productive inputs use
  cloud references while multipart remains compatible for development and smoke tests.
- Do not read, print or edit `secrets/`; never hardcode credentials, provider
  identifiers, bucket names, collection IDs or production paths.
- Do not delete staged cloud files at job completion; lifecycle manages S3/GCS.
  Delete temporary Rekognition `FaceId` values after processing.
- Do not commit, push, merge, rebase, open a PR or deploy unless explicitly asked.

## Validation And Reporting

- Use the smallest relevant checks from `docs/development/DEVELOPMENT_GUIDE.md`
  and `docs/testing/TESTING_STRATEGY.md`; report checks not run and why.
- For an implementation report: changed files, validation results, remaining
  risks and `git status`. For reviews: findings first, with evidence.
