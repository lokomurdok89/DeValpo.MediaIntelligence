# Plan Fase Por Fase Para Producto Final

Este documento define el plan operativo para llevar DeValpo MediaIntelligence desde el MVP actual hasta un producto final usable por clientes reales.

Regla principal: no se avanza a la siguiente fase hasta que la fase actual tenga implementacion completa, QA ejecutado, hallazgos corregidos o registrados como fuera de alcance aprobado, validaciones automatizadas pasando y PR mergeada.

## Protocolo De Trabajo

Cada fase se ejecuta con el mismo flujo:

1. Crear rama dedicada desde `main`.
2. Revisar el alcance de la fase contra `docs/productization-plan.md`.
3. Confirmar criterios QA usando `docs/devalpo-mediaintelligence-qa/references/quality-gates.md`.
4. Implementar solo lo que pertenece a la fase.
5. Ejecutar validaciones automaticas.
6. Ejecutar QA real definido para la fase.
7. Corregir Blockers, Criticals y Majors dentro del alcance.
8. Documentar evidencia QA en `docs/qa-review.md` o en un archivo QA especifico de la fase.
9. Crear PR con resumen, validaciones y pendientes explicitos.
10. No iniciar la fase siguiente hasta que la PR este aprobada y mergeada.

## Reglas QA

QA puede bloquear avance de fase.

Severidades:

- Blocker: impide merge y avance.
- Critical: impide avance salvo decision explicita documentada.
- Major: debe corregirse si pertenece al alcance de la fase.
- Minor: puede quedar registrado si no afecta el objetivo de la fase.

Evidencia minima por fase:

- Branch y commit SHA.
- Comandos ejecutados.
- Ambiente usado: local, Docker, Development, Staging.
- Endpoints probados.
- HTTP status codes.
- Respuestas sanitizadas.
- Jobs/result ids cuando aplique.
- Resultado QA: pass/fail por criterio.

## Fase 0: Higiene De Base Y Limpieza Documental

Objetivo: dejar `main` en una base limpia antes de implementar features de producto.

Implementacion:

- Eliminar rutas absolutas personales de la documentacion.
- Mantener solo rutas relativas al repo o rutas contractuales Docker.
- Confirmar que no hay secretos, tokens, credenciales JSON ni rutas locales personales en el diff.
- Confirmar que PRs previas estan mergeadas o cerradas correctamente.

QA:

- Buscar rutas personales:
  - `rg "C:\\Users\\|AppData|\\.codex|Downloads\\" README.md docs deploy src tests`
- Buscar secretos:
  - `rg "private_key|client_secret|accessToken|refresh_token|BEGIN PRIVATE KEY" .`
- Validar que `/run/secrets`, `/app`, `/var/lib/postgresql/data` solo aparezcan como rutas Docker.

Exit criteria:

- No hay rutas personales agregadas en docs o codigo.
- No hay secretos versionados.
- `git status` queda limpio.
- PR de limpieza mergeada.

## Fase 1: Hardening De Fundacion

Objetivo: remover atajos de MVP que puedan contaminar comportamiento productivo.

Implementacion:

- Refactorizar `GoogleDocumentAiClientFactory`.
- Separar proveedor de credenciales de la construccion del cliente Google.
- Eliminar codigo obsoleto o pragmas innecesarios.
- Hacer explicito el soporte ADC y el soporte de credencial por archivo montado.
- Normalizar nombre del perfil real a `generic-document-ocr-v1`.
- Mantener `invoice-extraction-v1` solo como alias temporal si se necesita compatibilidad.
- Gatear mocks por ambiente/configuracion.
- Hacer fallar startup si un perfil productivo depende de mocks.
- Completar recursos `.resx` pendientes si QA detecta nuevos textos visibles.

QA:

- Ejecutar build/test.
- Ejecutar busqueda de textos hardcoded visibles.
- Levantar Docker local.
- Validar Google status local y remoto.
- Ejecutar un PDF real con `generic-document-ocr-v1`.
- Confirmar que mocks no quedan disponibles en configuracion productiva.

Exit criteria:

- `dotnet build` pasa con 0 warnings.
- `dotnet test` pasa.
- Google Document AI procesa un PDF real.
- Produccion no registra mocks por defecto.
- QA no tiene Blockers/Criticals.

## Fase 2: API Keys Y Emision JWT Real

Objetivo: reemplazar el flujo local de token por autenticacion real para clientes.

Estado: implementada y mergeada en PR #9.

Implementacion:

- Crear modelo de API client.
- Agregar storage para API keys hasheadas.
- Implementar generacion de API key.
- Implementar rotacion, revocacion y expiracion.
- Nunca guardar API keys crudas.
- Crear endpoint de emision JWT usando API key.
- Mantener `/api/v1/dev/token` solo en Development.
- Agregar claims: `sub`, `client_key`, `scope`, `jti`, `iat`, `exp`.
- Documentar flujo en Swagger.

QA:

- Crear cliente real de prueba.
- Generar API key.
- Validar que la API key cruda solo aparece una vez.
- Validar que la DB guarda hash, no secreto.
- Pedir JWT con API key valida.
- Validar rechazo con API key invalida, revocada y expirada.
- Validar que `/api/v1/dev/token` no existe fuera de Development.

Exit criteria:

- Cliente puede obtener JWT real.
- Secretos no aparecen en logs, DB, Swagger ni tests.
- Keys pueden rotarse sin romper keys activas no revocadas.
- QA no tiene Blockers/Criticals.

Evidencia QA:

- `docs/qa-phase-2-customer-access-security.md`

## Fase 3: Tenant Isolation Y Autorizacion

Objetivo: impedir acceso cruzado entre clientes.

Estado: implementada y mergeada en PR #10.

Implementacion:

- Enlazar jobs, assets, results, review tasks y webhooks a client/tenant.
- Rechazar requests donde `clientKey` del body no coincida con el JWT.
- Autorizar perfiles por cliente.
- Agregar policies: client, operator, admin.
- Proteger status remoto Google con scope operativo.
- Asegurar queries filtradas por cliente.

QA:

- Crear dos clientes reales de prueba.
- Cliente A sube documento.
- Cliente B intenta leer job/result de A y debe recibir 403 o 404 controlado.
- Cliente sin permiso intenta ejecutar perfil no asignado.
- Operador/admin accede a diagnosticos permitidos.
- Revisar logs para confirmar que no exponen datos sensibles.

Exit criteria:

- No existe lectura cruzada entre clientes.
- No existe ejecucion de perfiles no autorizados.
- Policies documentadas en Swagger.
- QA no tiene Blockers/Criticals.

Evidencia QA:

- `docs/qa-phase-3-tenant-isolation.md`

## Fase 4: Jobs Asincronos Y Worker

Objetivo: convertir el analisis en flujo recuperable, auditable y seguro para documentos reales.

Estado: implementada y mergeada en PR #11.

Implementacion:

- Cambiar `/api/v1/analyze` para encolar y responder `202 Accepted`, o definir modo sync explicito.
- Crear background worker.
- Agregar estados: `Pending`, `Running`, `Completed`, `Failed`, `Cancelled`, `RequiresReview`.
- Agregar retry para errores transientes de proveedor.
- Persistir transiciones de estado.
- Exponer endpoint de consulta por job id.

QA:

- Enviar PDF real y verificar `202`.
- Consultar job hasta `Completed`.
- Simular error transiente y validar retry.
- Simular error permanente y validar `Failed` estable.
- Reiniciar API durante procesamiento y validar recuperacion o estado consistente.

Exit criteria:

- Job puede consultarse despues del envio.
- Estados son consistentes.
- Fallos de proveedor no dejan jobs colgados.
- QA no tiene Blockers/Criticals.

Evidencia QA:

- `docs/qa-phase-4-async-jobs-worker.md`

## Fase 5: Ingestion Real De Archivos Y Normalizacion

Objetivo: hacer robusto el manejo de documentos reales.

Estado: implementada y mergeada en PR #12.

Implementacion:

- Validar extension permitida.
- Validar content type real.
- Detectar PDF corrupto.
- Detectar archivo vacio.
- Controlar tamano por configuracion.
- Guardar texto normalizado y metadata relevante.
- Registrar issues machine-readable.
- Preparar hook futuro para antivirus.

QA:

- Probar PDF valido pequeno.
- Probar PDF multipagina.
- Probar PDF escaneado.
- Probar imagen documento.
- Probar archivo vacio.
- Probar PDF corrupto.
- Probar extension no soportada.
- Probar archivo sobredimensionado.

Exit criteria:

- Archivos invalidos devuelven errores controlados.
- API no crashea con corruptos.
- Resultado real queda persistido con metadata util.
- QA no tiene Blockers/Criticals.

## Fase 6: Human Review Operativo

Objetivo: convertir la revision humana en flujo usable.

Estado: implementada y mergeada en PR #13.

Implementacion:

- Crear endpoints para listar tareas pendientes.
- Obtener detalle de tarea.
- Aprobar, corregir o rechazar resultado.
- Guardar operador, timestamp y comentario.
- Mantener resultado original y resultado corregido.
- Agregar estados de review: `Pending`, `InReview`, `Completed`, `Rejected`.

QA:

- Forzar documento con baja confianza.
- Validar creacion de tarea de revision.
- Operador toma tarea.
- Operador corrige campos.
- Validar auditoria original vs corregido.
- Validar que cliente ve resultado final autorizado.

Exit criteria:

- Low confidence abre review task.
- Review puede completarse por API.
- Auditoria queda persistida.
- QA no tiene Blockers/Criticals.

## Fase 7: Webhooks Y Entrega De Resultados

Objetivo: entregar resultados a sistemas externos de forma confiable.

Estado: implementada y mergeada en PR #14.

Implementacion:

- Crear configuracion de webhook por cliente.
- Firmar payloads.
- Registrar intentos.
- Implementar retry con backoff.
- Exponer historial de intentos.
- Documentar payloads y headers.

QA:

- Levantar receptor webhook local.
- Completar job y validar callback.
- Validar firma.
- Simular receptor caido.
- Validar retry.
- Validar que payload no contiene secretos.

Exit criteria:

- Callback real llega a receptor.
- Fallos son visibles y retryables.
- Firma verificable.
- QA no tiene Blockers/Criticals.

## Fase 8: Observabilidad, Readiness Y Operacion

Objetivo: hacer diagnosticable el sistema sin leer codigo.

Estado: implementada y mergeada en PR #15.

Implementacion:

- Logs estructurados con correlation id, job id, client key, profile key y provider.
- Evitar contenido de documentos en logs.
- Agregar metricas basicas.
- Separar `/health` de `/ready`.
- Cachear status remoto de proveedores con timeout.
- Crear runbooks de fallos Google, DB y storage.

QA:

- Validar `/health` sin dependencias pesadas.
- Validar `/ready` con DB y provider configurado.
- Cortar DB y validar readiness fallido.
- Forzar error Google y validar clasificacion.
- Revisar logs sanitizados.

Exit criteria:

- Incidente basico diagnosticable por logs/status.
- Readiness distingue fallos reales.
- No hay secretos ni texto OCR en logs.
- QA no tiene Blockers/Criticals.

## Fase 9: Seguridad, Gobernanza Y Retencion

Objetivo: tratar documentos de clientes como datos sensibles.

Estado: implementada en PR #16 desde la rama `codex/phase-9-security-governance-retention`.

Implementacion:

- Politica de retencion por cliente.
- Borrado o archivado de assets.
- Auditoria de auth, lectura de resultados, cambios review y rotacion de keys.
- Redaccion de logs.
- Reglas para PII.
- Preparacion para secret manager productivo.

QA:

- Validar auditoria de acciones criticas.
- Validar que retencion elimina o marca datos segun politica.
- Validar que secretos no se loguean.
- Validar que operadores solo acceden por rol.

Exit criteria:

- Acciones sensibles auditadas.
- Retencion ejecutable y verificable.
- QA no tiene Blockers/Criticals.

## Fase 10: CI/CD, Staging Y Release

Objetivo: poder desplegar una version candidata fuera del ambiente local.

Estado: implementada en PR #17 desde la rama `codex/phase-10-cicd-staging-release`.

Implementacion:

- GitHub Actions para build, test y docker build.
- Compose config validation.
- Secret scanning.
- Migration validation.
- Container vulnerability scan.
- Configuracion staging.
- Runbook de migraciones.
- Deshabilitar Swagger o protegerlo en produccion.
- Validar que Production no arranca con secrets dummy.

QA:

- Ejecutar pipeline en PR.
- Desplegar staging.
- Ejecutar smoke test real.
- Validar endpoint protegido.
- Validar Google Document AI con credenciales de staging.
- Validar rollback/runbook basico.

Exit criteria:

- Release candidata desplegada en staging.
- Pipeline bloquea fallos criticos.
- Produccion no permite configuracion insegura.
- QA no tiene Blockers/Criticals.

## Fase 11: Certificacion De Producto

Objetivo: declarar el producto listo para primer cliente real.

Estado: en implementacion en la rama `codex/phase-11-product-certification`.

Implementacion:

- Congelar scope de release.
- Revisar todos los quality gates.
- Completar matriz QA con documentos reales.
- Completar documentacion de cliente.
- Completar documentacion operacional.
- Preparar version taggeada.

QA:

- Ejecutar suite completa real:
  - Auth.
  - Tenant isolation.
  - Google provider.
  - File ingestion.
  - Async job lifecycle.
  - Human review.
  - Webhooks.
  - Observabilidad.
  - Seguridad.
  - Staging deployment.

Exit criteria:

- Todos los Blockers, Criticals y Majors corregidos.
- Pendientes menores documentados.
- Version taggeada.
- Producto listo para onboarding controlado del primer cliente.

## Orden De Implementacion

1. Fase 0: higiene base.
2. Fase 1: hardening fundacion.
3. Fase 2: API keys y JWT real.
4. Fase 3: tenant isolation.
5. Fase 4: jobs asincronos.
6. Fase 5: ingestion real.
7. Fase 6: human review.
8. Fase 7: webhooks.
9. Fase 8: observabilidad.
10. Fase 9: seguridad y gobernanza.
11. Fase 10: CI/CD y staging.
12. Fase 11: certificacion producto.

## Primera Fase A Ejecutar

La primera fase activa debe ser Fase 0.

No se debe iniciar Fase 1 hasta que:

- Las rutas locales personales esten eliminadas o convertidas a placeholders.
- La PR correspondiente este mergeada.
- `main` este actualizado localmente.
- La evidencia QA de Fase 0 este documentada.
