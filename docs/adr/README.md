# Architecture Decision Records

An ADR records a durable architectural choice and its tradeoffs. Create one
before or with a decision that changes public contracts, persistence, provider
boundaries, security posture, deployment model, operational model or a costly
long-term dependency. Do not create an ADR for routine implementation detail.

Existing decisions are not automatically ADRs. Candidate retrospective records
are tracked in `docs/backlog/ENGINEERING_BACKLOG.md`.

## Template

```md
# ADR-NNNN: Short Decision Name

Status: Proposed | Accepted | Superseded
Date: YYYY-MM-DD

## Context

What situation forced a decision?

## Options

- Option A
- Option B
- Option C

## Decision

What option was selected and why?

## Consequences

What improves, what becomes harder and what must be watched?

## Evidence

Links to code, validation, operational constraints or related documentation.
```
