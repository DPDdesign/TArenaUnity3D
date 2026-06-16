# TArenaUnity3D Agents

This folder contains project-local agent instructions for TArenaUnity3D.

## Workspace Identity

This workspace is TArenaUnity3D, not Retsot Horde.

Do not use context, tasks, agents, or markdown files from another project unless
the user explicitly asks for comparison or migration.

Chat/task titles for this project must start with `[TARENA]`.

Use TextMesh Pro only for UI and code text references in this project. Prefer
TMP types such as `TMP_Text` and `TextMeshProUGUI`, never legacy
`UnityEngine.UI.Text`.

## Default Agent

Use `_codex/agents/coding-agent.md` for normal Unity/C# work.

Use `_codex/agents/project-director-agent.md` for production direction,
workflow setup, task ordering, and scope control.

Use `_codex/agents/qa-architecture-review-agent.md` for architecture review
after a completed implementation protocol.

## Shared Sources

All project agents may use these local files:

- `AGENTS.md`
- `_codex/Context/CONTEXT-MAP.md`
- `_codex/agents/runbooks/`
- `_codex/agents/docs/codebase-map.md`
- `_codex/tasks/`
- `_codex/skills/`

Agents should only read optional docs when they are relevant to the current
task.

## First Prompt Template

```text
[TARENA]
This is TArenaUnity3D, not Retsot Horde.
Workspace root: D:\Unity\Projects\TArenaUnity3D.
Use local AGENTS.md.
Do not use context from another project unless I explicitly ask for comparison
or migration.
```


