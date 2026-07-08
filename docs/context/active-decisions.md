# Active Decisions

## Architecture

- The project keeps a layered architecture: `Api`, `Application`, `Domain`, `Contracts`, `Infrastructure`.
- Provider SDK types stay in `Infrastructure`.
- Public DTOs stay in `Contracts`.
- Business orchestration stays in `Application`.
- Domain entities store normalized job state, not provider SDK objects.

## Product contracts

- `targetKey` is the canonical technical identifier for search targets.
- `dataKey` is only a business synonym and should not replace `targetKey` in API contracts.
- `consentReferenceId` is nullable and must not block processing.
- MediaIntelligence stores external consent references only; legal documents belong to another system.

## Cloud assets

- Production input should use cloud references through `assetSelectorJson`.
- Multipart upload stays for compatibility, development and smoke tests.
- Productive input must not accept public URLs or signed URLs.
- The external platform owns events, users, live asset catalog and legal consent records.
- MediaIntelligence stores technical job snapshots, resolved assets, matches, issues, audit and webhook status.

## Staging

- Jobs must not delete staged cloud files when processing completes.
- Lifecycle policies in S3/GCS are the primary cleanup mechanism.
- Monthly sweep is only a secondary operational safety net.
- Temporary biometric `FaceId` cleanup remains mandatory because it is privacy cleanup, not file staging cleanup.

## Provider routing

- AWS Rekognition is the provider for face recognition.
- Google Vision supports OCR/visible-number image search, not identity face recognition.
- Google Document AI is the provider for document analysis.
- Cross-cloud processing may create temporary staging in the provider-compatible bucket.
