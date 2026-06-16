# [TARENA] Coding Completion - PRD019 Run Battle

- Date: 2026-06-15 01:09
- Task: `_codex/tasks/022_PRD019_RunBattle.md`
- Agent: Coding Agent
- Status: ready-for-qa

## Scope

Implemented the PRD019 Run Battle bridge as a narrow Offline Mode domain slice.
The implementation prepares explicit battle launch payloads and records
completion payloads with win/loss, surviving army snapshot, stack losses, gold
gain, result source, and next-screen decision.

No existing battle gameplay, `HexMap`, `TeamClass`, unit stats, skill behavior,
cooldowns, scenes, prefabs, materials, `.asmdef`, `.asmref`, PlayFab, PUN, or
Photon paths were changed.

## Files Changed

- `TArenaUnity3D/Assets/Scripts/RunMetagame/RunBattle/RunBattleModels.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/RunBattle/RunBattleContracts.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/RunBattle/DefaultRunBattleEncounterCatalog.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/RunBattle/OfflineRunBattleLaunchAdapter.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/RunBattle/RunBattleService.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/RunBattle/OfflineRunBattleAdapter.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/RunBattleServiceTests.cs`
- `_codex/Gen_Im/RETSOT ONLINE/src/game/ui.js`
- `_codex/Gen_Im/RETSOT ONLINE/src/game/renderer.js`

## Implementation Notes

- `RunBattleService.PrepareBattle(...)` validates run id, route node id, current
  army snapshot, and encounter lookup before creating a launch view.
- `RunBattleLaunchPayload` carries run battle id, run id, route node id,
  encounter id, current army snapshot id, enemy army source id, enemy goal, and
  source marker.
- `OfflineRunBattleLaunchAdapter` creates a legacy launch record that explicitly
  labels current `HexMap`/`TeamClass`/PlayerPrefs/local-file paths as adapter
  surfaces, not domain authority.
- `RunBattleService.CompleteBattle(...)` requires a prepared battle, records
  before/after army snapshots, calculates per-stack and total losses, records
  run gold gained, and decides next screen:
  - normal win -> `Reward`
  - final win -> `FinalSummary`
  - non-win -> `RunLoss`
- `DefaultRunBattleEncounterCatalog` is intentionally small authored local data
  for the current Offline Mode slice.
- Task 22 HTML mockup was added to the existing PRD019 prototype menu and shows
  launch context, completion payload, and next-screen transition.

## Validation

- Ran `node --check _codex\Gen_Im\RETSOT ONLINE\src\game\ui.js`: passed.
- Ran `node --check _codex\Gen_Im\RETSOT ONLINE\src\game\renderer.js`: passed.
- Unity EditMode tests were authored but not run automatically, per project
  policy.

## Tests Added

- `RunBattleServiceTests.PrepareBattle_ReturnsOfflineLaunchPayloadAndLegacyAdapterRecord`
- `RunBattleServiceTests.CompleteBattle_RecordsWinLossesAndRoutesToReward`
- `RunBattleServiceTests.CompleteBattle_RoutesFinalWinToSummaryAndLossToRunLoss`
- `RunBattleServiceTests.PrepareBattle_RejectsMissingArmy`

## Known Limits

- This does not launch a Unity battle scene yet.
- This does not generate temporary `PanelArmii.BuildG` files or set
  `PlayerPrefs`; those remain future adapter work when Unity scene wiring is
  explicitly authorized.
- Encounter catalog values are local V1 placeholders for bridge behavior and
  should be replaced by Run Map authored/generated data when task 021 is
  implemented.
- No Unity prefab or scene mockup was created; PRD019 asks for an HTML/JS task
  mockup under `_codex/Gen_Im/RETSOT ONLINE/`.
