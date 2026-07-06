# QA Fase 2: API Keys Y Emision JWT Real

Fecha: 2026-07-06

Branch: `codex/phase-2-customer-access-security`

## Alcance

Esta revision valida la Fase 2 del plan de producto:

- Clientes API persistidos.
- API keys generadas una sola vez para el operador.
- API keys almacenadas solo como hash.
- Emision JWT real mediante `POST /api/v1/auth/token`.
- Rotacion, revocacion y expiracion de API keys.
- `/api/v1/dev/token` mantenido solo para Development.
- Claims JWT de cliente: `sub`, `client_key`, `scope`, `jti`, `iat`, `exp`.

## Cambios Revisados

- Entidad `ClientApiKey` y relacion con `Client`.
- Migracion EF `AddClientApiKeys`.
- Repositorio `IClientApiKeyRepository`.
- Servicio de administracion `IClientAccessApplicationService`.
- Servicio de emision JWT `IClientAccessTokenService`.
- Endpoints:
  - `POST /api/v1/admin/clients`
  - `POST /api/v1/admin/clients/{clientKey}/api-keys/{apiKeyId}/rotate`
  - `POST /api/v1/admin/clients/{clientKey}/api-keys/{apiKeyId}/revoke`
  - `POST /api/v1/auth/token`

## Hallazgos QA Y Remediacion

**[Minor] El primer comando de QA con `curl.exe -d` envio JSON invalido desde PowerShell**

Evidencia:

- `/api/v1/dev/token` respondia `400`.
- La respuesta indicaba JSON mal formado antes de llegar al flujo de autenticacion.

Correccion:

- QA real se ejecuto con `Invoke-RestMethod` para requests JSON y `curl.exe` solo para GET/multipart.
- No requirio cambio de codigo.

**[Minor] Consulta SQL inicial de verificacion perdia comillas por escape PowerShell/Docker**

Evidencia:

- PostgreSQL recibia identificadores sin comillas para tablas PascalCase.

Correccion:

- La consulta final se envio por stdin a `psql`.
- La verificacion confirmo hash de 64 caracteres y prefijo de 16 caracteres por API key.

**[Major] La migracion inicial agregaba `AllowedProfileKeysJson` con default JSON inseguro**

Evidencia:

- La migracion `AddClientApiKeys` agregaba la columna `AllowedProfileKeysJson` como `jsonb` con default vacio.
- En una base con clientes existentes, ese default podia ser invalido o dejar datos inconsistentes.

Correccion:

- Se cambio el default a `[]`, que es JSON valido.
- Se actualizo la configuracion EF, la migracion y el snapshot.
- `dotnet ef migrations script` confirma `ALTER TABLE "Clients" ADD "AllowedProfileKeysJson" jsonb NOT NULL DEFAULT '[]';`.

## Validaciones Ejecutadas

### Build Y Tests

```powershell
dotnet build
dotnet test
```

Resultado:

- Build pasa con 0 warnings y 0 errores.
- Tests pasan: 22/22.

### Docker Compose

```powershell
docker compose -f deploy\docker-compose.yml config --quiet
docker compose -f deploy\docker-compose.yml up -d --build
docker compose -f deploy\docker-compose.yml ps
curl.exe -s -w "`n%{http_code}" http://localhost:8085/health
```

Resultado:

- Compose config pasa.
- API y PostgreSQL levantan correctamente.
- `/health`: `Healthy`, HTTP `200`.

### Flujo Real API Key A JWT

Ambiente:

- Docker local.
- PostgreSQL real del compose.
- API en `http://localhost:8085`.
- Token admin local generado con `/api/v1/dev/token` porque el ambiente es Development.

Resultado:

- `POST /api/v1/admin/clients`: cliente creado.
- API key cruda retornada una vez en la respuesta de creacion.
- `POST /api/v1/auth/token`: retorna JWT Bearer de cliente.
- `GET /api/v1/capabilities` sin token: HTTP `401`.
- `GET /api/v1/capabilities` con JWT de cliente: HTTP `200`.

Evidencia sanitizada:

```text
RawApiKeyReturnedOnce: true
ClientTokenType: Bearer
UnauthenticatedCapabilitiesStatus: 401
AuthenticatedCapabilitiesStatus: 200
```

### Rotacion Y Revocacion

Resultado:

- API key valida antes de rotar: HTTP `200`.
- API key antigua despues de rotar: HTTP `401`.
- API key nueva despues de rotar: HTTP `200`.
- API key nueva despues de revocar: HTTP `401`.
- API key expirada: HTTP `401`.

Evidencia sanitizada:

```text
BeforeRotateExchangeStatus: 200
OldKeyAfterRotateExchangeStatus: 401
NewKeyAfterRotateExchangeStatus: 200
NewKeyAfterRevokeExchangeStatus: 401
ExpiredKeyExchangeStatus: 401
```

### Persistencia De Secretos

Consulta ejecutada contra PostgreSQL por stdin a `psql`:

```sql
select length("KeyHash") as hash_length,
       length("KeyPrefix") as prefix_length,
       "RevokedAt" is not null as revoked
from "ClientApiKeys"
where "ClientId" = (select "Id" from "Clients" where "ClientKey" = '<qa-client-key>')
order by "CreatedAt";
```

Resultado:

```text
64,16,true
64,16,true
```

Interpretacion:

- La API key cruda no se guarda en la tabla.
- Se almacena hash SHA-256 hexadecimal de 64 caracteres.
- El prefijo de 16 caracteres queda solo como identificador operativo.
- Las keys rotadas/revocadas quedan marcadas como revocadas.

### End-To-End Real Con Google Document AI

Ambiente:

- Docker local.
- Cliente API creado para QA.
- JWT emitido por `/api/v1/auth/token`.
- PDF real local enviado a `/api/v1/analyze`.
- Perfil: `generic-document-ocr-v1`.
- Proveedor: Google Document AI.

Resultado sanitizado:

```text
Status: 200
Confidence: 0.89
RequiresHumanReview: false
ExtractedFieldCount: 3
IssueCount: 0
FirstProvider: google-document-ai
```

## Resultado QA

Pass.

No hay Blockers, Criticals ni Majors abiertos para Fase 2.

## Pendientes Fuera De Alcance

Estos puntos pertenecen a fases posteriores:

- Tenant isolation completo para jobs/results por cliente.
- Autorizacion de perfiles por cliente durante `/api/v1/analyze`.
- Rate limiting por cliente.
- Auditoria operacional de auth, lectura de resultados, rotacion y revocacion.
- Verificacion de `/api/v1/dev/token` en ambiente no Development.

## Decision

Fase 2 queda lista para PR y revision.

No se debe iniciar Fase 3 hasta que esta PR este mergeada contra `main`.
