# Cloud Assets

Cloud assets let productive jobs receive S3/GCS references instead of pushing large files through the API.

## Inputs

- `assetSelectorJson` selects source assets.
- `referenceAssetSelectorJson` selects reference images for face-search flows.
- Supported selector modes: `AllPrefix`, `ExplicitAssets`, `MultipartUpload`.
- Supported storage providers: `S3`, `GCS`.

## Rules

- Bucket/prefix must be allowed by client/profile configuration before processing.
- Public URLs and signed URLs are not accepted.
- MediaIntelligence stores a technical snapshot per job, not the event asset master catalog.
- Staged files are kept for lifecycle cleanup and are not deleted by the job.

## Full reference

- [Cloud references and staging](../cloud-asset-references-and-staging.md)
- [Cloud credentials and provider profiles](../reference/cloud-identities-and-provider-profiles.md)
