# TArenaUnity3D - Agent Entry Point

## Purpose

This file is the small, always-loaded project entry point for TArenaUnity3D.
Keep it short. All project-specific agent context, docs, skills, and task files
belong under `_codex/` so the local harness can be removed by deleting that
folder.

## Workspace Identity

This workspace is TArenaUnity3D, not Retsot Horde.

Workspace root: `D:\Unity\Projects\TArenaUnity3D`
Unity project root: `D:\Unity\Projects\TArenaUnity3D\TArenaUnity3D`
Assets root: `D:\Unity\Projects\TArenaUnity3D\TArenaUnity3D\Assets`

Do not use context, tasks, agents, or markdown files from another project unless
the user explicitly asks for comparison or migration.

Chat/task titles for this project must start with `[TARENA]`.

First prompt template:

```text
[TARENA]
This is TArenaUnity3D, not Retsot Horde.
Workspace root: D:\Unity\Projects\TArenaUnity3D.
Use local AGENTS.md.
Do not use context from another project unless I explicitly ask for comparison
or migration.
```

## Cleanup Boundary

Project-specific agent material must live under `_codex/`:

- `_codex/agents/`
- `_codex/Context/`
- `_codex/Documentation/`
- `_codex/skills/`
- `_codex/tasks/`

Exception: this root `AGENTS.md` exists only so Codex can find the local entry
point.

## Default Role

Default agent:

- `_codex/agents/coding-agent.md`

Use it for normal Unity/C# implementation work.

Use another role only when the user explicitly asks for it, for example:

- `_codex/agents/project-director-agent.md`
- `_codex/agents/qa-architecture-review-agent.md`
- `_codex/agents/change-integrity-qa-agent.md`

## Project

TArenaUnity3D is a Unity project. Project-specific gameplay, production, and
technical truth must be written into local templates under `_codex/Context/` as
it becomes known.

## Hard Project Rules

- Work in small, safe, testable steps.
- Do not build large systems unless explicitly asked.
- Do not rename public or serialized fields without permission.
- Do not change gameplay float values without permission.
- Do not edit Unity assets, prefabs, scenes, materials, controllers,
  `.inputactions`, generated Unity files, `.asmdef`, or `.asmref` unless the
  user explicitly permits it.
- Work only on `.cs`, `.md`, or other plain text files when needed.
- Do not run or suggest Git, `dotnet`, Unity builds, external build scripts,
  package restore commands, or SDK installation commands.
- The user compiles and tests inside Unity unless a specific Unity test command
  is explicitly allowed.

## Conditional Runbooks

Load only the runbooks relevant to the current task:

- Unity/C# coding: `_codex/agents/runbooks/unity-coding.md`
- testing or final response after code changes: `_codex/agents/runbooks/testing.md`
- local task tracker, QA protocol, `/implement`, `/fix`, `/close`: `_codex/agents/runbooks/task-tracker.md`
- Git-related request: `_codex/agents/runbooks/git-policy.md`
- code navigation: `_codex/agents/docs/codebase-map.md`
- design/production/gameplay context routing: `_codex/Context/CONTEXT-MAP.md`

## Skills

Project-local workflow skills live in `_codex/skills/`.

Use skills only when they match the request. Do not load another project's
skills by default.

For direct small Unity/C# coding tasks, prefer:

- `_codex/skills/unity-small-task/SKILL.md`

Use `analyze-task` for pre-implementation analysis, `implement-task` for formal
local task execution, `fix-task` for a small post-implementation fix,
`qa-review` for architecture review, and `close-task` for task closure.

## Source Priority

When sources conflict:

1. Current user instruction.
2. Specific task file.
3. Requested role file.
4. This `AGENTS.md`.
5. Relevant runbook.
6. `_codex/Context/CONTEXT-MAP.md` routing.
7. Specific context document.
8. Existing C# code for implementation details.


