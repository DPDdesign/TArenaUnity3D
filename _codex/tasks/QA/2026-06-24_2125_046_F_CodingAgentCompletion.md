# [TARENA] Coding Agent Completion Protocol - PRD046-F

Task: `_codex/tasks/046_F_PRD_TacticalAI_CastManagerSkillBridge.md`
Date: 2026-06-24

## Summary

Implemented the Tactical AI CastManager skill bridge on top of the PRD046-C execution bridge:

- Added a default `TacticalAICastManagerSkillIntentExecutor` for `ITacticalAISkillIntentExecutor`.
- Wired `TacticalAIExecutionBridge` to use the CastManager skill executor by default when no custom executor is supplied.
- Added `MouseControler.TryStartSkillAction(...)` as a live, multiplayer-compatible skill command entry point for one revalidated skill intent.
- Extended `TacticalAIIntentRevalidator` so skill intents preserve and lightly revalidate optional `TargetHex`, `TargetUnitId`, and `DestinationHex`.
- Added CastManager helpers for checking mode/cast method existence and canceling prepared skill mode state without committing a fake skill use.
- Added focused EditMode coverage for skill slot/id preservation, used non-toggle rejection, and repeatable toggle acceptance.

## Files Changed

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAICastManagerSkillIntentExecutor.cs`
  - New default skill executor that resolves the live actor and target hex, then delegates execution to `MouseControler.TryStartSkillAction(...)`.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIExecutionBridge.cs`
  - Defaults `ITacticalAISkillIntentExecutor` to `TacticalAICastManagerSkillIntentExecutor.Instance`, so existing PRD046-C bridge/probe paths can execute skill intents without extra scene wiring.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIIntentRevalidator.cs`
  - Skill revalidation now carries optional target/destination coordinates into `TacticalAIRevalidatedIntent`.
  - Target unit ids are checked for continued existence/aliveness and position match when provided.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/MouseControler.cs`
  - Added `TryStartSkillAction(...)` for a single validated skill command.
  - The method rechecks slot/id, cooldown/action state, CastManager mode/cast methods, CastManager target flags, and explicit target hexes before invoking `startSpell`.
  - Single-click/self/toggle/direct target skills are supported; staged movement/target-selection skills that need multiple clicks are rejected with a diagnostic reason until intents can represent that sequence.
  - Skill cleanup now tolerates missing optional UI references when the action path is AI/network driven.
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/CastManager.cs`
  - Added method-existence helpers for skill mode/cast methods.
  - Split mode-state reset so rejected AI skill execution can clear prepared CastManager state without sending chat or completing a skill.
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAIExecutionBridgeTests.cs`
  - Added focused pure tests for skill revalidation behavior.

## Scope Boundaries

- No scenes, prefabs, materials, controllers, `.inputactions`, `.asmdef`, `.asmref`, generated Unity files, or Unity assets were changed.
- No gameplay float values, cooldown values, damage formulas, skill ids, targeting ranges, movement values, VFX/SFX, animation names, or turn rules were intentionally changed.
- No `SkillPredictionCatalog` or second skill rules catalog was introduced.
- Full multi-click skill sequence representation remains out of scope for this slice.

## Verification

Automatic execution was not run because project rules prohibit command-line Unity, `dotnet`, Git, external build scripts, package restore, and SDK installation commands in this workflow.

Manual Unity EditMode tests to run:

- `TacticalAIExecutionBridgeTests`
- existing `TacticalAICandidateGeneratorTests`
- existing `BattleSnapshotBuilderTests`

Manual Play Mode verification to run:

- Use `TacticalAISnapshotProbe` in a tactical battle scene.
- Capture/generate candidates, then execute them through `Execute Tactical AI Candidates Through Bridge`.
- Verify skill intents with explicit valid targets execute through `MouseControler`/`CastManager` and complete through `BattleActionLifecycle`.
- Verify invalid target/cooldown/stale actor cases reject and fall back through the PRD046-C attempt queue.
- Verify staged skills that cannot yet be represented by one intent reject with a diagnostic reason rather than leaving CastManager in a hidden prepared state.

## Notes For QA

- `MouseControler.TryStartSkillAction(...)` intentionally delegates final skill behavior to current CastManager methods. This keeps legacy skill execution authoritative.
- The new executor does not choose targets. It executes the explicit target/destination carried by the intent or lets self/toggle skills resolve through CastManager mode behavior.
- Staged legacy skills such as movement-then-target flows are rejected for now because one `TacticalAIActionIntent` cannot safely represent both clicks yet.
- Rejections include diagnostic text through the existing `TacticalAIExecutionBridge` warning path.
