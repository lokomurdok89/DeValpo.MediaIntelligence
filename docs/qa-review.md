# QA Review

Review date: 2026-07-06

Branch: `codex/docs-swagger-resx-qa-productization`

Skill used:

- `docs/devalpo-mediaintelligence-qa/SKILL.md`
- `docs/devalpo-mediaintelligence-qa/references/quality-gates.md`

## Summary

This review focused on the scope of the documentation, Swagger, interface segregation, resource centralization and productization plan PR.

## Findings And Remediation

**[Major] Runtime/user-facing strings were still hardcoded**

Evidence: source scan found validation, status, health check, Google Document AI and skill description messages in code.

Impact: messages were scattered across layers and harder to localize or audit.

Fix: added resource files and resource helpers for API, Application and Infrastructure. Runtime/user-facing messages in the PR scope now resolve through `.resx` resources.

**[Major] Application interfaces shared files with implementations**

Evidence: application service interfaces were declared in the same files as concrete services.

Impact: this weakens interface segregation and makes ownership boundaries less clear.

Fix: moved each application service interface into its own `I*.cs` file while preserving namespaces and signatures.

**[Major] Swagger endpoints lacked enough reader-facing detail**

Evidence: Swagger previously exposed endpoints with minimal descriptions.

Impact: real consumers had to infer request intent, response types and authentication behavior.

Fix: added XML summaries, remarks, response metadata and `ProducesResponseType` attributes for primary endpoints.

**[Major] Product gaps remain beyond this PR**

Evidence: QA review still identifies product gaps: production mock gating, API key lifecycle, tenant isolation, async execution, human review workflow, webhook delivery, observability and production deployment controls.

Impact: the system is not product-ready yet.

Fix: captured these items in `docs/productization-plan.md`. They are explicitly outside this PR's implementation scope.

## Hardcoded Text Audit

Moved to `.resx`:

- API validation and error messages.
- API health check messages.
- JWT option validation messages.
- Swagger security description.
- Application validation and orchestration issue messages.
- Human review reason.
- Profile descriptions.
- Infrastructure option validation messages.
- Storage path validation messages.
- Google Document AI status and execution messages.
- Skill descriptions and non-production issue messages.

Intentionally left as code constants:

- HTTP routes.
- Configuration keys.
- JWT claim names and token type.
- Error/issue codes.
- Severity/status values.
- Profile keys, skill names and capability keys.
- MIME types and file extensions.
- Database table/column names and EF migration output.
- XML documentation comments used to generate Swagger endpoint documentation.

## Validation Commands

```powershell
dotnet build
dotnet test
Test-Path docs\devalpo-mediaintelligence-qa\SKILL.md
Test-Path docs\devalpo-mediaintelligence-qa\references\quality-gates.md
```

Expected result:

- Build passes with zero warnings.
- Tests pass.
- QA skill and quality gates are present inside the repository.

## AWS Rekognition Disabled-Mode QA

Review date: 2026-07-07

Branch: `agent/aws-rekognition-disabled-architecture`

Commit: pending after conflict remediation

Skill used:

- `docs/devalpo-mediaintelligence-qa/SKILL.md`
- `docs/devalpo-mediaintelligence-qa/references/quality-gates.md`

### Scope

This review validated the AWS Rekognition disabled-mode architecture, provider separation, no-credentials startup behavior and the existing real Google Video Intelligence result path.

### Findings And Remediation

No Blocker or Critical findings were found in this QA pass.

**[Minor] Local Docker emitted HTTPS redirect warning**

Evidence: `docker logs media-intelligence-api --tail 120` included `Failed to determine the https port for redirect` during HTTP local testing.

Impact: local log noise only. It did not block `/health`, provider status, Swagger or video result retrieval.

Fix: HTTPS redirection now remains enabled outside Development, but local Development Docker no longer attempts HTTPS redirection when only HTTP is exposed.

**[Major] Google Video remote operations could run for hours without a bounded timeout**

Evidence: real job `cdecf8d6-8643-4ccc-9577-f473171d449b` stayed `Running` for multiple hours and completed only after Google returned an internal provider error.

Impact: a worker could remain tied to a single large video without an explicit operation limit or clear timeout issue.

Fix: added `GoogleVideoIntelligence__RemoteOperationTimeoutSeconds`, resource-backed timeout messages and provider classification so long Google operations finish with an explicit issue instead of waiting indefinitely.

### Runtime Evidence

| Check | Evidence |
| --- | --- |
| Docker rebuild | `docker compose -f deploy\docker-compose.yml up --build -d` succeeded |
| Health | `/health` returned `200` |
| Dev token | `POST /api/v1/dev/token` returned JWT, length `412` |
| AWS basic status | `/api/v1/providers/aws-rekognition/status` returned `enabled=false`, `configured=false` |
| AWS remote status without token | returned `401` |
| AWS remote status with client-only token | returned `403` |
| AWS face jobs | Historical pre-provider check returned `404`; current implementation exposes `/api/v1/face-search-jobs` with JWT/profile access |
| Google Video remote status | `configured=true`, staging bucket reachable |
| Swagger | `/swagger/v1/swagger.json` contains `/api/v1/providers/aws-rekognition/status` |
| Existing real video job | `00cf5f09-6e1d-4c0c-a9f5-5bd3a9ff2740` returned `Completed` |
| Existing video result | target group `participant-428`, visible number `428`, `60` match segments, `2819` generic face occurrences, `0` issues |
| New long-running Google job | `cdecf8d6-8643-4ccc-9577-f473171d449b` completed with provider issue `Google Video Intelligence request failed with status Internal.` |
| Timeout remediation | Google remote operation now has configurable `RemoteOperationTimeoutSeconds` defaulting to `7200` |
| Runtime log check after remediation | no recent application errors; only expected local DataProtection warnings in Development Docker |

### Validation Commands

```powershell
dotnet build DeValpo.MediaIntelligence.sln
dotnet test DeValpo.MediaIntelligence.sln
python -m mkdocs build --strict
docker compose -f deploy\docker-compose.yml config
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\ci\secret-scan.ps1 -Root .
```

Expected result:

- Build passes with zero warnings.
- Tests pass.
- Docs build succeeds.
- Compose config succeeds.
- Secret scan passes.

## AWS Face Search Credential-Ready QA

Review date: 2026-07-07

Branch: `agent/video-e2e-aws-face-gating-fixes`

### Scope

This review closes the AWS Face Search gaps found before credential integration: safe AWS request shape, sanitized audit events, release safety coverage, polling-only documentation and no persistent face index.

### Findings And Remediation

No Blocker or Critical findings remain for the no-credentials path.

**[Major] Raw `targetKey` could violate AWS `ExternalImageId` constraints**

Fix: the provider no longer sends raw `targetKey` as `ExternalImageId`. The API keeps `targetKey` as the domain/result identifier and maps Rekognition matches through temporary `FaceId` values only during the job.

**[Major] Face search audit requirements were documented but not wired**

Fix: the face search controller now records sanitized `face_search.created` and `face_search.result_read` audit events. Metadata contains profile/status/count fields only, not manifest content, consent IDs, S3 paths, `FaceId` or credentials.

**[Minor] AWS release safety lacked a direct regression case**

Fix: production safety tests now include `AwsRekognition:AllowUserCredentials=true` as a forbidden option.

### Credential Boundary

The repository is ready for credential integration, but a real AWS smoke test remains intentionally pending until external credentials, a private S3 bucket and a Rekognition collection are provided.

### Validation Commands

```powershell
dotnet build DeValpo.MediaIntelligence.sln
dotnet test DeValpo.MediaIntelligence.sln
python -m mkdocs build --strict
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\ci\secret-scan.ps1 -Root .
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\ci\validate-release-config.ps1 -Root .
docker compose -f deploy\docker-compose.yml config --quiet
```

Expected result:

- Build and tests pass.
- Docs build succeeds.
- Secret and release config checks pass.
- Compose config is valid.
- Real AWS smoke remains pending credentials/resources only.
