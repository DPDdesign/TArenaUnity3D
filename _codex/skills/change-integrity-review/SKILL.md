---
name: change-integrity-review
description: Review recent TArenaUnity3D changes for lost behavior or drift.
---

# Change Integrity Review

Check what was added, removed, or changed against the relevant local task, docs,
and files. Produce a concise risk checklist and manual Unity checks.

## Project UI Text Rule

TArenaUnity3D uses TextMesh Pro only. If reviewed changes touch UI or code text
references, flag legacy `UnityEngine.UI.Text` and expect TMP types such as
`TMP_Text` and `TextMeshProUGUI`.

Do not implement fixes during the review unless explicitly asked after the
report.
