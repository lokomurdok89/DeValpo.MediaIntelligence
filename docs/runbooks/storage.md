# Storage Runbook

## Symptoms

- `/ready` reports `local-storage` as `Unhealthy`.
- Uploads fail before jobs are queued.
- Jobs fail when opening stored media assets.

## First Checks

```bash
curl http://localhost:8085/ready
docker compose -f deploy/docker-compose.yml ps
docker compose -f deploy/docker-compose.yml logs media-intelligence-api --tail 100
```

Confirm configuration:

```text
Storage__Provider=Local
Storage__LocalRootPath=/app/storage/media-assets
```

## Common Causes

- Storage directory cannot be created.
- Docker volume is not mounted.
- Container user cannot write to the configured path.
- Host disk is full.

## Remediation

1. Confirm the `media_assets` Docker volume is mounted.
2. Confirm the configured root path stays inside the intended storage root.
3. Restart the API container after mount/config changes.
4. Re-run `/ready` and a real PDF upload.

## Safety

- Do not move or delete stored media assets without understanding retention requirements.
- Logs must not include document text or raw file content.
