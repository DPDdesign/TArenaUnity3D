# Unity Coding Runbook

Use this for Unity/C# implementation tasks in TArenaUnity3D.

## Before Editing

1. Read the relevant task file, if provided.
2. Read likely script areas from `_codex/agents/docs/codebase-map.md`.
3. Inspect only relevant C# files under `TArenaUnity3D/Assets`.
4. Briefly explain the current structure.
5. State the smallest safe change before editing.

Do not scan the whole `Assets` folder unless explicitly needed.

## Unity Safety

Do not change without explicit permission:

- public field names,
- serialized field names,
- prefab-facing references,
- gameplay float values,
- method names used by UnityEvents,
- existing Inspector setup assumptions.

When the user explicitly asks to replace legacy Inspector wiring, remove the
obsolete `[SerializeField]` or public field so it disappears from the Unity
Inspector. Transitional old logic may be commented if it is useful, but legacy
fields must not remain visible in the Inspector.

Do not edit:

- `.prefab`,
- `.asset`,
- `.unity`,
- `.mat`,
- `.controller`,
- `.inputactions`,
- generated Unity files,
- `.asmdef`,
- `.asmref`.

Allowed by default:

- `.cs`,
- `.md`,
- other plain text files when needed.

## Army Stack UI

When a runtime UI displays army stacks, expose a stack row parent `Transform`
and a stack row prefab reference in the controller Inspector. The row prefab
must contain `StackRepresentation`; instantiate one prefab per stack and bind
it through `StackRepresentation.DisplayStackInfo(...)` or the shared
`RunMetagameStackListPresenter.DisplayStackInfo(...)` helper.

Do not build production stack lists from fixed scene children, child-name
lookups, or serialized arrays of sample rows. Builders may create prefab
templates, but screen controllers should render live DTO/snapshot data into
instantiated row prefabs.
