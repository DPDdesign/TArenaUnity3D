# [TARENA] QA Architecture Review - PRD046-B

Task: `_codex/tasks/archive/046_B_PRD_TacticalAI_ActionIntentCandidates.md`
Protocol: `_codex/tasks/QA/2026-06-24_2037_046_B_CodingAgentCompletion.md`
Date: 2026-06-24

## Verdict

Pass for the requested Action Intent + Candidates slice. No blocking architecture findings in the pure intent model, snapshot-level candidate generation seam, or the added EditMode coverage.

## Reviewed Files

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIActionIntent.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAICandidateGenerator.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAICandidateGeneratorTests.cs`
- nearby related systems:
  - `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotModels.cs`
  - `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotLiveAdapter.cs`
  - `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/MouseControler.cs`
  - `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/HexClass.cs`
  - `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/HexMap.cs`
  - `TArenaUnity3D/Assets/Scripts/DataMapper.cs`

## Findings

No blocking architecture findings for the requested PRD046-B scope.

## Non-Blocking Observations

- The generator keeps ownership clean: snapshot legality stays in a pure seam, while live execution authority remains reserved for later `MouseControler` / `CastManager` / lifecycle bridge work.
- The injected skill-metadata provider is the right boundary for this stage because it lets deterministic tests stay pure while still allowing a runtime adapter to current `DataMapper` metadata.
- Skill candidates are intentionally conservative in this slice: one legal slot/id candidate per usable active skill, without full target-shape expansion yet. That is acceptable for PRD046-B, but PRD046-F should extend the bridge with richer target-aware skill intents where needed.
- Snapshot movement/adjacency currently follows the legacy hex-neighbour contract, which matches the current default `HexMap.useLegacyMap = true` path. If future AI planning must support non-legacy map topology, that topology flag should be carried explicitly in the snapshot/model seam instead of being inferred.

## Test Review

The new `TacticalAICandidateGeneratorTests` cover:

- wait and defend gating before/after moved/used-skill states;
- occupied-destination exclusion for movement;
- melee `MoveAndAttack` generation from an already-adjacent source hex;
- ranged attack targeting restricted to enemy units;
- passive and cooldown-blocked skill exclusion with preserved slot/id pairing;
- stable candidate ordering across equivalent snapshots.

Tests were not executed during QA because project rules prohibit command-line Unity, `dotnet`, build, package restore, and Git tooling in this workflow.
