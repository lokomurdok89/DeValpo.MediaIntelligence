# Quality Gates

Use these gates when auditing DeValpo MediaIntelligence.

## Blocker

- A protected endpoint can be used without valid authentication.
- A client can read or modify another client's job/result.
- A secret, credential JSON, token or API key appears in logs, docs, Swagger examples or committed files.
- Production configuration can start with dummy JWT/API key values.
- Real Google Document AI processing fails for a valid configured processor.
- The production profile depends on mock skills.
- Submitted files can escape the configured storage root.
- Corrupt or unsupported files crash the API instead of returning a controlled error.

## Critical

- Google provider errors are ambiguous or misleading.
- Job state can become inconsistent after provider failure.
- Results are persisted without job/client ownership.
- Human review is required but no operator workflow exists.
- Webhook delivery is promised by model/docs but not implemented or clearly disabled.
- Resource files are bypassed for user-facing errors.
- Swagger exposes misleading auth or request behavior.

## Major

- Analysis is synchronous without timeout/retry strategy.
- Provider remote status performs expensive calls without caching or timeout.
- Logs lack correlation id/job id/client key.
- Health checks do not distinguish liveness from readiness.
- Test coverage depends only on mocks for product-critical flows.
- Profile names imply a business use case that is not actually the product scope.

## Minor

- XML comments exist but are vague.
- Docs mention old profile names.
- Error messages are grammatically inconsistent.
- Tests assert brittle literal messages instead of resource-backed behavior where possible.

## Required Evidence

For each QA run, capture:

- Commit SHA and branch.
- Docker Compose command used.
- Environment mode.
- Google project/location/processor id, without secrets.
- Exact endpoint calls or test names.
- HTTP status codes.
- Sanitized response snippets.
- Database/job ids when useful.
- Pass/fail and severity.
