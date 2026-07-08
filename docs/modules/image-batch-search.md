# Image Batch Search

Image batch search finds visible numbers or reference faces across a group of images.

## Providers

- AWS Rekognition supports visible numbers and face references.
- Google Vision supports visible numbers/OCR.
- Google Vision returns a controlled unsupported issue for identity face search.

## Inputs

- Multipart images for development and smoke.
- S3/GCS references through `assetSelectorJson`.
- Reference images through `referenceAssetSelectorJson`.

## Output

Results are grouped by `targetKey`; each target returns the matching images.

## Full reference

- [Image batch search](../image-batch-search.md)
- [Cloud assets](cloud-assets.md)
