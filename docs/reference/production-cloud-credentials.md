# Credenciales Cloud Para Produccion

Esta guia define como deben manejarse las credenciales, llaves e identidades cloud de `DeValpo.MediaIntelligence` al pasar desde desarrollo local a staging o produccion. No reemplaza las politicas de seguridad del cliente; fija el minimo operativo para no llevar llaves locales, usuarios humanos ni secretos del repositorio a un ambiente productivo.

## Principios

- Los clientes externos nunca reciben credenciales AWS o Google. Solo usan API keys/JWT de MediaIntelligence.
- Las credenciales cloud pertenecen al runtime de la API, no al usuario humano que despliega.
- En produccion se prefieren identidades administradas, roles o federation antes que llaves de larga duracion.
- Si una llave debe existir, vive en el secret manager o mecanismo de secretos del ambiente, no en `deploy/.env`, `secrets/`, GitHub, logs ni tickets.
- Cada ambiente usa identidades, buckets, processors y collections separados.
- Cada identidad tiene permisos minimos para los recursos exactos que usa la app.
- La rotacion y revocacion deben estar documentadas antes de habilitar trafico real.

## Privacidad, Consentimiento Y Revision Legal

El sistema puede procesar imagenes, videos, caras y referencias biometricas. Antes de pasar a produccion debe existir una revision legal y de privacidad del cliente para confirmar base legal, consentimiento, retencion, derecho de eliminacion, auditoria y region de procesamiento. Este documento no reemplaza esa revision; define controles tecnicos minimos para operar con menor riesgo.

Controles minimos:

- No usar datos reales sin consentimiento o autorizacion documentada.
- No mezclar datasets de desarrollo, staging y produccion.
- No exponer imagenes, videos, face IDs, job IDs ni resultados con datos personales en logs publicos, tickets o canales no aprobados.
- Definir retencion maxima para objetos staged, resultados y referencias de rostro.
- Mantener trazabilidad de quien crea, rota o revoca credenciales y quien ejecuta busquedas biometricas.
- Validar que los buckets, processors y collections vivan en regiones aprobadas por el cliente.

## Separacion De Identidades

| Identidad | Uso correcto | No debe usarse para |
| --- | --- | --- |
| Usuario humano cloud | Administrar IAM, billing y recursos cloud | Ejecutar la API o quedar configurado en contenedores |
| Google service account / workload identity | Llamar Document AI, Video Intelligence, Vision y GCS | Autenticacion de clientes externos |
| AWS IAM role / IAM user runtime | Llamar S3 y Rekognition | Autenticacion de clientes externos |
| API key MediaIntelligence | Intercambiar por JWT de cliente en `/api/v1/auth/token` | Acceso directo a AWS/GCP |
| JWT MediaIntelligence | Llamar endpoints protegidos de la API | Llamar servicios cloud |

## Google Cloud

### Produccion Recomendada

Usar una service account dedicada por ambiente o Workload Identity Federation cuando el runtime no vive dentro de Google Cloud. Evitar user ADC y evitar service account keys de larga duracion cuando el runtime puede recibir identidad administrada.

Variables de runtime:

```text
GoogleDocumentAi__Enabled=true
GoogleDocumentAi__ProjectId=<project-id>
GoogleDocumentAi__Location=<location>
GoogleDocumentAi__ProcessorId=<processor-id>
GoogleDocumentAi__CredentialsPath=<managed-secret-mounted-path-if-needed>
GoogleDocumentAi__AllowUserCredentials=false

GoogleVideoIntelligence__Enabled=true
GoogleVideoIntelligence__BucketName=<gcs-staging-bucket>
GoogleVideoIntelligence__CredentialsPath=<managed-secret-mounted-path-if-needed>
GoogleVideoIntelligence__AllowUserCredentials=false

GoogleVision__Enabled=true
GoogleVision__CredentialsPath=<managed-secret-mounted-path-if-needed>
GoogleVision__AllowUserCredentials=false

CloudAssets__AllowedGcsBuckets__0=<allowed-gcs-bucket>
```

Si el runtime usa identidad administrada y no requiere archivo, no montar `CredentialsPath`; el proveedor debe resolver Application Default Credentials desde el ambiente.

### Permisos Minimos Google

Asignar permisos sobre recursos concretos, no sobre todo el proyecto si se puede evitar.

| Flujo | Recurso | Permisos esperados |
| --- | --- | --- |
| Document OCR | Processor Document AI | Ejecutar el processor y leer metadata para status remoto |
| Video target search | GCS staging bucket | Leer/escribir objetos usados por Video Intelligence |
| Video target search | Video Intelligence | Ejecutar annotate video |
| Image batch OCR | Vision API | Ejecutar OCR de imagenes |
| Cloud assets GCS | Buckets permitidos | Leer/listar objetos solo en buckets/prefixes autorizados |

En local se puede usar ADC de usuario para desarrollo. En staging/produccion debe quedar:

```text
GoogleDocumentAi__AllowUserCredentials=false
GoogleVideoIntelligence__AllowUserCredentials=false
GoogleVision__AllowUserCredentials=false
```

### Llaves Google

Si se usa una service account key porque no hay federation/identidad administrada:

1. Crear la key solo para el ambiente correspondiente.
2. Guardarla en el secret manager del ambiente.
3. Montarla como archivo de solo lectura en el contenedor.
4. No copiarla a `secrets/` salvo para desarrollo local.
5. Rotarla en una ventana controlada.
6. Revocar la version anterior cuando el nuevo despliegue este verificado.

## AWS

### Produccion Recomendada

En AWS, usar IAM role del workload: ECS task role, EC2 instance profile, Lambda role o equivalente. Evitar access keys de IAM users en produccion.

Variables de runtime:

```text
AwsRekognition__Enabled=true
AwsRekognition__FaceSearchEnabled=true
AwsRekognition__Region=<aws-region>
AwsRekognition__BucketName=<private-s3-bucket>
AwsRekognition__CollectionId=<rekognition-collection-id>
AwsRekognition__AllowUserCredentials=false
AwsRekognition__MinSimilarity=90
AwsRekognition__UsePolling=true
AwsRekognition__PollingIntervalSeconds=5
AwsRekognition__PollingTimeoutSeconds=900
AwsRekognition__DeleteStagedObjectsAfterProcessing=false

CloudAssets__AllowedS3Buckets__0=<private-s3-bucket>
```

No configurar `AwsRekognition__CredentialsPath` ni `AwsRekognition__ProfileName` en produccion si el runtime tiene role. Esos campos son para desarrollo local o ambientes donde un secret externo monta un credentials file temporal.

### Permisos Minimos AWS

Bucket:

```json
{
  "Effect": "Allow",
  "Action": [
    "s3:GetBucketLocation",
    "s3:ListBucket"
  ],
  "Resource": "arn:aws:s3:::<private-s3-bucket>"
}
```

Objetos:

```json
{
  "Effect": "Allow",
  "Action": [
    "s3:GetObject",
    "s3:PutObject"
  ],
  "Resource": "arn:aws:s3:::<private-s3-bucket>/*"
}
```

Collection:

```json
{
  "Effect": "Allow",
  "Action": [
    "rekognition:DescribeCollection",
    "rekognition:IndexFaces",
    "rekognition:DeleteFaces",
    "rekognition:SearchFacesByImage",
    "rekognition:DetectText"
  ],
  "Resource": "arn:aws:rekognition:<region>:<account-id>:collection/<collection-id>"
}
```

Video jobs de Rekognition requieren permisos de job. En AWS IAM estos permisos no siempre se pueden limitar al ARN de la collection; para el flujo probado se requiere:

```json
{
  "Effect": "Allow",
  "Action": [
    "rekognition:StartFaceSearch",
    "rekognition:GetFaceSearch"
  ],
  "Resource": "*"
}
```

Mantener `Resource: "*"` solo para esas acciones si AWS no permite un recurso mas especifico para el job. No ampliar `Resource: "*"` a S3 ni a acciones administrativas.

### Acciones Solo De Setup

Estas acciones pueden ser necesarias para crear infraestructura, pero no deben quedar en el role runtime despues del setup:

```text
s3:CreateBucket
s3:PutPublicAccessBlock
rekognition:CreateCollection
rekognition:DeleteCollection
iam:*
```

### Buckets S3

Todo bucket productivo debe tener:

- Block Public Access habilitado.
- Sin objetos publicos.
- Cifrado en reposo.
- Lifecycle policy para staging temporal.
- Logging/auditoria segun la plataforma del cliente.
- Separacion por ambiente.

El job no debe borrar objetos staged como mecanismo principal de limpieza. La limpieza productiva debe depender de lifecycle policy con TTL corto.

## MediaIntelligence API Keys

Las API keys de MediaIntelligence no son llaves cloud. Son credenciales de cliente para esta API.

Reglas:

- La API key cruda se muestra una sola vez al crear o rotar.
- La base guarda hash, no la key cruda.
- El cliente intercambia la API key por JWT en `/api/v1/auth/token`.
- Los JWT deben incluir `client_key` y scope `media-intelligence:client`.
- `allowedProfileKeys` define que flujos puede ejecutar cada cliente.
- Rotar o revocar una API key no debe afectar credenciales AWS/GCP.

## Rotacion

### Google

1. Crear nueva identidad/key o actualizar federation.
2. Montar el nuevo secreto en staging.
3. Validar `/ready` y status remoto del provider.
4. Promover a produccion.
5. Revocar la key anterior.
6. Registrar fecha, responsable y motivo.

### AWS

1. Si se usa role, actualizar la policy o trust relationship.
2. Si se usa access key temporal, crear una segunda key.
3. Montar el nuevo secret externo.
4. Validar `sts get-caller-identity`, S3 y Rekognition.
5. Desactivar la key antigua.
6. Eliminar la key antigua despues de ventana de rollback.

### API Clients

1. Crear nueva API key con `POST /api/v1/admin/clients/{clientKey}/api-keys/{apiKeyId}/rotate`.
2. Entregar la nueva key por canal seguro.
3. Validar `POST /api/v1/auth/token`.
4. Revocar la key anterior.

## Validacion Antes De Produccion

```text
[ ] No hay access keys del root user.
[ ] No hay user ADC habilitado fuera de Development.
[ ] No hay credenciales reales en Git, `deploy/.env`, logs o tickets.
[ ] Cada ambiente tiene service account/role/bucket/collection propios.
[ ] `CloudAssets__AllowedS3Buckets` y `CloudAssets__AllowedGcsBuckets` contienen solo buckets aprobados.
[ ] `/health` responde OK.
[ ] `/ready` responde OK o documenta providers deshabilitados explicitamente.
[ ] Status remoto Google pasa para providers habilitados.
[ ] Status remoto AWS pasa para bucket y collection.
[ ] Prueba real E2E pasa con un archivo no sensible.
[ ] Secret scan pasa.
[ ] `validate-release-config` pasa.
```

## Referencias Oficiales

- Google Application Default Credentials: <https://docs.cloud.google.com/docs/authentication/provide-credentials-adc>
- Google service account best practices: <https://docs.cloud.google.com/iam/docs/best-practices-service-accounts>
- Google Workload Identity Federation: <https://docs.cloud.google.com/iam/docs/workload-identity-federation>
- AWS IAM best practices: <https://docs.aws.amazon.com/IAM/latest/UserGuide/best-practices.html>
- AWS access keys for IAM users: <https://docs.aws.amazon.com/IAM/latest/UserGuide/id_credentials_access-keys.html>
- AWS IAM roles: <https://docs.aws.amazon.com/IAM/latest/UserGuide/id_roles.html>
- AWS Rekognition actions, resources and condition keys: <https://docs.aws.amazon.com/service-authorization/latest/reference/list_amazonrekognition.html>
