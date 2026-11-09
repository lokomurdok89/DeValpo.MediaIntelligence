# Dirección Del Producto

## Lo Que Ya Tenemos

Un backend cloud-first con referencias privadas S3/GCS, procesamiento asíncrono,
resultados persistidos y adaptadores Google/AWS. El visor local permite abrir un
video con su resultado AWS guardado y saltar a una coincidencia sin llamar a AWS.

Consulta los [resultados existentes](examples/video-result-evidence.md) y el
[alcance actual](specs/s3-video-face-search-map.md).

## Siguiente Fase — Integración YOLO

Integrar YOLO para detección de objetos, con el procesamiento en tiempo real
como dirección prevista. Requiere un nuevo adaptador y orquestación de streaming;
ninguno está implementado. El mismo enfoque de extensión permite incorporar
otros modelos, herramientas o motores, adaptando casos de uso y contratos cuando
sea necesario.

## Demo Existente Y Extensiones Posteriores

- La galería de tres videos ya está implementada. La ejecución S3 autorizada
  del 2026-11-09 añadió 39 registros AWS seleccionables para la copia H.264 de
  1:12, junto a los resultados guardados de los videos cortos. No hace falta
  volver a procesar para mostrarla.
- Recuperar el JSON original de Google si aparece; no atribuirle el de otro
  video ni inventar una línea temporal a partir del resumen histórico.
- Mantener reconocimiento facial como experiencia principal y mostrar también
  lo encontrado por Google, distinguiendo detección de caras de identidad facial.
- Después, desarrollar el piloto de dorsales y ampliar la búsqueda configurable
  a palabras y códigos. Estas extensiones no se presentan como implementadas.

La arquitectura permite extender proveedores y consumidores. Los workers
actuales se alojan en la API; escalar su ejecución de forma distribuida sería
trabajo posterior, no una capacidad ya demostrada.

Sin refactor general, más ejecuciones cloud ni repetición de resultados
históricos para ver la demo guardada. No hay un plan de fases ni certificación que
bloquee mostrar lo que ya funciona.
