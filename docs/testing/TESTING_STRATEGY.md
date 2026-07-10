# Testing Strategy

## Current Coverage Shape

The repository has xUnit tests with FluentAssertions. Existing tests cover
application services, request validation, provider adapters through fakes,
configuration/DI, cloud-selector rules, webhooks and API-host authorization
behavior. CI runs the solution tests but does not publish or enforce a coverage
threshold.

## Test Levels

| Level | Current state | Expected use |
| --- | --- | --- |
| Unit | Exists for parsers, validation, services and provider mapping. | Fast behavioral checks without network or database. |
| Application | Exists through repository/provider fakes. | Rules, state transitions and orchestration. |
| API host | Exists with `WebApplicationFactory`. | Authentication, authorization and controller wiring. |
| PostgreSQL persistence | Pending. | Add real database coverage, preferably isolated/Testcontainers. |
| Provider smoke | Manual/approved environment only. | Required when provider configuration, permissions or cloud routing changes. |
| End-to-end | Manual and environment-dependent. | Use sanitized real assets and record evidence. |

## CI Validation

CI restores tools, builds Release, runs tests, checks EF model drift, scans for
secrets, validates release configuration, builds Docker, validates Compose and
runs a non-blocking Trivy scan. It does not run real cloud smoke tests or a
coverage gate.

## Selecting Checks

- Documentation-only: MkDocs build, link review, secret scan and Git checks.
- Application/API behavior: focused tests plus solution build/test.
- Persistence model: focused tests and EF drift check; add database coverage if
  persistence behavior changed.
- Provider/cloud routing: focused tests plus an approved smoke test when real
  credentials and test assets are explicitly available.

Do not invent coverage percentages or report provider validation without an
executed command and sanitized evidence.
