# [TARENA] PRD030_3: Integer Identity Migration

- Status: completed
- Type: Architecture Task
- Area: Offline Mode, Local Identity, SQLite Keys
- Parent: `_codex/tasks/030_DB-001_OfflineModeDatabasePersistence.md`
- Blocked by: none

## Goal

Move local runtime persistence identity from string/Guid-style ids to plain
SQLite integer primary keys and integer foreign keys.

## Current Problem

PRD019 prototype code creates ids such as:

- `run-<guid>`
- `snapshot-<guid>`
- `route-map-<guid>`
- `reward-choice-<guid>`
- `shop-visit-<guid>`
- `saved-army-<guid>`
- `slot-01`
- authored node strings such as `node-final`

That is fine for PRD019 prototypes, but PRD030 local DB identity should be
simpler and stricter.

## Rules

- Local DB primary keys are `integer primary key`.
- Local DB foreign keys are integer ids.
- Authored/catalog references remain text ids:
  - `unit_id`
  - `skill_id`
  - `template_id`
  - `encounter_id`
  - authored route/path/template ids
- Future online/sync ids are explicit optional fields, not local primary keys.
- Runtime `node_id` is a globally unique integer from `route_nodes`.
- Authored route node strings may exist only as seed/catalog input.

## What To Build

- Identity mapping policy for PRD030 persistence adapters.
- Integer id fields in persistence DTOs.
- Conversion layer from existing PRD019 string ids to DB integer ids where a
  slice still exposes string ids.
- Route node seeding that converts authored node ids into integer `node_id`
  records.
- Slot identity strategy: physical slots use integer `slot_id` plus
  `slot_index`.

## Implemented

- `Assets/Scripts/RunMetagame/030_Database/OfflineDatabaseLegacyIdentity.cs`
- `Assets/Scripts/RunMetagame/030_Database/OfflineRouteMapSeedModels.cs`
- `Assets/Scripts/RunMetagame/030_Database/OfflineRouteMapSeedFactory.cs`
- `Assets/Scripts/RunMetagame/030_Database/OfflineArmySnapshotMapper.cs`
- `Assets/Scripts/Tests/EditMode/OfflineDatabaseLegacyIdentityTests.cs`

## Completion Notes

- Added one shared conversion layer for legacy string ids like
  `snapshot-42`, `saved-army-14`, and `slot-03` into integer local ids.
- Shared snapshot mapping now preserves numeric runtime ids when a slice still
  exposes legacy string labels.
- Route map seeding now has an explicit integer identity layer for
  `route_map_id`, `route_path_id`, and globally unique `node_id`, while keeping
  authored node/path strings only as seed input context.
- Physical slot labels can remain legacy UI text while DB-facing code uses
  integer slot identity.

## Acceptance Criteria

- DB-facing code does not create new string/Guid ids for local runtime records.
- Runtime relations use integer FK values.
- Authored ids remain text references and are not converted into fake local PKs.
- Existing PRD019 UI can still display legacy labels during transition.
- Tests cover creating records and loading them by integer id.

## Out Of Scope

- Online identity.
- Cloud sync.
- Import/export formats.
- Renaming serialized Unity fields.
