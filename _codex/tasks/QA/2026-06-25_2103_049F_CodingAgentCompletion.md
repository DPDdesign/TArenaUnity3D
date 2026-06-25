# 049F Coding Agent Completion

Date: 2026-06-25
Task: `_codex/tasks/049F_PRD_LegacySkillSystemCleanup.md`

## Scope

Implemented a focused PRD049F cleanup slice for the migrated Tactical AI skill execution path.

This pass removes the legacy CastManager-based AI skill executor after PRD049ED introduced the shared `SkillRules` execution path. It does not delete the broader `TacticalAIActionIntent` non-skill compatibility shell because movement, basic attack, wait, and defend still use it.

## Files Changed

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIExecutionBridge.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIAsyncDecisionPipeline.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISkillRulesExecutor.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAICastManagerSkillIntentExecutor.cs` deleted
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAIExecutionBridgeTests.cs`

## What Changed

- Removed `TacticalAICastManagerSkillIntentExecutor.cs`, the legacy AI bridge that executed skill actions through `MouseControler.TryStartSkillAction(...)` and the CastManager-compatible live path.
- Replaced `ITacticalAISkillIntentExecutor` with `ITacticalAISkillActionExecutor`.
- Changed AI skill execution so `TacticalAIExecutionBridge` passes only the revalidated `SkillCast`/`SkillRules` action payload to `TacticalAISkillRulesExecutor`.
- Updated `TacticalAIAsyncTurnIntegrator.CreateFromScene(...)` to accept the new action executor contract.
- Added EditMode coverage confirming `TacticalAISkillRulesExecutor` implements the action executor contract and that planned skill actions do not carry a legacy intent into execution.

## Reference Audit

- `rg` found no remaining references to:
  - `TacticalAICastManagerSkillIntentExecutor`
  - `ITacticalAISkillIntentExecutor`
  - `TryExecuteSkillIntent(`
  - `skillIntentExecutor`

Remaining intentional legacy references:

- `TacticalAIActionIntent` remains for non-skill movement, move-and-attack, basic ranged attack, wait, and defend compatibility.
- `TacticalAICandidateGenerator`, `TacticalAISearchCandidateExpander`, and `TacticalAIIntentRevalidator` still use the legacy intent shell for non-skill action candidates and revalidation.
- `TacticalAIActionIntent` still acts as the internal search candidate container for validated skill candidates before conversion to `TacticalAIPlannedAction`; execution no longer consumes it for skill actions.

## Automatic Test

- Not run automatically. Unity compilation and Unity Test Runner execution remain manual per project rules.
- Lightweight source brace-balance check passed for changed files.
- Added/updated EditMode tests:
  - `TacticalAIExecutionBridgeTests.SkillRulesExecutor_UsesActionExecutorContract`
  - `TacticalAIExecutionBridgeTests.PlannedSkillAction_DoesNotCarryLegacyIntent`

## Unity Test

### Unity Setup

- No new scene, prefab, asset, or Inspector setup is required.
- Existing `DataMapper`, skill catalog, and unit catalog references must remain wired.

### Play Mode Test

- Run a tactical battle with an enemy AI unit that has an active skill.
- Let the enemy AI turn execute through the async/live Tactical AI path.
- Confirm skill execution still applies through `TacticalAISkillRulesExecutor` / `SkillRules`.
- Confirm there are no CastManager AI skill bridge logs or errors.
- Validate `Stone_Throw` manually because PRD049ED already identified it as the highest-risk parity case.

## Notes

- This is a safe PRD049F cleanup slice, not full PRD049F closure.
- Full deletion of `TacticalAIActionIntent`, `TacticalAICandidateGenerator`, `TacticalAISearchCandidateExpander`, and `TacticalAIIntentRevalidator` still requires a replacement for non-skill movement/attack/wait/defend action candidates.
- No gameplay float values, serialized Inspector fields, assets, prefabs, or scenes were changed.
