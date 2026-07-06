# DeValpo.MediaIntelligence

Reusable multi-client media and document intelligence engine.

This repository is intentionally separate from the Contador repository. It exposes public DTOs through `DeValpo.MediaIntelligence.Contracts` and keeps providers, storage and persistence behind Application abstractions.

## Requirements

- .NET SDK 10.0.x
- Docker Desktop
- PostgreSQL 17 when running outside Docker

## Architecture Overview

The system is a reusable media and document intelligence engine. The API receives assets, validates JWT access issued from client API keys, stores the input file, queues background analysis, persists the normalized result and exposes the job/result through HTTP endpoints.

### End-To-End Flow

```mermaid
flowchart TD
    A["Client app"] --> B["POST /api/v1/auth/token<br/>clientKey + API key"]
    B --> C["JWT accessToken"]
    C --> D["POST /api/v1/analyze<br/>Bearer token + multipart file"]
    D --> E["API validation<br/>file size, asset type, metadata"]
    E --> F["Create AnalysisJob<br/>status Pending"]
    F --> G["Save MediaAsset<br/>local Docker volume"]
    G --> H["Return 202 Accepted<br/>jobId"]
    H --> I["Background worker claims job<br/>status Running"]
    I --> J["Resolve analysis profile"]
    J --> K["Run profile skills"]
    K --> L{"Google Document AI enabled?"}
    L -- "yes" --> M["GoogleDocumentAiDocumentOcrSkill"]
    M --> N["Document AI OCR processor"]
    L -- "no or not configured" --> O["Return provider issue<br/>requires review when needed"]
    N --> P["SkillExecutionResult<br/>fields, labels, entities, issues"]
    O --> P
    P --> Q["Persist AnalysisResult<br/>fields, issues, confidence"]
    Q --> R{"Confidence or errors require review?"}
    R -- "yes" --> S["Create HumanReviewTask<br/>status RequiresReview"]
    R -- "no" --> T["Complete job"]
    S --> U["Client polls result endpoint"]
    T --> U
```

### Runtime Components

```mermaid
flowchart LR
    subgraph LocalDocker["Docker local"]
        Api["DeValpo.MediaIntelligence.Api<br/>ASP.NET Core"]
        Worker["AnalysisJobWorker<br/>background polling"]
        Pg["PostgreSQL 17"]
        Storage["media_assets volume<br/>/app/storage/media-assets"]
        Secrets["secrets bind mount<br/>/run/secrets:ro"]
    end

    subgraph AppLayer["Application layer"]
        AnalyzeSvc["MediaAnalysisApplicationService"]
        Orchestrator["MediaAnalysisOrchestrator"]
        ProfileResolver["IAnalysisProfileResolver"]
        Skills["IMediaAnalysisSkill implementations"]
    end

    subgraph InfraLayer["Infrastructure layer"]
        DbContext["MediaIntelligenceDbContext"]
        Repos["Repositories + UnitOfWork"]
        AssetStorage["LocalMediaAssetStorage"]
        GoogleCredentials["IGoogleCredentialProvider"]
        GoogleFactory["GoogleDocumentAiClientFactory"]
        GoogleStatus["GoogleDocumentAiStatusService"]
    end

    Google["Google Document AI<br/>OCR_PROCESSOR"]

    Api --> AnalyzeSvc
    Worker --> AnalyzeSvc
    AnalyzeSvc --> Orchestrator
    AnalyzeSvc --> AssetStorage
    AnalyzeSvc --> Repos
    Repos --> DbContext
    DbContext --> Pg
    AssetStorage --> Storage
    Orchestrator --> ProfileResolver
    Orchestrator --> Skills
    Skills --> GoogleFactory
    GoogleStatus --> GoogleFactory
    GoogleFactory --> GoogleCredentials
    GoogleCredentials --> Secrets
    GoogleFactory --> Google
```

### API Key And JWT Sequence

```mermaid
sequenceDiagram
    autonumber
    actor Operator
    participant Admin as Admin API
    participant Db as PostgreSQL
    actor Client
    participant Auth as Auth API
    participant Api as Protected API

    Operator->>Admin: POST /api/v1/admin/clients
    Admin->>Db: Store client + API key hash
    Admin-->>Operator: Raw API key returned once
    Client->>Auth: POST /api/v1/auth/token
    Auth->>Db: Find API key by hash
    Auth-->>Client: Short-lived Bearer JWT
    Client->>Api: Request with Authorization header
    Api-->>Client: Protected response
```

### Analyze Sequence

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant Swagger as Swagger/curl
    participant Api as ASP.NET API
    participant Auth as JWT Bearer
    participant App as MediaAnalysisApplicationService
    participant Storage as LocalMediaAssetStorage
    participant Orch as MediaAnalysisOrchestrator
    participant Skill as GoogleDocumentAiDocumentOcrSkill
    participant Google as Google Document AI
    participant Db as PostgreSQL

    User->>Swagger: Request dev token
    Swagger->>Api: POST /api/v1/dev/token
    Api-->>Swagger: accessToken
    User->>Swagger: Upload document with Bearer token
    Swagger->>Api: POST /api/v1/analyze
    Api->>Auth: Validate JWT
    Auth-->>Api: Authenticated
    Api->>App: AnalyzeAsync(request, file stream)
    App->>Db: Insert AnalysisJob Pending
    App->>Storage: Save uploaded file
    Storage-->>App: StoredMediaAsset
    App-->>Api: CreateAnalysisJobResponse
    Api-->>Swagger: 202 Accepted + job id
    loop Worker polling
        App->>Db: Claim Pending job as Running
    end
    App->>Storage: Open stored asset
    App->>Orch: AnalyzeAsync(profile, asset stream)
    Orch->>Skill: ExecuteAsync(context)
    Skill->>Google: ProcessDocument
    Google-->>Skill: OCR document text
    Skill-->>Orch: SkillExecutionResult
    Orch-->>App: AnalysisOrchestrationResult
    App->>Db: Persist MediaAsset, SkillExecution, AnalysisResult
    App->>Db: Mark Completed or RequiresReview
    Swagger->>Api: GET /api/v1/analysis-jobs/{jobId}/result
    Api-->>Swagger: 200 OK + extracted fields when ready
```

### Google Provider Status Sequence

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant Api as API
    participant Auth as JWT Bearer
    participant Status as GoogleDocumentAiStatusService
    participant Factory as GoogleDocumentAiClientFactory
    participant Secret as ADC JSON in /run/secrets
    participant Google as Google Document AI

    User->>Api: GET /api/v1/providers/google-document-ai/status
    Api->>Status: GetStatusAsync(checkRemote=false)
    Status-->>Api: Local configuration status
    Api-->>User: enabled, configured, credentialFilePresent

    User->>Api: GET /status?checkRemote=true + Bearer
    Api->>Auth: Validate JWT
    Auth-->>Api: Authenticated
    Api->>Status: GetStatusAsync(checkRemote=true)
    Status->>Factory: Create Document AI client
    Factory->>Secret: Load ADC credential JSON
    Factory-->>Status: DocumentProcessorServiceClient
    Status->>Google: GetProcessor
    Google-->>Status: Processor metadata
    Status-->>Api: Processor reachable
    Api-->>User: configured=true
```

### Main Class Diagram

```mermaid
classDiagram
    class AnalyzeController {
        +Analyze(clientKey, profileKey, assetType, file, metadataJson)
    }

    class AnalysisJobsController {
        +Create(request)
        +GetById(jobId)
        +GetResult(jobId)
        +Cancel(jobId)
    }

    class GoogleDocumentAiProviderController {
        +Status(checkRemote)
    }

    class IMediaAnalysisApplicationService {
        <<interface>>
        +AnalyzeAsync(request, content)
        +GetResultAsync(jobId)
    }

    class MediaAnalysisApplicationService {
        +AnalyzeAsync(request, content)
        +GetResultAsync(jobId)
    }

    class IMediaAnalysisOrchestrator {
        <<interface>>
        +AnalyzeAsync(request, content)
    }

    class MediaAnalysisOrchestrator {
        +AnalyzeAsync(request, content)
    }

    class IAnalysisProfileResolver {
        <<interface>>
        +ResolveAsync(profileKey)
        +ListAsync()
    }

    class StaticAnalysisProfileResolver {
        +ResolveAsync(profileKey)
        +ListAsync()
    }

    class IMediaAnalysisSkill {
        <<interface>>
        +SkillName
        +SupportedAssetTypes
        +ExecuteAsync(context)
    }

    class GoogleDocumentAiDocumentOcrSkill {
        +SkillName
        +ExecuteAsync(context)
    }

    class IGoogleDocumentAiClientFactory {
        <<interface>>
        +CreateAsync(options)
        +CreateProcessorName(options)
    }

    class GoogleDocumentAiClientFactory {
        +CreateAsync(options)
        +CreateProcessorName(options)
    }

    class IGoogleCredentialProvider {
        <<interface>>
        +GetCredentialAsync(options)
    }

    class GoogleCredentialProvider {
        +GetCredentialAsync(options)
    }

    class IGoogleDocumentAiStatusService {
        <<interface>>
        +GetStatusAsync(checkRemote)
    }

    class GoogleDocumentAiStatusService {
        +GetStatusAsync(checkRemote)
    }

    class IAnalysisJobRepository {
        <<interface>>
    }

    class IMediaAssetStorage {
        <<interface>>
    }

    AnalyzeController --> IMediaAnalysisApplicationService
    AnalysisJobsController --> IMediaAnalysisApplicationService
    GoogleDocumentAiProviderController --> IGoogleDocumentAiStatusService
    IMediaAnalysisApplicationService <|.. MediaAnalysisApplicationService
    MediaAnalysisApplicationService --> IMediaAnalysisOrchestrator
    MediaAnalysisApplicationService --> IAnalysisJobRepository
    MediaAnalysisApplicationService --> IMediaAssetStorage
    IMediaAnalysisOrchestrator <|.. MediaAnalysisOrchestrator
    MediaAnalysisOrchestrator --> IAnalysisProfileResolver
    MediaAnalysisOrchestrator --> IMediaAnalysisSkill
    IAnalysisProfileResolver <|.. StaticAnalysisProfileResolver
    IMediaAnalysisSkill <|.. GoogleDocumentAiDocumentOcrSkill
    GoogleDocumentAiDocumentOcrSkill --> IGoogleDocumentAiClientFactory
    IGoogleDocumentAiClientFactory <|.. GoogleDocumentAiClientFactory
    GoogleDocumentAiClientFactory --> IGoogleCredentialProvider
    IGoogleCredentialProvider <|.. GoogleCredentialProvider
    IGoogleDocumentAiStatusService <|.. GoogleDocumentAiStatusService
    GoogleDocumentAiStatusService --> IGoogleDocumentAiClientFactory
```

### Domain Class Diagram

```mermaid
classDiagram
    class Client {
        Guid Id
        string ClientKey
        string DisplayName
        string ContactEmail
        string AllowedProfileKeysJson
        bool IsActive
        DateTimeOffset CreatedAt
    }

    class ClientApiKey {
        Guid Id
        Guid ClientId
        string Name
        string KeyPrefix
        string KeyHash
        string Scope
        DateTimeOffset CreatedAt
        DateTimeOffset ExpiresAt
        DateTimeOffset RevokedAt
        DateTimeOffset LastUsedAt
    }

    class AnalysisProfile {
        Guid Id
        string ProfileKey
        string ClientKey
        string Description
        string DefinitionJson
        bool IsActive
        DateTimeOffset CreatedAt
    }

    class AnalysisJob {
        Guid Id
        string ClientKey
        string ProfileKey
        string AssetType
        string Status
        string ErrorMessage
        DateTimeOffset CreatedAt
        DateTimeOffset CompletedAt
    }

    class MediaAsset {
        Guid Id
        Guid JobId
        string AssetType
        string OriginalFileName
        string StoragePath
        string ContentType
        long SizeInBytes
        DateTimeOffset CreatedAt
    }

    class SkillExecution {
        Guid Id
        Guid JobId
        string SkillName
        string Status
        decimal Confidence
        DateTimeOffset StartedAt
        DateTimeOffset CompletedAt
        string ErrorMessage
    }

    class AnalysisResult {
        Guid Id
        Guid JobId
        decimal Confidence
        bool RequiresHumanReview
        DateTimeOffset CreatedAt
    }

    class ExtractedField {
        Guid Id
        Guid AnalysisResultId
        string Name
        string Value
        decimal Confidence
        string SourceSkill
    }

    class DetectedEntity {
        Guid Id
        Guid AnalysisResultId
        string EntityType
        string Value
        decimal Confidence
        string SourceSkill
    }

    class DetectedLabel {
        Guid Id
        Guid AnalysisResultId
        string Label
        decimal Confidence
        string SourceSkill
    }

    class GeneratedClip {
        Guid Id
        Guid AnalysisResultId
        string ClipKey
        string StoragePath
        decimal Confidence
        string SourceSkill
    }

    class AnalysisIssue {
        Guid Id
        Guid AnalysisResultId
        string Code
        string Message
        string Severity
        string SourceSkill
    }

    class HumanReviewTask {
        Guid Id
        Guid JobId
        string Status
        string Reason
        DateTimeOffset CreatedAt
    }

    class WebhookDelivery {
        Guid Id
        Guid JobId
        string TargetKey
        string Status
        int AttemptCount
        DateTimeOffset CreatedAt
        DateTimeOffset LastAttemptAt
    }

    Client "1" --> "many" ClientApiKey
    AnalysisJob "1" --> "many" MediaAsset
    AnalysisJob "1" --> "many" SkillExecution
    AnalysisJob "1" --> "0..1" AnalysisResult
    AnalysisJob "1" --> "many" HumanReviewTask
    AnalysisResult "1" --> "many" ExtractedField
    AnalysisResult "1" --> "many" DetectedEntity
    AnalysisResult "1" --> "many" DetectedLabel
    AnalysisResult "1" --> "many" GeneratedClip
    AnalysisResult "1" --> "many" AnalysisIssue
```

### Database Diagram

```mermaid
erDiagram
    Clients {
        uuid Id PK
        string ClientKey UK
        string DisplayName
        string ContactEmail
        jsonb AllowedProfileKeysJson
        bool IsActive
        timestamptz CreatedAt
    }

    ClientApiKeys {
        uuid Id PK
        uuid ClientId FK
        string Name
        string KeyPrefix
        string KeyHash UK
        string Scope
        timestamptz CreatedAt
        timestamptz ExpiresAt
        timestamptz RevokedAt
        timestamptz LastUsedAt
        uuid RotatedFromKeyId
    }

    AnalysisProfiles {
        uuid Id PK
        string ProfileKey UK
        string ClientKey
        string Description
        jsonb DefinitionJson
        bool IsActive
        timestamptz CreatedAt
    }

    AnalysisJobs {
        uuid Id PK
        string ClientKey
        string ProfileKey
        string AssetType
        string Status
        string ErrorMessage
        timestamptz CreatedAt
        timestamptz CompletedAt
    }

    MediaAssets {
        uuid Id PK
        uuid JobId FK
        string AssetType
        string OriginalFileName
        string StoragePath
        string ContentType
        bigint SizeInBytes
        timestamptz CreatedAt
    }

    SkillExecutions {
        uuid Id PK
        uuid JobId FK
        string SkillName
        string Status
        decimal Confidence
        timestamptz StartedAt
        timestamptz CompletedAt
        string ErrorMessage
    }

    AnalysisResults {
        uuid Id PK
        uuid JobId FK, UK
        decimal Confidence
        bool RequiresHumanReview
        timestamptz CreatedAt
    }

    ExtractedFields {
        uuid Id PK
        uuid AnalysisResultId FK
        string Name
        string Value
        decimal Confidence
        string SourceSkill
    }

    DetectedEntities {
        uuid Id PK
        uuid AnalysisResultId FK
        string EntityType
        string Value
        decimal Confidence
        string SourceSkill
    }

    DetectedLabels {
        uuid Id PK
        uuid AnalysisResultId FK
        string Label
        decimal Confidence
        string SourceSkill
    }

    GeneratedClips {
        uuid Id PK
        uuid AnalysisResultId FK
        string ClipKey
        string StoragePath
        decimal Confidence
        string SourceSkill
    }

    AnalysisIssues {
        uuid Id PK
        uuid AnalysisResultId FK
        string Code
        string Message
        string Severity
        string SourceSkill
    }

    HumanReviewTasks {
        uuid Id PK
        uuid JobId FK
        string Status
        string Reason
        timestamptz CreatedAt
    }

    WebhookDeliveries {
        uuid Id PK
        uuid JobId
        string TargetKey
        string Status
        int AttemptCount
        timestamptz CreatedAt
        timestamptz LastAttemptAt
    }

    Clients ||--o{ ClientApiKeys : owns
    AnalysisJobs ||--o{ MediaAssets : has
    AnalysisJobs ||--o{ SkillExecutions : runs
    AnalysisJobs ||--o| AnalysisResults : produces
    AnalysisJobs ||--o{ HumanReviewTasks : may_require
    AnalysisResults ||--o{ ExtractedFields : contains
    AnalysisResults ||--o{ DetectedEntities : contains
    AnalysisResults ||--o{ DetectedLabels : contains
    AnalysisResults ||--o{ GeneratedClips : contains
    AnalysisResults ||--o{ AnalysisIssues : contains
```

`WebhookDeliveries.JobId` is currently persisted as an identifier for future outbound delivery tracking. The current EF configuration does not define a navigation/FK relationship for it yet.

## Run locally

```bash
dotnet restore DeValpo.MediaIntelligence.sln
dotnet tool restore
dotnet build DeValpo.MediaIntelligence.sln
dotnet test DeValpo.MediaIntelligence.sln
dotnet run --project src/DeValpo.MediaIntelligence.Api
```

Swagger is enabled by `Swagger:Enabled` and is available at `/swagger`.

## Run with Docker

Create a local environment file and keep it out of git:

```bash
cp deploy/.env.example deploy/.env
```

Google Document AI supports Application Default Credentials (ADC). For Docker local, create ADC with `gcloud` and copy the generated local credential into the ignored `secrets` folder:

```bash
gcloud auth application-default login
gcloud auth application-default set-quota-project primeval-wind-499515-k6
```

On Windows:

```powershell
Copy-Item "$env:APPDATA\gcloud\application_default_credentials.json" secrets\google-application-default-credentials.json
```

Then set these values in `deploy/.env` when you want to use the real provider in Docker:

```text
GoogleDocumentAi__Enabled=true
GoogleDocumentAi__ProjectId=primeval-wind-499515-k6
GoogleDocumentAi__Location=us
GoogleDocumentAi__ProcessorId=44b3274ce1e84552
GoogleDocumentAi__CredentialsPath=/run/secrets/google-application-default-credentials.json
GoogleDocumentAi__AllowUserCredentials=true
Jwt__SigningKey=replace-with-local-dev-secret-min-32-chars
```

```bash
docker compose -f deploy/docker-compose.yml config
docker compose -f deploy/docker-compose.yml up --build
```

This single Compose file starts both components:

- `postgres`: PostgreSQL database on host port `5435`.
- `media-intelligence-api`: API on host port `8085`.

In Docker, migrations run automatically on API startup because `deploy/.env.example` sets `Database__ApplyMigrationsOnStartup=true`.

The API listens on `http://localhost:8085`, Swagger is available at `http://localhost:8085/swagger`, and health is exposed at `/health`.

Functional endpoints require JWT Bearer. For real client access, an operator first creates an API client using an admin-scoped token. In Development/Docker local, generate the admin token with:

```bash
curl -X POST http://localhost:8085/api/v1/dev/token \
  -H "Content-Type: application/json" \
  -d "{\"subject\":\"local-admin\",\"clientKey\":\"admin\",\"scope\":\"admin media-intelligence:admin\"}"
```

Create a client and copy the returned `apiKey` immediately. The platform stores only its hash:

```bash
curl -X POST http://localhost:8085/api/v1/admin/clients \
  -H "Authorization: Bearer <adminAccessToken>" \
  -H "Content-Type: application/json" \
  -d "{\"clientKey\":\"my-client\",\"displayName\":\"My Client\",\"contactEmail\":\"ops@example.com\",\"allowedProfileKeys\":[\"generic-document-ocr-v1\"],\"keyName\":\"primary\",\"scope\":\"media-intelligence:client\"}"
```

Exchange that API key for a short-lived JWT:

```bash
curl -X POST http://localhost:8085/api/v1/auth/token \
  -H "Content-Type: application/json" \
  -d "{\"clientKey\":\"my-client\",\"apiKey\":\"<one-time-api-key>\"}"
```

Use the returned `accessToken` in Swagger Authorize or with:

```bash
curl http://localhost:8085/api/v1/capabilities \
  -H "Authorization: Bearer <accessToken>"
```

## Database migrations

Apply migrations from the repository root:

```bash
dotnet tool restore
dotnet dotnet-ef database update --project src/DeValpo.MediaIntelligence.Infrastructure --startup-project src/DeValpo.MediaIntelligence.Api
```

Automatic migration execution is controlled by `Database:ApplyMigrationsOnStartup`. It defaults to `false` in local appsettings and is enabled in Docker through `deploy/.env.example`.

## Google Document AI

For the complete local setup, including Google ADC, Docker secrets and JWT tokens, follow:

- [Local tokens and Google secrets setup](docs/local-tokens-and-google-secrets.md)

Set these values through environment variables or appsettings:

```text
GoogleDocumentAi__Enabled=true
GoogleDocumentAi__ProjectId=your-project
GoogleDocumentAi__Location=us
GoogleDocumentAi__ProcessorId=your-processor
GoogleDocumentAi__CredentialsPath=
```

If `GoogleDocumentAi__CredentialsPath` is empty, the Google SDK uses Application Default Credentials. If you run inside Docker local with a mounted ADC user file, set it to the mounted file under `/run/secrets` and set `GoogleDocumentAi__AllowUserCredentials=true`. Production should use a service account or managed identity path and keep user credentials disabled. Do not commit Google credential JSON files.

Check provider readiness:

```bash
curl http://localhost:8085/api/v1/providers/google-document-ai/status
curl "http://localhost:8085/api/v1/providers/google-document-ai/status?checkRemote=true" \
  -H "Authorization: Bearer <adminOrOperatorAccessToken>"
```

The basic status endpoint checks local configuration and credential file presence. `checkRemote=true` validates the configured processor against Google and requires a Bearer token.

## Analyze an asset

```bash
curl -X POST http://localhost:8085/api/v1/analyze \
  -H "Authorization: Bearer <accessToken>" \
  -F "clientKey=<clientKey>" \
  -F "profileKey=generic-document-ocr-v1" \
  -F "assetType=Document" \
  -F "metadataJson={\"source\":\"manual-test\"}" \
  -F "file=@sample.pdf"
```

The response is `202 Accepted` with a `jobId`. Poll the job until it reaches `Completed` or `RequiresReview`:

```bash
curl http://localhost:8085/api/v1/analysis-jobs/<jobId> \
  -H "Authorization: Bearer <accessToken>"
```

Then retrieve the result:

```bash
curl http://localhost:8085/api/v1/analysis-jobs/<jobId>/result \
  -H "Authorization: Bearer <accessToken>"
```

## Add a skill

1. Implement `IMediaAnalysisSkill`.
2. Register it in `Infrastructure/DependencyInjection.cs`.
3. Add its name to a profile definition.
4. Add focused tests for the skill or orchestrator behavior.

## Add a profile

Profiles are resolved through `IAnalysisProfileResolver`. The initial implementation is static and can be replaced by a database-backed resolver without changing controllers or skills.

## CI/CD status

GitHub Actions validates restore, build, tests, Docker build and Docker Compose config. There is no automatic deployment in this phase.

## Productization and QA

The remaining work to move from MVP to product is tracked in:

- [Productization plan](docs/productization-plan.md)
- [Product final phase plan](docs/product-final-phase-plan.md)

The repository also includes a reusable QA skill for future product-readiness audits:

- [DeValpo MediaIntelligence QA skill](docs/devalpo-mediaintelligence-qa/SKILL.md)

The QA review for this change is recorded in:

- [QA review](docs/qa-review.md)
- [Phase 0 base hygiene QA](docs/qa-phase-0-base-hygiene.md)
- [Phase 1 foundation hardening QA](docs/qa-phase-1-foundation-hardening.md)
- [Phase 2 customer access and security QA](docs/qa-phase-2-customer-access-security.md)
- [Phase 3 tenant isolation QA](docs/qa-phase-3-tenant-isolation.md)
- [Phase 4 async jobs and worker QA](docs/qa-phase-4-async-jobs-worker.md)
