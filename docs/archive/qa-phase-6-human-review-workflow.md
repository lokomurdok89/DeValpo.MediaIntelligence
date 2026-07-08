# QA Phase 6: Human Review Workflow

## Scope

Phase 6 turns persisted human review tasks into an operator workflow.

Included:

- List review tasks.
- Read review task detail.
- Start a pending review task.
- Complete a task with `Approve`, `Correct` or `Reject`.
- Store operator id, comment, decision and timestamps.
- Preserve original extracted fields and corrected fields.
- Write corrected fields back to the result for client retrieval.

Out of scope:

- Web UI for reviewers.
- Assignment queues beyond a single operator identity.
- Webhook delivery after review completion.
- Production audit log table.

## Automated QA

Executed on branch `codex/phase-6-human-review-workflow`:

- `dotnet build`: passed, 0 warnings.
- `dotnet test`: passed, 46/46.
- `dotnet ef migrations has-pending-model-changes --project src\DeValpo.MediaIntelligence.Infrastructure --startup-project src\DeValpo.MediaIntelligence.Api`: passed, no pending model changes.
- `docker compose -f deploy/docker-compose.yml config --quiet`: passed.

Expected unit coverage:

- Start pending task moves to `InReview`.
- Approve completes task and job.
- Correct preserves original fields, stores corrected fields and updates result fields.
- Reject marks task `Rejected` and job `Failed`.
- Correct without fields fails with a controlled error.
- Starting a completed task fails with a controlled error.

## Real Docker QA Matrix

Environment:

- Docker Compose: `deploy/docker-compose.yml`.
- API: `http://localhost:8085`.
- Database: PostgreSQL container.
- Auth: local Development JWT with `media-intelligence:operator` and `media-intelligence:client` scopes.
- Review tasks for workflow paths were attached to an existing real completed job in PostgreSQL because available real OCR samples did not naturally produce low confidence.

| Scenario | Expected Result | Status |
| --- | --- | --- |
| Operator lists pending tasks | `200` with review task collection | Passed |
| Client token lists review tasks | `403` | Passed |
| Operator starts task | Task moves to `InReview`, operator id stored | Passed: task `d7804512-b87b-42ad-b21f-7a3ff8fed00d`, operator `qa-operator` |
| Operator approves task | Task moves to `Completed`, job moves to `Completed` | Passed |
| Operator corrects task | Original/corrected fields preserved, result uses corrected fields | Passed: task `6544eace-648f-4c2a-9349-d142867f01d8`, result first field `qa_transactional_field` |
| Operator rejects task | Task moves to `Rejected`, job moves to `Failed` | Passed |
| Real image low-confidence attempt | Low-confidence/empty OCR should create `RequiresReview` | Attempted with `4656215.jpg`; job completed because OCR extracted text/confidence above threshold |

## QA Notes

The Google OCR skill now reports `0.50` confidence when Document AI returns empty text, which can trigger `RequiresReview`. The available real files used in local QA still produced text and completed successfully, so automatic low-confidence review creation was not fully proven with a naturally low-confidence real document in this phase.

## QA Result

Passed for the human review API workflow after remediation.

## Findings

### Major: Correct Decision Failed With EF Concurrency Error

Initial `Correct` workflow returned `500` while replacing `ExtractedFields`.

Root cause:

- The service cleared a loaded EF navigation collection, which caused EF to reconcile required child rows incorrectly during save.

Remediation:

- Moved field replacement to `IHumanReviewTaskRepository.ReplaceExtractedFieldsAsync`.
- Replaced fields with `RemoveRange` and `AddRange` inside the same unit of work.
- Re-ran `Correct`; result endpoint returned the corrected field.

Status: fixed.

### Major: Operator Identity Was Stored As Fallback

Initial `Start` response stored `unknown-operator`.

Root cause:

- JWT inbound mapping exposed `sub` as `ClaimTypes.NameIdentifier`; the controller only read `JwtRegisteredClaimNames.Sub`.

Remediation:

- `HumanReviewTasksController` now checks both `sub` and `ClaimTypes.NameIdentifier`.
- Re-ran start workflow; operator id stored as `qa-operator`.

Status: fixed.

### Major: Automatic Low-Confidence Trigger Not Proven With Available Real Files

Available real test files completed successfully, so QA could not naturally create a low-confidence review task from a real document.

Remediation:

- Adjusted `GoogleDocumentAiDocumentOcrSkill` so empty OCR text yields `0.50` confidence instead of a successful fixed `0.89`.

Residual risk:

- Needs a real poor-quality or empty-OCR document in a future certification QA matrix to prove automatic `RequiresReview` creation end to end.

Status: documented residual risk, does not block the review API workflow.
