# Real Video MVP Route

This route separates the MVP into two parts. Part 1 is ready for a real Google test. Part 2 is defined for Amazon Rekognition without fake endpoints or simulated results.

## Part 1: Google Video Intelligence

Goal: upload one or more real videos, detect visible numbers, detect generic faces, and return time ranges.

Current state:

| Piece | State |
| --- | --- |
| `POST /api/v1/video-search-jobs` | Implemented |
| `GET /api/v1/video-search-jobs/{jobId}` | Implemented |
| `GET /api/v1/video-search-jobs/{jobId}/result` | Implemented |
| `GET /api/v1/providers/google-video-intelligence/status` | Implemented |
| Video worker | Implemented |
| Google Cloud Storage staging | Implemented |
| `TEXT_DETECTION` | Implemented |
| Generic `FACE_DETECTION` | Implemented |
| Face recognition | Not included in Part 1 |

## What Is Missing For A Real Google Test

1. Create a private Google Cloud Storage bucket.
2. Enable `videointelligence.googleapis.com` and `storage.googleapis.com`.
3. Grant permissions to the credential used by Docker.
4. Create local `deploy/.env` with `GoogleVideoIntelligence__Enabled=true`.
5. Start Docker Compose.
6. Submit a real video job.
7. Poll status and read the result.

## Google Configuration

```powershell
$projectId = "<google-project-id>"
$bucketName = "<unique-video-staging-bucket>"
$location = "us"

gcloud services enable videointelligence.googleapis.com storage.googleapis.com --project $projectId
gcloud storage buckets create "gs://$bucketName" --project $projectId --location $location --uniform-bucket-level-access
```

Create `deploy/.env` locally:

```env
GoogleVideoIntelligence__Enabled=true
GoogleVideoIntelligence__CredentialsPath=/run/secrets/google-application-default-credentials.json
GoogleVideoIntelligence__BucketName=<unique-video-staging-bucket>
GoogleVideoIntelligence__AllowUserCredentials=true

Skills__EnableMockSkills=false
AnalysisProfiles__EnableMockProfiles=false

VideoAnalysis__FaceMatchingEnabled=false
```

## Real Test

```powershell
docker compose -f deploy\docker-compose.yml up --build
```

```powershell
$tokenJson = curl.exe -s -X POST http://localhost:8085/api/v1/dev/token `
  -H "Content-Type: application/json" `
  -d "{}"

$token = ($tokenJson | ConvertFrom-Json).accessToken
```

```powershell
curl.exe "http://localhost:8085/api/v1/providers/google-video-intelligence/status?checkRemote=true" `
  -H "Authorization: Bearer $token"
```

Submit a real video:

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
    }
  ]
}'

$response = curl.exe -s -X POST http://localhost:8085/api/v1/video-search-jobs `
  -H "Authorization: Bearer $token" `
  -F "clientKey=generic" `
  -F "profileKey=event-video-target-search-v1" `
  -F "manifestJson=$manifest" `
  -F "videos=@<absolute-video-path>"

$jobId = ($response | ConvertFrom-Json).jobId
```

```powershell
curl.exe "http://localhost:8085/api/v1/video-search-jobs/$jobId" `
  -H "Authorization: Bearer $token"

curl.exe "http://localhost:8085/api/v1/video-search-jobs/$jobId/result" `
  -H "Authorization: Bearer $token"
```

## Part 2: Amazon Rekognition Face Search

Goal: search reference faces inside videos and return timestamps. This must be implemented with a real AWS provider only.

Future endpoints:

```http
POST /api/v1/face-search-jobs
GET /api/v1/face-search-jobs/{jobId}
GET /api/v1/face-search-jobs/{jobId}/result
GET /api/v1/providers/aws-rekognition/status
```

Rules:

- No fake AWS endpoints.
- No face indexing without `consentReferenceId`.
- No personal names, national IDs, emails, or user identifiers.
- Face matching disabled by default.
- Google `VisibleNumber` matches and AWS `FaceReference` matches stay separated in the result model.

## Real Smoke Test Evidence

Local smoke test validated on 2026-07-07:

| Item | Result |
| --- | --- |
| Provider | Google Video Intelligence |
| Real video | 774.23 MB |
| Video count | 1 |
| Remote GCS bucket | Reachable |
| Final job status | `Completed` |
| Attempts | 1 |
| Approximate duration | 24 minutes 50 seconds |

The full result was exported locally for functional review, but it must not be committed because it may contain data derived from a real video.
