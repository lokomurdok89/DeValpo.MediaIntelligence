# QA Phase 5: File Ingestion Validation

## Scope

Phase 5 validates real upload handling before assets are stored or queued.

Included:

- Allowed extension validation by asset type.
- Declared content type validation.
- Binary signature validation.
- Corrupt or incomplete PDF detection.
- Empty and oversized file rejection.
- Machine-readable API error codes.
- Future threat-scanning hook.

Out of scope:

- Full antivirus provider integration.
- Human review workflow.
- Webhook delivery.
- Provider-specific structured field extraction beyond existing OCR output.

## Automated QA

Executed on branch `codex/phase-5-file-ingestion-validation`:

- `dotnet build`: passed, 0 warnings.
- `dotnet test`: passed, 40/40.
- `dotnet ef migrations has-pending-model-changes --project src\DeValpo.MediaIntelligence.Infrastructure --startup-project src\DeValpo.MediaIntelligence.Api`: passed, no pending model changes.
- `docker compose -f deploy/docker-compose.yml config --quiet`: passed.

Unit coverage:

- Valid PDF returns valid validation result.
- Empty file returns `FILE_EMPTY`.
- Unsupported extension returns `FILE_EXTENSION_UNSUPPORTED`.
- Declared content type mismatch returns `FILE_CONTENT_TYPE_UNSUPPORTED`.
- Invalid binary signature returns `FILE_SIGNATURE_INVALID`.
- Incomplete PDF returns `PDF_CORRUPT`.
- Oversized file returns `FILE_TOO_LARGE`.
- Threat scanner failure returns `FILE_THREAT_DETECTED`.

## Real Docker QA Matrix

Environment:

- Docker Compose: `deploy/docker-compose.yml`.
- API: `http://localhost:8085`.
- Database: PostgreSQL container.
- Provider: Google Document AI remote processor.
- Auth: local Development JWT with `media-intelligence:operator` scope.

Provider readiness:

- `/health`: `Healthy`.
- `/api/v1/providers/google-document-ai/status?checkRemote=true`: `enabled=true`, `configured=true`, `credentialFilePresent=true`, remote processor reachable.

| Scenario | Expected Result | Status |
| --- | --- | --- |
| Valid small PDF | `202 Accepted`, job reaches `Completed` | Passed: `DTE Daniel.pdf`, job `172784b3-7e68-406d-a5c6-f59bb948c0b1`, status `Completed`, attempts `1`, confidence `0.8900`, extracted fields `3` |
| Additional real PDF | `202 Accepted`, job reaches `Completed` | Passed: `DTE - Empresa 78128534 - Folio Nº 101.pdf`, job `3d41c81a-bf82-4948-af9d-c93a0bdfb847`, status `Completed`, attempts `1`, extracted fields `3` |
| Long OCR PDF | Long extracted text persists without DB failure | Passed after remediation: `DOCUMENTO_FIRMADO_01_06_2026_11_51_21.pdf`, job `f5c15151-56cc-47c8-aaf7-4890255e9a95`, status `Completed`, attempts `1`, extracted fields `3` |
| Image input | Accepted as `Document` input and processed by Google OCR | Passed: `bet.png`, job `5bcdd40c-359d-4e90-8aca-164573888d5c`, status `Completed`, attempts `1`, extracted fields `3` |
| Empty file | `400 FILE_EMPTY` | Passed |
| Corrupt PDF | `400 PDF_CORRUPT` or `400 FILE_SIGNATURE_INVALID` | Passed: `400 PDF_CORRUPT` |
| Unsupported extension | `400 FILE_EXTENSION_UNSUPPORTED` | Passed |
| Declared content type mismatch | `400 FILE_CONTENT_TYPE_UNSUPPORTED` | Passed |
| Oversized file | `400 FILE_TOO_LARGE` | Passed after remediation |

## QA Result

Passed after remediation.

## Findings

### Major: Oversized Upload Bypassed API Error Contract

Initial oversized upload returned ASP.NET's generic form parsing validation error instead of the API contract's `ApiErrorResponse`.

Root cause:

- The multipart request limit matched the max file size exactly, so multipart overhead could be rejected before `UploadedFileValidationService` ran.

Remediation:

- Increased the endpoint multipart request envelope limit to `125829120` bytes while keeping `MediaAnalysis:MaxFileSizeBytes=104857600`.
- Re-ran oversized real API request and confirmed `400 FILE_TOO_LARGE`.

Status: fixed.

### Major: Real OCR Text Exceeded `ExtractedFields.Value` Length

The real file `DOCUMENTO_FIRMADO_01_06_2026_11_51_21.pdf` failed after Google OCR because extracted text exceeded `varchar(2000)`.

Root cause:

- `ExtractedFields.Value` was capped at 2000 characters, but full OCR text can be longer.

Remediation:

- Changed `ExtractedFields.Value` mapping to PostgreSQL `text`.
- Added migration `AllowLongExtractedFieldValues`.
- Rebuilt Docker, applied migration through startup, and re-ran the failing PDF.

Status: fixed.
