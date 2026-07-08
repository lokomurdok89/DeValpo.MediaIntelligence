# QA Fase 0: Higiene De Base Y Limpieza Documental

Fecha: 2026-07-06

Branch: `codex/phase-0-base-hygiene`

## Alcance

Esta revision valida que la documentacion y el repositorio no mantengan rutas personales del ambiente local ni secretos versionados antes de avanzar a hardening de producto.

## Cambios Revisados

- `README.md`: se reemplazo la referencia a una ruta local de otro repositorio por una descripcion portable.
- `docs/local-tokens-and-google-secrets.md`: se reemplazaron rutas personales de Windows por variables o placeholders portables.

## Validaciones Ejecutadas

### Build Y Tests

```powershell
dotnet build
dotnet test
```

Resultado obtenido:

- Build pasa con 0 warnings y 0 errores.
- Tests pasan: 14/14.

### Rutas Personales

```powershell
$terms = @('C:' + '\Us' + 'ers\', 'App' + 'Data', '.co' + 'dex', 'Down' + 'loads\')
$terms | ForEach-Object { rg -n --fixed-strings $_ README.md docs deploy src tests --glob '!**/bin/**' --glob '!**/obj/**' }
```

Resultado esperado:

- Sin matches.

Resultado obtenido:

- Sin matches.

### Secretos O Credenciales

```powershell
$terms = @('private' + '_key', 'client' + '_secret', 'access' + 'Token', 'refresh' + '_token', 'BEGIN PRIVATE' + ' KEY', '---' + '--BEGIN', 'token' + '_usage', 'authorization' + ' code')
rg -n ($terms -join '|') . --glob '!**/.git/**' --glob '!**/bin/**' --glob '!**/obj/**' --glob '!secrets/**'
```

Resultado obtenido:

- No se encontraron secretos reales, llaves privadas, refresh tokens ni client secrets.
- Los matches restantes del nombre de propiedad de token son placeholders, nombres de propiedad o variables documentales.

### Rutas Absolutas Permitidas

```powershell
$drivePattern = '[A-Za-z]:' + '\\'
rg -n $drivePattern README.md docs deploy src tests --glob '!**/bin/**' --glob '!**/obj/**' --glob '!docs/qa-phase-0-base-hygiene.md'

$terms = @('C:/', 'file://', '/home/', '/Us' + 'ers/', '/mnt/', '/tmp/', '/run/', '/app/', '/var/', 'App' + 'Data', '.co' + 'dex', 'Us' + 'ers\', 'Down' + 'loads\')
$terms | ForEach-Object { rg -n --fixed-strings $_ README.md docs deploy src tests --glob '!**/bin/**' --glob '!**/obj/**' --glob '!docs/qa-phase-0-base-hygiene.md' }
```

Resultado obtenido:

- Las rutas restantes son rutas contractuales de contenedor Docker:
  - `/run/secrets`
  - `/app/storage/media-assets`
  - `/app/publish`
  - `/var/lib/postgresql/data`

## Resultado QA

Pass.

No hay Blockers, Criticals ni Majors abiertos para Fase 0.

## Decision

Fase 0 queda lista para PR y revision. No se debe iniciar Fase 1 hasta que esta PR este mergeada contra `main`.
