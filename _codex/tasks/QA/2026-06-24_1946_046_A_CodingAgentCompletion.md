# [TARENA] Coding Agent Completion Protocol - PRD046-A

Task: `_codex/tasks/archive/046_A_PRD_TacticalAI_BattleSnapshotV1.md`
Date: 2026-06-24

## Summary

Implemented the BattleSnapshot V1 foundation as a pure tactical-state module plus a live battle reader:

- Added pure snapshot model classes for battle turn state, hexes, units, statuses, and the top-level battle snapshot.
- Added a deterministic snapshot builder that normalizes ordering and computes a stable `snapshotHash` from planning-relevant tactical state.
- Added a live adapter that reads current tactical runtime state from `HexMap`, `TeamClass`, `TosterHexUnit`, `SpellOverTime`, `TurnManager`, `MouseControler`, and `BattleActionLifecycle` without storing Unity object references inside the snapshot model.
- Added focused EditMode tests for stable runtime unit ids, hash stability, hash change triggers, and no-Unity-reference model constraints.

## Files Changed

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotModels.cs`
  - New pure snapshot model classes and runtime id helper.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotBuilder.cs`
  - New deterministic builder and `snapshotHash` generation.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotLiveAdapter.cs`
  - New live adapter that builds snapshots from current tactical runtime state.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionLifecycle.cs`
  - Added read-only active actor/action-kind accessors for snapshot reads.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/SpellOverTime.cs`
  - Added read-only status/source/effect accessors for snapshot reads.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TurnManager.cs`
  - Added read-only new-turn-sequence state accessor for snapshot reads.
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/BattleSnapshotBuilderTests.cs`
  - New focused EditMode tests for snapshot hashing and model purity.

## Scope Boundaries

- No scenes, prefabs, materials, controllers, `.inputactions`, `.asmdef`, `.asmref`, or generated Unity files changed.
- No gameplay float values, cooldown rules, targeting rules, damage rules, or turn rules changed.
- No AI action selection, execution bridge, skill prediction rewrite, or persistence wiring was added in this slice.

## Verification

Automatic execution was not run because project rules prohibit command-line Unity, `dotnet`, Git, external build scripts, package restore, and SDK installation commands in this workflow.

Manual Unity EditMode tests to run:

- `BattleSnapshotBuilderTests`

Manual Play Mode verification to run:

- open a tactical battle scene with live units, statuses, cooldowns, and at least one trap-capable skill;
- call `BattleSnapshotLiveAdapter.BuildCurrentSceneSnapshot()` from an existing debug hook, temporary inspector button, or debugger watch;
- confirm the snapshot contains stable `team-{teamIndex}-slot-{rosterIndexWithinTeam}` ids, active unit id, current round/action state, skill slot ids/cooldowns, visible status durations, and trap markers when present.

## Notes For QA

- `BattleSnapshotBuilder` intentionally sorts hexes, units, statuses, and used-skill ids so equivalent tactical state hashes identically even if source list order differs.
- Runtime stats such as attack/defense/move/initiative/min-max damage are captured as raw base runtime values, while status modifiers stay explicit in `BattleStatusSnapshot` instead of being baked into final derived stats.
- The live adapter exposes a convenience `BuildCurrentSceneSnapshot()` entry point, but higher-level AI systems can also pass explicit scene references to avoid repeated `FindObjectOfType` calls in future planning loops.
