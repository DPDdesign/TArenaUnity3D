# [TARENA] PRD030_5: Start Run And Run Map DB Integration

- Status: completed
- Type: Integration Task
- Area: Start Run, Run Map, Offline DB
- Parent: `_codex/tasks/030_DB-001_OfflineModeDatabasePersistence.md`
- Related:
  - `_codex/tasks/archive/020_PRD019_StartRun.md`
  - `_codex/tasks/archive/021_PRD019_RunMap.md`
- Blocked by:
  - `_codex/tasks/030_1_PRD030_DatabaseFoundation.md`
  - `_codex/tasks/030_2_PRD030_SharedArmySnapshotAndPersistenceModels.md`
  - `_codex/tasks/030_3_PRD030_IntegerIdentityMigration.md`

## Goal

Wire Start Run and Run Map to the Offline Mode SQLite database.

## What To Build

- Replace production Start Run in-memory created-run storage with DB
  persistence.
- Begin Run creates `offline_runs`.
- Begin Run creates start army snapshot.
- Begin Run seeds `route_maps`, `route_paths`, and all `route_nodes` upfront.
- `offline_runs.current_node_id` points to an integer `route_nodes.node_id`.
- Run Map loads route state by integer `run_id`.
- Travel updates route node states, current node, stage progress, and route
  progress.

## Acceptance Criteria

- Start Run creates a durable run id.
- The starting army snapshot persists.
- Run Map can reload after adapter recreation.
- Route nodes are seeded once per run.
- Route progress survives reload.
- Current node uses integer node id.
- UI still talks to adapters/services, not SQLite directly.
- No backend SDK, PlayFab, Photon, PUN, or Online Mode is introduced.

## Implemented

- `OfflineStartRunAdapter` now persists created runs through SQLite-backed
  `OfflineStartRunDbStore`.
- Begin Run now creates `offline_runs`, stores the starting army snapshot, and
  seeds `route_maps`, `route_paths`, and `route_nodes` upfront.
- `OfflineRunMapAdapter` now uses SQLite-backed `OfflineRunMapDbStore`.
- Run Map reloads persisted route progress and current node after service or
  adapter recreation.
- EditMode integration tests were added for Start Run and Run Map DB flow.
