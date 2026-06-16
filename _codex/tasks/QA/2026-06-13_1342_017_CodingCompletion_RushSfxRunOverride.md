# [TARENA] Coding Completion: PRD 017 Rush Early Cast SFX And Run Movement Override

- Task: `_codex/tasks/017_PRD_RushEarlyCastSfxRunMovementOverride.md`
- Completed: 2026-06-13 13:42
- Agent: Coding Agent
- Scope: Unity/C# implementation only

## Files Changed

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillPresentationManager.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/CastManager.cs`

## What Changed

No Inspector fields changed.

`TosterHexUnit`:

- Added a private movement animation override state.
- Added `SetMovementAnimationOverride(string stateName)`.
- Added `ClearMovementAnimationOverride()`.
- Added `GetMovementAnimationState()`, which returns the override when set and
  otherwise returns the default movement animation state `walk`.
- Changed movement animation playback in `SetHex(...)` from hardcoded `walk` to
  `GetMovementAnimationState()`.

`SkillPresentationManager`:

- Added `PlayCastSfxOnly(string skillId)` for commit-time SFX-only feedback.
  It looks up the existing skill presentation entry and plays only `castSfx`.
  It does not spawn cast VFX and does not play weapon trails.
- Added `PlaySequencedHexImpactOnly(...)` for post-arrival impact presentation
  without replaying cast feedback.
- Extracted caster animation playback into `PlayCasterAnimation(...)` so normal
  sequenced presentation can still use animation plus cast, while `Rush` can use
  animation plus impact only.

`CastManager`:

- Updated `RushMoveAttackAndPlayHexEffect(...)` to play `Rush` cast SFX
  immediately when the committed Rush coroutine starts.
- Wrapped the existing `DoMoves(...)` movement call with a temporary movement
  animation override set to `run`.
- Clears the movement override in a `finally` block after the movement coroutine.
- Replaced the post-movement sequenced cast/impact call with the new
  impact-only sequence so `castSfx` is not played a second time after arrival.

## Checks Performed

- Read the changed regions after patching.
- Searched for the new helper calls and override methods to confirm only `Rush`
  uses the new movement override path.
- Confirmed the changed code keeps the existing `Rush` movement and optional
  target attack branch intact.

## Not Run

- Unity compile was not run by the agent.
- Unity Test Runner was not run by the agent.
- Play Mode validation was not run by the agent.
- No command-line build, `dotnet`, package restore, SDK, or Git commands were
  run.

## Manual Unity Validation Recommended

Unity Setup:

- Ensure the battle scene has its existing `SkillPresentationManager` with an
  `AudioSource` and assigned `SkillPresentationCatalog`.
- Ensure the `Rush` catalog entry has a `castSfx` assigned for the immediate
  commit sound.
- Ensure the Rusher Animator has the existing `walk`, `run`, and selected
  `skillN` animation states expected by current content.

Play Mode Test:

- Cast `Rush` onto an empty highlighted valid hex.
- Confirm `castSfx` plays immediately once at commit.
- Confirm the unit moves through the normal movement path but plays `run`, not
  `walk`.
- Confirm the selected `skillN` animation and impact feedback play after
  arrival.
- Move the same or another unit normally afterward and confirm normal movement
  still plays `walk`.
- Cast `Rush` toward an enemy and confirm the immediate SFX, `run` movement,
  post-arrival skill/impact presentation, and existing target follow-up behavior
  do not duplicate feedback.

## Notes

- Gameplay values, targeting, cooldowns, movement distance, movement speed,
  pathfinding, stat/status values, scenes, prefabs, Animator Controllers,
  animation clips, `.asmdef`, `.asmref`, and generated files were not changed.
- Missing `SkillPresentationManager`, catalog, or `Rush` cast SFX remains a safe
  no-op under the existing presentation manager behavior.
- The new movement override is general, but only `Rush` uses it in this task.

