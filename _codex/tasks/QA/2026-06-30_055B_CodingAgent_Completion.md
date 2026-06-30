# [TARENA] PRD055B Coding Agent Completion

- Task: `_codex/tasks/055B_PRD_SkillDamageMigration_CombatDamageService.md`
- Date: 2026-06-30
- Agent: Coding Agent
- Status: ready for QA architecture review

## Scope Implemented

- Migrated skill result damage generation for combat-style skill damage to
  `CombatDamageService`.
- Removed the runtime skill damage fallback that called
  `TosterHexUnit.CalculateDamageBetweenTosters(...)`.
- Preserved skill targeting, cooldown, turn-cost, movement, trap, spawn,
  status, stance, and presentation application responsibilities outside
  `CombatDamageService`.

## Files Changed

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionRules.cs`
  - `Apply(..., CombatDamageService)` now passes the service into skill result
    generation.
  - Replaced skill result conversion through `SkillRules.Preview(...)` with
    direct `SkillCast.Effects` event generation so damage effects can resolve
    committed amounts before live application.
  - `SkillDamageMode.BasicAttackDamage` and
    `SkillDamageMode.RangedBasicAttackDamage` now call
    `CombatDamageService.CalculateDamage(...)`.
  - Skill roll purpose is distinct per skill/effect/target:
    `skill:<skillId>:<effectIndex>:<targetIndex>`.
  - Missing snapshot/catalog/deterministic damage data rejects the whole result
    through the existing `ActionRejected` path.
  - Non-combat damage modes still use authored `fixedDamageValue`.

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISkillRulesExecutor.cs`
  - `ApplyDamage(...)` now applies the committed `BattleActionResultEvent.Amount`
    only.
  - Removed the fallback call to
    `actor.CalculateDamageBetweenTosters(actor, target, scale)`.
  - Removed the now-unused `FirstDamageEffect(...)` helper.

## Important Implementation Notes

- This implementation treats these modes as PRD055B in-scope combat-style
  skill damage:
  - `SkillDamageMode.BasicAttackDamage`
  - `SkillDamageMode.RangedBasicAttackDamage`
- `SkillDamageMode.FixedDamageThroughDefense`, `PureDamage`, `DamageOverTime`,
  and `PercentOfDamageTaken` are not forced into `CombatDamageService`.
- Existing legacy calls in `BattleActionAutomaticResultApplier`,
  `MostStupidAIEver`, and old `TosterHexUnit` combat methods remain for PRD055C
  cleanup or separate non-skill/legacy scopes.
- No Unity assets, prefabs, scenes, `.asmdef`, or `.asmref` files were edited.

## Tests

- No tests have been added yet in this workflow stage.
- Per `implement-task`, focused EditMode tests should be added after QA review
  and any follow-up fixes.

## Manual Verification Not Run

- Unity Editor compile/tests were not run by the agent.
- No `dotnet`, Unity batchmode, build, package restore, or Git commands were
  run.

## QA Review Request

Please review:

- Whether skill event generation preserves the previous non-damage behavior.
- Whether combat-style skill damage is fully routed through
  `CombatDamageService`.
- Whether `TacticalAISkillRulesExecutor` is now correctly formula-free.
- Whether roll purpose construction is deterministic and sufficiently distinct
  from basic attack and retaliation.
- Whether any in-scope skill damage path still falls back to legacy live
  `TosterHexUnit` damage.
