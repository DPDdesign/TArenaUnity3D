# [TARENA] PRD030_2: Shared Army Snapshot And Persistence Models

- Status: completed
- Type: Architecture Task
- Area: Offline Mode, Army Snapshot, Persistence DTOs
- Parent: `_codex/tasks/030_DB-001_OfflineModeDatabasePersistence.md`
- Blocked by: none

## Goal

Create one shared durable army snapshot model for Offline Mode.

This replaces the database meaning of slice-specific snapshot DTOs, but it does
not require deleting all view DTOs at once.

## Problem

PRD019 currently has separate army snapshot shapes:

- `RunArmySnapshot`
- `RunBattleArmySnapshot`
- `RewardMapArmySnapshot`
- `RunShopArmySnapshot`
- `SummaryValueArmySnapshot`
- `SavedArmy`
- `BattleResultSavedArmySnapshot`

Those shapes are acceptable as temporary view/adapter DTOs. They are not
acceptable as separate database truth.

## What To Build

- Shared durable army snapshot model.
- Snapshot stack model.
- Snapshot stack skill model.
- Persistence adapter for creating immutable snapshots.
- Mapping adapters from existing PRD019 slice DTOs into the shared model.
- Mapping adapters from shared model back to current slice DTOs where needed.
- Snapshot diff helper for losses before/after battle.

## Implemented

- `Assets/Scripts/RunMetagame/030_Database/OfflineArmySnapshotModels.cs`
- `Assets/Scripts/RunMetagame/030_Database/OfflineArmySnapshotFactory.cs`
- `Assets/Scripts/RunMetagame/030_Database/OfflineArmySnapshotMapper.cs`
- `Assets/Scripts/RunMetagame/030_Database/OfflineArmySnapshotDiff.cs`
- `Assets/Scripts/RunMetagame/030_Database/DataMapperOfflineArmySnapshotCatalogResolver.cs`
- `Assets/Scripts/Tests/EditMode/OfflineArmySnapshotMapperTests.cs`

## Completion Notes

- Shared persistence snapshot keeps only durable runtime fields:
  `unit_id`, `amount`, `formation_slot`, assigned `skill_id`.
- Slice DTO-only fields such as `lost`, `level`, `experience`, temporary
  values, and `unlocked` flags are treated as adapter/view data.
- Mappers now exist for Start Run, Run Battle, Reward Map, Run Shop, Summary
  Value, Saved Armies, and Battle Result snapshot shapes.
- Battle losses are derived by comparing snapshots by `formation_slot`.

## Persistence Rules

- `army_snapshots.snapshot_id` is integer primary key.
- `army_snapshots.node_id` is integer FK to `route_nodes.node_id`.
- Snapshot stacks store `unit_id`, `amount`, and `formation_slot`.
- Snapshot stack skills store assigned `skill_id`.
- Do not store `lost` in snapshot stacks.
- Do not store stack `level` or `experience` in V1 snapshot persistence.
- Do not store `temporary_power`.
- Do not store `unlocked` in snapshot skills. If a skill row exists, the skill
  is assigned/available for that stack in that snapshot.
- UI/helper fields such as display name, tier, combat value, level, lost, and
  unlocked may still exist in view DTOs, but they are derived or adapter data.

## Acceptance Criteria

- One shared persistence snapshot can represent Start Run, Battle, Reward,
  Shop, Summary, Saved Armies, and Battle Result needs.
- Snapshot round-trip covers stacks, amounts, formation slots, and assigned
  skills.
- Battle losses can be derived by comparing before/after snapshots.
- Existing authored unit and skill catalogs remain the legal content source.
- Existing PRD019 screens can keep view DTOs until their integration task.

## Out Of Scope

- Removing all PRD019 DTOs.
- Rebalancing units, skills, reward values, or shop values.
- Moving authored unit/skill catalogs into SQLite.
