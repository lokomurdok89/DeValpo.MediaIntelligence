# Codex Bugfix Prompt

Use this for a small bugfix.

```text
Fix one bug only.

Read:
- docs/context/AI_ONBOARDING.md
- docs/context/ACTIVE_DECISIONS.md
- The smallest set of files needed to reproduce or understand the bug

Do not read secrets/ or site/.
Do not refactor unrelated code.
Do not change public contracts unless required and called out.

Bug:
[Describe the observed behavior, expected behavior and evidence.]

Expected:
- Minimal code change.
- Focused regression test when practical.
- Relevant validation output.
- Short risk note.
- No commit unless explicitly asked.
```
