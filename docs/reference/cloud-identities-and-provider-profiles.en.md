# Cloud Credentials, Users And Provider Profiles

This guide documents how Google Cloud and AWS identities are obtained and configured for `DeValpo.MediaIntelligence`, and how they connect to the API's internal profiles. It is the baseline for future integrations and production rollouts.

## Concepts That Must Not Be Mixed

| Concept | Where it lives | Purpose |
| --- | --- | --- |
| Human cloud user | Google Cloud Console / AWS Console | Administers cloud resources. It must not be embedded in the application. |
| Runtime identity | ADC, service account, IAM role or IAM user | Lets the API call Google/AWS. |
| CLI profile | `gcloud` config or AWS named profile | Lets operators validate from PowerShell. It is not a business profile. |
| MediaIntelligence profile | `generic-document-ocr-v1`, `image-batch-search-v1`, etc. | Defines which API workflow a client can execute. |
| DeValpo API client | `Clients` table + API key hash | Authorizes external clients against this API. |

Google/AWS own cloud permissions; MediaIntelligence owns business permissions and public API contracts.

## Official Sources

- Google Application Default Credentials: <https://docs.cloud.google.com/docs/authentication/provide-credentials-adc>
- Google ADC for local development: <https://docs.cloud.google.com/docs/authentication/set-up-adc-local-dev-environment>
- Google service accounts: <https://docs.cloud.google.com/iam/docs/service-accounts-create>
- Google service account best practices: <https://docs.cloud.google.com/iam/docs/best-practices-service-accounts>
- AWS IAM best practices: <https://docs.aws.amazon.com/IAM/latest/UserGuide/best-practices.html>
- AWS access keys for IAM users: <https://docs.aws.amazon.com/IAM/latest/UserGuide/id_credentials_access-keys.html>
- AWS CLI named profiles: <https://docs.aws.amazon.com/cli/latest/reference/configure/>
- AWS shared config and credentials files: <https://docs.aws.amazon.com/sdkref/latest/guide/file-format.html>

## Internal MediaIntelligence Profiles

These are product profiles, not cloud profiles:

| ProfileKey | Asset type | Providers |
| --- | --- | --- |
| `generic-document-ocr-v1` | Document | Google Document AI |
| `invoice-extraction-v1` | Document | Google Document AI |
| `event-video-target-search-v1` | Video | Google Video Intelligence |
| `event-face-search-v1` | Video + Image | AWS Rekognition |
| `image-batch-search-v1` | Image | AWS Rekognition + Google Vision |

Access is controlled by `clientKey`, JWT and `allowedProfileKeys`. External clients do not receive direct cloud permissions.

## Google Cloud

For local development, use ADC generated with `gcloud auth application-default login` and mount it as `secrets/google-application-default-credentials.json`. In staging/production, prefer service accounts, managed workload identity, or Workload Identity Federation. Keep `GoogleDocumentAi__AllowUserCredentials=false`, `GoogleVideoIntelligence__AllowUserCredentials=false` and `GoogleVision__AllowUserCredentials=false` outside Development.

Required APIs depend on enabled providers:

| Service | When used | API |
| --- | --- | --- |
| Document AI | Document/PDF OCR | `documentai.googleapis.com` |
| Video Intelligence | Video OCR and generic face detection | `videointelligence.googleapis.com` |
| Vision | Image batch OCR | `vision.googleapis.com` |
| Cloud Storage | Video Intelligence staging | `storage.googleapis.com` |

Local ADC setup:

```powershell
gcloud auth login
gcloud config set project <google-cloud-project-id>
gcloud auth application-default login --no-launch-browser
gcloud auth application-default set-quota-project <google-cloud-project-id>
New-Item -ItemType Directory -Force secrets
Copy-Item "$env:APPDATA\gcloud\application_default_credentials.json" `
  secrets\google-application-default-credentials.json `
  -Force
```

Docker settings:

```text
GoogleDocumentAi__CredentialsPath=/run/secrets/google-application-default-credentials.json
GoogleVideoIntelligence__CredentialsPath=/run/secrets/google-application-default-credentials.json
GoogleVision__CredentialsPath=/run/secrets/google-application-default-credentials.json
```

Validate providers:

```powershell
curl.exe "http://localhost:8085/api/v1/providers/google-document-ai/status?checkRemote=true" -H "Authorization: Bearer $token"
curl.exe "http://localhost:8085/api/v1/providers/google-video-intelligence/status?checkRemote=true" -H "Authorization: Bearer $token"
curl.exe "http://localhost:8085/api/v1/providers/google-vision/status?checkRemote=true" -H "Authorization: Bearer $token"
```

## AWS

For local development, use a dedicated IAM user with a restricted access key and a named AWS CLI profile. For production on AWS, prefer an IAM role for the workload. Do not create root user access keys.

Local identity pattern:

```text
IAM user: devalpo-mediaintelligence-dev
IAM group: devalpo-mediaintelligence-dev-aws-rekognition
AWS named profile: devalpo-mediaintelligence-dev
Region: us-east-1
S3 bucket: devalpo-mediaintelligence-dev-aws-rekognition-<account-id>
Rekognition collection: devalpo-mediaintelligence-dev-face-search
```

Configure the CLI:

```powershell
aws configure --profile devalpo-mediaintelligence-dev
aws sts get-caller-identity --profile devalpo-mediaintelligence-dev
New-Item -ItemType Directory -Force secrets
Copy-Item "$env:USERPROFILE\.aws\credentials" secrets\aws-credentials -Force
```

Runtime settings:

```text
AwsRekognition__Enabled=true
AwsRekognition__FaceSearchEnabled=true
AwsRekognition__Region=us-east-1
AwsRekognition__BucketName=devalpo-mediaintelligence-dev-aws-rekognition-<account-id>
AwsRekognition__CollectionId=devalpo-mediaintelligence-dev-face-search
AwsRekognition__CredentialsPath=/run/secrets/aws-credentials
AwsRekognition__ProfileName=devalpo-mediaintelligence-dev
AwsRekognition__AllowUserCredentials=true
AwsRekognition__UsePolling=true
```

Set `AwsRekognition__AllowUserCredentials=false` outside Development.

Minimum runtime actions:

```text
s3:GetBucketLocation
s3:PutObject
s3:GetObject
rekognition:DescribeCollection
rekognition:IndexFaces
rekognition:DeleteFaces
rekognition:ListFaces
rekognition:StartFaceSearch
rekognition:GetFaceSearch
rekognition:SearchFacesByImage
rekognition:DetectText
```

Setup-only actions:

```text
s3:CreateBucket
rekognition:CreateCollection
```

Validate:

```powershell
aws s3api get-bucket-location --bucket <bucket> --profile devalpo-mediaintelligence-dev
aws rekognition describe-collection --collection-id <collection> --region us-east-1 --profile devalpo-mediaintelligence-dev
curl.exe "http://localhost:8085/api/v1/providers/aws-rekognition/status?checkRemote=true" -H "Authorization: Bearer $token"
```

## MediaIntelligence API Clients

External clients never receive Google/AWS credentials. They use:

1. `POST /api/v1/admin/clients` to create a client and one-time API key.
2. `POST /api/v1/auth/token` to exchange that API key for a JWT.
3. `Authorization: Bearer <token>` to call functional endpoints.

`allowedProfileKeys` controls which internal workflows the client can run.
