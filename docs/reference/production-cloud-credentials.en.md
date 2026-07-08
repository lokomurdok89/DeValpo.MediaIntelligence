# Production Cloud Credentials

This guide defines how `DeValpo.MediaIntelligence` should handle cloud credentials, keys and runtime identities when moving from local development to staging or production. It does not replace the customer's security policy; it sets the minimum operating standard so local keys, human users and repository secrets do not reach production.

## Principles

- External clients never receive AWS or Google credentials. They only use MediaIntelligence API keys and JWTs.
- Cloud credentials belong to the API runtime, not to the human deployer.
- Production should prefer managed identities, roles or federation over long-lived keys.
- If a key must exist, it lives in the environment secret manager, not in `deploy/.env`, `secrets/`, GitHub, logs or tickets.
- Each environment uses separate identities, buckets, processors and collections.
- Each identity has minimum permissions for the exact resources used by the app.
- Rotation and revocation must be documented before real traffic is enabled.

## Privacy, Consent And Legal Review

The system can process images, videos, faces and biometric references. Before production, the customer must complete a legal and privacy review covering legal basis, consent, retention, deletion rights, auditability and processing region. This document does not replace that review; it defines minimum technical controls for lower-risk operation.

Minimum controls:

- Do not use real data without documented consent or authorization.
- Do not mix development, staging and production datasets.
- Do not expose images, videos, face IDs, job IDs or personal-data results in public logs, tickets or unapproved channels.
- Define maximum retention for staged objects, results and face references.
- Keep traceability for credential creation, rotation and revocation, and for biometric searches.
- Validate that buckets, processors and collections live in customer-approved regions.

## Identity Separation

| Identity | Correct use | Must not be used for |
| --- | --- | --- |
| Human cloud user | Manage IAM, billing and cloud resources | Running the API or being configured in containers |
| Google service account / workload identity | Call Document AI, Video Intelligence, Vision and GCS | External client authentication |
| AWS IAM role / runtime IAM user | Call S3 and Rekognition | External client authentication |
| MediaIntelligence API key | Exchange for a client JWT at `/api/v1/auth/token` | Direct AWS/GCP access |
| MediaIntelligence JWT | Call protected API endpoints | Calling cloud services |

## Google Cloud

Use one service account per environment or Workload Identity Federation when the runtime does not run in Google Cloud. Avoid user ADC and avoid long-lived service account keys when the runtime can receive managed identity.

Production values:

```text
GoogleDocumentAi__AllowUserCredentials=false
GoogleVideoIntelligence__AllowUserCredentials=false
GoogleVision__AllowUserCredentials=false
CloudAssets__AllowedGcsBuckets__0=<allowed-gcs-bucket>
```

If the runtime uses managed identity and does not need a file, do not mount `CredentialsPath`; the provider should resolve Application Default Credentials from the environment.

Minimum Google permissions should be assigned on concrete resources whenever possible:

| Flow | Resource | Expected permissions |
| --- | --- | --- |
| Document OCR | Document AI processor | Run processor and read processor metadata for remote status |
| Video target search | GCS staging bucket | Read/write objects used by Video Intelligence |
| Video target search | Video Intelligence | Run video annotation |
| Image batch OCR | Vision API | Run image OCR |
| GCS cloud assets | Allowed buckets | Read/list objects only in authorized buckets/prefixes |

## AWS

In AWS, use the workload IAM role: ECS task role, EC2 instance profile, Lambda role or equivalent. Avoid IAM user access keys in production.

Production values:

```text
AwsRekognition__AllowUserCredentials=false
CloudAssets__AllowedS3Buckets__0=<private-s3-bucket>
```

Do not configure `AwsRekognition__CredentialsPath` or `AwsRekognition__ProfileName` in production when the runtime has a role.

Minimum AWS permissions:

- S3 bucket: `s3:GetBucketLocation`, `s3:ListBucket`.
- S3 objects: `s3:GetObject`, `s3:PutObject`.
- Rekognition collection: `DescribeCollection`, `IndexFaces`, `DeleteFaces`, `SearchFacesByImage`, `DetectText`.
- Rekognition video jobs: `StartFaceSearch`, `GetFaceSearch`.

For the tested video flow, `StartFaceSearch` and `GetFaceSearch` required `Resource: "*"` because Rekognition job permissions are not scoped only to the collection ARN in this path. Keep `Resource: "*"` limited to those actions only.

## MediaIntelligence API Keys

MediaIntelligence API keys are not cloud keys. They authenticate clients to this API.

- The raw API key is shown once at create or rotate time.
- The database stores a hash, not the raw key.
- The client exchanges the API key for a JWT at `/api/v1/auth/token`.
- JWTs must include `client_key` and `media-intelligence:client`.
- `allowedProfileKeys` defines which flows each client may run.

## Production Checklist

```text
[ ] No root user access keys.
[ ] User ADC is disabled outside Development.
[ ] No real credentials in Git, `deploy/.env`, logs or tickets.
[ ] Each environment has its own service account/role/bucket/collection.
[ ] `CloudAssets__AllowedS3Buckets` and `CloudAssets__AllowedGcsBuckets` contain approved buckets only.
[ ] `/health` is OK.
[ ] `/ready` is OK or disabled providers are explicitly documented.
[ ] Remote Google status passes for enabled providers.
[ ] Remote AWS status passes for bucket and collection.
[ ] A real E2E test passes with a non-sensitive file.
[ ] Secret scan passes.
[ ] `validate-release-config` passes.
```

## Official References

- Google Application Default Credentials: <https://docs.cloud.google.com/docs/authentication/provide-credentials-adc>
- Google service account best practices: <https://docs.cloud.google.com/iam/docs/best-practices-service-accounts>
- Google Workload Identity Federation: <https://docs.cloud.google.com/iam/docs/workload-identity-federation>
- AWS IAM best practices: <https://docs.aws.amazon.com/IAM/latest/UserGuide/best-practices.html>
- AWS access keys for IAM users: <https://docs.aws.amazon.com/IAM/latest/UserGuide/id_credentials_access-keys.html>
- AWS IAM roles: <https://docs.aws.amazon.com/IAM/latest/UserGuide/id_roles.html>
- AWS Rekognition actions, resources and condition keys: <https://docs.aws.amazon.com/service-authorization/latest/reference/list_amazonrekognition.html>
