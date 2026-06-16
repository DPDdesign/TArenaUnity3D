# 020 Coding Agent Completion

## Task

Implemented `_codex/tasks/archive/020_PRD019_StartRun.md` as the first PRD019 Start
Run slice.

## Files Changed

- `TArenaUnity3D/Assets/Scripts/RunMetagame/StartRun/StartRunModels.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/StartRun/StartRunContracts.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/StartRun/DefaultStartRunCatalog.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/StartRun/DataMapperStartRunUnitSource.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/StartRun/StartRunService.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/StartRun/OfflineStartRunAdapter.cs`
- `_codex/Gen_Im/RETSOT ONLINE/src/game/ui.js`

## Systems Touched

- Added a new `RunMetagame/StartRun` domain boundary for Offline Mode run
  creation.
- Added SQLite-ready and online-ready DTOs for selected starting army, route
  preview, initial army snapshot, run id, status, authority source, and command
  result.
- Added an authored local starting-army/route catalog separate from saved
  armies and legacy build slots.
- Added a `DataMapper` adapter for reading existing unit costs and legal skill
  ownership without changing unit stats, skills, cooldowns, saved army files,
  battle behavior, scenes, or prefabs.
- Updated the HTML/JS mockup so Task 20 uses the corrected scene flow:
  starting-army selection and inspector are one scene, route preview is the
  second scene, and begin/result is the third scene.

## Behavior Or Setup Summary

- `OfflineStartRunAdapter.BuildScreen(...)` returns player-facing Start Run
  view data:
  - starting army options,
  - selected army details,
  - stack tier/level/amount/combat value,
  - total army value,
  - per-unit locked/unlocked skill indicators,
  - three route previews,
  - selected route and validation state.
- `OfflineStartRunAdapter.BeginRun(...)` creates an Offline run record with a
  mode-neutral payload:
  - account/player id,
  - starting army template id,
  - starting army variant id,
  - selected starting army id,
  - route preview option id,
  - starting currency,
  - created run id,
  - active run status,
  - initial army snapshot.
- The implementation does not treat legacy saved-army/build files as the
  starting-army roster.
- The implementation does not expose offence, defence, or in-use states on
  Start Run data.
- Starting currency is currently `0` because task 20 says currency is included
  only if confirmed by grill; no currency amount was confirmed.

## Unity Checks

- No Unity test run was executed automatically.
- No Unity scene, prefab, Canvas, material, generated Unity file, `.asmdef`, or
  `.asmref` was edited.
- Static JavaScript syntax check passed for the modified mockup file with
  `node --check`.

## Intentionally Not Included

- No Unity Canvas/scene/prefab wiring.
- No PlayFab, PUN, Photon, cloud sync, backend calls, login, or online adapter.
- No mutation of existing gameplay stats, unit XML, skill XML, cooldowns,
  battle flow, saved-army files, or `PlayerPrefs`.
- No final reward, shop, run map, battle-result, saved-army roster, offence, or
  defence implementation.

## Manual Integration Addendum - 2026-06-15

Task 020 is archived and has a manual Unity integration PRD at
`_codex/tasks/RunMetaGame_Tests/020_PRD019_StartRun_ManualIntegrationTest.md`.

The later `PRD_19_20.prefab` UI should be validated there. Before Play Mode,
confirm `StartRunScreenController.backButton` and `beginButton` are assigned to
the Back and Begin button components.
