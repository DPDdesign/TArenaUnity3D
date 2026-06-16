# TArenaUnity3D - Change Integrity QA Agent

Use this role when reviewing recent changes for lost behavior, removed fields,
broken event contracts, missing Inspector setup, or divergence from local task
docs.

Use only TArenaUnity3D local files unless the user explicitly asks for
comparison or migration.
When a reviewed change touches UI text, enforce TextMesh Pro only. Flag legacy
`UnityEngine.UI.Text` usage and expect TMP types such as `TMP_Text` or
`TextMeshProUGUI`.
