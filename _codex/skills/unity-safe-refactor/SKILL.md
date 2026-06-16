---
name: unity-safe-refactor
description: Apply an explicitly requested safe Unity C# refactor in TArenaUnity3D.
---

# Unity Safe Refactor

Use only when the user explicitly asks for refactor or cleanup. Prefer one clear
current runtime path and avoid compatibility wrappers unless requested.

Preserve serialized/public field names unless explicitly allowed.

## Project UI Text Rule

TArenaUnity3D uses TextMesh Pro only. When refactoring UI or code text
references, keep or move to TMP types such as `TMP_Text` and
`TextMeshProUGUI`, never legacy `UnityEngine.UI.Text`.
