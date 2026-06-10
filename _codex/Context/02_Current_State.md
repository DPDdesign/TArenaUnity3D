# 02 Current State

Status: active
Project: TArenaUnity3D
Last updated: 2026-06-10

## Current Project Goal

TArenaUnity3D is currently a legacy recovery project. The main goal is to make
the Unity project workable again for humans and agents by:

- excavating and documenting the legacy code,
- cutting non-working features, especially PlayFab, PUN, and multiplayer paths,
- replacing assets where needed,
- improving code architecture in small, safe, testable steps.

This is not a feature-growth phase. New systems should not be built before the
legacy surface is mapped, dead dependencies are isolated, and the playable local
loop is understood.

## Current Code State

The first repository scan found 443 `.cs` files under `TArenaUnity3D/Assets`.
Most source volume is third-party or vendor code:

- Photon/PUN package and demos: 190 files.
- PlayFab SDK and editor extensions: 124 files total.
- Other copied plugins: PhotonChatApi, AsyncAwaitUtil, WebSocket, OutlineEffect,
  AShopExport, UnityOutlineFX.
- Game/legacy scripts outside vendor folders: about 96 files.

The apparent game core is a hex-grid tactics prototype around toster units:

- map generation, hex state, movement, traps, turns, and highlighting,
- army/build selection saved through local files and `PlayerPrefs`,
- units, stats, cooldowns, damage, status effects, and skills,
- menu/shop/profile UI,
- AI prototype and simulation helpers.

The code is heavily coupled to Unity scene objects, Inspector fields,
`PlayerPrefs`, Resources XML, local serialized build files, Photon/PUN, and
PlayFab singleton access. Even the local/single-player path still passes through
some PUN classes and PlayFab calls.

## Production Interpretation

The project should be treated as a salvage/refactor workspace:

- Preserve working local gameplay behavior until it is explicitly replaced.
- Prefer documentation and dependency isolation before deletion.
- Remove multiplayer/backend systems only through narrow tasks with compile and
  scene checks in Unity.
- Do not rename serialized/public fields or change gameplay floats without
  explicit permission.

## Must Not Be Copied Here

- Do not import design decisions, current state, tasks, milestones, enemy lists,
  skills, map designs, or gameplay truth from another project unless the user
  explicitly asks for a comparison or migration note.
- Do not reference another project's local files as default context.
