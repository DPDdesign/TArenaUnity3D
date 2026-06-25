# 049ED Coding Agent Follow-Up 2

Date: 2026-06-25
Task: `_codex/tasks/049ED_PRD_TacticalAIActionSelectionAndExecutionMigration.md`
Follow-up to: `_codex/tasks/QA/2026-06-25_1716_049ED_QA_FinalArchitectureReview.md`

## Files Changed

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIPlannedAction.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIActionIntent.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIAsyncDecisionPipeline.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISearchScoring.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIExecutionBridge.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAILiveTurnIntegrator.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISkillRulesExecutor.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillDefinitionSpec.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillContext.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillRules.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillDefinitionMigrationDefaults.cs`
- `TArenaUnity3D/Assets/Scripts/SkillDefinitionAsset.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAIAsyncDecisionPipelineTests.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAILiveTurnIntegrationTests.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAISearchScoringTests.cs`

## What Changed

- Added `TacticalAIPlannedAction` as the ranked plan item consumed by live/async AI execution.
- Skill planned actions now carry `SkillUse`, `SkillCast`, and `SkillResult`; they do not carry a legacy `TacticalAIActionIntent`.
- `TacticalAISearchPlan` now exposes `BestAction` and `OrderedActions`; legacy `BestIntent`/`OrderedActionIntents` remain as compatibility views for non-skill actions.
- `TacticalAILiveTurnIntegrator` and `TacticalAIAsyncTurnIntegrator` now execute `OrderedActions`.
- `TacticalAIExecutionBridge` now has `TryExecuteOrderedActions(...)`; skill actions are revalidated directly from `SkillUse` against the current snapshot into a fresh `SkillCast`.
- Added `SkillDefinitionSpec` as an immutable copied skill definition shape.
- `SkillContext` and `SkillRules` now support `SkillDefinitionSpec` as well as `SkillDefinitionAsset`.
- `TacticalAICopiedSkillMetadataProvider` now captures `SkillDefinitionSpec`; it no longer stores `SkillDefinitionAsset` references for async planning.
- Expanded `SkillEffect` with status modifier fields and updated live status application to pass those modifiers into `SpellOverTime`.
- Implemented live `UnitSpawned` handling through existing `TeamClass.AddNewUnit(...)`, `SetTosterPrefab(...)`, and `HexMap.GenerateToster(...)`.
- Added representative legacy default status modifier data for several known migrated effects.
- Updated focused tests for planned skill actions and copied immutable specs.

## Automatic Test

Not run automatically. Project rules leave Unity compilation and Test Runner execution to the user.

Lightweight source brace-balance checks passed for the changed files.

Updated/added focused EditMode tests:

- `TacticalAISearchScoringTests.SearchPlan_SkillActionDoesNotCarryLegacyIntent`
- `TacticalAIAsyncDecisionPipelineTests.CopiedSkillMetadataProvider_CapturesMetadataWithoutLiveReadsAfterCapture` now also verifies copied skill spec access.
- `TacticalAILiveTurnIntegrationTests` now uses `ExecutedAction`/`BestAction`.

## Remaining Notes

- `TacticalAIActionIntent` still exists for legacy non-skill movement/attack/wait/defend and PRD049F cleanup.
- `TacticalAIIntentRevalidator` remains for legacy non-skill and compatibility paths.
- The runtime spawn path is implemented, but `Stone_Throw` legacy half-stack split remains a known high-risk parity edge because current SO effect data does not yet express "split half of current stack" directly.
