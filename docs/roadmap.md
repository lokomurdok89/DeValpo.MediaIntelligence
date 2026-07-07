# Roadmap

El objetivo es pasar de MVP local validado a producto operable para clientes reales.

## Estado Actual

- API y PostgreSQL levantan con Docker Compose.
- JWT protege endpoints funcionales.
- Google Document AI procesa documentos reales.
- Jobs, resultados, human review y webhooks estan persistidos.
- Runbooks y QA por fases existen.

## Siguientes Fases

| Fase | Resultado esperado |
| --- | --- |
| Seguridad producto | Rate limiting, issuer real, gestion de secretos y auditoria completa. |
| Operacion productiva | Staging, monitoreo, trazas, backups y deploy controlado. |
| QA real | Matriz de documentos reales por cliente/caso de uso. |
| Portal cliente | Superficie para generar credenciales, ver jobs y revisar resultados. |
| Escalamiento | Workers dedicados, colas externas y storage gestionado. |

## Documentos Base

- [Productization plan](productization-plan.md)
- [Product final phase plan](product-final-phase-plan.md)
- [Release scope v0.1.0-rc.1](release-scope-v0.1.0-rc.1.md)
