# [TARENA] PRD055B Coding Agent Follow-Up

- Task: `_codex/tasks/055B_PRD_SkillDamageMigration_CombatDamageService.md`
- Date: 2026-06-30
- Agent: Coding Agent
- Status: ready for final QA architecture review
- Responds to:
  `_codex/tasks/QA/2026-06-30_055B_QA_ArchitectureReview.md`

## QA Finding Addressed

QA found that snapshot-only AI simulation could call
`BattleActionRules.Apply(snapshot, action)` without an explicit combat catalog.
After PRD055B, combat-style skill damage would then use the default
`DataMapper`-backed service and could reject otherwise deterministic snapshot
skill damage in EditMode/AI contexts.

## Files Changed

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISearchScoring.cs`
  - `TacticalAISearchEngine.ToPlannedAction(...)` now calls
    `BattleActionRules.Apply(...)` with an explicit `CombatDamageService`.
  - `TacticalAISnapshotSimulator.ApplyAction(...)` now calls
    `BattleActionRules.Apply(...)` with an explicit `CombatDamageService`.
  - `SnapshotCombatUnitCatalog` was moved from a private nested
    `TacticalAIDamagePredictor` detail to an `internal sealed` top-level helper
    in the same file so snapshot planning, simulation, and damage prediction
    share the same catalog adapter.

## Design Boundary

- `CombatDamageService` still has no hidden fallback.
- Live/default `BattleActionRules.Apply(snapshot, action)` still uses
  `CombatDamageService.Default`.
- Snapshot-only AI callers now inject their deterministic snapshot catalog
  explicitly at the call site.

## Tests

- No tests have been added yet; per workflow, tests follow the final QA verdict.

## Manual Verification Not Run

- Unity Editor compile/tests were not run by the agent.
- No `dotnet`, Unity batchmode, build, package restore, or Git commands were
  run.
