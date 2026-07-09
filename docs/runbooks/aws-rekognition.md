# AWS Rekognition Runbook

Esta guia deja AWS funcionando igual que Google: credenciales fuera del repo, montadas como secreto local y configuradas por variables.

Para el paso a produccion, permisos runtime y rotacion de llaves, ver [Credenciales cloud para produccion](../reference/production-cloud-credentials.md).

## Archivos

Seguro para versionar:

```text
deploy/.env.example
docs/runbooks/aws-rekognition.md
```

Nunca versionar:

```text
deploy/.env
secrets/aws-credentials
```

## Preparar Credenciales Locales

```powershell
aws configure --profile <profile-name>
Copy-Item "$env:USERPROFILE\.aws\credentials" secrets\aws-credentials
```

Configura `deploy/.env`:

```text
AwsRekognition__Enabled=true
AwsRekognition__FaceSearchEnabled=true
AwsRekognition__Region=<aws-region>
AwsRekognition__BucketName=<private-s3-bucket>
AwsRekognition__CollectionId=<rekognition-collection-id>
AwsRekognition__CredentialsPath=/run/secrets/aws-credentials
AwsRekognition__ProfileName=<profile-name>
AwsRekognition__AllowUserCredentials=true
AwsRekognition__MinSimilarity=90
AwsRekognition__UsePolling=true
AwsRekognition__PollingIntervalSeconds=5
AwsRekognition__PollingTimeoutSeconds=900
AwsRekognition__StagingPrefix=staging
AwsRekognition__DeleteStagedObjectsAfterProcessing=false
```

V1 usa polling obligatorio con `GetFaceSearch`; `SnsTopicArn` y `RoleArn` no son usados por el worker actual.

## Validar Recursos AWS

```powershell
aws sts get-caller-identity --profile <profile-name>
aws s3api get-bucket-location --bucket <private-s3-bucket> --profile <profile-name>
aws rekognition describe-collection --collection-id <rekognition-collection-id> --region <aws-region> --profile <profile-name>
```

Permisos minimos:

```text
s3:PutObject
s3:GetObject
s3:ListBucket
s3:GetBucketLocation
rekognition:DescribeCollection
rekognition:IndexFaces
rekognition:DeleteFaces
rekognition:StartFaceSearch
rekognition:GetFaceSearch
```

Nota: para el flujo de video `StartFaceSearch`/`GetFaceSearch`, AWS puede requerir `Resource: "*"` en esas acciones de job. Mantener ese wildcard solo para esas acciones y no extenderlo a S3 ni a permisos administrativos.

## Validar API

```powershell
curl.exe "http://localhost:8085/api/v1/providers/aws-rekognition/status?checkRemote=true" `
  -H "Authorization: Bearer $token"
```

## Crear Job Real

Manifest minimo:

```json
{
  "minSimilarity": 90,
  "targets": [
    {
      "targetKey": "participant-001",
      "referenceImageKey": "reference-1"
    }
  ]
}
```

Request:

```powershell
curl.exe -X POST http://localhost:8085/api/v1/face-search-jobs `
  -H "Authorization: Bearer $token" `
  -F "clientKey=<client-key>" `
  -F "profileKey=event-face-search-v1" `
  -F "consentReferenceId=<external-consent-reference>" `
  -F "manifestJson={\"minSimilarity\":90,\"targets\":[{\"targetKey\":\"participant-001\",\"referenceImageKey\":\"reference-1\"}]}" `
  -F "videos=@<path-to-video.mp4>" `
  -F "referenceImages=@<path-to-reference-face.jpg>"
```

Consultar:

```powershell
curl.exe "http://localhost:8085/api/v1/face-search-jobs/<jobId>/result" `
  -H "Authorization: Bearer $token"
```

## Reglas

- `targetKey` debe ser opaco.
- `consentReferenceId` es obligatorio y debe venir de un sistema externo.
- No poner nombre, RUT, email, telefono, userId ni personId en el manifest.
- El resultado no expone `FaceId`, access keys, bucket internals ni rutas de credenciales.
- La API no elimina objetos S3 staged al terminar. La limpieza principal debe hacerse con lifecycle policy del bucket y TTL corto.
- Las caras indexadas se eliminan al cerrar el job; no existe indice persistente ni revocacion posterior por `targetKey` en v1.
