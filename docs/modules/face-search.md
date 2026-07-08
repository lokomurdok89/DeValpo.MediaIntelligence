# Face Search

Face search finds reference faces in event videos using AWS Rekognition.

## Provider

- AWS Rekognition.
- Uses temporary face indexing.
- Deletes temporary `FaceId` values when processing finishes.
- Does not persist biometric identifiers.

## Inputs

- Multipart videos/reference images for smoke and compatibility.
- S3/GCS references through `assetSelectorJson` and `referenceAssetSelectorJson` for productive flows.
- `consentReferenceId` is optional.

## Output

Results are grouped by `targetKey` and include video time segments where a reference appears.

## Full reference

- [AWS Rekognition face search](../aws-rekognition-face-search.md)
- [Cloud assets](cloud-assets.md)
