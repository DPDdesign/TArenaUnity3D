# [TARENA] 051 Coding Agent Completion Protocol

- Task: `_codex/tasks/051_CombatAPIValidatorAI_AuditHardening.md`
- Agent: Coding Agent
- Date: 2026-06-25 22:12
- Status: ready for QA architecture review

## 1. Current Combat Flow After Changes

Current intended flow is request -> validation -> execution -> result.

- Player move/basic attack/wait/defend still enter through `MouseControler` live input paths. The PRD050-compatible authority methods in `MouseControler.TryStartMoveAction`, `TryStartMoveAndAttackAction`, `TryStartBasicRangedAttackAction`, `TryStartWaitAction`, and `TryStartDefenseAction` remain the live execution adapter for these actions.
- Player skill start/targeting uses `SkillRules` through the current `MouseControler.TryStartSkillAction` and target validation paths.
- AI planning uses snapshots and produces `TacticalAIPlannedAction` with `BattleActionUse`, `BattleAction`, and `BattleActionResult` where available.
- AI live execution flows through `TacticalAIExecutionBridge.TryExecuteOrderedActions`, recaptures the live snapshot, revalidates the selected `BattleActionUse` through `BattleActionRules.Validate`, then applies through `BattleActionLiveApplier`.
- AI skill execution revalidates skill actions through the `BattleActionRules` -> `SkillRules` path, then commits through `TacticalAISkillRulesExecutor`.
- Rejected action requests now return validation/revalidation failure before live execution when the battle turn state is action-blocking or resolving a new-turn sequence.

## 2. Main Classes By Responsibility

- Battle Action API: `BattleActionModels`, `BattleActionUse`, `BattleAction`, `BattleActionResult`, `BattleActionValidationResult`.
- Validator: `BattleActionRules` for battle actions, delegating skill actions to `SkillRules`.
- Action request/use: `BattleActionUse`, `SkillUse`, and legacy `TacticalAIActionIntent`.
- Action result: `BattleActionResult`, `SkillResult`, and `TacticalAIPlannedAction`.
- AI decision/execution: `TacticalAICandidateGenerator`, `TacticalAISearchScoring`, `TacticalAIAsyncDecisionPipeline`, `TacticalAILiveTurnIntegrator`, `TacticalAIExecutionBridge`, `BattleActionLiveApplier`, `TacticalAISkillRulesExecutor`.
- Battle initialization/readiness: `HexMap.IsBattleReadyForTacticalActions`, `BattleSnapshotLiveAdapter.BuildSnapshot`, `BattleActionLifecycle`, `TurnManager`, and `MostStupidAIEver.IsBattleReadyForAI`.

## 3. What Was Changed

- `BattleActionRules.Validate` now treats both `BattleTurnStateSnapshot.IsActionBlocking` and `IsResolvingNewTurnSequence` as action-blocking states.
- `BattleActionRules.GenerateLegalActions` now returns no legal actions while the snapshot says the battle is action-blocking or resolving the new-turn sequence.
- `TacticalAICandidateGenerator.GenerateCandidates` now returns no legacy AI candidates while the snapshot says the battle is action-blocking or resolving the new-turn sequence.
- `TacticalAIIntentRevalidator.TryRevalidate` now rejects stale legacy AI intents while the live snapshot says the battle is action-blocking or resolving the new-turn sequence.
- `TODO_LEGACY_REVIEW` markers were added at the legacy AI candidate, legacy AI intent revalidation, and non-skill live adapter boundaries.
- `BattleActionRulesTests` now covers the new-turn sequence guard for direct validation, generated battle actions, and legacy AI candidates.
- No Inspector-visible fields were added, removed, or renamed.

## 4. What Remains Risky

- Non-skill AI execution still delegates final live mutation to `MouseControler.TryStart*` methods through `BattleActionLiveApplier`. This remains expected PRD050 adapter debt, but it is guarded by live snapshot revalidation first.
- Player non-skill live input still relies on `MouseControler` as the authority path and is not fully converted into `BattleActionUse` entry points.
- `CastManager` still contains legacy skill compatibility methods and state flags. This task did not prove it safe to delete.
- Passive/status/trap side effects still live in older battle objects such as `TosterHexUnit`, `SpellOverTime`, and `HexClass`.
- Unity import/compile and Test Runner execution were not run by agent instruction.

## 5. Server-Side Validation Readiness

PARTIAL.

`BattleActionUse`, `BattleAction`, `BattleActionResult`, `SkillUse`, `SkillCast`, and `SkillResult` remain mostly DTO-like. `BattleActionRules` validates against `BattleSnapshot` data and delegates skill rules to `SkillRules`.

Remaining extraction before server authority:

- Final non-skill live apply still uses scene-facing `MouseControler` methods.
- Some skill metadata resolution can still flow through `DataMapper`/Unity `Resources` when copied metadata is not supplied.
- Legacy `CastManager` compatibility remains outside the pure validator boundary.

## 6. Found And Removed Legacy Paths

No large legacy systems were removed. The safe correction was to close a validation/readiness gap in existing validator and AI legacy-candidate surfaces.

## 7. Legacy Paths Left Intentionally

Added `TODO_LEGACY_REVIEW` comments at the specific legacy runtime boundaries touched by this task:

Left intentionally:

- `BattleActionLiveApplier` delegating non-skill action commits to `MouseControler.TryStartMoveAction`, `TryStartMoveAndAttackAction`, `TryStartBasicRangedAttackAction`, `TryStartWaitAction`, and `TryStartDefenseAction`.
- `TacticalAIActionIntent`, `TacticalAICandidateGenerator`, and `TacticalAIIntentRevalidator` as legacy AI intent support.
- `MouseControler` player-input and skill preparation paths.
- `CastManager` compatibility methods and flags.
- Passive/trap/status mutation surfaces in `TosterHexUnit`, `HexClass`, and related legacy battle objects.

## 8. Things Deliberately Not Changed

- No gameplay float/int balance values changed.
- No skill definitions, damage values, cooldowns, ranges, targeting counts, movement budgets, initiative values, or status durations changed.
- No Unity assets, scenes, prefabs, materials, controllers, generated files, `.asmdef`, or `.asmref` files changed.
- No full PRD050 legacy purge attempted.
- No public or serialized fields renamed.
- No server, networking, rollback, or replay work added.

## 9. Tests Added/Updated And Tests Not Run

Added/updated:

- `BattleActionRulesTests.ActionsAndAICandidates_AreRejectedDuringNewTurnSequence`

Expected manual test path:

- Open Unity Test Runner.
- Go to `Window > General > Test Runner`.
- Select EditMode.
- Run `BattleActionRulesTests`.

Not run:

- Unity import/compile was not run.
- Unity Test Runner was not run.
- Play Mode battle verification was not run.

## 10. Manual Unity Checks Still Required

- Import/compile the project in Unity.
- Run EditMode `BattleActionRulesTests`.
- Start a normal battle and verify enemy AI does not act during new-turn passive/status/initialization resolution.
- Verify AI resumes after readiness and lifecycle blocking clears.
- Verify normal AI move, move-and-attack, basic ranged attack, wait, defend, and skill usage still work.
- Verify stale/illegal AI plans are rejected before live execution.
