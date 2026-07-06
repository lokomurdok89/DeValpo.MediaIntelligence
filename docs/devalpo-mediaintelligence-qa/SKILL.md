---
name: devalpo-mediaintelligence-qa
description: Product QA audit workflow for DeValpo MediaIntelligence. Use when Codex needs to review, test, or certify this repository for MVP-to-product readiness, including real Google Document AI flows, JWT/API access, tenant isolation, Docker/PostgreSQL runtime, Swagger/API contract, persistence, human review, webhooks, observability, security, and release readiness.
---

# DeValpo MediaIntelligence QA

Use this skill to act as a product QA engineer who understands what DeValpo MediaIntelligence is supposed to do and can produce prioritized, evidence-backed findings.

## Operating Rules

- Prefer real flows over synthetic-only tests.
- Do not expose credential JSON, tokens, API keys or document-sensitive content.
- Treat mocks as non-product unless a test explicitly targets development scaffolding.
- Report findings first, ordered by severity.
- Include file/endpoint/test evidence for every finding.
- Distinguish "not implemented" from "implemented but failing".
- Do not mark product-ready while any blocker remains.

Read `references/quality-gates.md` when classifying severity.

## Audit Workflow

1. Inspect repository state.
   - Run `git status --short --branch`.
   - Note uncommitted changes.
   - Review `README.md`, `docs/productization-plan.md`, `docs/architecture.md`, `docs/profiles.md`, `docs/skills.md`.

2. Build the current mental model.
   - Identify API endpoints in `src/DeValpo.MediaIntelligence.Api/Controllers`.
   - Identify product flows in `Application`.
   - Identify provider/storage/database behavior in `Infrastructure`.
   - Identify persisted entities in `Domain`.
   - Identify public contracts in `Contracts`.

3. Check product-critical areas.
   - Authentication and authorization.
   - Client/tenant isolation.
   - Google Document AI configuration and real provider execution.
   - File ingestion and storage safety.
   - Job lifecycle and persistence.
   - Human review behavior.
   - Webhook behavior.
   - Swagger/API contract accuracy.
   - Observability and diagnostics.
   - Docker and deployment configuration.
   - Resource-backed messages.
   - Mock isolation from production paths.

4. Validate with commands.
   - Run `dotnet build`.
   - Run `dotnet test`.
   - If Docker is relevant, run `docker compose -f deploy/docker-compose.yml config`.
   - If the environment is configured for real Google Document AI, validate `/api/v1/providers/google-document-ai/status?checkRemote=true` with a valid token.
   - If a real test document is provided, validate `/api/v1/analyze` with the real provider.

5. Produce QA output.
   - Findings first.
   - Each finding must include severity, evidence and product impact.
   - Then list open questions.
   - Then list recommended next fixes.
   - Then include verification commands that were run.

## Severity Model

- Blocker: prevents safe product use or leaks data/secrets.
- Critical: breaks a promised product workflow or creates serious operational risk.
- Major: product can run but is incomplete, fragile or misleading.
- Minor: polish, clarity or maintainability issue.

Use `references/quality-gates.md` for concrete examples.

## Expected Product Behavior

The product should:

- Accept authenticated client requests.
- Restrict clients to their own jobs, results and allowed profiles.
- Store uploaded media outside PostgreSQL and store only metadata/path in the database.
- Run real Document AI OCR for the generic document profile.
- Persist jobs, assets, skill executions, normalized results and issues.
- Create human review tasks when confidence/errors require review.
- Expose accurate provider status without leaking secrets.
- Document all functional endpoints through Swagger.
- Run API and PostgreSQL together through Docker Compose.
- Keep user-facing text in resource files.
- Avoid mock skills in production execution paths.

## Finding Template

Use this format:

```markdown
**[Severity] Title**
Evidence: file, endpoint, command or response.
Impact: why this matters for product readiness.
Fix: the smallest practical remediation.
```

## Real Test Checklist

Use real assets when available:

- Valid PDF.
- Multi-page PDF.
- Scanned PDF.
- Empty file.
- Corrupt PDF.
- Oversized file.
- Unsupported extension.
- Low-quality OCR document.

For each tested asset, capture job id, status, confidence, review flag and issue codes. Redact sensitive extracted text unless the user explicitly asks to inspect it.
