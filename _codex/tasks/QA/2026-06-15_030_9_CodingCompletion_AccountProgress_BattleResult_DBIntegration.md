# [TARENA] 030_9 Coding Completion - Account Progress And Battle Result DB Integration

- Date: 2026-06-15
- Task: `_codex/tasks/030_9_PRD030_AccountProgress_BattleResult_DBIntegration.md`
- Status: implemented-qa-pending

## Scope

Integrated Battle Result with the shared offline SQLite state so async battle
results, account XP/rank progress, unlock progress, and saved-army history can
persist across adapter recreation.

## Completed

- Added `OfflineBattleResultDbStore` as the SQLite-backed `IBattleResultStore`
  for `async_battle_results`, local account progression updates, unlock record
  persistence, and saved-army history writes tied to async result ids.
- Added a local `async_battle_result_details` companion table created on demand
  to persist exact attacker/defender payloads, opponent metadata, and the
  no-steal/no-destroy preservation record needed to rebuild the Battle Result
  screen after adapter recreation.
- Changed `BattleResultService` to deep-clone attacker/defender/opponent data,
  return the persisted record when a store exists, and generate DB-safe default
  async result ids.
- Changed `OfflineBattleResultAdapter` and `BattleResultScreenController` so the
  prototype Battle Result screen reloads persisted local result data instead of
  recreating a fresh in-memory store every `Awake()`.
- Corrected the default offline account bootstrap to start with 2 unlocked
  saved-army slots, matching project metaprogression rules.

## Files Changed

- `TArenaUnity3D/Assets/Scripts/RunMetagame/027_BattleResult/OfflineBattleResultDbStore.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/027_BattleResult/BattleResultService.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/027_BattleResult/OfflineBattleResultAdapter.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/027_BattleResult/BattleResultScreenController.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineDatabaseAccountBootstrap.cs`

## Validation Performed

- Read-path review across Battle Result service/store/controller and the shared
  offline DB module: pass.
- Acceptance-criteria review for result persistence, account XP/rank updates,
  unlock record typing, saved-army preservation, and history lookup by saved
  army id: pass in code review.
- Manual logic review caught and fixed an attacker-history mapping bug for
  defence outcomes before QA handoff.

## Not Run

Unity compilation, EditMode execution, and Play Mode execution were not run in
this Codex pass, in line with the project rule that the user compiles/tests in
Unity unless explicitly allowed.

## Notes

- The new `async_battle_result_details` table is intentionally created lazily by
  the DB store so existing version-1 databases can gain the missing Battle
  Result payload persistence without a wider schema migration task in this PRD.
- Result persistence uses JSON copies of the battle-result payload so the Battle
  Result screen can rebuild exact army summaries and preservation messaging
  without mutating or reinterpreting saved armies through another service.
- Saved-army history rows are keyed by `saved_army_id` and linked back to the
  persisted `async_battle_result_id`; duplicate history rows for the same
  result id are soft-deactivated before reinsertion.
