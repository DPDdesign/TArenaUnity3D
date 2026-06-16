---
name: qa-review
description: Run the TArenaUnity3D QA Architecture Review workflow.
---

# QA Review

Review selected completion protocols in `_codex/tasks/QA/`.

Read only the protocol, named files, and nearby related systems needed to check
ownership, duplication, naming, hidden coupling, and architecture consistency.

## Project UI Text Rule

TArenaUnity3D uses TextMesh Pro only. If the reviewed work touches UI or code
text references, require TMP types such as `TMP_Text` and `TextMeshProUGUI`
instead of legacy `UnityEngine.UI.Text`.

Save reports into `_codex/tasks/QA/`.
Do not implement fixes during review.
