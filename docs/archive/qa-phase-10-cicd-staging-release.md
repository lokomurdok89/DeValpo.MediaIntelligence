# QA Phase 10: CI/CD, Staging And Release

## Scope

Validate the Phase 10 release controls:

- CI runs build, tests, Docker build and Docker Compose validation.
- CI validates EF migration drift.
- CI runs secret scan.
- CI runs container vulnerability scan.
- Staging and production defaults reject unsafe local configuration.
- Swagger is not public in production.
- Migrations are documented as a controlled release step.

## QA Gates

| Gate | Expected Result | Status |
| --- | --- | --- |
| Build | `dotnet build` succeeds with 0 warnings | Passed |
| Tests | `dotnet test` succeeds | Passed |
| Migration drift | EF reports no pending model changes | Passed |
| Compose config | Docker Compose config is valid | Passed |
| Secret scan | `scripts/ci/secret-scan.ps1` passes | Passed |
| Release config | `scripts/ci/validate-release-config.ps1` passes | Passed |
| Production safety tests | Startup fails on dummy release secrets and unsafe release switches | Passed |
| Docker image | Image builds locally | Passed |
| Container scan | Vulnerability scan executes in report mode | Passed |
| CI | PR checks pass in GitHub Actions | Pending |

## Local Evidence

- Branch: `codex/phase-10-cicd-staging-release`.
- Base commit before Phase 10 changes: `292fba5`.
- `dotnet build` passed with `0` warnings after stopping a stale local API process.
- `dotnet test` passed with `72/72`.
- `dotnet ef migrations has-pending-model-changes --project src\DeValpo.MediaIntelligence.Infrastructure --startup-project src\DeValpo.MediaIntelligence.Api` reported no model drift.
- `docker compose -f deploy\docker-compose.yml config --quiet` passed.
- `powershell -NoProfile -File scripts\ci\secret-scan.ps1 -Root .` passed.
- `powershell -NoProfile -File scripts\ci\validate-release-config.ps1 -Root .` passed.
- `ASPNETCORE_ENVIRONMENT=Production dotnet run --project src\DeValpo.MediaIntelligence.Api --no-build --no-launch-profile` failed startup with the expected unsafe JWT signing key error.
- `docker compose -f deploy\docker-compose.yml up --build -d` rebuilt and started local API plus PostgreSQL.
- `/health` returned `200`.
- `/ready` returned `Healthy`, including PostgreSQL, local storage and Google Document AI.
- `docker build -t devalpo-media-intelligence-api:ci -f src\DeValpo.MediaIntelligence.Api\Dockerfile .` passed.
- Trivy `aquasec/trivy:0.69.3` vulnerability scan ran against `devalpo-media-intelligence-api:ci` and reported `0` HIGH/CRITICAL vulnerabilities.

## Findings

- Major fixed before final validation: release safety validation originally ran before test-host configuration overrides were applied. It now validates the final built app configuration before middleware is registered and before traffic is served.
- Minor fixed before final validation: local secret scan initially inspected ignored `secrets/` files. It now excludes ignored local secrets and fails if any real secret file under `secrets/` is tracked, while allowing `secrets/.gitkeep`.
- Minor fixed before final validation: Trivy scan used deprecated `--vuln-type`. CI now uses `--pkg-types os,library`.

QA result so far: no open Blocker, Critical or Major findings. Final CI evidence is pending PR execution.
