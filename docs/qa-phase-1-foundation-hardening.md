# QA Fase 1: Hardening De Fundacion

Fecha: 2026-07-06

Branch: `codex/phase-1-foundation-hardening`

## Alcance

Esta revision valida el hardening de fundacion definido para Fase 1:

- Google Document AI credential provider separado del client factory.
- Carga de credenciales sin APIs obsoletas ni `#pragma warning disable CS0618`.
- Perfil real `generic-document-ocr-v1`.
- Alias temporal `invoice-extraction-v1`.
- Mock skills y mock profiles apagados por defecto y habilitables solo por configuracion local/demo.
- Startup validation para perfiles que referencian skills no registrados o mocks cuando los mocks estan deshabilitados.

## Cambios Revisados

- `GoogleCredentialProvider` resuelve ADC o archivo de credenciales.
- `GoogleDocumentAiClientFactory` construye el cliente y ya no resuelve credenciales directamente.
- `GoogleDocumentAi__AllowUserCredentials` permite usar ADC user credentials solo cuando se habilita explicitamente.
- `Skills__EnableMockSkills` controla registro de mocks.
- `AnalysisProfiles__EnableMockProfiles` controla exposicion de perfiles mock/demo.
- `generic-document-ocr-v1` es el perfil real para OCR con Google Document AI.

## Hallazgo QA Y Remediacion

**[Critical] El primer refactor de credenciales rechazaba el ADC JSON local montado en Docker**

Evidencia:

- El archivo montado por `gcloud auth application-default login` era de tipo `authorized_user`.
- El status remoto devolvia credenciales invalidas.
- El analisis real con `generic-document-ocr-v1` quedaba en `RequiresHumanReview=true` por error de credenciales.

Correccion:

- Se agrego `GoogleDocumentAi__AllowUserCredentials`.
- El default productivo es `false`.
- Docker local lo habilita explicitamente en `deploy/.env.example`.
- El provider soporta `service_account` y `authorized_user`, rechazando `authorized_user` si no esta permitido.
- Se agregaron tests para rechazar ADC user credentials cuando no estan permitidas y para rechazar tipos desconocidos.

## Validaciones Ejecutadas

### Build Y Tests

```powershell
dotnet build
dotnet test
```

Resultado obtenido:

- Build pasa con 0 warnings y 0 errores.
- Tests pasan: 18/18.

### Docker Compose

```powershell
docker compose -f deploy\docker-compose.yml config --quiet
docker compose -f deploy\docker-compose.yml up -d --build
```

Resultado obtenido:

- Compose config pasa.
- API y PostgreSQL levantan correctamente.

### Google Document AI Real

Ambiente:

- Docker local.
- Google project: `primeval-wind-499515-k6`.
- Location: `us`.
- Processor id: `44b3274ce1e84552`.
- Credential file mounted under `/run/secrets`.
- Credential content was not logged or copied.

Resultado:

- `/health`: `Healthy`.
- Development token generated locally.
- `GET /api/v1/providers/google-document-ai/status?checkRemote=true`: configured `true`.
- Status message: Google Document AI processor reachable.
- `GET /api/v1/profiles`: includes `generic-document-ocr-v1` and compatibility alias `invoice-extraction-v1`.
- `POST /api/v1/analyze` with a real PDF and `profileKey=generic-document-ocr-v1`:
  - Confidence: `0.89`.
  - RequiresHumanReview: `false`.
  - Issues: `0`.
  - Provider field: `google-document-ai`.
  - Extracted text length: `852`.

### Mock Gating

```powershell
rg -n "Mock|EnableMockSkills|RegisterMockSkills" src\DeValpo.MediaIntelligence.Infrastructure\DependencyInjection.cs
```

Resultado obtenido:

- Mock registration exists only inside `RegisterMockSkills`.
- `RegisterMockSkills` is called only when `Skills:EnableMockSkills=true`.
- Startup validation rejects mock skill references when mocks are disabled.

### Credential Loader

```powershell
rg -n "#pragma warning disable CS0618|GoogleCredential\.FromFileAsync|GoogleCredential\.FromStreamAsync" src
```

Resultado obtenido:

- Sin matches.

### Hardcoded Text

Se ejecuto busqueda focalizada de strings visibles relacionados con errores/configuracion.

Resultado:

- No se detectaron nuevos mensajes visibles fuera de recursos.
- Los matches restantes corresponden a constantes tecnicas, nombres de propiedades, configuracion EF, estados de contrato, capability keys, profile keys o nombres de skills.

## Resultado QA

Pass.

No hay Blockers, Criticals ni Majors abiertos para Fase 1.

## Decision

Fase 1 queda lista para PR y revision.

No se debe iniciar Fase 2 hasta que esta PR este mergeada contra `main`.
