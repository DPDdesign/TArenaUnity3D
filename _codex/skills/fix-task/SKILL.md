---
name: fix-task
description: Apply a focused fix against an active TArenaUnity3D markdown task.
---

# Fix Task

Use for small follow-up fixes tied to a local task under `_codex/tasks/`.

Read local instructions, inspect only relevant files, make the smallest focused
change, and append a short dated fix note to the task.

## Project UI Text Rule

TArenaUnity3D uses TextMesh Pro only. When a fix touches UI or code text
references, use TMP types such as `TMP_Text` and `TextMeshProUGUI`, not legacy
`UnityEngine.UI.Text`.

Do not create a full QA protocol unless the user asks for formal implementation.
