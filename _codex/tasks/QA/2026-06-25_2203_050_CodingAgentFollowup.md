# [TARENA] Coding Agent Follow-Up - PRD050

Task: `_codex/tasks/050_PRD_BattleActionAPI_FullMigrationPurge.md`
Initial QA: `_codex/tasks/QA/2026-06-25_2202_050_QA_ArchitectureReview.md`
Date: 2026-06-25

## Summary

Applied focused fixes for the QA findings that were safe to address without broadening into a second full PRD050 migration:

- replaced nondeterministic `string.GetHashCode()` use in basic attack damage seeding,
- added focused EditMode tests for the new Battle Action rules seam.

## Changed Files

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionRules.cs`
  - Replaced `string.GetHashCode()` in `ResolveBasicAttackDamage(...)` with a stable deterministic character-fold hash.
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/BattleActionRulesTests.cs`
  - Added focused tests for:
    - occupied move destination rejection,
    - legal move result event output,
    - wait/defend rejection after movement,
    - stable deterministic ranged attack damage,
    - `TacticalAIPlannedAction.FromBattleAction(...)` carrying `Use`, `Action`, and `Result` without `LegacyIntent`.

## Tests

- Unity Test Runner was not run automatically.
- Lightweight source brace-balance check passed for:
  - `BattleActionRules.cs`
  - `BattleActionRulesTests.cs`

## Remaining QA Findings

Still intentionally open after this focused follow-up:

- full legacy AI intent/candidate purge is not complete,
- live apply still delegates current mutation to existing lifecycle/MouseControler paths,
- skill-only DTO/rules classes remain in use,
- AI search internals still do not fully use Battle Action pure apply,
- player input is not fully migrated to `BattleActionUse`.

These are broader PRD050 migration items and should not be hidden as completed by this follow-up.
