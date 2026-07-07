# Roadmap

The goal is to move from a locally validated MVP to an operable product for real clients.

## Current State

- API and PostgreSQL start with Docker Compose.
- JWT protects functional endpoints.
- Google Document AI processes real documents.
- Jobs, results, human review and webhooks are persisted.
- Runbooks and phase-based QA exist.

## Next Phases

| Phase | Expected Outcome |
| --- | --- |
| Product security | Rate limiting, real issuer, managed secrets and complete audit. |
| Production operations | Staging, monitoring, traces, backups and controlled deployment. |
| Real QA | Real-document matrix by client and use case. |
| Client portal | Surface to generate credentials, inspect jobs and review results. |
| Scaling | Dedicated workers, external queues and managed storage. |

## Base Documents

- [Productization plan](productization-plan.md)
- [Product final phase plan](product-final-phase-plan.md)
- [Release scope v0.1.0-rc.1](release-scope-v0.1.0-rc.1.md)
