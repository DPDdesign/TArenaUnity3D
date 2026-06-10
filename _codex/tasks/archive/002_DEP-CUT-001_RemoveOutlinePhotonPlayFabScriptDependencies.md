# 002 DEP-CUT-001 Remove Outline Photon PlayFab Script Dependencies

- Status: closed
- Type: cleanup
- Area: legacy dependency removal
- Owner: coding-agent

## Goal

Cut script-level dependencies on Photon/PUN and PlayFab SDK, and fix the
OutlineEffect compile error without removing the Outline plugin.

## Scope

Do:

- Remove direct `using Photon.*`, `PhotonNetwork`, PUN inheritance, and
  `[PunRPC]` SDK dependency from game scripts.
- Replace former RPC calls with local-only execution where needed for gameplay
  flow.
- Replace direct PlayFab SDK calls with local no-op/default data paths.
- Keep the Outline plugin because gameplay/highlighting still uses it.
- Fix the obsolete `UnityEngine.VR` import in `OutlineEffect`.
- Document which Unity packages, SDK folders, and scene objects are candidates
  for removal after Unity validation.

Do not:

- Edit Unity scenes, prefabs, materials, controllers, `.inputactions`,
  generated files, `.asmdef`, or `.asmref`.
- Rename public or serialized fields unless unavoidable.
- Change gameplay float values.
- Delete SDK/plugin folders in this task.
- Run Unity builds, `dotnet`, package restore, or Git commands.

## Acceptance Criteria

Done when:

- Non-vendor game scripts no longer require Photon/PUN or PlayFab SDK to
  compile.
- Outline compiles without the obsolete `UnityEngine.VR` namespace error.
- Existing scene-facing backend/network components remain as compile-safe local
  stubs or no-op behaviours.
- The task has a completion protocol under `_codex/tasks/QA/`.
- The final report lists uninstall/removal candidates and manual Unity checks.

## Implementation Summary - 2026-06-10

- Added a local legacy runtime adapter for former PUN RPC call sites.
- Replaced PlayFab SDK usage with a local compatibility backend that feeds
  profile, shop, inventory, and unit ownership from local project data.
- Converted Photon/PUN scene-facing scripts to dependency-free stubs.
- Kept Outline in the project and fixed the `UnityEngine.VR` compile error in
  `OutlineEffect`.
- Preserved multiplayer UI intent and `PlayerPrefs.Multi`; custom multiplayer
  transport remains a future task.
- Added completion and QA review protocols under `_codex/tasks/QA/`.

## Closure - 2026-06-10

Closed after user confirmed the project works in Unity. Reopened note:
Outline is required by this project; only the obsolete `UnityEngine.VR` import
was removed from OutlineEffect.

Next smallest production step: create a dedicated custom multiplayer design task
to replace the transitional local RPC adapter with project-owned session,
build-exchange, and turn/action replication contracts.
