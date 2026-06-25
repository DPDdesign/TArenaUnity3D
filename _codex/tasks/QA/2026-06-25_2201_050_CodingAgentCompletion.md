# [TARENA] Coding Agent Completion Protocol - PRD050

Task: `_codex/tasks/050_PRD_BattleActionAPI_FullMigrationPurge.md`

Date: 2026-06-25

## Summary

Implemented an initial PRD050 Battle Action API migration slice for Tactical AI command validation and execution handoff.

This pass adds the requested command/action/result model names and routes ranked Tactical AI root actions through validated `BattleAction` data before live execution. It does not complete the full PRD050 purge of all legacy action/skill systems.

## Changed Files

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionModels.cs`
  - Added `BattleActionUse`, `BattleAction`, `BattleActionResult`, `BattleActionResultEvent`, `BattleActionKind`, and validation result/helper types.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionRules.cs`
  - Added shared snapshot-based validation for `Move`, `MoveAndAttack`, `BasicMeleeAttack`, `BasicRangedAttack`, `Wait`, `Defend`, `Skill`, and `Stance`.
  - Added legal action generation from `BattleSnapshot` for Tactical AI.
  - Added pure result-event preview for validated Battle Actions, including deterministic basic attack damage from snapshot seed/action context.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionLiveApplier.cs`
  - Added a live applier that consumes revalidated `BattleAction` data and applies through the current battle lifecycle/mutation entry points.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIPlannedAction.cs`
  - Added `BattleActionUse`, `BattleAction`, and `BattleActionResult` payloads to planned actions.
  - Added conversion helpers between legacy Tactical AI action type names and new Battle Action kinds.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISearchScoring.cs`
  - Ranked root search output now validates selected intents into `BattleAction` with `BattleActionRules.Validate(...)` before creating `TacticalAIPlannedAction`.
  - Planned actions now carry Battle Action result-event previews from `BattleActionRules.Apply(...)`.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIExecutionBridge.cs`
  - Non-skill revalidation now prefers `BattleActionRules.Validate(action.Use, liveSnapshot, ...)` instead of trusting raw legacy intent fields.
  - Execution now calls `BattleActionLiveApplier` when a revalidated Battle Action is available.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIIntentRevalidator.cs`
  - Extended revalidated execution payload with `BattleActionUse`, `BattleAction`, and `BattleActionResult`.

## Deleted Files

- None.

## Edited Assets

- None.

## Migrated Action Kinds

- Command/validation/result representation added for:
  - `Move`
  - `MoveAndAttack`
  - `BasicMeleeAttack`
  - `BasicRangedAttack`
  - `Wait`
  - `Defend`
  - `Skill`
  - `Stance`
- `Passive`, `Trap`, and `Automatic` exist in the model enum but are not fully migrated as automatic runtime trigger paths in this slice.

## Removed Legacy References

- No files or legacy classes were removed in this slice.
- Tactical AI ranked root actions now carry Battle Action payloads before execution.
- Remaining runtime/reference debt is explicit:
  - `TacticalAIActionIntent` still exists and is still used inside search/scoring expansion and tests.
  - `TacticalAICandidateGenerator` still exists.
  - `TacticalAISearchCandidateExpander` still exists inside `TacticalAISearchScoring.cs`.
  - `TacticalAIIntentRevalidator` still exists as an intent-only fallback for older caller/test paths.
  - `MouseControler.TryStart*` methods still exist and are used by `BattleActionLiveApplier` to preserve current live mutation/presentation parity.
  - `CastManager`, `SkillUse`, `SkillCast`, `SkillResult`, `SkillRules`, and `SkillQuery` remain.

## Tests Added Or Updated

- No EditMode tests were added before the QA pass, per the formal implement-task workflow order.
- Lightweight source brace-balance check passed for changed files:
  - `BattleActionModels.cs`
  - `BattleActionRules.cs`
  - `BattleActionLiveApplier.cs`
  - `TacticalAIPlannedAction.cs`
  - `TacticalAISearchScoring.cs`
  - `TacticalAIExecutionBridge.cs`
  - `TacticalAIIntentRevalidator.cs`

## Tests Not Run

- Unity compilation was not run.
- Unity Test Runner was not run.
- No `dotnet` or Unity command-line build/test command was run, per project rules.

## Manual Unity Checks Still Required

- Unity import/compile for the new C# files.
- EditMode tests after the test pass is authored.
- Play Mode enemy AI checks:
  - AI chooses and executes a move.
  - AI chooses and executes move-and-attack.
  - AI chooses and executes ranged attack.
  - AI chooses wait/defend when scored best.
  - AI chooses and executes a skill.
  - Validate `Stone_Throw` manually because PRD049ED already marked it as the highest-risk parity edge.

## Parity Issues Found And Fixed

- No Play Mode parity issues were found because Unity Play Mode was not run.
- This slice intentionally preserves current live movement/attack/wait/defend mutation by delegating through existing lifecycle entry points from the new live applier.

## Known Incomplete PRD050 Scope

- Full PRD050 acceptance is not met.
- Player input still does not submit all actions through `BattleActionUse`.
- AI search internals still score and simulate `TacticalAIActionIntent` in several paths.
- AI simulation is not yet fully converted to pure `BattleActionRules.Apply(...)`.
- Skill-only DTO/rules classes remain and are still used by the skill branch.
- CastManager remains in the project and player skill commit paths still retain legacy dependencies.
- Passive/trap/automatic trigger migration is not complete.
