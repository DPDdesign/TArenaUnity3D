# 03 Production Rules

Status: active
Project: TArenaUnity3D
Last updated: 2026-06-10

## Operating Mode

TArenaUnity3D is in legacy recovery mode. Default work should support one of
these outcomes:

- make the existing code easier to understand,
- isolate or remove broken backend/multiplayer paths,
- replace unusable assets,
- reduce architectural coupling without changing gameplay by accident.

## Project Director Rules

- Keep task slices small and independently verifiable in Unity.
- Prefer maps, notes, and local task files before broad implementation.
- Make the next task easy for a Coding agent to execute without rediscovering
  the whole project.
- Do not approve large rewrites until the local gameplay loop and serialized
  scene dependencies are known.

## Coding Rules

- Work only on `.cs`, `.md`, or other plain text files unless the user explicitly
  permits Unity asset edits.
- Do not edit prefabs, scenes, materials, controllers, `.inputactions`,
  generated Unity files, `.asmdef`, or `.asmref` without permission.
- Do not rename public or serialized fields without permission.
- Do not change gameplay float values without permission.
- Do not remove PlayFab/PUN/Photon assets in one broad deletion. First map
  references, isolate game code from those APIs, then remove in a dedicated
  task.
- Treat root-level scripts and `Scripts/Lesisz/*` as likely game code until
  proven otherwise.
- Treat `Assets/Photon*`, `Assets/PlayFab*`, and plugin/demo folders as
  external or removal-candidate code unless a local scene still depends on them.

## Verification Rules

- The user compiles and tests inside Unity unless an explicit Unity test command
  is allowed.
- Documentation-only work should be verified by file inspection and source-map
  searches, not by build commands.
- Code removal or dependency decoupling must include a manual Unity checklist:
  main menu, local battle start, turn selection, movement, attack/skill, end
  condition, and console errors.

## Must Not Be Copied Here

- Do not import design decisions, current state, tasks, milestones, enemy lists,
  skills, map designs, or gameplay truth from another project unless the user
  explicitly asks for a comparison or migration note.
- Do not reference another project's local files as default context.
