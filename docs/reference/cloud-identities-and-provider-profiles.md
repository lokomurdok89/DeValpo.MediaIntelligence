# Credenciales, Usuarios Y Perfiles Cloud

Esta guia documenta como se obtienen y configuran las identidades de Google Cloud y AWS usadas por `DeValpo.MediaIntelligence`, y como se conectan con los perfiles internos de la API. Sirve como base para futuras integraciones y puestas en produccion.

## Conceptos Que No Deben Mezclarse

| Concepto | Donde vive | Uso |
| --- | --- | --- |
| Usuario humano cloud | Google Cloud Console / AWS Console | Administra recursos cloud. No debe quedar embebido en la aplicacion. |
| Identidad runtime | ADC, service account, IAM role o IAM user | Permite que la API llame a Google/AWS. |
| Perfil CLI | `gcloud` config o AWS named profile | Permite validar desde PowerShell. No es un perfil de negocio. |
| Perfil MediaIntelligence | `generic-document-ocr-v1`, `image-batch-search-v1`, etc. | Define que flujo de la API puede ejecutar un cliente. |
| API client DeValpo | Tabla `Clients` + API key hash | Autoriza clientes externos contra esta API. |

Regla practica: Google/AWS resuelven permisos cloud; MediaIntelligence resuelve permisos de negocio y contratos API.

## Fuentes Oficiales

- Google Application Default Credentials: <https://docs.cloud.google.com/docs/authentication/provide-credentials-adc>
- Google ADC en desarrollo local: <https://docs.cloud.google.com/docs/authentication/set-up-adc-local-dev-environment>
- Google service accounts: <https://docs.cloud.google.com/iam/docs/service-accounts-create>
- Google service account best practices: <https://docs.cloud.google.com/iam/docs/best-practices-service-accounts>
- AWS IAM best practices: <https://docs.aws.amazon.com/IAM/latest/UserGuide/best-practices.html>
- AWS access keys for IAM users: <https://docs.aws.amazon.com/IAM/latest/UserGuide/id_credentials_access-keys.html>
- AWS CLI named profiles: <https://docs.aws.amazon.com/cli/latest/reference/configure/>
- AWS shared config and credentials files: <https://docs.aws.amazon.com/sdkref/latest/guide/file-format.html>

## Perfiles Internos De MediaIntelligence

Estos son perfiles del producto, no perfiles cloud:

| ProfileKey | Asset type | Providers usados |
| --- | --- | --- |
| `generic-document-ocr-v1` | Document | Google Document AI |
| `invoice-extraction-v1` | Document | Google Document AI |
| `event-video-target-search-v1` | Video | Google Video Intelligence |
| `event-face-search-v1` | Video + Image | AWS Rekognition |
| `image-batch-search-v1` | Image | AWS Rekognition + Google Vision |

El acceso se controla por `clientKey`, JWT y `allowedProfileKeys`. Un cliente externo no recibe permisos cloud directos.

## Google Cloud

### Identidades Usadas

| Escenario | Identidad recomendada | Archivo local |
| --- | --- | --- |
| Desarrollo local | ADC de usuario con `gcloud auth application-default login` | `secrets/google-application-default-credentials.json` |
| Staging/produccion fuera de Google Cloud | Workload Identity Federation o service account con key solo si no hay alternativa | Montaje externo fuera del repo |
| Produccion dentro de Google Cloud | Service account asignada al workload | Sin archivo de credenciales |

Google diferencia el login del CLI de las credenciales ADC. El login normal de `gcloud` sirve para comandos manuales; la app .NET usa ADC o el archivo configurado en `Google*__CredentialsPath`.

### APIs Google Que Puede Requerir El Proyecto

| Servicio | Cuando se usa | API |
| --- | --- | --- |
| Document AI | OCR de documentos/PDF | `documentai.googleapis.com` |
| Video Intelligence | OCR y deteccion generica de caras en video | `videointelligence.googleapis.com` |
| Vision | OCR de lotes de imagenes | `vision.googleapis.com` |
| Cloud Storage | Staging para Video Intelligence | `storage.googleapis.com` |

### Crear ADC Para Desarrollo Local

```powershell
gcloud auth login
gcloud config set project <google-cloud-project-id>
gcloud auth application-default login --no-launch-browser
gcloud auth application-default set-quota-project <google-cloud-project-id>
```

Copiar la credencial al montaje local:

```powershell
New-Item -ItemType Directory -Force secrets
Copy-Item "$env:APPDATA\gcloud\application_default_credentials.json" `
  secrets\google-application-default-credentials.json `
  -Force
```

Validar:

```powershell
Test-Path secrets\google-application-default-credentials.json
```

Nunca imprimir ni commitear ese JSON.

### Configuracion Google En Docker

```text
GoogleDocumentAi__Enabled=true
GoogleDocumentAi__ProjectId=<google-cloud-project-id>
GoogleDocumentAi__Location=<document-ai-location>
GoogleDocumentAi__ProcessorId=<document-ai-processor-id>
GoogleDocumentAi__CredentialsPath=/run/secrets/google-application-default-credentials.json
GoogleDocumentAi__AllowUserCredentials=true

GoogleVideoIntelligence__Enabled=true
GoogleVideoIntelligence__CredentialsPath=/run/secrets/google-application-default-credentials.json
GoogleVideoIntelligence__BucketName=<gcs-staging-bucket>
GoogleVideoIntelligence__AllowUserCredentials=true

GoogleVision__Enabled=true
GoogleVision__CredentialsPath=/run/secrets/google-application-default-credentials.json
GoogleVision__AllowUserCredentials=true
```

`AllowUserCredentials=true` es solo para `Development`. En `Staging` y `Production` debe ser `false`.

### Validaciones Google

Status local:

```powershell
curl.exe http://localhost:8085/api/v1/providers/google-document-ai/status
curl.exe http://localhost:8085/api/v1/providers/google-video-intelligence/status
curl.exe http://localhost:8085/api/v1/providers/google-vision/status
```

Status remoto con token operator/admin:

```powershell
curl.exe "http://localhost:8085/api/v1/providers/google-document-ai/status?checkRemote=true" `
  -H "Authorization: Bearer $token"

curl.exe "http://localhost:8085/api/v1/providers/google-video-intelligence/status?checkRemote=true" `
  -H "Authorization: Bearer $token"

curl.exe "http://localhost:8085/api/v1/providers/google-vision/status?checkRemote=true" `
  -H "Authorization: Bearer $token"
```

### Produccion Google

Para produccion, preferir una identidad no humana:

1. Crear service account dedicada por ambiente.
2. Asignar permisos minimos al proyecto y recursos concretos.
3. Evitar service account keys si el runtime permite identidad administrada o Workload Identity Federation.
4. Si se usa key temporalmente, montarla desde secret manager/infra externa, nunca desde el repo.
5. Mantener `GoogleDocumentAi__AllowUserCredentials=false`, `GoogleVideoIntelligence__AllowUserCredentials=false` y `GoogleVision__AllowUserCredentials=false`.

Permisos minimos esperados:

| Servicio | Permisos funcionales |
| --- | --- |
| Document AI | Ejecutar processor OCR y leer metadata del processor para status remoto. |
| Video Intelligence | Ejecutar annotate video y acceder al bucket GCS de staging. |
| Vision | Ejecutar OCR de imagenes. |
| Storage | Subir/leer objetos del bucket de staging cuando aplica. |

## AWS

### Identidades Usadas

| Escenario | Identidad recomendada | Archivo local |
| --- | --- | --- |
| Desarrollo local | IAM user restringido + access key | `secrets/aws-credentials` |
| Staging/produccion en AWS | IAM role para el workload | Sin access key local |
| Produccion fuera de AWS | Secret externo o federation; evitar long-lived keys si existe alternativa | Montaje externo fuera del repo |

No crear access keys del root user. El root debe quedar para tareas de cuenta/billing/recuperacion, no para la aplicacion.

### Flujo Local Que Se Uso

1. Crear cuenta AWS.
2. Elegir plan segun necesidad de pruebas.
3. Instalar AWS CLI.
4. Crear IAM user dedicado:

```text
devalpo-mediaintelligence-dev
```

5. Crear IAM group:

```text
devalpo-mediaintelligence-dev-aws-rekognition
```

6. Asociar el usuario al grupo.
7. Crear policy restringida para S3 y Rekognition.
8. Crear access key del IAM user para CLI/local code.
9. Configurar named profile:

```powershell
$profile = "devalpo-mediaintelligence-dev"
$region = "us-east-1"
aws configure --profile $profile
```

10. Copiar credenciales al montaje local:

```powershell
New-Item -ItemType Directory -Force secrets
Copy-Item "$env:USERPROFILE\.aws\credentials" secrets\aws-credentials -Force
```

### AWS Named Profile

El perfil AWS es una seccion dentro de los archivos del CLI:

```text
%USERPROFILE%\.aws\credentials
%USERPROFILE%\.aws\config
```

La aplicacion usa:

```text
AwsRekognition__CredentialsPath=/run/secrets/aws-credentials
AwsRekognition__ProfileName=devalpo-mediaintelligence-dev
```

No poner `AWS Access Key ID` ni `AWS Secret Access Key` en `deploy/.env`.

### Recursos AWS Requeridos

| Recurso | Valor local usado como patron |
| --- | --- |
| Region | `us-east-1` |
| S3 bucket privado | `devalpo-mediaintelligence-dev-aws-rekognition-<account-id>` |
| Rekognition collection | `devalpo-mediaintelligence-dev-face-search` |
| IAM user | `devalpo-mediaintelligence-dev` |
| IAM group | `devalpo-mediaintelligence-dev-aws-rekognition` |

Crear bucket privado:

```powershell
$profile = "devalpo-mediaintelligence-dev"
$region = "us-east-1"
$bucket = "devalpo-mediaintelligence-dev-aws-rekognition-<account-id>"

aws s3api create-bucket `
  --bucket $bucket `
  --region $region `
  --profile $profile
```

Para `us-east-1`, AWS devuelve `LocationConstraint: null`; eso es normal.

Bloquear acceso publico:

```powershell
aws s3api put-public-access-block `
  --bucket $bucket `
  --public-access-block-configuration BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true `
  --profile $profile
```

Crear collection:

```powershell
$collection = "devalpo-mediaintelligence-dev-face-search"

aws rekognition create-collection `
  --collection-id $collection `
  --region $region `
  --profile $profile
```

Validar:

```powershell
aws rekognition describe-collection `
  --collection-id $collection `
  --region $region `
  --profile $profile
```

### Policy Minima AWS

La policy debe limitar acciones al bucket y collection usados por la app.

Acciones S3:

```text
s3:GetBucketLocation
s3:PutObject
s3:GetObject
```

Acciones Rekognition:

```text
rekognition:DescribeCollection
rekognition:IndexFaces
rekognition:DeleteFaces
rekognition:ListFaces
rekognition:StartFaceSearch
rekognition:GetFaceSearch
rekognition:SearchFacesByImage
rekognition:DetectText
```

Acciones administrativas que solo se necesitan durante setup:

```text
s3:CreateBucket
rekognition:CreateCollection
```

Despues de crear bucket y collection, estas acciones pueden salir de la policy runtime.

### Configuracion AWS En Docker

```text
AwsRekognition__Enabled=true
AwsRekognition__FaceSearchEnabled=true
AwsRekognition__Region=us-east-1
AwsRekognition__BucketName=devalpo-mediaintelligence-dev-aws-rekognition-<account-id>
AwsRekognition__CollectionId=devalpo-mediaintelligence-dev-face-search
AwsRekognition__CredentialsPath=/run/secrets/aws-credentials
AwsRekognition__ProfileName=devalpo-mediaintelligence-dev
AwsRekognition__AllowUserCredentials=true
AwsRekognition__MinSimilarity=90
AwsRekognition__UsePolling=true
AwsRekognition__DeleteStagedObjectsAfterProcessing=false
```

`AllowUserCredentials=true` es solo para `Development`. En `Staging` y `Production` debe ser `false`.

### Validaciones AWS

CLI:

```powershell
aws sts get-caller-identity --profile devalpo-mediaintelligence-dev
aws s3api get-bucket-location --bucket <bucket> --profile devalpo-mediaintelligence-dev
aws rekognition describe-collection --collection-id <collection> --region us-east-1 --profile devalpo-mediaintelligence-dev
```

API:

```powershell
curl.exe "http://localhost:8085/api/v1/providers/aws-rekognition/status?checkRemote=true" `
  -H "Authorization: Bearer $token"
```

Resultado esperado:

```text
AWS Rekognition S3 bucket and collection are reachable.
```

### Produccion AWS

Para produccion, preferir IAM role del runtime en vez de access keys:

1. Crear role por ambiente.
2. Asociar policy de menor privilegio al role.
3. Entregar el role al runtime: ECS task role, EC2 instance profile, Lambda role o equivalente.
4. No montar `secrets/aws-credentials` en produccion si el runtime tiene role.
5. Mantener `AwsRekognition__AllowUserCredentials=false`.
6. Mantener bucket privado, block public access y cleanup habilitado.

## MediaIntelligence API Clients

Los clientes externos no deben recibir credenciales Google/AWS. El flujo correcto es:

1. Admin crea client en `POST /api/v1/admin/clients`.
2. La API devuelve una API key una sola vez.
3. El cliente llama `POST /api/v1/auth/token`.
4. La API emite JWT.
5. El cliente usa `Authorization: Bearer <token>`.

Ejemplo local:

```powershell
$adminTokenResponse = Invoke-RestMethod `
  -Method Post `
  -Uri http://localhost:8085/api/v1/dev/token `
  -ContentType "application/json" `
  -Body (@{
    subject = "local-admin"
    clientKey = "admin"
    scope = "admin media-intelligence:admin"
  } | ConvertTo-Json -Compress)

$adminToken = $adminTokenResponse.accessToken
```

Crear client:

```powershell
$clientResponse = Invoke-RestMethod `
  -Method Post `
  -Uri http://localhost:8085/api/v1/admin/clients `
  -ContentType "application/json" `
  -Headers @{ Authorization = "Bearer $adminToken" } `
  -Body (@{
    clientKey = "generic"
    displayName = "Generic Local Client"
    contactEmail = "ops@example.com"
    allowedProfileKeys = @(
      "generic-document-ocr-v1",
      "event-video-target-search-v1",
      "event-face-search-v1",
      "image-batch-search-v1"
    )
    keyName = "primary"
    scope = "media-intelligence:client"
    expiresAt = $null
  } | ConvertTo-Json -Compress)
```

## Checklist De Puesta En Produccion

```text
[ ] Google service account o identidad administrada creada por ambiente
[ ] Google APIs habilitadas segun providers usados
[ ] Google user ADC deshabilitado fuera de Development
[ ] AWS root user sin access keys
[ ] AWS IAM role/user separado por ambiente
[ ] AWS policy runtime sin permisos setup innecesarios
[ ] S3 bucket privado con public access block
[ ] Rekognition collection creada
[ ] Credenciales montadas desde infraestructura externa, no desde repo
[ ] deploy/.env no versionado
[ ] secrets/* no versionado
[ ] allowedProfileKeys definidos por client
[ ] /status local y remoto OK para providers habilitados
[ ] secret scan OK
[ ] validate-release-config OK
```
