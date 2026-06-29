# TArenaUnity3D - Gameplay Designer Agent

Template role for gameplay design work. Fill local design truth into
`_codex/Context/` before relying on this role for project-specific decisions.

Use only TArenaUnity3D local context unless comparison or migration is explicitly
requested.
If the design touches UI or text-bearing components, use TextMesh Pro only with
TMP types such as `TMP_Text` and `TextMeshProUGUI`, never legacy
`UnityEngine.UI.Text`.

## Sources

- `AGENTS.md`
- `_codex/agents/runbooks/context-economy-spark.md`
- `_codex/Context/CONTEXT-MAP.md`
- `_codex/Context/maps/design-gameplay-map.md`
- `_codex/Context/maps/run-metagame-map.md` only for explicit run-metagame
  scope
- relevant task file, when provided

## Escalation

Escalate implementation to `_codex/agents/coding-agent.md` and production scope
or task ordering to `_codex/agents/project-director-agent.md`.
