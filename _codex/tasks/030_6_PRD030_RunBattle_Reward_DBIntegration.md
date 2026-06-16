# [TARENA] PRD030_6: Run Battle And Reward DB Integration

- Status: draft
- Type: Integration Task
- Area: Run Battle, Reward Map, Events, Offline DB
- Parent: `_codex/tasks/030_DB-001_OfflineModeDatabasePersistence.md`
- Related:
  - `_codex/tasks/022_PRD019_RunBattle.md`
  - `_codex/tasks/023_PRD019_RewardMap.md`
- Blocked by: `_codex/tasks/030_5_PRD030_StartRun_RunMap_DBIntegration.md`

## Goal

Persist run battles and reward choices through the Offline Mode database.

## What To Build

- Battle preparation creates `run_events` of type `Battle`.
- `run_battles` stores battle-specific data and references `event_id`.
- Battle completion creates post-battle snapshot.
- Battle completion updates before/after snapshot references.
- Reward choice creates `run_events` of type `Reward`.
- `reward_choices` and `reward_cards` persist generated reward choices.
- Reward preview can create preview snapshot records where needed.
- Applying reward creates applied snapshot and updates run current army/gold.

## Acceptance Criteria

- Battle node receives integer `node_id` and authored `encounter_id`.
- Battle completion persists outcome and post-battle snapshot.
- Losses are derivable from before/after snapshots.
- Reward choice reloads by integer `reward_choice_id`.
- Selected reward persists and cannot be re-applied incorrectly.
- Preview-vs-apply consistency remains testable.
- Reward templates remain authored data, not DB catalog truth.
- Existing battle gameplay is not changed.

## Implementation - 2026-06-15

### What Changed

- Added DB-backed battle persistence in `OfflineRunBattleDbStore`:
  - prepares `run_events` + `run_battles`,
  - saves pre-battle and post-battle army snapshots,
  - writes `run_battle_losses`,
  - updates `offline_runs` status, current snapshot, current node, and run gold.
- Added DB-backed reward persistence in `OfflineRewardMapDbStore`:
  - prepares `run_events` + `reward_choices` + `reward_cards`,
  - saves reward preview snapshots for the focused card,
  - applies selected reward into a new snapshot,
  - updates `offline_runs` status, current snapshot, and run gold,
  - blocks double-apply on the same `reward_choice_id`.
- Added shared snapshot SQLite helper in `OfflineArmySnapshotDbRepository` so battle/reward code uses one save/load path for army snapshots, stacks, and stack skills.
- Updated `RunBattleService` and `RewardMapService` store contracts so DB stores can return persisted IDs and persisted snapshot surfaces back into the runtime flow.
- Updated `OfflineArmySnapshotMapper` so runtime stack ids reconstructed from DB stay stable for reward/battle logic instead of collapsing to slot-only ids.
- Added `RewardMapError.AlreadyApplied` and persisted selected reward tracking.
- Added `ToLegacyRunBattleId(...)` to `OfflineDatabaseLegacyIdentity`.
- No Inspector fields changed.

### Automatic Test

- Added `Assets/Scripts/Tests/EditMode/OfflineRunBattleRewardDbTests.cs`.
- Test coverage in that file:
  - start run creates persisted run context,
  - battle prepare persists `run_battle_id` and battle snapshot,
  - battle completion persists post-battle snapshot,
  - reward choice persists `reward_choice_id`,
  - reward apply persists applied snapshot,
  - second apply on the same choice is rejected with `AlreadyApplied`.
- Tests were not run automatically. Run them manually in Unity Test Runner:
  - `Window > General > Test Runner > EditMode`
  - run `OfflineRunBattleRewardDbTests`
  - expected result: green pass.

### Unity Test

#### Unity Setup

- Ensure `OfflineDatabaseHandler` is already active from Main Menu flow as in PRD030_1.
- Start the game from the normal offline flow that creates a run in DB.
- Use the existing DB file at `Application.persistentDataPath/TArenaOffline.db`.

#### Play Mode Test

- Start an offline run.
- Enter one battle node and finish the battle with a win.
- Verify in the SQLite DB:
  - new row in `run_events` with battle type,
  - new row in `run_battles`,
  - new post-battle row in `army_snapshots`,
  - rows in `run_battle_losses`.
- Open reward flow for that run and select one reward.
- Verify in the SQLite DB:
  - new row in `run_events` with reward type,
  - new row in `reward_choices`,
  - rows in `reward_cards`,
  - selected reward fills `selected_reward_id`,
  - applied reward writes a new `army_snapshots` row,
  - `offline_runs.current_army_snapshot_id` and `offline_runs.current_run_gold` update.
- Try to apply the same reward twice through the same choice context.
- Expected result: first apply succeeds, second apply is blocked.

### QA Verdict

- QA review report not generated in this implementation pass.
- Architecture risk to watch:
  - reward/battle runtime stack ids are still reconstructed from unit-based runtime ids,
  - duplicate stacks of the same unit type may need a stricter runtime stack identity rule in a later task.
- No follow-up fixes from a QA pass were applied because no separate QA pass was run yet.

### Notes

- This task implements DB persistence and reload logic for `RunBattle` and `RewardMap`.
- It does not wire a new dedicated battle screen flow or replace mock/sample screen setup.
- Reward templates remain authored in code and are not moved into database catalog tables.
- Existing local sample/mock controllers that still use in-memory stores were left untouched.

### Next Steps

- Run `OfflineRunBattleRewardDbTests` in Unity EditMode Test Runner.
- Manually validate one full offline sequence: Start Run -> Battle -> Reward -> Apply Reward.
- If duplicate-unit stacks become common in runtime armies, introduce an explicit persisted runtime stack identity in a follow-up DB task.
