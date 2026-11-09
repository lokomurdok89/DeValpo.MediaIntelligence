# Primeros Pasos

Para la demo offline, sigue el README del repositorio: no
necesita credenciales cloud ni API. Esta guía corresponde a una instalación del
backend; los chequeos remotos y envíos de trabajos requieren autorización separada
y pueden generar costos.

## Requisitos

- Docker Desktop / Compose para API y PostgreSQL 17.
- .NET SDK 10.x solo para compilar o ejecutar fuera de Docker.
- Un proveedor configurado para el flujo elegido; Google no es requisito del reconocimiento facial AWS.
- Secreto de firma JWT externo y nunca versionado.

## 1. Configurar El Flujo Elegido

Copia el ejemplo solo si no existe el archivo local:

```powershell
if (-not (Test-Path deploy/.env)) { Copy-Item deploy/.env.example deploy/.env }
```

Configura `Jwt__SigningKey` con un valor privado de al menos 32 caracteres y
mantén issuer/audience coherentes con la configuración local Development.
`Jwt__RequireHttpsMetadata=false` es una opción de desarrollo local, no una
recomendación productiva. `Database__ApplyMigrationsOnStartup=true` aplica cambios
de esquema al iniciar localmente; producción usa el
[runbook de migraciones](runbooks/release-and-migrations.md).

| Flujo | Configuración canónica |
| --- | --- |
| Cara de referencia en video S3 | [Configuración y permisos AWS](aws-rekognition-face-search.md) |
| Números / caras genéricas en video | [Google Video Intelligence](video-target-search.md) |
| OCR documental | [Runbook Document AI](runbooks/google-document-ai.md) y [ADC](reference/local-tokens-and-google-secrets.md) |
| Identidades productivas | [Roles administrados y secretos externos](reference/production-cloud-credentials.md) |

Configura solo los proveedores necesarios. Las credenciales quedan fuera de Git;
Compose monta `secrets/` en solo lectura bajo `/run/secrets`. El montaje no autoriza
imprimir credenciales ni publicar esa carpeta.

## 2. Levantar Localmente

```powershell
docker compose -f deploy/docker-compose.yml config --quiet
docker compose -f deploy/docker-compose.yml up --build
```

API: `http://localhost:8085`; Swagger Development: `/swagger`;
puerto PostgreSQL del host: `5435`. `/health` verifica el proceso; `/ready`
comprueba las dependencias configuradas. No compartas config Compose completa
ni logs que contengan secretos.

## 3. Dar De Alta Un Cliente Y Obtener Token

Solo en `Development` local, genera el token admin para dar de alta al cliente:

```powershell
# Solo Development; no usar este helper en producción.
$baseUrl = "http://localhost:8085"
$body = @{ subject = "local-admin"; clientKey = "admin"; scope = "media-intelligence:admin" } | ConvertTo-Json
$adminToken = (Invoke-RestMethod -Method Post -Uri "$baseUrl/api/v1/dev/token" -ContentType "application/json" -Body $body).accessToken
```

Continúa con [alta de clientes](client-onboarding-guide.md): permite únicamente
el perfil elegido, guarda la API key entregada una sola vez e intercámbiala en
`POST /api/v1/auth/token`. Usa ese JWT cliente para enviar trabajos y consultar
resultados, no el token admin. Producción necesita una identidad administradora
autorizada; el helper de desarrollo no está disponible allí.

`/api/v1/capabilities`, `/api/v1/profiles` y `/api/v1/skills` aceptan bearer token.
Los chequeos remotos requieren scope operator/admin; un token de desarrollo con
valores predeterminados no sustituye esos permisos.

## 4. Enviar Y Consultar Cuando Esté Autorizado

Los [contratos API](api.md) enlazan cada flujo. Video/imágenes productivos usan
selectores cloud; multipart sigue disponible para desarrollo. Consulta el estado,
lee `/result` e inspecciona `issues`, no solo status. Los documentos pueden
requerir revisión humana; la referencia API conserva estados, rutas de revisión
y headers de webhook.

[Operación y diagnóstico](operations.md) cubre logs y fallos.
No hace falta repetir trabajos cloud históricos para mostrar la demo.
