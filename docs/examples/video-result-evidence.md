# Resultados De Video Guardados — Evidencia Anonimizada

## Piloto Facial S3 Ejecutado — 2026-11-09

El usuario autorizó buscar en el video S3 de 1:12 con su foto de referencia
existente usando la API actual. Se conservaron dos respuestas API reales separadas:

| Entrada | Estado API | Filas coincidentes | Issues | Interpretación |
| --- | --- | ---: | ---: | --- |
| Original MP4 / HEVC, 72,040 s | `Completed` | 0 | 1 | `AWS_REKOGNITION_FACE_SEARCH_FAILED`: `Unsupported codec/format`; no es una búsqueda vacía exitosa |
| Copia MP4 / H.264 autorizada, 72,072 s | `Completed` | 39 | 0 | Búsqueda facial completada; 32 timestamps distintos |

Se convirtió una copia sin modificar el original, se subió al mismo bucket S3
privado con cifrado AES256 del lado del servidor y se envió mediante
`POST /api/v1/face-search-jobs`. Ambos selectores usaron `ExplicitAssets`;
se reutilizó la foto de referencia de S3 con `targetKey` opaco y
`minSimilarity: 80`. Los archivos no atravesaron la API como uploads.

La respuesta exitosa contiene registros puntuales entre **6,506 s y 71,871 s**,
con similitud **81,23–99,99 / 100**. Se creó a las **19:44:03 UTC** y terminó a
las **19:45:05 UTC**. Son tiempos de esta ejecución, no un benchmark.
Los timestamps repetidos se conservan en el JSON original y el visor;
no se afirma deduplicación ni verificación de identidad.

Tras cada ejecución, la colección informó **FaceCount: 0**. No se borraron
objetos S3; el ciclo de vida sigue siendo responsable de su retención.
Se compiló y levantó el backend existente con PostgreSQL local aislado, sin
cambiar código backend, contratos ni permisos. No se repitieron trabajos
históricos ni ejecuciones Google.

Los bytes originales de las respuestas API, recibos, evidencia de conversión y
subida y el video compatible están en `outputs/private/`, ignorado por Git y
excluido de los contextos de build Docker. No se publican. La galería asocia
la copia compatible a su propio JSON y conserva los dos videos cortos y sus
resultados existentes. Se verificó en el navegador el salto a **32,966 s** y
**71,871 s**, y la reproducción desde la coincidencia elegida.
El visor no llama a proveedores.

Requisito de códec: [documentación AWS de video almacenado](https://docs.aws.amazon.com/rekognition/latest/dg/video.html).

## Qué Se Encontró

El registro histórico de QA documenta un trabajo de video de Google Video Intelligence con estado `Completed`, **60 segmentos para el objetivo reportado**, **2.819 ocurrencias genéricas de caras** y **0 issues**.

Fuente histórica (retirada del árbol de trabajo; conservada en Git): `docs/qa-review.md`, sección **AWS Rekognition Disabled-Mode QA → Runtime Evidence**, revisión del **2026-07-07**. Revisión del repositorio inspeccionada: `2c27a288e459378514b52f676c16080c95be250e`. Resumen preparado el 2026-11-09. La fuente es un registro escrito de QA, no la respuesta original del proveedor. La fecha corresponde a la revisión, no a un instante de procesamiento verificado independientemente.

## Cómo Interpretarlo

| Observación | Significado | No demuestra |
| --- | --- | --- |
| `Completed` | Estado terminal registrado | Que todos los trabajos actuales funcionen |
| 60 segmentos coincidentes | Intervalos devueltos para el objetivo reportado | 60 videos, personas o coincidencias correctas verificadas manualmente |
| 2.819 ocurrencias de caras | Detecciones genéricas a lo largo del tiempo | Personas únicas o identificación contra una referencia |
| 0 issues | No se reportaron issues para ese resultado | Precisión/recall medidos o ausencia de todo defecto |

La utilidad es entregar momentos a los que puede saltar un reproductor externo. No se reconstruyen timestamps, confianza, duración de clips ni enlaces: el resultado bruto no está disponible entre los archivos inspeccionados.

## Alcance Y Limitaciones

Los siguientes puntos describen la recuperación histórica de archivos,
anterior al piloto cloud autorizado descrito arriba.

- La búsqueda inicial, limitada al repositorio, no encontró los medios. Una búsqueda posterior de nombres de archivo en la carpeta superior, autorizada por el usuario, encontró JSON AWS y una carpeta local de pruebas fuera del repositorio. Solo se analizaron resultados pertinentes, no secretos ni contenidos de otros proyectos. No se accedió a servicios, bases de datos ni storage cloud. El JSON original de Google sigue sin localizarse.
- No se hicieron pruebas end-to-end, llamadas a proveedores, subidas ni repeticiones del trabajo.
- La misma sección QA registra otro caso prolongado con error del proveedor. Seleccionar este resultado completado no implica fiabilidad universal.
- El registro no acredita si el origen fue multipart, S3 o GCS. El diagrama S3 del README explica la implementación de referencias cloud de forma independiente.
- Es evidencia de OCR/detección genérica en video Google, **no de reconocimiento facial AWS** ni certificación del piloto configurable de fotos.
- No se infieren precisión, costo, rendimiento ni preparación actuales.

## Extracción Segura Para Documentación

Solo se reproducen fecha de revisión, proveedor, estado y conteos agregados. Se excluyen identificadores/números objetivo, IDs de trabajos, nombres de archivos, identificadores/rutas cloud, credenciales y medios originales. Es un resumen, no JSON API inventado. La sección AWS usa conteos y límites temporales de archivos guardados. Publicar los medios originales sigue requiriendo autorización explícita; encontrar un archivo no autoriza publicar su contenido.

## Evidencia De La Integración S3

Estos hechos de inspección del código respaldan el flujo documentado, independientemente del resultado histórico:

- `VideoSearchJobsController`: acepta `assetSelectorJson` en form, valida acceso de cliente/perfil, devuelve aceptación y permite consultas del propietario.
- `CloudAssetSelectorParser`: acepta selectores S3 y rechaza campos URL.
- `AssetReferenceResolver`: aplica allowlists de buckets; no demuestra aislamiento completo por cliente/prefijo.
- `GoogleVideoIntelligenceSearchProvider`: lee referencias cloud y realiza staging de videos no GCS en GCS antes de Google Video Intelligence.
- `AwsRekognitionFaceSearchProvider`: flujo separado de video facial mediante S3.

Son hechos de inspección del código, no una nueva demostración ejecutada sobre S3.

## Archivos AWS Guardados

La inspección local ampliada del 2026-11-09 encontró estos archivos fuera del repositorio:

| Nombre local de la fuente | Contenido verificado |
| --- | --- |
| `face-search-api-result-threshold-80.json` | Respuesta API: `Completed`, 18 coincidencias faciales, sin issues, proveedor `AwsRekognition`; fecha de finalización 2026-07-08 |
| `face-search-threshold-summary.json` | Cinco umbrales históricos (80, 70, 60, 50, 40), cada uno con 18 coincidencias; el umbral 80 comparte ID con la respuesta API |
| `aws-face-search-result.json` | Respuesta del proveedor: `SUCCEEDED`, referencia S3, 86 entradas de personas, ninguna con coincidencias faciales |
| `face-search-video-compare.json` | Dos filas: una con 0 coincidencias y campo de issues no vacío; otra con 18 y campo de issues vacío |

El nombre de video de la respuesta API coincide con un archivo de la carpeta
local de pruebas. Es una asociación por nombre, no una verificación de hash
contra S3. Los JSON se decodificaron localmente (incluidas exportaciones UTF-16),
sin copiar sus originales al repositorio.

Las similitudes de la respuesta API abarcan **98,63–99,94**, escala 0–100.
Hay **18 ocurrencias puntuales**, desde **0,000 s** hasta **8,508 s**, con inicio
igual a fin. No son clips continuos, personas distintas, verdaderos positivos
verificados manualmente ni una precisión medida del 98%.

El ID de trabajo del proveedor AWS y el de la aplicación pertenecen a espacios
distintos; no se recuperó su mapeo. Los IDs del archivo comparativo tampoco
coinciden con esta respuesta API. No se deben unir como una única ejecución ni
inferir que éxito del proveedor significa coincidencias faciales. La referencia
S3 confirma su presencia como entrada del proveedor, no toda la traza de envío API.

Google mantiene evidencia documental de QA; AWS tiene evidencia JSON local.
La recuperación histórica no consultó el estado cloud ni el comportamiento
actual del proveedor; el piloto separado descrito arriba documenta la ejecución
posterior. Se excluyen de estas páginas y ambos README los nombres de
buckets, claves de objetos, IDs de trabajo/cliente/consentimiento, referencias,
FaceId y medios originales.

## Registro Histórico Adicional De Instalación Google

La guía de instalación de video retirada registra una ejecución Google Video
Intelligence del **2026-07-07**: **774,23 MB**, **1 video**, bucket GCS accesible,
`Completed`, **1 intento** y aproximadamente **24 minutos 50 segundos**.
Es historia documental, no una ejecución nueva, readiness actual ni benchmark.
No se recuperó ID/JSON que permita afirmar que sea el mismo trabajo del registro
de 60 segmentos anterior. La exportación original sigue privada y sin localizar.

Fuente conservada en Git: `docs/reference/video-mvp-real-route.md` y su versión
inglesa en la revisión `2c27a288e459378514b52f676c16080c95be250e`.
La instalación vigente está en [búsqueda de video Google](../video-target-search.md).
