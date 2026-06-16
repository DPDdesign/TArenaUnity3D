# [TARENA] PRD030_4: Remove PRD26 Arena Import

- Status: completed
- Type: Cleanup Task
- Area: Saved Armies, Legacy Import Removal, Offline DB Seed Data
- Parent: `_codex/tasks/030_DB-001_OfflineModeDatabasePersistence.md`
- Related: `_codex/tasks/026_PRD019_SavedArmies.md`
- Blocked by: `_codex/tasks/030_2_PRD030_SharedArmySnapshotAndPersistenceModels.md`

## Goal

Remove the temporary PRD026 `Import from Arena` path from the PRD030 product
database direction.

PRD030 starts from a clean database. If test data is needed, it should be
created as explicit seed snapshots in the new database, not imported from legacy
Arena/custom armies.

## Current Problem

PRD026 added `Import from Arena` as a development shortcut. PRD030 product
direction rejects that path because it adds unnecessary legacy coupling.

Known current-code areas:

- `LegacyArenaSavedArmiesSource`
- `ISavedArmiesArenaImportSource`
- `SampleSavedArmiesArenaImportSource`
- `SavedArmyImportCommand`
- `ImportFromArena`
- import UI command/state in Saved Armies screen

## What To Build

- Remove or demote Arena import code from production Saved Armies flow.
- Remove import UI command from PRD030 production path.
- Remove import-specific tests or convert them to seed-snapshot tests.
- Add explicit DB seed snapshot path if dev/test data is needed.
- Keep PRD26 roster rules: saved army identity, slots, current defence, history,
  overwrite confirmation, and active/inactive state.

## Acceptance Criteria

- Saved Armies production flow no longer depends on legacy Arena/custom army
  import.
- Empty DB can still be seeded with known saved armies for dev/test when
  explicitly requested.
- Overwrite still creates a new saved army identity.
- Old saved army becomes inactive via `is_active = 0`, not deleted.
- Current defence is cleared when its active saved army is replaced.
- No legacy `buildN.d` migration or helper flow is introduced.

## Implemented

- Saved Armies production flow now uses explicit `SavedArmiesSeedSnapshotSource`
  seed snapshots instead of `LegacyArenaSavedArmiesSource`.
- `SavedArmiesService` loads seed armies into slots through `LoadSeedArmy`,
  while `ImportFromArena` remains only as a compatibility wrapper.
- Screen/controller messaging and prefab-builder labels now describe seed armies
  instead of Arena import.
- Tests were updated to validate the seed-army flow and overwrite semantics.

## Out Of Scope

- Rewriting the old custom/Arena army creator.
- Migrating old custom army data.
- Deleting unrelated legacy Arena code outside the PRD030 Saved Armies path.
