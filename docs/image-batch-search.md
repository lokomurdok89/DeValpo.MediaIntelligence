# Image Batch Search

`image-batch-search-v1` permite subir un lote de imagenes y buscar multiples `targetKey` por numero visible o por imagen de referencia facial. `targetKey` es el nombre tecnico canonico; si negocio habla de `dataKey`, se trata como sinonimo funcional, no como campo del contrato.

## Capacidades V1

| Provider | VisibleNumber | FaceReference |
| --- | --- | --- |
| AWS Rekognition | Si, con `DetectText`. | Si, con collection temporal, `SearchFacesByImage` y cleanup de `FaceId`. |
| Google Vision | Si, con OCR. | No. Devuelve issue `Unsupported` por target. |

V1 devuelve lista de imagenes por target, no segmentos temporales. No persiste `FaceId`, rutas S3/GCS completas, credenciales ni OCR completo fuera del texto minimo que justifica un match.

## Endpoints

```text
POST /api/v1/image-batch-search-jobs
GET  /api/v1/image-batch-search-jobs/{jobId}
GET  /api/v1/image-batch-search-jobs/{jobId}/result
GET  /api/v1/providers/google-vision/status
```

`POST` usa `multipart/form-data`. En desarrollo puede enviar archivos:

```text
clientKey=generic
profileKey=image-batch-search-v1
manifestJson={"targets":[{"targetKey":"bib-123","queryType":"VisibleNumber","visibleNumber":"123"},{"targetKey":"participant-001","queryType":"FaceReference","referenceImageKey":"reference-1"}]}
images=@photo-1.jpg
images=@photo-2.jpg
referenceImages=@face.jpg
```

Las imagenes fuente se numeran como `image-1`, `image-2`, etc. Las imagenes de referencia se numeran como `reference-1`, `reference-2`, etc.

En produccion puede enviar referencias cloud sin subir archivos por la API:

```text
assetSelectorJson={"selectorMode":"AllPrefix","storageProvider":"S3","bucketName":"event-bucket","prefix":"events/race/photos/","region":"us-east-1"}
referenceAssetSelectorJson={"selectorMode":"ExplicitAssets","storageProvider":"S3","bucketName":"event-bucket","assets":[{"assetKey":"reference-1","objectKey":"events/race/selfies/person-1.jpg","contentType":"image/jpeg"}]}
```

`assetSelectorJson` tambien soporta `ExplicitAssets` para listas cerradas de imagenes. No se aceptan URLs publicas ni signed URLs.

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

`providers` es opcional. Si no viene, se usan los providers habilitados y compatibles. El manifest rechaza campos personales como `name`, `rut`, `email`, `phone`, `personId` y `userId`.

## Resultado

La respuesta se agrupa por `targetKey`:

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

## Configuracion

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
```

Las credenciales se montan externamente. No se deben commitear archivos de credenciales, access keys ni secrets.
