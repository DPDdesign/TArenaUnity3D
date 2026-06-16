# TArenaUnity3D - Coding Agent

## Role

This is the default agent for Unity/C# implementation work in TArenaUnity3D.

## Workspace Boundary

Use only local TArenaUnity3D context unless the user explicitly asks for
comparison or migration. Chat/task titles must start with `[TARENA]`.

## Project UI Text Rule

Use TextMesh Pro only for UI and code text references in TArenaUnity3D.
Prefer TMP types such as `TMP_Text` and `TextMeshProUGUI`. Do not introduce or
recommend legacy `UnityEngine.UI.Text`.

## Required Sources Before Coding

- `AGENTS.md`
- `_codex/agents/coding-agent.md`
- `_codex/agents/runbooks/unity-coding.md`
- `_codex/agents/runbooks/testing.md`
- `_codex/agents/docs/codebase-map.md`
- the specific `_codex/tasks/` file, when implementing a task
- relevant C# files under `D:\Unity\Projects\TArenaUnity3D\TArenaUnity3D\Assets`

Use `_codex/Context/CONTEXT-MAP.md` only when the task needs design,
production, gameplay, AI, skill, level, difficulty, or architecture context.

## Standard Coding Loop

1. Read the relevant classes.
2. Briefly state the current structure.
3. Propose the smallest safe change.
4. Edit only what is needed.
5. Inspect changed files.
6. Explain Unity test steps.

## Scope

Prefer simple C#, explicit methods, Unity-friendly Inspector fields, focused
EditMode tests when logic can be isolated, and small changes that can be tested
in one Unity scene.

Avoid large refactors, new frameworks, unnecessary interfaces, unrelated file
changes, and editing assets or Unity-generated files.

