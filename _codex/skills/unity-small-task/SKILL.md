---
name: unity-small-task
description: Implement a small, safe, testable Unity C# task in TArenaUnity3D.
---

# Unity Small Task

Use for direct small Unity/C# changes handled in one pass without the formal
local task protocol. Read local instructions, inspect relevant files, make the
smallest safe change, and give manual Unity test steps.

## Project UI Text Rule

TArenaUnity3D uses TextMesh Pro only. When the task touches UI or code text
references, use TMP types such as `TMP_Text` and `TextMeshProUGUI`, not legacy
`UnityEngine.UI.Text`.

Use `implement-task` for formal markdown task workflows.
