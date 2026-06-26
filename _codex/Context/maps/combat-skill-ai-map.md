# TArenaUnity3D Combat, Skill, And Tactical AI Router

Status: active
Last updated: 2026-06-26

## Purpose

This file routes combat, skill, and Tactical AI context. Keep it short.

Active maps should describe current code and responsibility boundaries.
Historical task references belong in the explicit-only history/risks map.

## Focused Maps

- Skill definitions, skill ownership, targeting, validation, preview/result
  shape, `SkillRules`, `SkillQuery`, and skill id cleanup:
  `_codex/Context/maps/skill-api-map.md`
- Tactical action validation, battle readiness, snapshots, action lifecycle,
  and live action application:
  `_codex/Context/maps/battle-action-api-map.md`
- Tactical AI planning, scoring, async decisions, fallback, and planned-action
  execution bridge:
  `_codex/Context/maps/tactical-ai-map.md`
- Combat animation, hit/death reactions, SFX, music, projectile/VFX
  presentation, and board-state visuals:
  `_codex/Context/maps/combat-presentation-map.md`
- Historical task references, migration notes, and known risk archaeology:
  `_codex/Context/maps/combat-skill-ai-history-risks-map.md`

## Loading Rule

For normal work, load only the focused active map that matches the task.

Load `combat-skill-ai-history-risks-map.md` only when debugging regressions,
investigating historical migration context, or when the prompt/task explicitly
asks which historical task touched the area.
