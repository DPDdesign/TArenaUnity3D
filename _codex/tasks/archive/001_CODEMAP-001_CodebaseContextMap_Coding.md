# 001 CODEMAP-001 Codebase Context Map

- Status: completed
- Type: documentation / architecture analysis
- Area: codebase navigation, agent context, legacy recovery
- Owner: Coding Agent

## Goal

Analyze all `.cs` files under `TArenaUnity3D/Assets`, separate game code from
vendor/demo/plugin code, and update local agent documentation so future agents
can work in this repository without rediscovering the legacy structure.

## Scope

Do:

- Inspect the current C# file inventory under `TArenaUnity3D/Assets`.
- Categorize script areas into game core, UI/menu, persistence, backend,
  networking/multiplayer, plugins, demos, and vendor SDK code.
- Update `_codex/agents/docs/codebase-map.md` with a practical map for Coding
  agents.
- Update `_codex/Context/CONTEXT-MAP.md` if routing needs to point to the new
  map or analysis output.
- Write a concise analysis report under `_codex/tasks/Analysis/`.
- Create a completion protocol under `_codex/tasks/QA/`.

Do not:

- Edit runtime `.cs` files.
- Edit Unity assets, prefabs, scenes, materials, controllers, `.inputactions`,
  generated Unity files, `.asmdef`, or `.asmref`.
- Run Git, `dotnet`, Unity builds, package restore, SDK install, or external
  build scripts.
- Delete PlayFab, PUN, Photon, or plugin files in this task.
- Import context from another project.

## Acceptance Criteria

Done when:

- The codebase map lists the main C# areas and marks likely game code versus
  vendor/removal-candidate code.
- The map identifies first-order PlayFab/PUN/multiplayer coupling points.
- The context map routes future code-architecture and backend-cleanup work to
  the right local documents.
- The analysis report records the scan method, counts, key findings, risks, and
  recommended next tasks.
- The completion protocol documents changed files and Unity verification notes.

## Implementation Notes

- Treat this as a documentation and architecture-navigation task.
- Use read-only source inspection for `.cs` files.
- The user compiles and validates inside Unity.

## Implementation Summary - 2026-06-10

Completed.

Updated project context docs with the current recovery goal: legacy code
excavation, removal/isolation of broken PlayFab/PUN/multiplayer features, asset
replacement where needed, and small-step architecture improvement.

Executed read-only `.cs` analysis under `TArenaUnity3D/Assets` and recorded:

- 443 total C# files,
- vendor/plugin split for Photon, PlayFab, PhotonChatApi, plugins, and copied
  outline/shop helpers,
- likely game-code areas and main runtime responsibilities,
- first-order PlayFab/PUN coupling points,
- architecture hotspots and recommended next cleanup tasks.

Files updated:

- `_codex/Context/02_Current_State.md`
- `_codex/Context/03_Production_Rules.md`
- `_codex/Context/04_Milestones.md`
- `_codex/Context/CONTEXT-MAP.md`
- `_codex/agents/docs/codebase-map.md`
- `_codex/tasks/Analysis/001_CODEMAP-001_CodebaseContextMap_Analysis.md`
- `_codex/tasks/QA/001_CODEMAP-001_CodebaseContextMap_Completion.md`
- `_codex/tasks/QA/001_CODEMAP-001_CodebaseContextMap_QAReview.md`

Runtime code changed: none.

Unity/build/test commands run: none, per project rules.
