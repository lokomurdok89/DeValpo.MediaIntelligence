# Primeros Pasos

Esta es la ruta corta para levantar el producto localmente y probar con un documento real.

## Requisitos

- .NET SDK 10.0.x.
- Docker Desktop.
- Google Cloud CLI.
- Acceso al proyecto y processor de Google Document AI.
- AWS CLI solo si vas a validar Rekognition real.

## 1. Preparar Variables Locales

```powershell
Copy-Item deploy\.env.example deploy\.env
```

Edita `deploy/.env` y configura:

```text
GoogleDocumentAi__Enabled=true
GoogleDocumentAi__ProjectId=<project-id>
GoogleDocumentAi__Location=<location>
GoogleDocumentAi__ProcessorId=<processor-id>
GoogleDocumentAi__CredentialsPath=/run/secrets/google-application-default-credentials.json
GoogleDocumentAi__AllowUserCredentials=true
Jwt__SigningKey=<local-dev-secret-min-32-chars>
```

AWS queda apagado por defecto. Para probar Rekognition real, configura tambien:

```text
AwsRekognition__Enabled=true
AwsRekognition__FaceSearchEnabled=true
AwsRekognition__Region=<aws-region>
AwsRekognition__BucketName=<private-s3-bucket>
AwsRekognition__CollectionId=<rekognition-collection-id>
AwsRekognition__CredentialsPath=/run/secrets/aws-credentials
AwsRekognition__ProfileName=default
AwsRekognition__AllowUserCredentials=true
AwsRekognition__UsePolling=true
```

## 2. Preparar Google ADC

```powershell
gcloud auth application-default login
gcloud auth application-default set-quota-project <project-id>
Copy-Item "$env:APPDATA\gcloud\application_default_credentials.json" secrets\google-application-default-credentials.json
```

Mas detalle: [tokens y secretos Google](local-tokens-and-google-secrets.md).

Ese documento explica desde instalar Google Cloud CLI hasta obtener la credencial ADC que usa esta API para llamar Document AI.

## 2.1. Preparar AWS Credentials Opcional

Solo si vas a probar face search real:

```powershell
aws configure --profile <profile-name>
Copy-Item "$env:USERPROFILE\.aws\credentials" secrets\aws-credentials
```

El archivo queda montado por Docker en `/run/secrets/aws-credentials`. No pegues access keys en `deploy/.env` ni las subas al repositorio.

## 3. Levantar Docker

```powershell
docker compose -f deploy\docker-compose.yml config
docker compose -f deploy\docker-compose.yml up --build
```

Servicios esperados:

- API: `http://localhost:8085`.
- Swagger: `http://localhost:8085/swagger`.
- PostgreSQL host port: `5435`.

## 4. Probar Salud

```powershell
curl.exe http://localhost:8085/health
curl.exe http://localhost:8085/ready
```

## 5. Generar Token Local

```powershell
$tokenJson = curl.exe -s -X POST http://localhost:8085/api/v1/dev/token `
  -H "Content-Type: application/json" `
  -d "{}"

$token = ($tokenJson | ConvertFrom-Json).accessToken
```

## 6. Validar Google

```powershell
curl.exe "http://localhost:8085/api/v1/providers/google-document-ai/status?checkRemote=true" `
  -H "Authorization: Bearer $token"
```

## 6.1. Validar AWS Opcional

```powershell
curl.exe "http://localhost:8085/api/v1/providers/aws-rekognition/status?checkRemote=true" `
  -H "Authorization: Bearer $token"
```

Resultado esperado cuando AWS esta bien configurado: `AWS Rekognition S3 bucket and collection are reachable.`

## 7. Subir Documento Real

```powershell
curl.exe -X POST http://localhost:8085/api/v1/analyze `
  -H "Authorization: Bearer $token" `
  -F "clientKey=generic" `
  -F "profileKey=generic-document-ocr-v1" `
  -F "assetType=Document" `
  -F "file=@<ruta-local-al-documento.pdf>"
```

Lee el `jobId` y consulta el resultado:

```powershell
curl.exe "http://localhost:8085/api/v1/analysis-jobs/<jobId>/result" `
  -H "Authorization: Bearer $token"
```
