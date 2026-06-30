# [TARENA] PRD055A Coding Agent Completion

- Task: `_codex/tasks/archive/055A_PRD_CoreCombatDamageService_BasicCombat.md`
- Date: 2026-06-30
- Agent: Coding Agent
- Status: completed and closed

## Scope Implemented

- Added a shared combat damage model/calculator/service path for PRD055A.
- Routed `BattleActionRules.Apply(...)` basic ranged, melee/move-and-attack,
  and retaliation damage through `CombatDamageService`.
- Kept `BattleActionRules.Validate(...)` as the tactical legality authority.
- Added explicit fail-fast result rejection for missing snapshot/catalog damage
  data instead of `0 damage` fallback.
- Added snapshot data required by PRD055A damage calculation:
  - `CatalogUnitId`
  - attack/defense/min-damage/max-damage modifiers
  - outgoing damage reduction percent
  - incoming damage reduction percent
  - flat damage reduction
  - pure damage
  - defense penetration
  - hated target runtime id
- Added explicit `ConsumesActorPureDamage` event data on `DamageApplied`.
- Updated live damage application and AI snapshot simulation to consume actor
  pure damage only when the damage event says so.
- Prevented rejected damage results from being applied as empty successful live
  or simulated actions.
- Updated snapshot clone/hash and Tactical AI clone/spawn copying so new damage
  state is preserved.

## Files Changed

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/CombatDamageModels.cs`
  - New `ICombatUnitCatalog`, catalog entry, request/input/result/forecast
    models, roll purpose constants, and pure `CombatDamageCalculator`.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/CombatDamageService.cs`
  - New snapshot/catalog resolver service and `DataMapper` catalog adapter.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionRules.cs`
  - Added `Apply(..., CombatDamageService)` overload.
  - Replaced basic/counterattack damage event generation with service calls.
  - Added deterministic damage rejection path with log.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionModels.cs`
  - Added `BattleActionResultEvent.ConsumesActorPureDamage`.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotModels.cs`
  - Added PRD055A snapshot damage/catalog fields.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotLiveAdapter.cs`
  - Populates new snapshot fields from live `TosterHexUnit`.
  - Preserves existing base combat stat fields and adds separate runtime
    modifier fields.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotBuilder.cs`
  - Clones and hashes the new snapshot fields.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionLiveApplier.cs`
  - Clears live actor `SpecialPUREDMG` only when the event consumes it.
  - Refuses to apply rejected `BattleActionResult` instances.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISearchScoring.cs`
  - Preserves new snapshot fields in clone/spawn paths.
  - Clears simulated actor pure damage when a damage event consumes it.
  - Leaves simulation unchanged when result damage calculation rejects.
  - Replaces heuristic average-damage prediction with deterministic committed
    damage prediction through `CombatDamageService`.
  - Preserves `GameSeed`/`NextActionIndex` across simulation and advances
    `NextActionIndex` after simulated applied actions.
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionLifecycle.cs`
  - Stores live battle `GameSeed` and `NextActionIndex` for deterministic
    runtime action generation.

## Intentional Non-Scope

- Did not migrate skill damage call sites; PRD055B owns that.
- Did not remove legacy `TosterHexUnit.CalculateDamageBetweenTosters(...)`
  traces; PRD055C owns cleanup.
- Did not edit prefabs, scenes, assets, `.asmdef`, `.asmref`, materials, or
  generated Unity files.

## Known Risks For QA

- `CombatDamageService` requires `CatalogUnitId` and catalog resolution. Live
  snapshots now populate it from `TosterHexUnit.Name`; existing test fixtures
  need test catalogs when exercising basic damage.
- `CombatDamageService` builds calculator input from catalog base stats plus
  snapshot runtime modifiers. Existing base stat fields remain available for
  older systems that already read `BattleUnitSnapshot`.
- Skill/passive legacy damage call sites still use
  `TosterHexUnit.CalculateDamageBetweenTosters(...)`; PRD055B/PRD055C own that
  migration and cleanup.

## Verification

- Not run: Unity EditMode tests.
- Not run: Unity compile/build.
- Manual inspection performed through targeted source reads and `rg`.
- Added EditMode coverage for `CombatDamageService`, `BattleActionRules`, and
  Tactical AI deterministic seed/index behavior, but did not execute the tests
  inside Unity.
