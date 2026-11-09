# Current State

## Live Git State

This document intentionally does not store a branch name, commit SHA, pull
request count or working-tree status. Those values are temporary: verify them
with `git branch --show-current`, `git status --short --branch` and `git log`.

## Operating State

- Standalone repo root: `C:\DeValpo\MediaIntelligence`.
- Backend service, plus an isolated local saved-result viewer in
  `scripts/local-viewer/`; no production frontend is deployed by this repository.
- Main solution: `DeValpo.MediaIntelligence.sln`.
- Docs use MkDocs Material through `mkdocs.yml`.
- Active modules include document analysis, video search, face search, image
  batch search and cloud assets.

## Product And Local Viewer

The first operational delivery is facial search in S3 videos with a minimal
occurrence viewer; bib photos follow afterwards. The
[current product scope](../specs/s3-video-face-search-map.md) replaces the
previous photo-first order. Existing AWS JSON/video evidence is reused; no
historical E2E is scheduled. The user authorized and received a simple local
gallery: select among the three existing videos, show associated detailed AWS
results or saved comparison summaries, and seek to clickable occurrences.
The prepared gallery now uses the 1:12 H.264 copy with 39 real AWS match rows,
the short H.264 video with 18 saved points, and the other short video with a
zero-match summary and issues. Missing results are not displayed as zero matches.
Google API exports are supported, but the original Google JSON was not found.
The viewer runs on loopback without AWS or a database. It is an offline result
consumer, not a deployed live S3 frontend.

On 2026-11-09, the user explicitly authorized a live AWS run for the 1:12 video
and its existing reference photo. The original HEVC input returned a codec
error. The user then approved a private H.264 copy/upload and a second run:
`Completed`, 39 point records (32 distinct timestamps), 0 issues, threshold 80,
similarity 81.23–99.99, times 6.506–71.871 s. Temporary collection faces returned
to zero after both runs. Real responses and media remain in ignored
`outputs/private/`; the original media were unchanged. Both README files
describe the result. Click-to-seek and playback were verified in the browser.
No backend refactor, historical replay, Google execution or commit was performed.
Further provider executions require task-specific authorization.

The repository also includes a distributable, privacy-safe offline demo in
`docs/assets/demo/`: an allowlisted 39-row JSON derivative, a generated H.264
replacement video with no original frames/audio, and an illustrated SVG preview.
It uses the existing viewer unchanged. This demonstrates seeking, not visual
identity accuracy. README EN/ES explain the pilot value, evidence and implemented
cloud-first boundaries without claiming deployed autoscaling.

The first additional documentation-cleanup batch removed four generic process
templates/checklists and their navigation. Architecture/API/provider references,
useful diagrams, presentation, application code and tests were preserved.

The second approved batch removed six redundant Markdown guides after merging
their useful content: setup/client authentication into Getting Started and client
onboarding; document lifecycle/webhook details into API; Google/AWS configuration
into their provider pages; layer boundaries into bilingual architecture; unique
sequences into Processing Flows. Historical Google setup measurements retain their
Git provenance in the evidence pages. Links/navigation target those canonical
pages. No application code, cloud execution or commit is part of this consolidation.

An independent marketing/editorial agent reviewed the bilingual README, demo
presentation and documentation entry points on 2026-11-09 and approved their
clarity, hierarchy, accessible demo, evidence honesty, architecture explanation
and language parity. This is editorial approval, not production certification
or authorization to publish preserved private material. Sales framing was removed;
the navigation now prioritizes visitors before contributor/AI context.

The superseded Archify phase plan and earlier bib-only sequences were removed
with explicit approval. The current configurable-search sequences and their
JSON sources remain, alongside the presentation and public demo. Documentation
no longer claims that the retired diagram versions are retained. GitHub is the
technical portfolio entry point, not a sales proposal.

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
