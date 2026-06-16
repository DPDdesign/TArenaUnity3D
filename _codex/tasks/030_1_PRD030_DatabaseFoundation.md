# [TARENA] PRD030_1: Database Foundation

- Status: completed
- Type: HITL Task
- Area: Offline Mode, SQLite, Persistence Foundation
- Parent: `_codex/tasks/030_DB-001_OfflineModeDatabasePersistence.md`
- Blocked by: none

## Goal

Create the local Offline Mode SQLite database foundation.

This task creates the database module only. It does not wire PRD019 screens to
the database yet.

## Decisions

- Database is local SQLite.
- Database file target: `Application.persistentDataPath/TArenaOffline.db`.
- Local DB records use plain integer primary keys.
- Authored/catalog references stay as text ids.
- SQLite foreign keys are enabled with `PRAGMA foreign_keys = ON`.
- Normal gameplay flow does not hard delete records.
- Runtime tables use `is_active` with default `1` for soft delete.
- Status/type values are stored as integer enum ids, not text.
- Stable enum ids live in one C# enum contract file, planned as
  `DB Enums.cs`.

## What To Build

- Offline Mode Database Module.
- SQLite dependency/plugin selection and setup, if not already present.
- Database open/create path.
- Schema version table.
- V1 migration runner.
- Transaction helper.
- Persistence error model.
- Foreign key enablement.
- Central DB enum ids.
- Dev/test reset hook only if explicitly allowed.

## Implemented

- `Assets/Scripts/RunMetagame/030_Database/DB Enums.cs`
- `Assets/Scripts/RunMetagame/030_Database/OfflineDatabaseModels.cs`
- `Assets/Scripts/RunMetagame/030_Database/OfflineDatabaseProvider.cs`
- `Assets/Scripts/RunMetagame/030_Database/OfflineDatabaseSchemaV1.cs`
- `Assets/Scripts/RunMetagame/030_Database/OfflineDatabaseModule.cs`
- `Assets/Scripts/RunMetagame/030_Database/OfflineDatabaseHandler.cs`
- `Assets/Scripts/Tests/EditMode/OfflineDatabaseSchemaTests.cs`

## Completion Notes

- SQLite provider path is set up for `Microsoft.Data.Sqlite` with
  `SQLitePCL` initialization.
- Database file path is `Application.persistentDataPath/TArenaOffline.db`.
- Main Menu can bootstrap database creation through `OfflineDatabaseHandler`.
- Physical DB creation and schema application happen on first successful
  `OfflineDatabaseModule.OpenOrCreate()`.

## Acceptance Criteria

- Database file can be created and opened locally.
- Schema V1 can be applied once and detected on later opens.
- `schema_version` is written and read.
- SQLite foreign keys are enabled on opened connections.
- Central enum ids are stable and manually assigned.
- Local primary keys are integer ids.
- No PRD019 screen depends on this database yet.
- No Unity scenes, prefabs, assets, `.asmdef`, or `.asmref` are edited.

## Out Of Scope

- Wiring Start Run, Run Map, Battle, Reward, Shop, Summary, Saved Armies, or
  Battle Result to SQLite.
- Online Mode.
- Backend sync.
- Migration/import from legacy Arena/custom army data.
