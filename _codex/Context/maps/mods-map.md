# TArenaUnity3D Mods Context Map

Status: active
Last updated: 2026-06-26

## Use When

Use this map when a task touches mods, modding, player-authored content,
external unit files, XML or JSON unit import, catalog validation, or future
user-created unit data.

## Sources

- `_codex/Context/Mods.md`
- `_codex/tasks/031_PRD_ModUnitDataImportValidation.md` when planning the future
  mod unit import and validation module
- `_codex/Context/09_CurrentSkills.md` when skill ids or skill ownership are
  involved
- `_codex/Context/10_Skill_Design_Rules.md` when skill strings, presentation
  catalog boundaries, or skill execution identity are involved
- `_codex/agents/docs/codebase-map.md` when checking current `DataMapper`,
  `UnitCatalog`, or unit runtime loading paths

Mods are future scope. Do not implement mod loading, mod folders, runtime
import, packaging, signing, dependency resolution, load order, or external
content execution unless a future task explicitly approves that work.
