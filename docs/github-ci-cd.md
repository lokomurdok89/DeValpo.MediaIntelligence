# GitHub CI/CD

The current workflow is `.github/workflows/ci.yml`.

It validates:

- checkout
- .NET 10 setup
- restore
- .NET tool restore
- release build
- tests
- EF migration drift
- secret scan
- release configuration safety
- Docker build
- Docker Compose config
- Trivy container vulnerability scan

It does not deploy automatically.

## Railway extension

Future Railway deployment should reuse the Dockerfile and provide environment variables for PostgreSQL, storage and Google Document AI. Automatic deploy secrets are intentionally not configured yet.

## Google Cloud extension

Future Google Cloud deployment can target Cloud Run with the existing Dockerfile, `/health` endpoint and environment-based configuration. Service account credentials must be provided through managed secrets, not committed files.
