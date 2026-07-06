# QA Fase 3: Tenant Isolation Y Autorizacion

Fecha: 2026-07-06

Branch: `codex/phase-3-tenant-isolation`

## Alcance

Esta revision valida la Fase 3 del plan de producto:

- Rechazar requests donde `clientKey` del request no coincida con el claim `client_key`.
- Autorizar ejecucion de perfiles por cliente usando `AllowedProfileKeysJson`.
- Evitar lectura cruzada de jobs y results entre clientes.
- Mantener bypass operativo para scopes admin/operator.
- Proteger status remoto de Google Document AI con scope operativo.

## Cambios Revisados

- `RequestClientAccessService` centraliza las reglas de acceso por request.
- `/api/v1/analyze` valida cliente y perfil antes de ejecutar el pipeline.
- `/api/v1/analysis-jobs` valida cliente y perfil antes de crear jobs.
- `GET /api/v1/analysis-jobs/{jobId}` oculta jobs ajenos con `404`.
- `GET /api/v1/analysis-jobs/{jobId}/result` oculta results ajenos con `404`.
- `POST /api/v1/analysis-jobs/{jobId}/cancel` oculta jobs ajenos con `404`.
- `GET /api/v1/providers/google-document-ai/status?checkRemote=true` requiere scope admin/operator.

## Hallazgos QA Y Remediacion

**[Minor] La primera prueba de perfil no asignado envio `assetType` como string**

Evidencia:

- ASP.NET devolvio `400` por model binding antes de ejecutar autorizacion.

Correccion:

- La prueba se repitio con el valor numerico del enum esperado por el contrato JSON actual.
- El endpoint devolvio `403`, validando la regla de perfil asignado.

## Validaciones Ejecutadas

### Build Y Tests

```powershell
dotnet build
dotnet test
```

Resultado:

- Build pasa con 0 warnings y 0 errores.
- Tests pasan: 28/28.

### Docker Compose

```powershell
docker compose -f deploy\docker-compose.yml config --quiet
docker compose -f deploy\docker-compose.yml up -d --build
```

Resultado:

- Compose config pasa.
- API y PostgreSQL levantan correctamente.

### Aislamiento Real De Jobs Y Results

Ambiente:

- Docker local.
- PostgreSQL real del compose.
- API en `http://localhost:8085`.
- Dos clientes reales creados por endpoint admin.
- JWTs reales emitidos por `/api/v1/auth/token`.
- PDF real enviado a `/api/v1/analyze`.
- Perfil: `generic-document-ocr-v1`.
- Proveedor: Google Document AI.

Resultado sanitizado:

```text
AnalyzeStatus: 200
Confidence: 0.89
Issues: 0
ClientAJobStatus: 200
ClientBJobStatus: 404
ClientAResultStatus: 200
ClientBResultStatus: 404
```

Interpretacion:

- El cliente dueño puede consultar job/result.
- Otro cliente autenticado no puede leer job/result del dueño.
- La API responde `404` para no exponer existencia de recursos ajenos.

### Perfil No Asignado

Resultado sanitizado:

```text
ProfileDeniedStatus: 403
```

Interpretacion:

- Un cliente autenticado no puede crear trabajo para un perfil que no esta en `AllowedProfileKeysJson`.

### Mismatch De Cliente

Resultado sanitizado:

```text
MismatchedClientStatus: 403
```

Interpretacion:

- Un JWT emitido para cliente A no puede crear trabajo usando `clientKey` de cliente B.

### Status Remoto Google

Resultado sanitizado:

```text
ClientRemoteStatusStatus: 403
AdminRemoteStatusStatus: 200
```

Interpretacion:

- Un cliente normal no puede ejecutar check remoto contra Google.
- Un token admin/operator si puede ejecutar el diagnostico remoto.

## Resultado QA

Pass.

No hay Blockers, Criticals ni Majors abiertos para Fase 3.

## Pendientes Fuera De Alcance

Estos puntos pertenecen a fases posteriores:

- Jobs asincronos y worker recuperable.
- Validacion profunda de archivo por contenido real, PDF corrupto y tamano por plan.
- Human review operativo.
- Webhooks productivos.
- Rate limiting y auditoria persistida de auth/lecturas.

## Decision

Fase 3 queda lista para PR y revision.

No se debe iniciar Fase 4 hasta que esta PR este mergeada contra `main`.
