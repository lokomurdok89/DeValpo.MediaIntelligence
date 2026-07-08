# Document Analysis

Document analysis extracts structured information from documents, including client-specific flows such as Arenysmed.

## Provider

- Google Document AI is the primary provider.
- Google Vision may be used for image OCR scenarios.

## Inputs

- Multipart remains available for existing flows.
- Productive cloud-reference processing should use GCS when the platform owns the source documents there.

## Output

Document flows return extracted fields, confidence and validation issues, not face/video matches.

## Full reference

- [Google Document AI runbook](../runbooks/google-document-ai.md)
- [API](../api.md)
