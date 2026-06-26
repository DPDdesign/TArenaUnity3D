# ADR 015: SkillDefinitionAsset Owns Skill Text And Rules

Status: accepted direction
Date: 2026-06-25
Project: TArenaUnity3D

## Context

TArenaUnity3D currently has skill data split across multiple sources:

- unit skill ownership in unit `ScriptableObject` catalog data,
- runtime skill ids in `TosterHexUnit.skillstrings`,
- skill descriptions and flags in `skills.xml`,
- gameplay behavior and target modes in `CastManager`,
- UI targeting/highlight logic in `MouseControler`,
- AI skill behavior through tactical AI intent and legacy bridge paths.

The 049 tactical action migration program defines a new model where the
existing `SkillDefinitionAsset` is extended into the long-term authoring source
for skill rules, targeting, effect data, and player-facing skill text. PRD49
does not create a separate `SkillActionDefinition` asset type.

## Decision

`skills.xml` will not remain as a long-term source of truth or fallback source
for skill descriptions, flags, cooldowns, targeting, or gameplay rules.

The desired target state is:

- skill descriptions live in skill SO data,
- activation and turn-cost rules live in skill SO data,
- target contracts live in skill SO data,
- effect data lives in skill SO data,
- runtime validation reads `BattleSnapshot` plus skill/action definition data,
- migrated execution reads `ValidatedTacticalAction` plus skill/action
  definition data,
- VFX/SFX presentation remains in `SkillPresentationCatalog`, joined by skill
  id.

`skills.xml` may be used only as a temporary migration reference while active
skills are converted. It should be deleted after SO descriptions/rules/effects
cover the active skill set and runtime/UI references have been replaced.

## Rationale

Keeping XML as a parallel source for text or flags would preserve the same
split-brain data problem that the 049 migration is intended to remove.

Skill text, validation rules, and effect data should travel together so player
UI, validator, AI, future server-side validation, and execution do not drift.

## Consequences

- Future skill work should not add new gameplay flags to `skills.xml`.
- Future skill text work should target skill SO data, not XML.
- Cleanup PRDs must remove runtime dependencies on `skills.xml`.
- Any importer from old XML must be treated as temporary migration tooling, not
  production truth.
- UI code that currently reads descriptions from XML must migrate to the SO
  skill definition model.

## Related PRDs

- `_codex/tasks/049_PRD_TacticalActionSkillMigrationProgram.md`
- `_codex/tasks/archive/049A_PRD_TacticalActionValidationSO_UI_VerticalSlice.md`
- `_codex/tasks/archive/049AC_PRD_SkillTargetAndEffectDataModel.md`
- `_codex/tasks/049F_PRD_LegacySkillSystemCleanup.md`
