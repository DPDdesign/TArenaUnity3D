# [TARENA] Coding Agent Completion - PRD046-G

Task: `_codex/tasks/archive/046_G_PRD_TacticalAI_LiveEnemyTurnIntegration.md`
Date: 2026-06-24
Agent: Coding Agent

## Implementation Summary

Implemented the live Tactical AI V1 enemy-turn integration slice so `MostStupidAIEver`
tries the new search/execution path before falling back to the legacy behavior.

The implementation adds:

- `TacticalAILiveTurnIntegrator` as a small seam for:
  - capturing the live `BattleSnapshot`,
  - resolving the Normal tactical AI profile through the existing profile catalog,
  - building a `TacticalAISearchPlan`,
  - executing ordered intents through `TacticalAIExecutionBridge`,
  - returning a clear started-or-fallback result.
- Tactical AI runtime diagnostics for:
  - planning start,
  - selected/best intent,
  - plan score, depth, fallback count, and opponent-response coverage,
  - successful action start,
  - fallback reason/status/attempt count.
- `MostStupidAIEver` integration so `AskAIwhattodo()`:
  - tries Tactical AI first,
  - returns early when a live action starts,
  - logs a concise fallback message,
  - preserves the old behavior in `RunLegacyFallbackAI()`.
- Focused EditMode coverage for the new pure integration seam and diagnostic formatting.

## Files Changed

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAILiveTurnIntegrator.cs`
  - New live integration helper and pure log-formatting helpers.
- `TArenaUnity3D/Assets/Scripts/Lesisz/MostStupidAIEver.cs`
  - `AskAIwhattodo()` now attempts Tactical AI first and falls back to extracted legacy logic.
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAILiveTurnIntegrationTests.cs`
  - Added tests for started/fallback outcomes and fallback log content.

## Scope Notes

- No scenes, prefabs, materials, controllers, `.inputactions`, `.asmdef`, `.asmref`, generated Unity files, or other Unity assets were changed.
- No public or serialized field names were renamed.
- No gameplay float values, damage formulas, movement values, cooldown values, or turn rules were intentionally changed.
- No live AI path bypasses `TacticalAIExecutionBridge`, `TacticalAIIntentRevalidator`, `MouseControler`, `CastManager`, or `BattleActionLifecycle`.

## Architecture Fit

- Live planning starts from `BattleSnapshotLiveAdapter.BuildCurrentSceneSnapshot()`.
- Profile resolution uses `TacticalAIProfileCatalog.LoadNormalProfileAsset()` first and falls back to runtime Normal through the existing catalog path when no asset is available.
- Search still flows through `TacticalAISearchPlanner`, preserving the advisory plan-cache seam from PRD046-E.
- Live execution still flows through `TacticalAIExecutionBridge.TryExecuteOrderedIntents(...)`, so PRD046-C revalidation and PRD046-F CastManager skill bridging remain authoritative.
- Legacy `MostStupidAIEver` behavior remains present as a temporary safety fallback for empty plans, invalid runtime context, busy lifecycle, and no-legal-action execution failures.

## Verification Performed

- Read the local task, Unity coding/testing/task-tracker runbooks, codebase map, and the PRD046-G task definition.
- Inspected the current Tactical AI planning/execution/profile stack and the existing `MostStupidAIEver` enemy-turn entry point.
- Added pure seam tests for integration result mapping and diagnostic formatting.

## Tests

Automatic Unity test execution was not run because project rules keep Unity compilation and test runs inside the editor unless explicitly allowed.

Added EditMode tests:

- `TacticalAILiveTurnIntegrationTests`

Recommended Unity EditMode tests to run:

- `TacticalAILiveTurnIntegrationTests`
- `TacticalAIExecutionBridgeTests`
- `TacticalAISearchScoringTests`

Recommended manual Play Mode verification:

- Start a tactical battle with enemy AI enabled.
- Let an enemy turn begin and confirm the Console shows:
  - Tactical AI planning start,
  - selected plan summary,
  - started action or fallback reason.
- Confirm a legal enemy move/attack/skill now starts through `TacticalAIExecutionBridge`.
- Confirm stale or rejected tactical intents fall back to the legacy `MostStupidAIEver` behavior instead of stalling the turn.
- Confirm skill-intent paths can still reach the PRD046-F CastManager bridge when the planner chooses a target-aware skill.

## Known Limitations

- The legacy fallback is intentionally still present in V1.
- If snapshot capture fails or the battle lifecycle is busy, the integration falls back immediately rather than retrying within the same call.
- Diagnostic logging is concise but still per enemy-turn attempt; deeper telemetry remains out of scope for this slice.
