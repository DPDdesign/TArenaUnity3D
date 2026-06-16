---
name: unity-readonly-analysis
description: Analyze TArenaUnity3D Unity C# code without editing files.
---

# Unity Readonly Analysis

Read local instructions, codebase map, and only relevant C# files under the
TArenaUnity3D Assets folder. Explain responsibilities, connections, risks, and
where a small change should be made.

## Project UI Text Rule

TArenaUnity3D uses TextMesh Pro only. When analysis touches UI or code text
references, expect TMP types such as `TMP_Text` and `TextMeshProUGUI`, not
legacy `UnityEngine.UI.Text`.

Do not edit files.
