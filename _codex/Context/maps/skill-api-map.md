# TArenaUnity3D Skill API Context Map

Status: active
Last updated: 2026-06-26

## Use When

Use this map when a task touches skill definitions, skill ownership, targeting,
skill validation, skill preview/result shape, `SkillRules`, `SkillQuery`,
`SkillUse`, `SkillCast`, `SkillResult`, `SkillDefinitionAsset`,
`SkillCatalog`, `DataMapper`, or skill id cleanup.

This active map should describe current code, not history. Use
`_codex/Context/maps/combat-skill-ai-history-risks-map.md` only for regression
debugging, bug archaeology, or explicit historical questions.

## Sources

Read the smallest relevant subset:

- `_codex/Context/09_CurrentSkills.md`
- `_codex/Context/10_Skill_Design_Rules.md`
- `_codex/Documentation/CurrentSkills.md`
- `_codex/Documentation/ADR_015_SkillActionDefinitionOwnsSkillTextAndRules.md`
- `_codex/agents/docs/codebase/skills-effects-code-map.md`

## Current Responsibility Boundary

Skill API owns rules for a specific skill:

- whether the current actor can start using that skill,
- which targets are legal,
- whether submitted target data validates,
- what preview/result data the skill produces,
- which `SkillDefinitionAsset`/catalog data backs the rule.

Skill API should not own broader turn lifecycle, battle readiness, AI scoring,
screen routing, persistence, or VFX timing.

## Current Code Surfaces

- `SkillDefinitionAsset.cs` - authored skill definition data.
- `SkillCatalog.cs` - catalog lookup for skill definitions.
- `DataMapper.cs` - project data lookup bridge.
- `SkillRules.cs` - start legality, target generation, validation, preview.
- `SkillQuery.cs` - AI/server-facing query helper.
- `SkillUse.cs` - untrusted submitted skill request shape.
- `SkillCast.cs` - normalized validated skill cast shape.
- `SkillResult.cs` - preview/result event shape.
- `SkillDefinitionSpec.cs` - copied skill data shape for non-live consumers.

## Current Operating Rules

- `SkillDefinitionAsset.skillName` is the canonical skill id.
- Unit skill ownership is loaded from the unit catalog into
  `TosterHexUnit.skillstrings`.
- UI selects a skill slot from `SelectedToster.skillstrings`; legality should
  route through `SkillRules`.
- Skill ids must stay aligned across unit catalog, skill catalog, icon paths,
  presentation entries, and remaining compatibility execution paths.
- Server/anti-cheat validation must recompute legality and result from battle
  snapshot plus skill definition data. Do not trust client-supplied affected
  units, damage, spawned unit state, or resolved target lists.

## Boundary With Battle Action API

Skill API answers: "Is this skill legal, and what does it mean?"

Battle Action API answers: "Is this whole tactical action legal in the current
turn/battle lifecycle, and how is it applied?"
