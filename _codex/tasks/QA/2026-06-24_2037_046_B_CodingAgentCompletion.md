# [TARENA] Coding Agent Completion Protocol - PRD046-B

Task: `_codex/tasks/archive/046_B_PRD_TacticalAI_ActionIntentCandidates.md`
Date: 2026-06-24

## Summary

Implemented the first pure Tactical AI action-intent and candidate-generation seam on top of `BattleSnapshot`:

- Added a pure `TacticalAIActionIntent` model with explicit action types, snapshot-safe hex coordinates, skill slot/id pairing, and stable ordering metadata.
- Added a pure `TacticalAICandidateGenerator` that enumerates snapshot-level candidates for wait, defend, move, move-and-attack, basic ranged attack, and legal active skills for the current active unit only.
- Added profile-style candidate cap options so candidate generation can already stay bounded before the later `TacticalAIProfile` slice lands.
- Added focused EditMode tests for wait/defend gating, occupied-move exclusion, melee move-and-attack adjacency, ranged enemy targeting, passive/cooldown skill exclusion, and stable ordering across equivalent snapshots.

## Files Changed

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIActionIntent.cs`
  - New pure Tactical AI intent types, hex coordinate model, candidate-generation limit options, and skill metadata provider seam.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAICandidateGenerator.cs`
  - New pure candidate generator for snapshot-based basic actions and skill intents, with deterministic ordering and per-type caps.
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAICandidateGeneratorTests.cs`
  - New focused EditMode tests for candidate legality filters and stable ordering.

## Scope Boundaries

- No scenes, prefabs, materials, controllers, `.inputactions`, `.asmdef`, `.asmref`, or generated Unity files changed.
- No gameplay float values, damage formulas, cooldown rules, targeting flags, or turn rules were intentionally changed.
- No live execution bridge, search, scoring, or `TacticalAIProfile` ScriptableObject asset was added in this slice.

## Verification

Automatic execution was not run because project rules prohibit command-line Unity, `dotnet`, Git, external build scripts, package restore, and SDK installation commands in this workflow.

Manual Unity EditMode tests to run:

- `TacticalAICandidateGeneratorTests`
- existing `BattleSnapshotBuilderTests` as a regression check for the upstream snapshot seam

Manual Play Mode verification to run later when the execution bridge exists:

- inspect generated candidates for an active unit from a temporary debug hook or debugger watch,
- confirm wait/defend disappear after blocked states,
- confirm occupied hexes are not emitted as move destinations,
- confirm melee units emit adjacent `MoveAndAttack` intents and ranged units emit `BasicRangedAttack` intents,
- confirm passive or cooldown-blocked skills are absent while legal active skills remain present with correct slot/id pairing.

## Notes For QA

- Skill candidate generation is intentionally snapshot-legal but conservative in V1: it emits one legal skill intent per usable slot/id pair without mutating live `CastManager` mode state or trying to fully predict skill target geometry yet.
- Movement and melee-attack candidate generation use a pure axial-neighbour/path-cost approximation over snapshot hexes, matching the current snapshot coordinate model instead of querying live `HexClass` objects.
- Stable candidate ordering is deterministic and bounded, but not yet profile-weighted by the later search/scoring slice.
