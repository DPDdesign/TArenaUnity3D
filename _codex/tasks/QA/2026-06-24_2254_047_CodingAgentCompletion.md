# [TARENA] Coding Agent Completion Protocol - PRD047

Task: `_codex/tasks/047_PRD_TacticalAI_AsyncDecisionPipeline.md`
Date: 2026-06-24

## Summary

Implemented the initial async Tactical AI decision pipeline at the current enemy-turn entry point so planning no longer has to run synchronously on the Unity main thread:

- Added a worker-safe copied skill metadata provider that captures only the skill metadata needed by the current `BattleSnapshot` before planning starts.
- Added `TacticalAIAsyncTurnIntegrator`, which splits the Tactical AI flow into main-thread snapshot/profile/metadata capture, worker-task search, and main-thread result consumption through the existing execution bridge.
- Kept live execution authoritative by reusing `TacticalAIExecutionBridge` and its existing revalidation path for the final action start.
- Added stale/fault/cancel handling and bounded async diagnostics for start, completion, stale, and fault events.
- Replaced `MostStupidAIEver`'s synchronous planner call with a coroutine that starts async planning, yields frames until the worker result is ready, then either executes the validated action or falls back to the legacy AI path.
- Exposed the existing execution fallback-reason formatter for reuse by the async path.

## Files Changed

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIAsyncDecisionPipeline.cs`
  - New async pipeline module.
  - Captures copied skill metadata from live data on the main thread.
  - Runs `TacticalAISearchEngine.Search(...)` inside `Task.Run(...)`.
  - Tracks pending decision lifecycle with completed/stale/faulted/cancelled handling.
  - Revalidates current snapshot hash, active actor id, and profile hash before executing a completed plan.
  - Uses the existing live execution bridge only on the main thread.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAILiveTurnIntegrator.cs`
  - Renamed the internal execution fallback-reason helper to a public static `BuildExecutionFallbackReason(...)` so the async path can share the same fallback mapping.
- `TArenaUnity3D/Assets/Scripts/Lesisz/MostStupidAIEver.cs`
  - Replaced the synchronous tactical planner call with coroutine-driven async polling.
  - Prevents overlapping AI planning coroutines for the same component instance.
  - Resets the coroutine handle on disable.
  - Preserves the existing legacy fallback AI path when async planning cannot produce a usable live action.

## Scope Boundaries

- No scenes, prefabs, materials, controllers, `.inputactions`, `.asmdef`, `.asmref`, generated Unity files, or Unity assets were changed.
- No gameplay float values, skill ids, cooldown values, damage formulas, movement values, targeting rules, or turn-order rules were intentionally changed.
- No early logical-commit hooks were added to `BattleActionLifecycle` yet; this slice uses the PRD's allowed initial integration path at the enemy-turn entry point.
- No command-line Unity or external test execution was run.

## Verification

Automatic execution was not run because project rules leave Unity compilation and Unity Test Runner execution to the user inside Unity.

Manual Unity Play Mode verification to run:

- Start a tactical battle with enemy AI enabled.
- Observe enemy turns that previously froze during Tactical AI planning.
- Confirm that scene animation/audio/frame flow continues while the AI thinks and that the chosen action starts after the worker result is consumed.
- Verify bounded Console diagnostics for `async-start`, `async-complete`, stale/fault cases when they happen, and the existing tactical fallback warning when async execution cannot start.
- Verify that if async planning cannot execute, the old fallback enemy AI still acts instead of deadlocking the turn.

## Notes For QA

- The worker task uses only the captured `BattleSnapshot`, cloned resolved profile, and copied skill metadata provider; it does not intentionally touch `DataMapper`, `Resources`, `ScriptableObject` instances, or scene objects after scheduling.
- This slice intentionally avoids reworking `BattleActionLifecycle` semantics. It removes the visible synchronous planning freeze first, using the PRD's lower-risk initial flow.
- `Task.Run(...)` is isolated behind the new async integrator so future slices can move the planning start earlier in the battle lifecycle without changing `MostStupidAIEver` again.
- Focus review on thread-safety boundaries, stale-result handling, and whether the current fallback behavior remains safe when async planning faults or becomes invalid.
