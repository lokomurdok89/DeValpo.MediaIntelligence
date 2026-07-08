# DeValpo MediaIntelligence Docs

Esta documentacion esta organizada para leer poco contexto y llegar rapido al modulo correcto.

## Lectura minima para Codex

1. [Contexto minimo](context/codex-minimal.md)
2. [Decisiones vigentes](context/active-decisions.md)
3. [Arquitectura actual](context/architecture-current.md)
4. [API](api.md)
5. Un modulo bajo `modules/` segun la tarea.

## Backend

- [Arquitectura actual](context/architecture-current.md)
- [API](api.md)
- [Modelo de datos](data-model.md)
- [Operacion](operations.md)

## AWS

- [Face search](modules/face-search.md)
- [Cloud assets](modules/cloud-assets.md)
- [AWS Rekognition face search](aws-rekognition-face-search.md)
- [Runbook AWS Rekognition](runbooks/aws-rekognition.md)

## Google

- [Document analysis](modules/document-analysis.md)
- [Video search](modules/video-search.md)
- [Google Document AI runbook](runbooks/google-document-ai.md)
- [Tokens y secretos Google](reference/local-tokens-and-google-secrets.md)

## QA y release

- [QA](qa.md)
- [QA Review](qa-review.md)
- [Release y migraciones](runbooks/release-and-migrations.md)
- [GitHub CI/CD](github-ci-cd.md)

## CloudAssets

- [Cloud assets](modules/cloud-assets.md)
- [Referencias cloud y staging](cloud-asset-references-and-staging.md)
- [Credenciales y perfiles cloud](reference/cloud-identities-and-provider-profiles.md)

## Referencia larga

Los documentos extensos viven en [reference](reference/api-endpoint-flows.md) y solo deben abrirse cuando una tarea lo requiera.

## Archivo historico

Planes antiguos, QA por fases y scopes cerrados viven en `archive/`.
