# Ruta MVP Video Real

Esta ruta separa el MVP en dos partes. La Parte 1 queda lista para prueba real con Google. La Parte 2 queda definida para Amazon Rekognition sin simular endpoints ni resultados.

## Parte 1: Google Video Intelligence

Objetivo: subir uno o varios videos reales, detectar numeros visibles, detectar caras genericas y devolver rangos de tiempo.

Estado actual:

| Pieza | Estado |
| --- | --- |
| `POST /api/v1/video-search-jobs` | Implementado |
| `GET /api/v1/video-search-jobs/{jobId}` | Implementado |
| `GET /api/v1/video-search-jobs/{jobId}/result` | Implementado |
| `GET /api/v1/providers/google-video-intelligence/status` | Implementado |
| Worker de video | Implementado |
| Staging a Google Cloud Storage | Implementado |
| `TEXT_DETECTION` | Implementado |
| `FACE_DETECTION` generico | Implementado |
| Face recognition | No incluido en Parte 1 |

## Lo Que Falta Para Probar Google Real

1. Crear un bucket privado en Google Cloud Storage.
2. Habilitar `videointelligence.googleapis.com` y `storage.googleapis.com`.
3. Dar permisos a la credencial usada por Docker.
4. Crear `deploy/.env` local con `GoogleVideoIntelligence__Enabled=true`.
5. Levantar Docker Compose.
6. Ejecutar un job con un video real.
7. Consultar estado y resultado.

## Crear Bucket GCS

Usar un nombre unico:

```powershell
$projectId = "<google-project-id>"
$bucketName = "<unique-video-staging-bucket>"
$location = "us"

gcloud services enable videointelligence.googleapis.com storage.googleapis.com --project $projectId
gcloud storage buckets create "gs://$bucketName" --project $projectId --location $location --uniform-bucket-level-access
```

El bucket debe ser privado. No usar acceso publico.

## Permisos Minimos

La credencial usada por la API necesita:

| Recurso | Permiso |
| --- | --- |
| Bucket GCS | Subir y leer objetos |
| Video Intelligence | Ejecutar analisis de video |

Para una cuenta de servicio, asignar permisos equivalentes a:

```powershell
gcloud projects add-iam-policy-binding $projectId `
  --member="serviceAccount:<service-account-email>" `
  --role="roles/videointelligence.editor"

gcloud storage buckets add-iam-policy-binding "gs://$bucketName" `
  --member="serviceAccount:<service-account-email>" `
  --role="roles/storage.objectAdmin"
```

Para una prueba local con ADC de usuario, validar que el usuario tiene permisos sobre el proyecto y el bucket.

## Configuracion Local

Crear `deploy/.env` sin subirlo al repositorio:

```env
GoogleVideoIntelligence__Enabled=true
GoogleVideoIntelligence__CredentialsPath=/run/secrets/google-application-default-credentials.json
GoogleVideoIntelligence__BucketName=<unique-video-staging-bucket>
GoogleVideoIntelligence__AllowUserCredentials=true
GoogleVideoIntelligence__RemoteOperationTimeoutSeconds=7200

Skills__EnableMockSkills=false
AnalysisProfiles__EnableMockProfiles=false

VideoAnalysis__MaxVideosPerJob=5
VideoAnalysis__MaxVideoSizeBytes=1073741824
VideoAnalysis__MaxDurationSeconds=3600
VideoAnalysis__MaxProcessingAttempts=3
VideoAnalysis__DefaultMinConfidence=0.7
VideoAnalysis__DefaultMergeGapSeconds=2
VideoAnalysis__FaceMatchingEnabled=false
```

`GoogleVideoIntelligence__RemoteOperationTimeoutSeconds` evita que un job quede esperando a Google indefinidamente. Para videos grandes, partir con 7200 segundos; si se reduce para pruebas, el job terminara con issue de timeout cuando Google no responda dentro del limite.

El archivo de credenciales debe quedar fuera del repo:

```text
secrets/google-application-default-credentials.json
```

## Levantar El MVP

```powershell
docker compose -f deploy\docker-compose.yml up --build
```

Validar salud:

```powershell
curl.exe http://localhost:8085/health
```

Generar token local:

```powershell
$tokenJson = curl.exe -s -X POST http://localhost:8085/api/v1/dev/token `
  -H "Content-Type: application/json" `
  -d "{}"

$token = ($tokenJson | ConvertFrom-Json).accessToken
```

Validar Google Video local:

```powershell
curl.exe "http://localhost:8085/api/v1/providers/google-video-intelligence/status" `
  -H "Authorization: Bearer $token"
```

Validar bucket remoto:

```powershell
curl.exe "http://localhost:8085/api/v1/providers/google-video-intelligence/status?checkRemote=true" `
  -H "Authorization: Bearer $token"
```

Debe retornar `configured=true`.

## Ejecutar Job Real

Preparar manifest:

```powershell
$manifest = '{
  "detectFaces": true,
  "detectNumbers": true,
  "minConfidence": 0.7,
  "mergeGapSeconds": 2,
  "targets": [
    {
      "targetKey": "participant-001",
      "visibleNumber": "123"
    },
    {
      "targetKey": "participant-002",
      "visibleNumber": "456"
    }
  ]
}'
```

Enviar un video real:

```powershell
$response = curl.exe -s -X POST http://localhost:8085/api/v1/video-search-jobs `
  -H "Authorization: Bearer $token" `
  -F "clientKey=generic" `
  -F "profileKey=event-video-target-search-v1" `
  -F "manifestJson=$manifest" `
  -F "videos=@<absolute-video-path>"

$jobId = ($response | ConvertFrom-Json).jobId
$jobId
```

Consultar estado:

```powershell
curl.exe "http://localhost:8085/api/v1/video-search-jobs/$jobId" `
  -H "Authorization: Bearer $token"
```

Consultar resultado:

```powershell
curl.exe "http://localhost:8085/api/v1/video-search-jobs/$jobId/result" `
  -H "Authorization: Bearer $token"
```

## Resultado Esperado

`targetMatches` contiene rangos donde aparece un numero visible asociado a `targetKey`.

`faceOccurrences` contiene rangos donde hay caras visibles, sin identidad.

## Parte 2: Amazon Rekognition Face Search

Objetivo: buscar una cara de referencia dentro de videos y devolver timestamps. Esta parte no debe compartir endpoint con Google ni activar biometria por accidente.

Ruta tecnica propuesta:

| Pieza | Decision |
| --- | --- |
| Provider | `AwsRekognitionFaceVerificationProvider` |
| Interfaz | `IFaceVerificationProvider` |
| Storage | Amazon S3 |
| Analisis | `StartFaceSearch` / `GetFaceSearch` |
| Coleccion | Amazon Rekognition Collection |
| Activacion | `AwsRekognition__FaceSearchEnabled=true` |
| Default | Apagado |
| Consentimiento | Obligatorio |
| Datos personales | No se reciben nombres, RUT, email ni usuario |

Endpoints futuros, solo cuando el provider real este implementado:

```http
POST /api/v1/face-search-jobs
GET /api/v1/face-search-jobs/{jobId}
GET /api/v1/face-search-jobs/{jobId}/result
```

El status del provider ya existe para operar sin credenciales:

```http
GET /api/v1/providers/aws-rekognition/status
GET /api/v1/providers/aws-rekognition/status?checkRemote=true
```

Contrato minimo futuro:

```json
{
  "clientKey": "generic",
  "profileKey": "event-face-search-v1",
  "consentReferenceId": "consent-opaque-id",
  "targets": [
    {
      "targetKey": "participant-001"
    }
  ]
}
```

Las imagenes de referencia se enviaran como archivos, pero MediaIntelligence solo persistira claves opacas, auditoria y resultado normalizado.

## No Hacer Todavia

- No crear endpoints AWS que devuelvan respuestas simuladas.
- No guardar nombres ni datos personales.
- No indexar caras sin `consentReferenceId`.
- No mezclar `FaceReference` con `VisibleNumber`.
- No activar `FaceMatchingEnabled` por defecto.

## Criterio De MVP Terminado

La Parte 1 se considera MVP real cuando:

1. `status?checkRemote=true` retorna `configured=true`.
2. Un video real se sube al bucket GCS desde Docker.
3. Google Video Intelligence procesa el video.
4. El job cambia a `Completed`.
5. El resultado devuelve `targetMatches` por numero visible.
6. El resultado devuelve `faceOccurrences` si hay caras.
7. No hay mocks habilitados en el flujo probado.

## Evidencia De Smoke Test Real

Smoke test local validado el 2026-07-07:

| Item | Resultado |
| --- | --- |
| Provider | Google Video Intelligence |
| Video real | 774.23 MB |
| Cantidad de videos | 1 |
| Bucket GCS remoto | Alcanzable |
| Estado final del job | `Completed` |
| Intentos | 1 |
| Tiempo aproximado | 24 minutos 50 segundos |

El resultado completo fue exportado localmente para revision funcional, pero no debe subirse al repositorio porque puede contener informacion derivada de un video real.
