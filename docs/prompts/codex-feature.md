# Codex Feature Prompt

Use this for one new implementation in Codex.

```text
Implement one feature only in this conversation.

Read first:
- docs/context/AI_ONBOARDING.md
- docs/context/PROJECT_CONTEXT.md
- docs/context/ACTIVE_DECISIONS.md
- docs/context/CURRENT_STATE.md
- The relevant docs/specs file for this feature

Do not read secrets/ or site/.
Do not touch unrelated code.
Keep the change scoped to existing layers.

Task:
[Paste the feature spec.]

Expected:
- Implement the feature.
- Add or update focused tests.
- Run relevant validation.
- Update docs/context/CURRENT_STATE.md if state changes.
- Prepare commit message and PR summary.
- Do not commit or open a PR unless explicitly asked.
```
