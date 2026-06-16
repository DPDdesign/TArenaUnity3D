---
name: daily-review
description: Run a simple daily Project Director review for TArenaUnity3D.
---

# Daily Review

Read local current state, milestones, and active tasks. Summarize what changed,
what remains uncertain, and the next small step.

## Project UI Text Rule

TArenaUnity3D uses TextMesh Pro only. If reviewed work includes UI or code text
references, treat TMP types such as `TMP_Text` and `TextMeshProUGUI` as the
project standard and legacy `UnityEngine.UI.Text` as drift.

Do not import status from another project.
