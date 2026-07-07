# GitHub CI/CD

The current application workflow is `.github/workflows/ci.yml`.

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

It does not deploy the application automatically.

## Documentation Pages

Documentation is built by `.github/workflows/docs.yml`.

The docs workflow:

- installs Python documentation dependencies from `requirements-docs.txt`
- runs `mkdocs build --strict`
- uploads the generated `site` artifact as `mkdocs-site`
- deploys GitHub Pages only on pushes to `main` when repository variable `DEPLOY_GITHUB_PAGES` is `true`

The current repository is private. If the GitHub account plan does not support Pages for private repositories, the public Pages URL will return `404`. To publish Pages, either make the repository public or use a plan that supports private Pages, then configure Pages source as **GitHub Actions** and set repository variable `DEPLOY_GITHUB_PAGES=true`.

Local validation:

```powershell
pip install -r requirements-docs.txt
mkdocs build --strict
```

## Railway Extension

Future Railway deployment should reuse the Dockerfile and provide environment variables for PostgreSQL, storage and Google Document AI. Automatic deploy secrets are intentionally not configured yet.

## Google Cloud Extension

Future Google Cloud deployment can target Cloud Run with the existing Dockerfile, `/health` endpoint and environment-based configuration. Service account credentials must be provided through managed secrets, not committed files.
