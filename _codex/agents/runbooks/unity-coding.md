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
