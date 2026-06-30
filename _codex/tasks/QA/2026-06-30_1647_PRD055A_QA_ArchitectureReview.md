# [TARENA] PRD055A QA Architecture Review

- Task: `_codex/tasks/archive/055A_PRD_CoreCombatDamageService_BasicCombat.md`
- Protocol: `_codex/tasks/QA/2026-06-30_1643_PRD055A_CodingAgent_Completion.md`
- Date: 2026-06-30
- Reviewer: QA Architecture Review Agent
- Verdict: Pass - no follow-up required before tests

## Sources Reviewed

- `_codex/agents/qa-architecture-review-agent.md`
- `_codex/tasks/QA/2026-06-30_1643_PRD055A_CodingAgent_Completion.md`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/CombatDamageModels.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/CombatDamageService.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionRules.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionModels.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotModels.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotLiveAdapter.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotBuilder.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionLiveApplier.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISearchScoring.cs`

## Findings

No actionable findings.

## Architecture Review

- `BattleActionRules` keeps tactical action validation ownership and delegates
  only damage resolution to `CombatDamageService`.
- `CombatDamageCalculator` is pure and has no Unity, catalog, snapshot, or
  `DataMapper` dependency.
- `CombatDamageService` is the correct boundary for snapshot/catalog data
  resolution and fail-fast error production.
- `DataMapper` coupling is isolated behind `ICombatUnitCatalog`.
- `DamageApplied.ConsumesActorPureDamage` keeps one-use pure damage mutation out
  of the calculator and makes the live/simulated mutators explicit.
- Rejected damage results are not applied as empty successful actions by the
  live applier or Tactical AI simulation.
- Snapshot clone/hash paths preserve the new combat state, so deterministic
  damage inputs are not hidden outside snapshot data.

## Non-Blocking Observations

- Skill and passive damage call sites still reference legacy
  `TosterHexUnit.CalculateDamageBetweenTosters(...)`. This is explicitly left
  for PRD055B/PRD055C.
- Existing test fixtures that exercise basic damage need a test
  `ICombatUnitCatalog` and explicit `CatalogUnitId` values to avoid relying on
  project assets.

## Post-Review Update

- Tactical AI now uses deterministic committed damage prediction through
  `CombatDamageService`, and live/simulated snapshot paths preserve
  `GameSeed`/`NextActionIndex`.

## Verification

- Static/architectural review only.
- Unity compile, Play Mode, and EditMode tests were not run by QA.
