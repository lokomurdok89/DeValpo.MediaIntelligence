# Codex Minimal Context

This repository is the standalone `DeValpo.MediaIntelligence` service. It is not part of `Contador` or any sibling checkout.

Read this file first, then `active-decisions.md`, `architecture-current.md`, `../api.md`, and only the relevant module summary under `../modules/`.

## Product shape

- Backend-only .NET service.
- No frontend lives in this repo.
- API receives analysis/search jobs and returns asynchronous results.
- Workers process pending jobs with provider SDKs.
- PostgreSQL stores job snapshots, assets, matches, issues, audit and webhook state.
- Docs are built with MkDocs.

## Current high-value modules

- Document analysis with Google Document AI.
- Video target search with Google Video Intelligence.
- AWS Rekognition face search for videos.
- Image batch search for visible numbers and face references.
- Cloud asset references and staging for S3/GCS.

## Context discipline

- Do not read `docs/reference/` unless a task explicitly needs provider setup, endpoint diagrams or long historical detail.
- Do not read `docs/archive/` unless auditing old planning or release evidence.
- Never read `secrets/` or generated `site/`.
