# QA Fase 4: Jobs Asincronos Y Worker

Fecha: 2026-07-06

Branch: `codex/phase-4-async-jobs-worker`

## Alcance

Esta revision valida la Fase 4 del plan de producto:

- `/api/v1/analyze` encola trabajo y responde `202 Accepted`.
- Worker procesa jobs `Pending` con archivo almacenado.
- Estados persistidos: `Pending`, `Running`, `Completed`, `Failed`, `Cancelled`, `RequiresReview`.
- Intentos persistidos con `ProcessingAttempts`.
- Fallos de procesamiento reintentan hasta `MediaAnalysis:MaxProcessingAttempts`.
- Jobs interrumpidos en `Running` vuelven a `Pending` al reiniciar API.
- Resultado se consulta por `GET /api/v1/analysis-jobs/{jobId}/result`.

## Cambios Revisados

- `AnalysisJobWorker` como `BackgroundService`.
- `MediaAnalysisApplicationService.EnqueueAsync`.
- `MediaAnalysisApplicationService.ProcessNextPendingAsync`.
- Repositorio con claim de job, completion transaccional y failure update directo.
- Migracion `AddAnalysisJobProcessingAttempts`.
- Docker runtime instala `libgssapi-krb5-2` para Google ADC.
- Swagger y README documentan `202 + polling`.

## Hallazgos QA Y Remediacion

**[Critical] El worker podia dejar un job atascado en `Running` tras un fallo de provider**

Evidencia:

- Un job real quedo `Running` tras una excepcion durante procesamiento.
- El worker capturo una excepcion de concurrencia al intentar marcar retry/failure con el grafo tracked de EF.

Correccion:

- Se agregaron operaciones especificas de repositorio:
  - `MarkRunningAsync`.
  - `SetProcessingFailureAsync`.
  - `CompleteProcessingAsync`.
- El worker ya no depende del grafo tracked para transiciones terminales.
- Los fallos pasan a `Pending` para retry o `Failed` cuando se agotan intentos.

**[Critical] El contenedor no tenia `libgssapi_krb5.so.2` para Google ADC**

Evidencia:

- El provider Google fallo en Docker con `Cannot load library libgssapi_krb5.so.2`.

Correccion:

- Se agrego `libgssapi-krb5-2` al runtime image en `src/DeValpo.MediaIntelligence.Api/Dockerfile`.

**[Major] El worker se activaba dentro de `WebApplicationFactory` y podia tocar DB real local durante tests**

Evidencia:

- Un test API levanto hosted services y el worker intento procesar jobs contra la configuracion local.

Correccion:

- `ApiSecurityTests` deshabilita `AnalysisWorker:Enabled=false`.

**[Minor] Swagger y README aun describian `/analyze` como flujo sincrono**

Evidencia:

- El test `SwaggerJson_IncludesEndpointDescriptions` esperaba el texto anterior de MVP sincrono.

Correccion:

- Swagger, README y guia local ahora documentan `202 Accepted`, polling de job y lectura de result.

## Validaciones Ejecutadas

### Build Y Tests

```powershell
dotnet build
dotnet test
dotnet ef migrations has-pending-model-changes --project src\DeValpo.MediaIntelligence.Infrastructure --startup-project src\DeValpo.MediaIntelligence.Api
```

Resultado:

- Build pasa con 0 warnings y 0 errores.
- Tests pasan: 32/32.
- EF no tiene cambios pendientes de modelo.

### Docker Compose

```powershell
docker compose -f deploy\docker-compose.yml config --quiet
docker compose -f deploy\docker-compose.yml up -d --build
curl.exe http://localhost:8085/health
```

Resultado:

- Compose config pasa.
- API y PostgreSQL levantan correctamente.
- `/health`: HTTP `200`.

### Migracion

```powershell
dotnet ef migrations script --project src\DeValpo.MediaIntelligence.Infrastructure --startup-project src\DeValpo.MediaIntelligence.Api
```

Evidencia:

```sql
ALTER TABLE "AnalysisJobs" ADD "ProcessingAttempts" integer NOT NULL DEFAULT 0;
```

### Flujo Real Async Con Google Document AI

Ambiente:

- Docker local.
- PostgreSQL real del compose.
- Cliente API real creado por endpoint admin.
- JWT real emitido por `/api/v1/auth/token`.
- PDF real enviado a `/api/v1/analyze`.
- Perfil: `generic-document-ocr-v1`.
- Proveedor: Google Document AI.

Resultado sanitizado:

```text
AnalyzeStatus: 202
QueuedStatus: Pending
QueuedAttempts: 0
FinalStatus: Completed
ProcessingAttempts: 1
ResultConfidence: 0.8900
RequiresHumanReview: false
IssueCount: 0
Provider: google-document-ai
```

Interpretacion:

- `/analyze` ya no devuelve el resultado sincrono.
- Worker toma el job y lo completa.
- El resultado queda disponible por `GET /api/v1/analysis-jobs/{jobId}/result`.

### Recuperacion Tras Reinicio

Simulacion:

- Se creo un job real via API.
- Se marco `Running` directamente en PostgreSQL para representar interrupcion durante procesamiento.
- Se reinicio `media-intelligence-api`.

Resultado sanitizado:

```text
BeforeRestartStatus: Running
BeforeError: interrupted
AfterRestartStatus: Pending
AfterError: null
AfterAttempts: 0
```

Interpretacion:

- El worker resetea jobs interrumpidos en startup.
- El job queda en estado consistente y retryable.

### Fallo Permanente

Evidencia:

- Un job con archivo no resoluble paso por 3 intentos.
- Estado final: `Failed`.
- `ProcessingAttempts`: `3`.

Interpretacion:

- Los fallos no dejan jobs colgados en `Running`.
- El intento maximo configurado se respeta.

## Resultado QA

Pass.

No hay Blockers, Criticals ni Majors abiertos para Fase 4.

## Pendientes Fuera De Alcance

Estos puntos pertenecen a fases posteriores:

- Validacion profunda de archivos corruptos, vacios, MIME real y sobredimensionados.
- Worker distribuido con leases o locks para multiples replicas.
- Observabilidad avanzada: metricas, queue depth, duration y tracing.
- Human review operativo.
- Webhook delivery.

## Decision

Fase 4 queda lista para PR y revision.

No se debe iniciar Fase 5 hasta que esta PR este mergeada contra `main`.
