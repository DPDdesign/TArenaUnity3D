# [TARENA] PRD055C QA Architecture Review

- Task: `_codex/tasks/archive/055C_PRD_LegacyDamageTraceCleanup.md`
- Protocol: `_codex/tasks/QA/2026-06-30_055C_CodingAgent_Completion.md`
- Date: 2026-06-30
- Reviewer: QA Architecture Review Agent
- Verdict: pass

## Reviewed Files

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/LiveCombatDamageResolver.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionAutomaticResultApplier.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/MostStupidAIEver.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/CombatDamageService.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/CombatDamageModels.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotLiveAdapter.cs`

## Findings

No QA-blocking findings.

## Architecture Assessment

- Remaining live `TosterHexUnit` damage entry points no longer own combat math.
  They resolve committed damage through `LiveCombatDamageResolver`, which in
  turn calls `CombatDamageService.Default`.
- The new live resolver is an adapter from live Unity objects to the canonical
  snapshot/service model. It is not a compatibility wrapper around deleted
  legacy damage methods.
- Trap combat-style DOT no longer calls legacy live damage methods and now uses
  canonical committed combat damage.
- The old fallback AI heuristics no longer reference legacy damage methods.
  Their `0` value is only a logged heuristic failure result, not committed
  battle damage.
- `DealMeDMGDef...` no longer keeps manual attack-vs-defense math. It uses the
  service `BaseDamageOverride`, which matches the PRD055 ownership boundary.
- Remaining `Random.Range` use in reviewed static evidence is outside committed
  combat damage: battle seed generation, SFX selection, and material selection.

## Non-Blocking Observations

- `LiveCombatDamageResolver` depends on
  `BattleSnapshotLiveAdapter.BuildCurrentSceneSnapshot()`. Manual Play Mode
  validation should confirm legacy `CastManager`, trap, and basic attack
  compatibility paths run only when the battle is ready enough for snapshot
  construction.
- Legacy method names such as `DealMeDMG...` remain as presentation/application
  entry points. They are not damage calculators after this cleanup.
- Pure damage consumption remains explicit: the legacy live entry points clear
  `SpecialPUREDMG` only when the service result reports
  `ConsumesActorPureDamage`.

## Required Follow-Up

None.

## Recommended Test Additions

- Add a focused EditMode audit test that fails if production `Lesisz` scripts
  reintroduce the deleted legacy damage method names.
- Keep this as a static guard; behavioral coverage already lives in
  `CombatDamageServiceTests` and `BattleActionRulesTests`.
