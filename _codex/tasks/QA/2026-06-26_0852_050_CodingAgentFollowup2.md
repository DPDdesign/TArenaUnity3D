# 050 Coding Agent Follow-Up 2 - 2026-06-26

## Scope

- Verified tasks 049ED, 049F, and 050 against current code.
- Closed 049ED and 049F at task level for their implemented focused scopes.
- Continued PRD050 only where the code audit showed small safe gaps.

## Changed Files

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIPlannedAction.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISearchScoring.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIExecutionBridge.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAILiveTurnIntegrator.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISnapshotProbe.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/BattleActionRulesTests.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAIExecutionBridgeTests.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAISearchScoringTests.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAIAsyncDecisionPipelineTests.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAILiveTurnIntegrationTests.cs`
- `_codex/tasks/049ED_PRD_TacticalAIActionSelectionAndExecutionMigration.md`
- `_codex/tasks/049F_PRD_LegacySkillSystemCleanup.md`
- `_codex/tasks/050_PRD_BattleActionAPI_FullMigrationPurge.md`

## Removed Runtime Surfaces

- `TacticalAIPlannedAction.LegacyIntent`
- `TacticalAISearchPlan.BestIntent`
- `TacticalAISearchPlan.OrderedActionIntents`
- `TacticalAIExecutionAttempt.Intent`
- `TacticalAIExecutionResult.ExecutedIntent`
- `TacticalAIExecutionBridge.TryExecuteOrderedIntents(...)`
- `TacticalAIExecutionFallbackPlanner.BuildAttemptQueue(...)`
- `TacticalAIPlannedAction.FromLegacyIntent(...)`
- `TacticalAIPlannedAction.FromCandidateIntent(...)`

## Verification

- Source audit found no runtime `Assets/Scripts` references to `LegacyIntent`, `ExecutedIntent`, `BestIntent`, `TryExecuteOrderedIntents`, `FromLegacyIntent`, or `FromCandidateIntent`.
- `TacticalAIExecutionBridge` no longer contains direct non-skill `MouseControler.TryStart*` calls.
- Changed-file brace balance passed.
- Unity compile and Unity Test Runner were not run automatically.

## Remaining PRD050 Work

- `BattleActionLiveApplier` still delegates live non-skill actions to `MouseControler.TryStart*`.
- Legacy AI search/candidate internals remain: `TacticalAIActionIntent`, `TacticalAICandidateGenerator`, `TacticalAISearchCandidateExpander`, and `TacticalAIIntentRevalidator`.
- `CastManager` and skill-only DTO/rules surfaces remain.
- Player command path is not fully migrated to `BattleActionUse`.

