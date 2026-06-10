---
name: implement-task
description: Run the TArenaUnity3D local task implementation workflow.
---

# Implement Task

Use for formal local task execution from `_codex/tasks/`.

Workflow:

1. Read `AGENTS.md`, the selected task, `_codex/agents/coding-agent.md`, and relevant runbooks.
2. Inspect only relevant files.
3. Implement the smallest safe change.
4. Create one completion protocol in `_codex/tasks/QA/`.
5. Run local `qa-review` if the task is code or architecture work.
6. Append a dated implementation summary to the task.

Do not use another project's tasks, agents, or context by default.
