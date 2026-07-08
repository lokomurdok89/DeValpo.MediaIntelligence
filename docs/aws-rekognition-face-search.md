# AWS Rekognition Face Search

Esta pagina define la arquitectura y los requisitos de Fase 2 para busqueda facial en videos con Amazon Rekognition.

La Fase 2 no reemplaza Google Video Intelligence. Google sigue siendo el flujo base para OCR de numeros visibles y deteccion generica de caras. AWS Rekognition se agrega como provider separado para comparar imagenes de referencia contra caras detectadas en videos.

Para el paso a paso completo de cuenta AWS, IAM user, IAM group, named profile, access key local, bucket, collection y diferencias con produccion, ver [Credenciales, usuarios y perfiles cloud](reference/cloud-identities-and-provider-profiles.md).

Referencias oficiales:

- [Amazon Rekognition collections](https://docs.aws.amazon.com/rekognition/latest/dg/collections.html)
- [Amazon Rekognition Video stored video operations](https://docs.aws.amazon.com/rekognition/latest/dg/video.html)
- [StartFaceSearch API](https://docs.aws.amazon.com/rekognition/latest/APIReference/API_StartFaceSearch.html)
- [Configuring Amazon Rekognition Video](https://docs.aws.amazon.com/rekognition/latest/dg/api-video-roles.html)

## Estado Actual

Hoy la API puede operar sin credenciales AWS. Expone status local, mantiene AWS apagado por defecto y permite crear jobs faciales solo para el perfil `event-face-search-v1`; si AWS esta apagado, el job termina con issue de provider deshabilitado.

| Pieza | Estado |
| --- | --- |
| `GET /api/v1/providers/aws-rekognition/status` | Implementado |
| `GET /api/v1/providers/aws-rekognition/status?checkRemote=true` | Implementado, requiere JWT operator/admin |
| `POST /api/v1/face-search-jobs` | Implementado, requiere JWT y acceso al perfil |
| Worker AWS face search | Implementado |
| S3 staging real | Implementado, requiere credenciales reales |
| Rekognition collection real | Implementado, requiere credenciales reales |
| Resultados simulados | Prohibidos |

## Modo Sin Credenciales

Usar esta configuracion para levantar la API sin AWS:

```env
AwsRekognition__Enabled=false
AwsRekognition__FaceSearchEnabled=false
AwsRekognition__Region=
AwsRekognition__BucketName=
AwsRekognition__CollectionId=
AwsRekognition__CredentialsPath=
AwsRekognition__ProfileName=
AwsRekognition__AllowUserCredentials=false
AwsRekognition__MinSimilarity=90
AwsRekognition__UsePolling=true
AwsRekognition__PollingIntervalSeconds=5
AwsRekognition__PollingTimeoutSeconds=900
AwsRekognition__StagingPrefix=staging
AwsRekognition__DeleteStagedObjectsAfterProcessing=false
```

Validar status basico:

```powershell
curl.exe http://localhost:8085/api/v1/providers/aws-rekognition/status
```

Respuesta esperada:

```json
{
  "enabled": false,
  "configured": false,
  "credentialSourcePresent": false,
  "credentialFilePresent": false,
  "region": "",
  "bucketName": "",
  "collectionId": "",
  "profileName": "",
  "missingFields": [],
  "message": "AWS Rekognition is disabled by configuration."
}
```

## Arquitectura Separada

```mermaid
flowchart TB
    API["API Controllers"] --> App["Application Use Cases"]
    App --> GooglePort["IVideoSearchProvider"]
    App -. futuro .-> FacePort["IFaceSearchApplicationService"]
    GooglePort --> GoogleProvider["Infrastructure/Providers/Google/VideoIntelligence"]
    FacePort -. futuro .-> AwsProvider["Infrastructure/Providers/Aws/Rekognition"]
    GoogleProvider --> GCS["Google Cloud Storage"]
    GoogleProvider --> GVI["Google Video Intelligence"]
    AwsProvider -. futuro .-> S3["Amazon S3"]
    AwsProvider -. futuro .-> Rek["Amazon Rekognition"]
```

Reglas:

- Google procesa numeros visibles y caras genericas.
- AWS procesara comparacion facial contra imagenes de referencia.
- No se mezclan providers en la misma clase.
- No se comparten buckets ni credenciales entre providers.
- Application define contratos; Infrastructure habla con SDKs cloud.

## Objetivo Fase 2

Permitir que un cliente envie:

- uno o mas videos reales;
- una o mas imagenes de referencia;
- un `targetKey` opaco por participante;
- un `consentReferenceId` opcional emitido fuera de MediaIntelligence.

El sistema debe devolver rangos de tiempo donde AWS encuentra una cara similar a la referencia.

## Requisitos AWS

| Recurso | Uso |
| --- | --- |
| AWS Account | Cuenta activa con billing habilitado. |
| Region | Region unica para S3 y Rekognition. Recomendado inicial: `us-east-1`. |
| S3 bucket privado | Assets originales o staging temporal para videos e imagenes de referencia. |
| Rekognition collection | Contenedor temporal de vectores faciales para el job. |
| IAM user/role app | Permite subir a S3, indexar caras y consultar resultados. |

V1 usa polling con `GetFaceSearch`. SNS/SQS queda reservado para una fase posterior porque requiere infraestructura adicional.

## Permisos Minimos

```text
s3:PutObject
s3:GetObject
s3:GetBucketLocation
rekognition:CreateCollection
rekognition:DescribeCollection
rekognition:IndexFaces
rekognition:DeleteFaces
rekognition:ListFaces
rekognition:StartFaceSearch
rekognition:GetFaceSearch
```

## Configuracion Con AWS Real

Cuando existan credenciales y recursos reales:

```env
AwsRekognition__Enabled=true
AwsRekognition__FaceSearchEnabled=true
AwsRekognition__Region=<aws-region>
AwsRekognition__BucketName=<private-s3-bucket>
AwsRekognition__CollectionId=<rekognition-collection-id>
AwsRekognition__CredentialsPath=/run/secrets/aws-credentials
AwsRekognition__ProfileName=<profile-name>
AwsRekognition__MinSimilarity=90
AwsRekognition__UsePolling=true
```

No versionar access keys. Para desarrollo local usar AWS CLI profile o variables seguras fuera del repo. Para produccion usar roles administrados o secret manager.
`UsePolling=true` es obligatorio en v1; `SnsTopicArn` y `RoleArn` no son usados por el worker actual.

Validar remoto:

```powershell
curl.exe "http://localhost:8085/api/v1/providers/aws-rekognition/status?checkRemote=true" `
  -H "Authorization: Bearer $token"
```

El check remoto valida:

- que exista una fuente de credenciales AWS;
- que el bucket S3 sea alcanzable;
- que la collection Rekognition exista;
- que la credencial tenga permisos suficientes.

## Comandos De Preparacion

Validar AWS CLI:

```powershell
aws --version
aws sts get-caller-identity
```

Crear bucket privado:

```powershell
$region = "us-east-1"
$bucketName = "<private-s3-bucket>"

aws s3api create-bucket `
  --bucket $bucketName `
  --region $region

aws s3api put-public-access-block `
  --bucket $bucketName `
  --public-access-block-configuration BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true
```

Crear collection:

```powershell
$collectionId = "<rekognition-collection-id>"

aws rekognition create-collection `
  --collection-id $collectionId `
  --region $region
```

Validar collection:

```powershell
aws rekognition describe-collection `
  --collection-id $collectionId `
  --region $region
```

## Flujo Fase 2A Recomendado

```mermaid
sequenceDiagram
    participant Client as Cliente externo
    participant API as MediaIntelligence API
    participant DB as PostgreSQL
    participant S3 as Amazon S3
    participant Rek as AWS Rekognition
    participant Worker as FaceSearchWorker

    Client->>API: POST /face-search-jobs con multipart o assetSelectorJson
    API->>API: Valida JWT, manifest y referencias
    API->>S3: Usa assets existentes o stagea temporalmente
    API->>Rek: IndexFaces en collection
    API->>DB: Crea job Pending con targetKey y faceId
    Worker->>Rek: StartFaceSearch
    Worker->>Rek: Poll GetFaceSearch
    Rek-->>Worker: Coincidencias con timestamps
    Worker->>DB: Persiste FaceReference matches
    Client->>API: GET /face-search-jobs/{jobId}/result
    API-->>Client: targetKey, videoKey, rangos y similarity
```

## Contrato Futuro

Endpoints futuros, solo cuando el provider real este implementado:

```http
POST /api/v1/face-search-jobs
GET  /api/v1/face-search-jobs/{jobId}
GET  /api/v1/face-search-jobs/{jobId}/result
```

`POST /api/v1/face-search-jobs`:

```text
clientKey
profileKey=event-face-search-v1
consentReferenceId opcional
manifestJson
videos[]
referenceImages[]
assetSelectorJson opcional
referenceAssetSelectorJson opcional
```

Manifest minimo:

```json
{
  "minSimilarity": 90,
  "targets": [
    {
      "targetKey": "participant-001"
    }
  ]
}
```

Resultado esperado:

```json
{
  "jobId": "00000000-0000-0000-0000-000000000000",
  "status": "Completed",
  "faceMatches": [
    {
      "targetKey": "participant-001",
      "videoKey": "video-1",
      "startTimeSeconds": 83.2,
      "endTimeSeconds": 95.8,
      "similarity": 94.7,
      "matchType": "FaceReference",
      "sourceProvider": "AwsRekognition"
    }
  ]
}
```

## Reglas Obligatorias

- `consentReferenceId` opcional; si no viene, no bloquea procesamiento.
- `targetKey` opaco obligatorio.
- Rechazar `name`, `rut`, `email`, `phone`, `userId`, `personId`.
- `FaceSearchEnabled=false` por defecto.
- No mezclar resultados de Google `VisibleNumber` con AWS `FaceReference`.
- No guardar imagenes de referencia permanentemente salvo definicion explicita de retencion.
- Registrar auditoria de consentimiento, indexacion, busqueda, resultado leido y borrado.
- No borrar objetos staged desde el job; la limpieza de archivos depende de lifecycle policy del bucket con TTL corto.
- Borrar caras indexadas temporales con `DeleteFaces` despues de completar o fallar el job.
- La revocacion posterior por target/job queda fuera de v1 porque la API no persiste `FaceId`; si se define un indice persistente, esa fase debe agregar almacenamiento y borrado por consentimiento.
- No devolver coincidencias fake.

## QA Sin Credenciales

Checklist para validar que la API opera sin AWS:

```powershell
curl.exe http://localhost:8085/health
curl.exe http://localhost:8085/api/v1/providers/aws-rekognition/status
curl.exe -s -o NUL -w "%{http_code}" "http://localhost:8085/api/v1/providers/aws-rekognition/status?checkRemote=true"
curl.exe -s -o NUL -w "%{http_code}" -X POST "http://localhost:8085/api/v1/face-search-jobs" -H "Authorization: Bearer $token"
```

Resultado esperado:

| Validacion | Resultado |
| --- | --- |
| `/health` | `200` |
| status AWS basico | `enabled=false`, `configured=false` |
| `checkRemote=true` sin token | `401` |
| `checkRemote=true` con token sin scope operativo | `403` |
| `POST /api/v1/face-search-jobs` sin token | `401` |
| `POST /api/v1/face-search-jobs` con request invalido | `400` |

Si el status remoto se prueba con AWS habilitado, debe usarse una credencial real y recursos reales. No usar respuestas simuladas.

## Criterio Para Probar Jobs AWS Reales

Antes de ejecutar un smoke test real:

1. AWS CLI autentica correctamente.
2. S3 bucket privado creado.
3. Rekognition collection creada.
4. Permisos IAM definidos.
5. `UsePolling=true` configurado; SNS/SQS no aplica a v1.
6. Si existe `consentReferenceId`, se envia como referencia externa; si no, el job igual procesa.
7. Existe imagen real de referencia y video real para smoke test local.
