# TArenaUnity3D Combat, Skill, And AI History/Risks Map

Status: explicit-only
Last updated: 2026-06-26

## Use When

Use this map only for regression debugging, bug archaeology, migration
questions, or when the user/task explicitly asks which historical PRD or task
touched a combat, skill, Battle Action API, or Tactical AI area.

Active context maps should describe current code. They should not rely on PRD
numbers as operational truth.

## Historical Task References

These task files are history and investigation aids, not default context:

- `_codex/tasks/archive/046_PRD_TacticalBattleAI_V1.md`
- `_codex/tasks/archive/047_PRD_TacticalAI_AsyncDecisionPipeline.md`
- `_codex/tasks/049_PRD_TacticalActionSkillMigrationProgram.md`
- `_codex/tasks/archive/049ABC_PRD_SkillAPIAndFullMigration.md`
- `_codex/tasks/049ED_PRD_TacticalAIActionSelectionAndExecutionMigration.md`
- `_codex/tasks/049F_PRD_LegacySkillSystemCleanup.md`
- `_codex/tasks/050_PRD_BattleActionAPI_FullMigrationPurge.md`
- `_codex/tasks/archive/051_CombatAPIValidatorAI_AuditHardening.md`
- `_codex/tasks/archive/052_CombatActionsSkills_AuditHardening.md`
- `_codex/Documentation/Cleanup_Report_PRD_Tasks_046_052.md`

## Current Known Risk Areas

Use current code inspection to verify each risk before acting:

- Legacy AI intent/candidate/search surfaces may still exist for non-skill
  compatibility.
- Non-skill live apply may still delegate through `MouseControler.TryStart*`
  after validation.
- Player skill commits may still use `CastManager.startSpell(...)`
  compatibility after `SkillRules` validation.
- Passive and trap trigger execution may still depend on legacy hooks in
  `TosterHexUnit`, `SpellOverTime`, `HexClass`, and `Traps`.
- `Stone_Throw` is historically high-risk because dynamic "half current stack"
  effect data may not be fully represented.
- `Double_Throw` projectile/VFX timing and Rusher tactical choice between
  `Chope`, move, and `Rush` require manual Unity gameplay validation before any
  coding fix.

## Rule

If this file conflicts with current code, current code wins. Update the active
map from code evidence, and treat this file as historical context only.
