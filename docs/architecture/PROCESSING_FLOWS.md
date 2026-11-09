# Processing Flows

## Common Job Lifecycle

1. A protected controller validates client access and request shape.
2. The application service persists a pending job and its asset metadata.
3. A hosted worker resets interrupted work at startup, then polls and claims the
   next pending job.
4. The service calls provider-neutral orchestration or a search provider,
   persists normalized results/issues and records retries or terminal failure.
5. Completion can enqueue signed webhook deliveries; a separate worker retries
   due deliveries with bounded exponential backoff.

## Documents

`POST /api/v1/analyze` accepts a validated multipart asset for an analysis
profile. `MediaAnalysisApplicationService` stores it locally, then
`AnalysisJobWorker` invokes the registered analysis skills. The implemented
production OCR skill is Google Document AI; normalized fields, labels, entities,
issues and review state are persisted.

## Video Target Search

Video jobs are created through `video-search-jobs`. `VideoSearchJobWorker`
processes pending work through Google Video Intelligence. Source videos already
in the configured GCS bucket can be used directly; other local/S3/GCS sources
are staged in GCS before the remote operation. Text and generic face occurrences
are normalized and persisted.

## Face Search

Face-search routes and workers are registered only when
`AwsRekognition:FaceSearchEnabled` is true. The workflow indexes reference
faces, runs Rekognition video face search, persists matches and attempts to
delete temporary Rekognition `FaceId` values. It deliberately does not delete
staged S3 files after processing.

## Image Batch Search

When `ImageBatchSearch:Enabled` is true, image jobs are processed by configured
providers. AWS supports visible-number and face-reference search; Google Vision
supports visible-number/OCR and reports face-reference search as unsupported.
Results are grouped by `targetKey` and persisted with issues.

## Cloud References, Review And Webhooks

`assetSelectorJson` resolves explicit assets or a prefix through S3/GCS browsers.
The resolver rejects public/signed URLs and buckets outside configured allow-lists.
Human-review tasks are created and progressed through protected endpoints.
Webhook targets are admin-managed; deliveries contain a normalized event payload
and are signed with HMAC-SHA256 before sending.

## Document Analysis Overview

Implementation diagrams, not recordings of a new execution. Authentication is
covered by the [API access sequence](../api.md).

```mermaid
flowchart TD
    A["Admin creates client"] --> B["Client receives API key once"]
    B --> C["Client requests JWT"]
    C --> D["Client uploads document"]
    D --> E["API validates file and permissions"]
    E --> F["API creates AnalysisJob Pending"]
    F --> G["API stores MediaAsset"]
    G --> H["Worker claims job"]
    H --> I["Orchestrator resolves profile"]
    I --> J["Skill calls Google Document AI"]
    J --> K["Normalized result"]
    K --> L{"Requires review?"}
    L -- "yes" --> M["HumanReviewTask"]
    L -- "no" --> N["Job Completed"]
    M --> O["Operator resolves review"]
    O -- "Approve / Correct" --> N
    O -- "Reject" --> R["Job Failed"]
    R --> P
    N --> P["Client reads result"]
    N --> Q["Optional webhook"]
```

## Document Analysis Sequence

```mermaid
sequenceDiagram
    autonumber
    actor Client as Client App
    participant API as AnalyzeController
    participant Access as RequestClientAccessService
    participant Validation as UploadedFileValidationService
    participant App as MediaAnalysisApplicationService
    participant Storage as LocalMediaAssetStorage
    participant DB as PostgreSQL
    participant Worker as AnalysisJobWorker
    participant Orchestrator as MediaAnalysisOrchestrator
    participant Skill as GoogleDocumentAiDocumentOcrSkill
    participant Google as Google Document AI

    Client->>API: POST /api/v1/analyze
    API->>Validation: Validate file
    Validation-->>API: Valid
    API->>Access: Validate client/profile access
    Access-->>API: Allowed
    API->>App: EnqueueAsync
    App->>DB: Insert AnalysisJob Pending
    App->>Storage: Save file
    App->>DB: Insert MediaAsset
    App-->>API: jobId
    API-->>Client: 202 Accepted
    Worker->>DB: Claim Pending job
    Worker->>Storage: Open file
    Worker->>Orchestrator: Analyze profile
    Orchestrator->>Skill: Execute OCR skill
    Skill->>Google: ProcessDocument
    Google-->>Skill: OCR response
    Skill-->>Orchestrator: Extracted fields and issues
    Orchestrator-->>Worker: Normalized result
    Worker->>DB: Persist result and status
```

## Webhook Delivery Sequence

```mermaid
sequenceDiagram
    autonumber
    actor Admin as Admin
    participant API as API
    participant DB as PostgreSQL
    participant Worker as WebhookDeliveryWorker
    participant Signer as WebhookSignatureService
    participant Receiver as Client Webhook

    Admin->>API: POST /api/v1/admin/clients/{clientKey}/webhook-targets
    API->>DB: Store target and signing secret
    API-->>Admin: 201 Created
    DB-->>Worker: Pending delivery
    Worker->>Signer: Sign timestamp + dot + raw payload
    Signer-->>Worker: sha256 signature
    Worker->>Receiver: POST analysis.completed
    Receiver-->>Worker: 2xx
    Worker->>DB: Mark delivery Delivered
```
