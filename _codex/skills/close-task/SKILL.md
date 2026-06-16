---
name: close-task
description: Close a TArenaUnity3D markdown task as Project Director.
---

# Close Task

Confirm the task is safe to close, move it to `_codex/tasks/archive/`, update
local current-state/context docs when needed, search related active local tasks,
and recommend the next smallest production step.

## Project UI Text Rule

TArenaUnity3D uses TextMesh Pro only. When closing a task that touched UI or
code text references, ensure it used TMP types such as `TMP_Text` and
`TextMeshProUGUI`, not legacy `UnityEngine.UI.Text`.

Do not close tasks from another project.
