# Code Review Guide

Review findings first and ground them in a file, contract, test or command.
Prioritize product and security impact over style.

## Review Checklist

- Scope is limited to the requested behavior; public compatibility is explicit.
- Layer ownership is preserved and provider/EF types do not cross boundaries.
- Authentication, tenant access, input validation, retention, audit and secrets
  are considered for affected paths.
- Configuration uses options/environment sources, not embedded production values.
- State transitions, retries, cancellation and error handling remain coherent.
- Tests cover changed behavior at the appropriate level.
- Documentation, migration implications and operational impact are addressed.
- Logs, errors, payloads and examples do not reveal sensitive data.

## Required Review Output

List findings by severity with evidence and impact. Then state assumptions,
remaining test gaps and a concise change summary. If no issues are found, say so
and name residual risk rather than implying exhaustive certification.
