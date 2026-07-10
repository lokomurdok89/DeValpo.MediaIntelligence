# Definition Of Done

Apply the relevant items; not every change requires provider smoke tests or a
migration.

- Requested scope is complete and out-of-scope behavior was not changed.
- Architecture boundaries and public compatibility were reviewed.
- Configuration has no embedded credentials, provider identifiers or local paths.
- Focused tests were added or updated when behavior changed.
- Required build/test, migration-drift, Docker or documentation checks were run,
  or explicitly reported as not run with a reason.
- Migrations were reviewed for persistence-model changes; none were created or
  applied without authorization.
- Documentation, API contracts and runbooks reflect the change where relevant.
- Security, authorization, sensitive logging and data handling were considered.
- Cloud smoke validation was performed only when provider/cloud behavior changed
  and an approved environment was available.
- Remaining risks, unsupported paths and validation gaps are reported.
- `git diff --check` passes and the final Git status is reported.
