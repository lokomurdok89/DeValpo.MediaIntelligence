# Referencias cloud y staging temporal

MediaIntelligence soporta dos formas de entrada:

- **Multipart upload**: usado para desarrollo, smoke tests y compatibilidad.
- **Cloud references**: flujo productivo recomendado. La plataforma externa sube los archivos a S3 o Google Cloud Storage y MediaIntelligence recibe referencias.

La plataforma externa decide donde vive el original:

- Eventos, carreras, bodas, triatlones y busqueda facial: S3 + AWS Rekognition.
- Documentos, OCR y validacion de imagenes tipo Arenysmed: GCS + Google Document AI/Vision.

MediaIntelligence no recibe URLs publicas ni signed URLs. Usa credenciales propias configuradas por ambiente.

## assetSelectorJson

Los endpoints productivos pueden recibir `assetSelectorJson` como campo opcional de form. Si no viene, el endpoint conserva el modo multipart actual.

```json
{
  "selectorMode": "AllPrefix",
  "storageProvider": "S3",
  "bucketName": "devalpo-events-prod",
  "prefix": "events/race-2026/photos/",
  "region": "us-east-1"
}
```

Para assets explicitos:

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

Para busqueda facial tambien existe `referenceAssetSelectorJson`, que apunta a selfies o imagenes de referencia ya subidas por la plataforma.

## Staging temporal

El staging se usa solo cuando el motor requerido no puede procesar directo desde el storage original.

| Original | Motor | Resultado |
|---|---|---|
| S3 | AWS Rekognition | Procesa directo si el bucket coincide con la config AWS. |
| GCS | Google Vision/Video/Document AI | Procesa directo cuando el SDK lo soporta. |
| S3 | Google Video Intelligence | Staging temporal a GCS. |
| GCS | AWS Rekognition | Staging temporal a S3. |

El job no borra archivos staged al terminar. La limpieza principal es la lifecycle policy del bucket:

- TTL default: 3 dias.
- Rango permitido: 1 a 7 dias.
- Barrido mensual: respaldo operativo, no mecanismo principal.

AWS Rekognition sigue borrando `FaceId` temporales con `DeleteFaces`; eso es privacidad biometrica, no cleanup de archivos staged.

## Seguridad

- `CloudAssets:AllowedS3Buckets` y `CloudAssets:AllowedGcsBuckets` son obligatorios para habilitar referencias cloud.
- Bucket/prefix fuera de allow-list se rechaza antes de llamar a proveedores.
- No se persisten credenciales, URLs publicas, signed URLs ni contenido de archivos.
- El snapshot tecnico del job guarda `assetKey`, provider, bucket, object key, checksum, version/generation, tamano y content type.

## Limites v1

Defaults recomendados:

- `MaxAssetsPerJob`: 10000.
- `MaxVideosPerJob`: 100 para referencias cloud, 5 para multipart.
- `MaxImagesPerJob`: 10000 para referencias cloud, 1000 para multipart.
- `MaxTargetsPerJob`: 200.
- `MaxConcurrentJobsPerClient`: 5.
- `MaxConcurrentProviderCallsPerClient`: 10.
- `MaxVideoDurationSeconds`: 14400.
- `EstimatedCostGuardrail`: reservado para fase futura.
