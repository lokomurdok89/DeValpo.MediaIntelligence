# Image Batch Search

`image-batch-search-v1` uploads an image batch and searches multiple `targetKey` values by visible number or face reference image. `targetKey` is the canonical technical name; if business users say `dataKey`, treat it as a functional synonym, not a contract field.

## V1 Capabilities

| Provider | VisibleNumber | FaceReference |
| --- | --- | --- |
| AWS Rekognition | Yes, through `DetectText`. | Yes, through a temporary collection index, `SearchFacesByImage` and `FaceId` cleanup. |
| Google Vision | Yes, through OCR. | No. It returns an `Unsupported` issue per target. |

V1 returns image lists per target, not time ranges. It does not persist `FaceId`, full S3/GCS paths, credentials or full OCR content beyond the minimum text needed to justify a match.

## Endpoints

```text
POST /api/v1/image-batch-search-jobs
GET  /api/v1/image-batch-search-jobs/{jobId}
GET  /api/v1/image-batch-search-jobs/{jobId}/result
GET  /api/v1/providers/google-vision/status
```

`POST` uses `multipart/form-data`. Multipart remains available for development, smoke tests and compatibility:

```text
clientKey=generic
profileKey=image-batch-search-v1
manifestJson={"targets":[{"targetKey":"bib-123","queryType":"VisibleNumber","visibleNumber":"123"},{"targetKey":"participant-001","queryType":"FaceReference","referenceImageKey":"reference-1"}]}
images=@photo-1.jpg
images=@photo-2.jpg
referenceImages=@face.jpg
```

Source images are numbered as `image-1`, `image-2`, etc. Reference images are numbered as `reference-1`, `reference-2`, etc.

Production jobs can also use cloud references instead of uploading files through the API:

```text
clientKey=generic
profileKey=image-batch-search-v1
manifestJson={"targets":[{"targetKey":"bib-123","queryType":"VisibleNumber","visibleNumber":"123"},{"targetKey":"participant-001","queryType":"FaceReference","referenceImageKey":"reference-1"}]}
assetSelectorJson={"selectorMode":"AllPrefix","storageProvider":"S3","bucketName":"event-bucket","prefix":"events/race/photos/","region":"us-east-1"}
referenceAssetSelectorJson={"selectorMode":"ExplicitAssets","storageProvider":"S3","bucketName":"event-bucket","assets":[{"assetKey":"reference-1","objectKey":"events/race/selfies/person-1.jpg","contentType":"image/jpeg"}]}
```

`assetSelectorJson` also supports `ExplicitAssets` for closed image lists. Public URLs and signed URLs are not accepted.

## Manifest

```json
{
  "providers": ["AwsRekognition", "GoogleVision"],
  "targets": [
    {
      "targetKey": "bib-123",
      "queryType": "VisibleNumber",
      "visibleNumber": "123",
      "minConfidence": 0.8
    },
    {
      "targetKey": "participant-001",
      "queryType": "FaceReference",
      "referenceImageKey": "reference-1",
      "minSimilarity": 90
    }
  ]
}
```

`providers` is optional. When omitted, enabled compatible providers are used. The manifest rejects personal fields such as `name`, `rut`, `email`, `phone`, `personId` and `userId`.

## Result

The response is grouped by `targetKey`:

```json
{
  "jobId": "00000000-0000-0000-0000-000000000000",
  "clientKey": "generic",
  "status": "Completed",
  "results": [
    {
      "targetKey": "bib-123",
      "queryType": "VisibleNumber",
      "matches": [
        {
          "imageKey": "image-1",
          "fileName": "photo-1.jpg",
          "score": 0.91,
          "detectedText": "runner 123",
          "sourceProvider": "GoogleVision"
        }
      ]
    }
  ],
  "issues": []
}
```

## Configuration

```text
ImageBatchSearch__Enabled=true
ImageBatchSearch__MaxImagesPerJob=1000
ImageBatchSearch__MaxTargetsPerJob=200
ImageBatchSearch__MaxImageSizeBytes=104857600
ImageBatchSearch__AwsProviderEnabled=true
ImageBatchSearch__GoogleVisionProviderEnabled=true
GoogleVision__Enabled=true
GoogleVision__CredentialsPath=/run/secrets/google-application-default-credentials.json
GoogleVision__AllowUserCredentials=false
CloudAssets__AllowedS3Buckets__0=<event-bucket>
CloudAssets__AllowedGcsBuckets__0=<docs-bucket>
CloudAssets__DefaultStagingTtlDays=3
CloudAssets__MinStagingTtlDays=1
CloudAssets__MaxStagingTtlDays=7
CloudAssets__MaxAssetsPerJob=10000
CloudAssets__MaxConcurrentJobsPerClient=5
CloudAssets__MaxConcurrentProviderCallsPerClient=10
```

Credentials are mounted externally. Do not commit credential files, access keys or secrets.
