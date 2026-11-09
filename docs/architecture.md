# Arquitectura

DeValpo.MediaIntelligence es una API independiente para analizar documentos,
imagenes y videos. Esta es la referencia canónica de arquitectura; las secuencias
de runtime están en [Flujos de procesamiento](architecture/PROCESSING_FLOWS.md).

## Capas

- `Contracts`: DTOs publicos y enums expuestos por API.
- `Domain`: entidades de persistencia y modelo principal.
- `Application`: casos de uso, orquestacion, perfiles y abstracciones de proveedores/storage.
- `Infrastructure`: EF Core, PostgreSQL, storage local, Google Document AI, Google Video Intelligence, AWS Rekognition y skills.
- `Api`: controladores, middleware, Swagger, health checks y composicion.

Direccion de dependencias:

```text
Domain
Contracts
Application -> Domain + Contracts
Infrastructure -> Application + Domain + Contracts
Api -> Application + Infrastructure + Contracts
```

## Flujo End-To-End

```mermaid
flowchart TD
    A["Client app"] --> B["JWT Bearer request"]
    B --> C["API controller"]
    C --> D["Application service"]
    D --> E["Media storage"]
    D --> F["PostgreSQL"]
    D --> G["Analysis orchestrator"]
    G --> H["Profile resolver"]
    G --> I["Skills"]
    I --> J["Google Document AI"]
    D --> L["Video/Face search services"]
    L --> M["Google Video Intelligence"]
    L --> N["AWS Rekognition + S3"]
    G --> K["Normalized result"]
    K --> F
    L --> F
```

## Componentes En Runtime

```mermaid
flowchart LR
    API["ASP.NET Core API"] --> App["Application services"]
    Worker["Background workers"] --> App
    App --> Storage["Local storage"]
    App --> DB["PostgreSQL"]
    App --> Orchestrator["MediaAnalysisOrchestrator"]
    Orchestrator --> Skills["Skills"]
    Skills --> Google["Google Document AI"]
    App --> VideoSearch["VideoSearchApplicationService"]
    App --> FaceSearch["FaceSearchApplicationService"]
    App --> ImageBatchSearch["ImageBatchSearchApplicationService"]
    VideoSearch --> GVI["Google Video Intelligence"]
    FaceSearch --> AWS["AWS Rekognition + S3"]
    ImageBatchSearch --> AWS
    ImageBatchSearch --> Vision["Google Vision OCR"]
```

## Profiles, Skills Y Capabilities

- Una capability describe que puede hacer la plataforma.
- Un skill es una unidad ejecutable que produce campos, labels, entidades, clips o issues.
- Un profile selecciona skills, orden, umbrales y reglas de revision para un caso de uso.

## Providers Cloud

- Google Document AI procesa documentos.
- Google Video Intelligence procesa OCR de numeros visibles y ocurrencias genericas de caras en videos.
- AWS Rekognition procesa comparacion facial consentida contra imagenes de referencia.
- Google Vision procesa OCR de imagenes para `image-batch-search-v1`; en v1 no hace identificacion facial por persona.
- Cada provider vive en su propio namespace de Infrastructure y usa opciones/credenciales separadas.

## Convencion De Modelos Y Mappers

- Los modelos canonicos de cada caso de uso viven bajo `Application/<Feature>/Models`.
- Los providers externos no proyectan directo a DTOs publicos; primero convierten respuestas del SDK a modelos canonicos de Application.
- Los mappers que conocen SDKs externos viven en `Infrastructure/Providers/<Provider>/<Service>/Mappers`.
- Usar nombres `ToDomain...` para convertir SDK/proveedor a modelo interno y `ToDto...` solo cuando el resultado sea un DTO de `Contracts`.

## Composición Y Límites

La solución contiene cinco proyectos productivos y uno de tests.
`Api/Program.cs` compone `AddApplication()` y `AddInfrastructure()`, vincula
opciones y configura JWT, Serilog, salud, controladores y workers activados por
configuración. Los workers hoy se alojan en la API, no en servicios independientes.

- Los controladores validan solicitudes/acceso y llaman a los casos de uso.
- Application define interfaces de proveedor/storage y puede depender de Domain y Contracts, no de modelos SDK cloud.
- Infrastructure implementa esas interfaces y concentra los mapeos SDK/EF.
- Domain guarda estado normalizado, no credenciales ni respuestas crudas.
- Contracts no expone tipos EF ni de proveedores cloud.

## Persistencia Y Sistemas Externos

`MediaIntelligenceDbContext` persiste clientes, hashes de API keys, perfiles,
trabajos, metadatos de assets, resultados normalizados, revisiones, auditoría,
retención y webhooks, además de entidades de video e imágenes por lotes.
Las migraciones pertenecen a `Infrastructure/Persistence/Migrations`;
PostgreSQL no almacena los binarios multimedia. Los uploads usan storage local
configurado. Las referencias S3/GCS respetan allowlists de buckets y se realiza
staging compatible con el proveedor cuando hace falta.

## Puntos De Entrada Internos

| Área | Servicios / interfaces principales |
| --- | --- |
| Acceso de clientes | `IClientAccessApplicationService`, `IClientAccessTokenService`, `IRequestClientAccessService` |
| Ingesta / trabajos | `IUploadedFileValidationService`, `IMediaAnalysisApplicationService`, `IAnalysisJobApplicationService`, `IMediaAssetStorage` |
| Orquestación | `IMediaAnalysisOrchestrator`, `IAnalysisProfileResolver`, `IMediaAnalysisSkill` |
| Credenciales / clientes Google | `IGoogleCredentialProvider`, `IGoogleDocumentAiClientFactory`, `IGoogleDocumentAiStatusService` |
| Revisión / callbacks | `IHumanReviewApplicationService`, `IWebhookDeliveryApplicationService`, `IWebhookDeliveryProcessor` |
| Operación / gobierno | `IOperationalMetricsApplicationService`, `IAuditApplicationService`, `IRetentionApplicationService` |

Las interfaces neutrales están separadas de sus implementaciones Infrastructure.
[Configuración](development/CONFIGURATION_GUIDE.md) y
[modelo de datos](data-model.md) detallan las opciones operativas y entidades.
