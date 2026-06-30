# [TARENA] PRD055C Coding Agent Completion

- Task: `_codex/tasks/archive/055C_PRD_LegacyDamageTraceCleanup.md`
- Date: 2026-06-30
- Agent: Coding Agent
- Status: ready for QA architecture review

## Scope Implemented

- Removed legacy combat-style damage calculators from
  `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`.
- Replaced live legacy damage entry points in `TosterHexUnit` with calls into
  `CombatDamageService` through a new live resolver.
- Replaced trap combat-style DOT calculation in
  `BattleActionAutomaticResultApplier` with canonical combat damage.
- Replaced old fallback AI heuristic damage calls in `MostStupidAIEver` with
  canonical committed damage prediction.
- Removed the unused empty `CalculateResult(...)` method.

## Files Changed

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/LiveCombatDamageResolver.cs`
  - Added a live adapter that builds the current battle snapshot, resolves live
    `TosterHexUnit` objects to `RuntimeUnitId`, and calls
    `CombatDamageService.Default`.
  - Returns explicit errors instead of fallback damage.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
  - Removed `CalculateDamageBetweenTostersWithQ`,
    `CalculateDamageBetweenTosters`, `ReCalculateDamageBetweenTosters`, and
    `CalculateDamageBetweenTostersH3`.
  - `AttackMeSequence`, `AttackMe`, ranged attack helpers, skill damage helper
    methods, and fixed-base damage helper methods now resolve committed damage
    through `CombatDamageService`.
  - Pure damage is cleared only when the service result marks it consumed.
  - Retaliation counter availability is consumed only after retaliation damage
    resolves successfully.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionAutomaticResultApplier.cs`
  - `Fire_Trap` and `Spike_Trap` DOT values now use canonical combat damage.
  - Trap damage failure logs a clear combat damage error and does not add a
    hidden zero-damage status event.
- `TArenaUnity3D/Assets/Scripts/Lesisz/MostStupidAIEver.cs`
  - Old fallback AI heuristic damage sorting now calls
    `LiveCombatDamageResolver` instead of legacy live damage methods.

## Evidence

- Static search found no remaining production references under
  `TArenaUnity3D/Assets/Scripts` to:
  - `CalculateDamageBetweenTosters`
  - `ReCalculateDamageBetweenTosters`
  - `CalculateDamageBetweenTostersH3`
  - `CalculateDamageBetweenTostersWithQ`
  - `CalculateResult`
- Static search found no remaining manual formula fragments under
  `TArenaUnity3D/Assets/Scripts/Lesisz` for:
  - `SpecialResistance` percent math,
  - `SpecialDMGModificator` percent math,
  - `FlatDMGReduce * ...`,
  - `defender == attacker.HATED`.
- Remaining `Random.Range` matches are non-combat committed damage:
  - `BattleActionLifecycle` battle seed generation,
  - `CombatSfxManager` SFX selection,
  - `HexMap` material selection.

## Automatic Tests

- Not added yet. Per `implement-task`, focused EditMode tests are added after
  final QA verdict.
- Planned test: static EditMode audit that blocks reintroduction of legacy
  damage symbols in production `Lesisz` scripts while allowing documented
  non-damage random uses.

## Manual Unity Validation Needed

- Run existing EditMode tests for:
  - `CombatDamageServiceTests`
  - `BattleActionRulesTests`
  - `BattleActionAutomaticResultApplierTests`
- In Play Mode, validate:
  - basic melee and ranged attack,
  - move-and-attack retaliation,
  - a combat-style skill still applied through legacy `CastManager` path,
  - `Fire_Trap` and `Spike_Trap` damage-over-time setup,
  - pure damage bonus consumption on a committed hit.

## Known Risks / QA Focus

- `LiveCombatDamageResolver` depends on the battle being ready enough for
  `BattleSnapshotLiveAdapter.BuildCurrentSceneSnapshot()`.
- Legacy `CastManager` call sites are not redesigned; their old damage helper
  methods now route through canonical damage.
- `DealMeDMGDef...` fixed-base damage paths now use
  `CombatDamageService` with `BaseDamageOverride` to avoid keeping manual
  attack-vs-defense math outside the service.
- The old `MostStupidAIEver` fallback remains disabled in normal AI flow, but
  its heuristic methods no longer reference legacy damage symbols.
