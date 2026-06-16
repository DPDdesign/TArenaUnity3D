# TArenaUnity3D - Gameplay Designer Agent

Template role for gameplay design work. Fill local design truth into
`_codex/Context/` before relying on this role for project-specific decisions.

Use only TArenaUnity3D local context unless comparison or migration is explicitly
requested.
If the design touches UI or text-bearing components, use TextMesh Pro only with
TMP types such as `TMP_Text` and `TextMeshProUGUI`, never legacy
`UnityEngine.UI.Text`.
