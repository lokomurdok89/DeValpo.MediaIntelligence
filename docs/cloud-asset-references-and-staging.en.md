# Cloud Asset References And Temporary Staging

MediaIntelligence supports two input modes:

- **Multipart upload**: for development, smoke tests and compatibility.
- **Cloud references**: recommended production flow. The external platform uploads files to S3 or Google Cloud Storage and MediaIntelligence receives references.

The external platform decides where the original object lives:

- Events, races, weddings, triathlons and face recognition: S3 + AWS Rekognition.
- Documents, OCR and Arenysmed-style image validation: GCS + Google Document AI/Vision.

MediaIntelligence does not accept public URLs or signed URLs. It uses environment-configured credentials.

## assetSelectorJson

Production endpoints can receive optional `assetSelectorJson` as a form field. When omitted, the endpoint keeps the current multipart behavior.

```json
{
  "selectorMode": "AllPrefix",
  "storageProvider": "S3",
  "bucketName": "devalpo-events-prod",
  "prefix": "events/race-2026/photos/",
  "region": "us-east-1"
}
```

For explicit assets:

```json
{
  "selectorMode": "ExplicitAssets",
  "storageProvider": "GCS",
  "bucketName": "arenysmed-docs-prod",
  "location": "us",
  "assets": [
    {
      "assetKey": "doc-001",
      "objectKey": "deliveries/2026/doc-001.pdf",
      "contentType": "application/pdf",
      "sizeInBytes": 345678,
      "generation": "1789000000000000"
    }
  ]
}
```

Face search flows also support `referenceAssetSelectorJson` for platform-owned selfies or reference images.

## Temporary staging

Staging is used only when the required engine cannot process directly from the original storage.

| Original | Engine | Result |
|---|---|---|
| S3 | AWS Rekognition | Direct processing when the bucket matches AWS config. |
| GCS | Google Vision/Video/Document AI | Direct processing when the SDK supports it. |
| S3 | Google Video Intelligence | Temporary staging to GCS. |
| GCS | AWS Rekognition | Temporary staging to S3. |

The job must not delete staged files when it finishes. The primary cleanup mechanism is bucket lifecycle policy:

- Default TTL: 3 days.
- Allowed range: 1 to 7 days.
- Monthly sweep: operational backup, not the primary mechanism.

AWS Rekognition still deletes temporary `FaceId` values with `DeleteFaces`; that is biometric privacy cleanup, not staged file cleanup.

## Security

- `CloudAssets:AllowedS3Buckets` and `CloudAssets:AllowedGcsBuckets` must be set before cloud references are usable.
- Bucket/prefix outside the allow-list is rejected before provider calls.
- Credentials, public URLs, signed URLs and file contents are not persisted.
- The technical job snapshot stores `assetKey`, provider, bucket, object key, checksum, version/generation, size and content type.

## V1 limits

Recommended defaults:

- `MaxAssetsPerJob`: 10000.
- `MaxVideosPerJob`: 100 for cloud references, 5 for multipart.
- `MaxImagesPerJob`: 10000 for cloud references, 1000 for multipart.
- `MaxTargetsPerJob`: 200.
- `MaxConcurrentJobsPerClient`: 5.
- `MaxConcurrentProviderCallsPerClient`: 10.
- `MaxVideoDurationSeconds`: 14400.
- `EstimatedCostGuardrail`: reserved for a future phase.
