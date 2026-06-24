# [TARENA] Coding Agent Completion Protocol - PRD046-C

Task: `_codex/tasks/archive/046_C_PRD_TacticalAI_LifecycleExecutionBridge.md`
Date: 2026-06-24

## Summary

Implemented the Tactical AI live execution bridge on top of the completed PRD046-B intent model and the existing battle lifecycle:

- Added a pure `TacticalAIIntentRevalidator` seam that revalidates a selected `TacticalAIActionIntent` against a fresh live `BattleSnapshot`.
- Added `TacticalAIExecutionBridge` and fallback-planning helpers that:
  - reject stale or busy execution attempts;
  - try ordered plan intents first;
  - fall back to fresh greedy legal candidates from the current snapshot;
  - keep `Defend` and `Wait` as the last legal fallback actions.
- Routed basic AI actions through existing `MouseControler` lifecycle entrypoints instead of direct scene mutation:
  - `TryStartMoveAction`;
  - `TryStartMoveAndAttackAction`;
  - `TryStartBasicRangedAttackAction`;
  - `TryStartWaitAction`;
  - `TryStartDefenseAction`.
- Reserved skill execution behind an explicit `ITacticalAISkillIntentExecutor` seam so PRD046-F can integrate `MouseControler` / `CastManager` skill flow without bypassing live validation.
- Added deterministic EditMode tests for revalidation and fallback queue behaviour.
- Extended the temporary `TacticalAISnapshotProbe` debug seam so generated AI candidates can be pushed through the new bridge during Play Mode inspection.

## Files Changed

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIIntentRevalidator.cs`
  - New pure live-revalidation seam for AI intents against a fresh snapshot.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIExecutionBridge.cs`
  - New runtime bridge, fallback queue builder, execution result types, and skill-executor interface seam.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISnapshotProbe.cs`
  - Added a debug execution hook and result summary output for manual Play Mode inspection.
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAIExecutionBridgeTests.cs`
  - New focused EditMode tests for revalidation and fallback behaviour.
- `_codex/tasks/archive/046_C_PRD_TacticalAI_LifecycleExecutionBridge.md`
  - Added implementation, verification, notes, and next-step closure details.
- `_codex/tasks/046_F_PRD_TacticalAI_CastManagerSkillBridge.md`
  - Updated dependency path to the archived PRD046-C task.
- `_codex/tasks/046_PRD_TacticalBattleAI_V1.md`
  - Updated the split-PRD list to reference archived PRD046-C.

## Scope Boundaries

- No scenes, prefabs, materials, controllers, `.inputactions`, `.asmdef`, `.asmref`, or generated Unity files changed.
- No gameplay float values, cooldown rules, targeting rules, damage formulas, movement rules, or turn rules were intentionally changed.
- No full action-validation rewrite was added; the bridge stays adapter-sized and leaves final live legality to existing runtime checks.
- No skill-execution adapter was added in this slice; live skill start remains reserved for PRD046-F through `ITacticalAISkillIntentExecutor`.

## Verification

Automatic execution was not run because project rules prohibit command-line Unity, `dotnet`, Git, external build scripts, package restore, and SDK installation commands in this workflow.

Manual Unity EditMode tests to run:

- `TacticalAIExecutionBridgeTests`
- `TacticalAICandidateGeneratorTests`
- `BattleSnapshotBuilderTests`

Manual Play Mode verification to run:

- use `TacticalAISnapshotProbe` or a temporary local debug hook to capture a snapshot, generate candidates, and execute them through `TacticalAIExecutionBridge`;
- confirm legal basic actions enter the existing lifecycle path and do not mutate tactical state directly;
- confirm stale top intents fall back to the next legal plan/fresh candidate instead of forcing execution;
- confirm lifecycle-busy state rejects overlapping AI execution cleanly;
- confirm `Skill` intents currently reject with a clear pending-PRD046-F message instead of bypassing legacy skill flow.

## Notes For QA

- The bridge intentionally separates pure revalidation from live execution: snapshot checks reject obviously stale work first, then existing `MouseControler` runtime methods remain the final authority for basic-action legality.
- Fallback generation intentionally reuses the pure PRD046-B candidate generator instead of introducing a second legality catalog.
- `ITacticalAISkillIntentExecutor` is the intended PRD046-F integration seam; this slice stops before manipulating legacy skill-selection state.
