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

## Unity Object Ambiguity

When adding `using System;` to Unity C# files that also use Unity object APIs,
avoid unqualified `Object`. Use `UnityEngine.Object` for calls such as
`UnityEngine.Object.FindObjectOfType<T>()`, `Instantiate`, or `Destroy` when
there is any chance of CS0104 ambiguity between `UnityEngine.Object` and
`object`.

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
Use the smallest matching domain map under `_codex/Context/maps/`.

PRD files and PRD-specific maps are task-scoped only. Load them only when the
current prompt, selected task, or brief explicitly requires that PRD scope.

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

## PRD019 Prefab Lock

PRD019 UI prefab folders are read-only for normal coding work:

- `TArenaUnity3D/Assets/Resources/UI/PRD_19/020_StartRun/`
- `TArenaUnity3D/Assets/Resources/UI/PRD_19/021_RunMap/`
- `TArenaUnity3D/Assets/Resources/UI/PRD_19/023_RewardMap/`
- `TArenaUnity3D/Assets/Resources/UI/PRD_19/024_RunShop/`
- `TArenaUnity3D/Assets/Resources/UI/PRD_19/025_SummaryValue/`
- `TArenaUnity3D/Assets/Resources/UI/PRD_19/026_SavedArmies/`
- `TArenaUnity3D/Assets/Resources/UI/PRD_19/027_BattleResult/`

Do not create, edit, move, delete, or regenerate prefabs, prefab `.meta` files,
or other Unity asset files in these folders unless the user explicitly grants
path-specific permission in the current task. If a PRD019 task appears to need
prefab changes, stop and ask for permission; prefer a C# adapter/controller fix
or manual Unity setup instructions.

