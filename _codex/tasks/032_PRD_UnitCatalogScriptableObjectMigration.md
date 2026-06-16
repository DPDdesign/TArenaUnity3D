# [TARENA] PRD: Unit Catalog ScriptableObject Migration

- Status: closed
- Type: PRD
- Area: Unit Data, DataMapper, Run Metagame
- Triage: closed

## Problem Statement

TArenaUnity3D used XML unit data as the runtime source for unit stats, costs,
sprite references, and skill ownership. That made unit data hard to author in
Unity, easy to drift from UI and metagame systems, and risky for future recovery
work.

The project needed a concrete Unity-authored unit catalog where each unit has
its own `ScriptableObject`, while preserving the existing data shape and keeping
the stable skill string contract used by battle, UI, skill info, and
`CastManager`.

## Solution

Migrate built-in unit runtime data from XML to a `ScriptableObject` unit catalog.

The new catalog keeps one unit definition asset per unit and exposes the same
runtime data through `DataMapper`, so existing battle, menu, UI, and run-metagame
callers can continue using the current data access path.

The migration also closes the shallow run-metagame adapter duplication by
centralizing unit-definition mapping in one small module. Run, reward, and shop
state still decide which legal unit skills are locked or unlocked for a specific
stack.

## User Stories

1. As a designer, I want each built-in unit to have its own unit definition
   asset, so that unit data is inspectable and editable in Unity.
2. As a designer, I want unit stats, cost, tier, sprite reference, and skill list
   stored together, so that a unit's authored data has good locality.
3. As a designer, I want skill slot order preserved, so that UI and animation
   behavior stays predictable.
4. As a developer, I want `DataMapper` to remain the unit data entry point, so
   that existing callers do not need a broad rewrite.
5. As a developer, I want XML removed as runtime unit truth, so that there is
   only one canonical built-in unit source.
6. As a developer, I want no runtime XML fallback, so that missing catalog setup
   fails visibly instead of silently using stale data.
7. As a developer, I want unit tier to come from the unit catalog, so that run
   templates do not become a second tier database.
8. As a developer, I want run-metagame unit mapping centralized, so that Start
   Run, Run Shop, and Reward Map cannot drift independently.
9. As a developer, I want Reward Map to stop using a hardcoded unit fallback, so
   that missing catalog data does not hide bugs.
10. As a player, I want run and reward screens to show unit data consistently,
    so that unit identity does not change between systems.
11. As a future metagame designer, I want the catalog to define legal unit
    skills while run state controls unlocks, so that progression can lock or
    unlock known skills without changing unit identity.

## Implementation Decisions

- Built-in unit runtime data now lives in a `UnitCatalog` `ScriptableObject`.
- Each built-in unit has one `UnitDefinitionAsset`.
- `UnitDefinitionAsset` stores unit name, tier, HP, attack, defense, initiative,
  speed, damage minimum, damage maximum, cost, sprite path, and ordered skill
  strings.
- `DataMapper` reads unit definitions from `UnitCatalog`; XML unit fallback is
  disabled.
- Legacy `Units.xml` may exist as editor migration input or historical
  reference, but it is not runtime truth.
- The existing `DataMapper` interface remains the main seam for battle, menu,
  UI, and run-metagame unit reads.
- Unit tier is catalog-owned. Start Run template tier does not override a
  catalog unit's tier when the unit exists.
- Run-metagame unit mapping is centralized in `RunMetagameUnitDefinitionMapper`.
- `DataMapperStartRunUnitSource`, `DataMapperRunShopUnitSource`, and
  `RewardMapDataMapperUnitSource` remain as small adapters for existing
  run-metagame interfaces.
- `RewardMapDataMapperUnitSource` no longer has a hardcoded fallback unit list.
- The unit catalog stores the full legal skill list for a unit.
- Run, reward, shop, and future player progression state may lock or unlock
  those legal skills for a specific stack.
- Skill strings remain the stable ids used across catalog data, UI, skill info,
  skill presentation, and `CastManager` reflection.

## Testing Decisions

- External behavior should be tested through the public run-metagame seams,
  not by asserting parser or asset internals.
- Start Run has coverage for using unit-source tier instead of template tier.
- Existing Start Run, Run Shop, Reward Map, Summary Value, Run Battle, and
  Battle Result EditMode tests remain the prior-art pattern for deterministic
  service tests over plain data.
- Manual Unity verification should confirm that `UnitCatalog.asset` is assigned
  on `DataMapper`, all unit definition assets are present, battle unit loading
  still initializes stats and skills, and Start Run/Run Shop/Reward Map display
  expected tiers and skill lock state.

## Out of Scope

- Implementing player mod support.
- Runtime XML or JSON unit import.
- Removing `skills.xml`.
- Rewriting `CastManager`.
- Renaming skill ids.
- Changing skill cooldowns, targeting, damage, movement, or gameplay float
  values.
- Changing unit stats, costs, or skill ownership beyond preserving the migrated
  catalog data.
- Replacing run/reward/shop skill unlock state with catalog data.
- Editing scenes, prefabs, materials, Animator Controllers, `.asmdef`,
  `.asmref`, or generated Unity files.

## Further Notes

This migration established the current built-in unit data model for TArenaUnity3D:
Unity-authored `ScriptableObject` unit definitions collected by one catalog,
with `DataMapper` as the runtime access seam.

Future mod unit data import and validation is tracked separately in:

- `_codex/tasks/031_PRD_ModUnitDataImportValidation.md`
