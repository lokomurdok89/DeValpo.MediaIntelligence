# PostgreSQL Runbook

## Symptoms

- `/ready` reports `postgres` as `Unhealthy`.
- API startup fails while applying migrations.
- Job, review or webhook endpoints return persistence errors.

## First Checks

```bash
docker compose -f deploy/docker-compose.yml ps
docker compose -f deploy/docker-compose.yml logs postgres --tail 100
curl http://localhost:8085/ready
```

Validate migrations:

```bash
dotnet ef migrations has-pending-model-changes \
  --project src/DeValpo.MediaIntelligence.Infrastructure \
  --startup-project src/DeValpo.MediaIntelligence.Api
```

## Common Causes

- PostgreSQL container is not healthy.
- Connection string points to the wrong host or port.
- Database credentials differ from Compose environment variables.
- Migration failed during startup.
- Disk or Docker volume is unavailable.

## Remediation

1. Restart only PostgreSQL if the API is healthy but DB is not.
2. Check `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` and `ConnectionStrings__MediaIntelligence`.
3. Review API logs for migration errors.
4. Do not delete Docker volumes unless data loss is acceptable for the environment.

## Safety

- Never run destructive DB commands in shared or production-like environments without a backup.
- Capture the `/ready` response and correlation id before changing configuration.
