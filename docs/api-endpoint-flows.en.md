# API Endpoint Flows

This page is the simple endpoint view. The documentation site provides real navigation tabs, and this page adds role tabs plus endpoint-level sequence diagrams.

## How To Read This Page

| Icon | Meaning | User |
| --- | --- | --- |
| 🟢 | Public | Monitoring, Docker, browser |
| 🔐 | Authenticated client | Integrations uploading and reading analysis results |
| 🛡️ | Administration | Operators with `media-intelligence:admin` scope |
| 🧑‍💼 | Human operation | Users reviewing uncertain results |
| 🔔 | Webhooks | External systems receiving events |
| 📊 | Operations | Monitoring and support |

Simple rule:

1. Check `/health`.
2. Generate or exchange a token.
3. Call protected endpoints with `Authorization: Bearer <token>`.
4. For real documents, use `POST /api/v1/analyze`.
5. For results, query `/api/v1/analysis-jobs/{jobId}` and then `/result`.

=== "Client"

    Main path:

    1. `POST /api/v1/auth/token`
    2. `POST /api/v1/analyze`
    3. `GET /api/v1/analysis-jobs/{jobId}`
    4. `GET /api/v1/analysis-jobs/{jobId}/result`

=== "Admin"

    Main path:

    1. `POST /api/v1/admin/clients`
    2. Rotate or revoke API keys when needed.
    3. Configure webhook targets.
    4. Review audit and retention.

=== "Operator"

    Main path:

    1. `GET /ready`
    2. `GET /api/v1/operations/metrics`
    3. `GET /api/v1/human-review-tasks`
    4. Resolve review tasks.

=== "QA"

    Main path:

    1. Validate `401` without token.
    2. Validate `200` with the right token.
    3. Upload a real document.
    4. Validate result, review and webhook if applicable.

## Quick Map

| Group | Endpoints | Security |
| --- | --- | --- |
| 🟢 Health | `GET /health`, `GET /api/v1/health`, `GET /ready` | Public |
| 🔐 Auth | `POST /api/v1/dev/token`, `POST /api/v1/auth/token` | Dev public, real auth uses API key |
| 🛡️ Clients | `POST /api/v1/admin/clients`, rotate/revoke API key | Admin |
| 📚 Catalog | `GET /api/v1/capabilities`, `GET /api/v1/profiles`, `GET /api/v1/skills` | Bearer |
| 📄 Analysis | `POST /api/v1/analyze`, `POST /api/v1/analysis-jobs`, job/result/cancel | Bearer |
| ☁️ Provider | `GET /api/v1/providers/google-document-ai/status` | Basic public, remote Bearer |
| 🧑‍💼 Review | `GET/POST /api/v1/human-review-tasks` | Operator/Admin |
| 🔔 Webhooks | targets and deliveries | Admin/Client |
| 📊 Operations | `GET /api/v1/operations/metrics` | Operator/Admin |
| 🛡️ Governance | audit events and retention policy | Admin |

## 🟢 Health And Readiness

```mermaid
sequenceDiagram
    autonumber
    actor Caller as Caller
    participant API as API
    participant DB as PostgreSQL
    participant Storage as Storage
    Caller->>API: GET /health
    API-->>Caller: 200 Healthy
    Caller->>API: GET /ready
    API->>DB: Check database
    API->>Storage: Check asset storage
    API-->>Caller: 200 Ready or 503 Not Ready
```

## 🔐 Authentication And Client Access

```mermaid
sequenceDiagram
    autonumber
    actor Admin as Admin
    participant API as API
    participant DB as PostgreSQL
    actor Client as Client App
    Admin->>API: POST /api/v1/admin/clients
    API->>DB: Store client and API key hash
    API-->>Admin: Raw API key returned once
    Client->>API: POST /api/v1/auth/token
    API->>DB: Validate API key hash
    API-->>Client: Bearer JWT
```

## 📄 Analysis Jobs And Results

```mermaid
sequenceDiagram
    autonumber
    actor Client as Client App
    participant API as API
    participant Storage as Local Storage
    participant Worker as Analysis Worker
    participant Google as Google Document AI
    participant DB as PostgreSQL
    Client->>API: POST /api/v1/analyze
    API->>Storage: Save uploaded file
    API->>DB: Create Pending job
    API-->>Client: 202 Accepted jobId
    Worker->>DB: Claim job
    Worker->>Google: Process document
    Google-->>Worker: OCR result
    Worker->>DB: Persist result
    Client->>API: GET /api/v1/analysis-jobs/{jobId}/result
    API-->>Client: 200 Result or 404 Not Ready
```

## ☁️ Google Document AI Status

```mermaid
sequenceDiagram
    autonumber
    actor Caller as Caller
    participant API as API
    participant Status as GoogleStatusService
    participant Google as Google Document AI
    Caller->>API: GET /api/v1/providers/google-document-ai/status
    API->>Status: Check local config
    Status-->>API: enabled and configured
    opt checkRemote true
        API->>Google: GetProcessor
        Google-->>API: Processor metadata
    end
    API-->>Caller: Status response
```

## 🧑‍💼 Human Review

```mermaid
sequenceDiagram
    autonumber
    actor Operator as Operator
    participant API as API
    participant DB as PostgreSQL
    Operator->>API: GET /api/v1/human-review-tasks
    API->>DB: Search tasks
    DB-->>API: Task list
    Operator->>API: POST /api/v1/human-review-tasks/{taskId}/start
    API->>DB: Mark InProgress
    Operator->>API: POST /api/v1/human-review-tasks/{taskId}/complete
    API->>DB: Store decision and update result
```

## 🔔 Webhooks

```mermaid
sequenceDiagram
    autonumber
    actor Admin as Admin
    participant API as API
    participant DB as PostgreSQL
    participant Worker as Webhook Worker
    participant Receiver as Client Webhook
    Admin->>API: POST /api/v1/admin/clients/{clientKey}/webhook-targets
    API->>DB: Store target and signing secret
    Worker->>DB: Claim pending delivery
    Worker->>Receiver: POST analysis.completed with HMAC headers
    Receiver-->>Worker: 2xx accepted
    Worker->>DB: Mark Completed
```

## 📊 Operations And Governance

```mermaid
sequenceDiagram
    autonumber
    actor Admin as Admin
    participant API as API
    participant DB as PostgreSQL
    Admin->>API: GET /api/v1/operations/metrics
    API->>DB: Count jobs, reviews and webhooks
    API-->>Admin: Metrics snapshot
    Admin->>API: GET /api/v1/admin/audit-events
    API->>DB: Query sanitized audit log
    API-->>Admin: Audit page
```

## Real Test Flow

```mermaid
flowchart TD
    A["1. Start Docker Compose"] --> B["2. GET /health"]
    B --> C["3. POST /api/v1/dev/token in Development"]
    C --> D["4. Create client with admin token"]
    D --> E["5. POST /api/v1/auth/token with real API key"]
    E --> F["6. GET /api/v1/providers/google-document-ai/status?checkRemote=true"]
    F --> G["7. POST /api/v1/analyze with real PDF"]
    G --> H["8. GET /api/v1/analysis-jobs/{jobId}"]
    H --> I{"Job complete?"}
    I -- "no" --> H
    I -- "yes" --> J["9. GET /api/v1/analysis-jobs/{jobId}/result"]
    J --> K{"RequiresHumanReview?"}
    K -- "yes" --> L["10. Review in /human-review-tasks"]
    K -- "no" --> M["11. Validate webhook or final result"]
    L --> M
```

## Quick Error Checklist

| Symptom | Common Cause | What To Check |
| --- | --- | --- |
| `401` | Missing Bearer token | `Authorization: Bearer <token>` header |
| `403` | Wrong scope or client | `client_key`, `scope`, `allowedProfileKeys` |
| `400` upload | Invalid file | Extension, binary signature, complete PDF, size |
| Google `configured=false` | Incomplete configuration | ProjectId, Location, ProcessorId, CredentialsPath |
| Invalid Google credential | Bad ADC or JSON | Mounted file under `/run/secrets` and local `AllowUserCredentials` |
| Job does not finish | Worker or provider error | API container logs and job table |
| Webhook fails | URL or signature | `/api/v1/webhook-deliveries` history |
