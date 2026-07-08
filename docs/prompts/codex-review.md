# Codex Review Prompt

Use this to review a PR or uncommitted changes.

```text
Review the current PR or local changes.

Read:
- docs/context/AI_ONBOARDING.md
- docs/context/ACTIVE_DECISIONS.md
- git diff / PR diff
- Relevant module docs only if needed

Focus on:
- Bugs and behavioral regressions
- Contract or architecture violations
- Security and credential risks
- Missing tests or weak validation
- Docs/state updates that should accompany the change

Output:
1. Findings ordered by severity, with file and line references
2. Open questions
3. Missing tests or validation
4. Short change summary
```
