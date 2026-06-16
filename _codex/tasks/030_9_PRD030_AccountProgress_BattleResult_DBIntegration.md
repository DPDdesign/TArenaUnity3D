# [TARENA] PRD030_9: Account Progress And Battle Result DB Integration

- Status: draft
- Type: Integration Task
- Area: Account Progress, Async Battle Results, Offline DB
- Parent: `_codex/tasks/030_DB-001_OfflineModeDatabasePersistence.md`
- Related: `_codex/tasks/027_PRD019_BattleResult.md`
- Blocked by: `_codex/tasks/030_8_PRD030_Summary_SavedArmies_DBIntegration.md`

## Goal

Persist local account progress, saved-army history, and local async battle
results through the Offline Mode database.

## What To Build

- `offline_accounts` default local account record.
- Account XP and rank updates.
- `account_unlocks` for units, skills, maps, and saved-army slots.
- `async_battle_results` records.
- `saved_army_history` entries for offence/defence results.
- No-steal/no-destroy preservation record.
- Battle Result screen reloads persisted result data.

## Acceptance Criteria

- Local account XP and rank survive reload.
- Battle results survive adapter recreation.
- Saved-army history is queried by saved army id, not slot id.
- Attacker and defender saved armies are not mutated by result recording.
- Unlock records use stable enum ids/types.
- Offline result data does not pretend to be backend-authoritative online data.

## Implementation - 2026-06-15

### What Changed

- `BattleResultService`, `OfflineBattleResultAdapter`, and new
  `OfflineBattleResultDbStore`: Battle Result now persists through SQLite
  instead of stopping at `InMemoryBattleResultStore`. The DB store writes
  `async_battle_results`, updates `offline_accounts.account_xp` and
  `offline_accounts.rank_value`, records stable `account_unlocks`, and writes
  `saved_army_history` rows linked by `saved_army_id` plus
  `async_battle_result_id`.
- `OfflineBattleResultDbStore`: added a lazy
  `async_battle_result_details` companion table to persist exact
  attacker/defender payloads, opponent metadata, and the no-steal/no-destroy
  preservation record so Battle Result can rebuild after adapter recreation.
- `BattleResultService`: deep-clones attacker, defender, and opponent payloads
  before saving, returns the persisted result when a store exists, and now
  generates DB-safe default async result ids with a numeric suffix instead of a
  GUID-only suffix.
- `BattleResultScreenController`: the Battle Result prototype now reloads the
  persisted sample result from a dedicated local preview DB file instead of
  recreating a fresh in-memory store on every `Awake()`. This keeps the
  prototype local-only and non-authoritative while proving the persistence seam.
- `OfflineDatabaseAccountBootstrap`: default offline accounts now start with 2
  unlocked saved-army slots, matching the project metaprogression baseline.
- New, changed, and removed Inspector/public fields: No Inspector fields
  changed.

### Automatic Test

- Added
  `TArenaUnity3D/Assets/Scripts/Tests/EditMode/OfflineBattleResultDbTests.cs`.
- `Record_PersistsAcrossAdapterRecreation_AndUpdatesAccountProgress` checks
  that a recorded offline async result survives service/store recreation,
  restores exact Battle Result payload data, updates persisted account XP/rank,
  unlocks the next saved-army slot at the configured threshold, and writes
  saved-army history rows keyed by `saved_army_id`.
- `Record_UsesIndependentCopiesOfInputArmies` checks that Battle Result stores
  deep copies of attacker/defender data so later mutations to the original
  request objects do not change the returned result or the reloaded persisted
  result.
- These tests are deterministic and do not require scene, prefab, or Inspector
  setup because they exercise pure Battle Result service/store logic against a
  temporary SQLite file.
- Run manually in Unity: `Window > General > Test Runner > EditMode`, select
  `OfflineBattleResultDbTests`, then click `Run Selected` or `Run All`.
  Expected result: 2 passing tests.
- I did not run Unity tests automatically; the user runs them inside Unity.

### Unity Test

#### Unity Setup

- Let Unity import the new scripts under
  `Assets/Scripts/RunMetagame/027_BattleResult/` and the new EditMode test
  file under `Assets/Scripts/Tests/EditMode/`.
- Open
  `Assets/Resources/UI/PRD_19/027_BattleResult/PRD_19_027_BattleResult.prefab`.
- Place the prefab under a Canvas in a test scene if you want to interact with
  it in Play Mode. No new serialized field wiring is required for this task.

#### Play Mode Test

- Enter Play Mode with the Battle Result prefab visible.
- Confirm the sample Battle Result renders attacker/defender summaries, rank
  delta, account XP progress, next unlock preview, and the no-army-lost
  message.
- Click `Continue` and verify the flow-status text reports `store lookup ok`,
  proving the controller can reload the persisted result through the DB-backed
  adapter.
- Exit Play Mode and enter Play Mode again. The same sample result id should
  render from the local preview DB instead of a fresh in-memory store.

### QA Verdict

- Final QA verdict: pass with Unity manual verification pending.
- QA report path:
  `_codex/tasks/QA/2026-06-15_030_9_QA_ArchitectureReview.md`.
- Actionable findings: none.
- Non-blocking observations: progression thresholds currently live in the DB
  store while the preview label text remains in the service; behavior is
  consistent for the current thresholds, but future progression changes should
  centralize those rules to avoid drift. The lazy
  `async_battle_result_details` table is a pragmatic schema-v1 extension and
  should be folded into a formal migration plan if a future task versions the
  DB schema again.
- Follow-up fixes applied after QA: none were required because no blocking
  findings remained.

### Notes

- Offline Battle Result persistence is explicitly local-only. The stored result
  uses `BattleResultAuthoritySource.LocalOfflineAdapter` and does not pretend to
  be backend-authoritative online data.
- Saved-army history rows are keyed by `saved_army_id`, not slot id, and the
  store soft-deactivates previous history rows for the same async result id
  before rewriting them.
- The Battle Result prototype uses a dedicated preview DB file so this task does
  not mutate unrelated runtime preview state while still validating persistence.

### Next Steps

- Run `OfflineBattleResultDbTests` in Unity Test Runner EditMode.
- Run the Battle Result prefab Play Mode smoke test and verify the persisted
  sample result reloads through the local preview DB.
- If the progression thresholds change in a later PRD, centralize the shared
  account-progress/unlock rules before extending Battle Result or other
  metaprogression screens.
