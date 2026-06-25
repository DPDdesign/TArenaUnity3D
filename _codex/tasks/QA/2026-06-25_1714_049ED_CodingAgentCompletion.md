# 049ED Coding Agent Completion

Date: 2026-06-25
Task: `_codex/tasks/049ED_PRD_TacticalAIActionSelectionAndExecutionMigration.md`
Agent: Coding Agent

## Files Changed

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIActionIntent.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIAsyncDecisionPipeline.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISearchScoring.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIIntentRevalidator.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIExecutionBridge.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISkillRulesExecutor.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAIExecutionBridgeTests.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAISearchScoringTests.cs`

## What Changed

- Extended AI action candidates so skill actions can carry the shared PRD49ABC `SkillCast` validation result and `SkillResult` preview.
- Added `ITacticalAISkillDefinitionProvider` so async planning can copy skill definition access with metadata instead of resolving definitions from live UI or `CastManager` paths.
- Replaced tactical AI skill target guessing in `TacticalAISearchCandidateExpander` with `SkillRules.GetTargets(...)` plus `SkillRules.Validate(...)`.
- Changed skill snapshot simulation to consume `SkillResult` preview events for damage, movement, traps, stack deltas, and turn/cooldown consequences.
- Changed live skill revalidation to rebuild a current `SkillUse` from the planned `SkillCast` selected hexes, then validate it into a fresh live `SkillCast`.
- Added `TacticalAISkillRulesExecutor` and `TacticalAISkillRuntime` so migrated AI skill execution uses `SkillRules.Apply(...)` and live effect application instead of `TacticalAICastManagerSkillIntentExecutor`.
- Switched `TacticalAIExecutionBridge` default skill executor from the CastManager bridge to the shared SkillRules executor.
- Tightened execution attempt queues: when a ranked plan exists, execution tries that ranked plan only; fresh emergency candidates are used only when no planned actions exist.
- Changed no-legal-action execution logging to `Debug.LogError`.
- Added focused EditMode coverage for validated skill candidates and the revised ranked-plan queue behavior.

## Inspector Fields

No Inspector fields changed.

## Automatic Test

Not run automatically. Project rules leave Unity compilation and Test Runner execution to the user.

Added/updated focused EditMode tests:

- `TacticalAISearchScoringTests.SearchCandidateExpansion_EmitsValidatedSkillCastAndPreview`
  verifies AI skill expansion emits a `TacticalAIActionIntent` carrying a shared `SkillCast` and `SkillResult` preview.
- `TacticalAIExecutionBridgeTests.FallbackPlanner_UsesRankedPlanOnlyWhenPlanExists`
  verifies execution no longer appends fresh hardcoded-style fallback actions after a ranked plan.
- `TacticalAIExecutionBridgeTests` provider now supplies test `SkillDefinitionAsset` data so skill revalidation exercises `SkillRules`.

Run manually in Unity Test Runner:

- Open `Window > General > Test Runner`.
- Select `EditMode`.
- Run `TacticalAISearchScoringTests`.
- Run `TacticalAIExecutionBridgeTests`.

Expected result: all selected EditMode tests pass.

## Unity Setup

No new scene, prefab, ScriptableObject, or Inspector setup is required for the code path. Existing `DataMapper` must still have the skill catalog available because live AI execution resolves `SkillDefinitionAsset` by skill id.

## Play Mode Test

1. Start a tactical battle where an enemy AI unit has an active skill from the skill catalog.
2. Let the enemy turn begin through the current `MostStupidAIEver` / `TacticalAIAsyncTurnIntegrator` path.
3. Observe the Console for `[TacticalAI] async-start` and `[TacticalAI] async-complete`.
4. Confirm the AI can select skills, movement, attacks, wait, or defend from the same ranked action list.
5. For a skill action, confirm the Console does not show `TacticalAICastManagerSkillIntentExecutor` rejection logs and that skill effects apply through the new shared executor path.
6. If all ranked actions fail revalidation, confirm Unity logs an error rather than silently forcing a basic attack/wait path.

## Notes

- The legacy `TacticalAIActionIntent` class still exists for non-skill movement/attack/wait/defend routing and for compatibility until PRD049F cleanup. New skill candidates carry `SkillCast` and `SkillResult` directly.
- The old `TacticalAICastManagerSkillIntentExecutor` remains in the project but is no longer the default runtime path.
- `TacticalAISkillRuntime` covers the current shared `SkillResult` event families used by migrated data: damage, hp cost, unit move, trap placement, stack delta, status placeholder, and stance. Spawn is logged as not live-migrated when encountered.
- Status event application currently creates a named `SpellOverTime` shell from effect duration/status id. Full legacy-equivalent status modifiers still depend on richer `SkillEffect` data or follow-up migration.
- New `.cs` file metadata was not hand-authored; Unity will generate `.meta` if needed on import.

## QA Request

Review whether the 049ED implementation keeps the existing Tactical AI surfaces, avoids the CastManager runtime skill path by default, and keeps shared validation/execution boundaries coherent without introducing a parallel AI-only action model beyond the existing legacy `TacticalAIActionIntent` compatibility shell.
