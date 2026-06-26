# TArenaUnity3D Architecture And Cleanup Context Map

Status: active
Last updated: 2026-06-26

## Use When

Use this map for current mechanics, code navigation, cleanup planning,
dependency cutting, backend/multiplayer removal, and agent handoffs.

## Current Mechanics Context

Use when the task asks what currently exists, what gameplay path is verified,
what systems are legacy, or what a Coding agent must avoid breaking:

- `_codex/Context/08_Current_Mechanics.md`
- `_codex/Context/BattleActionRules.md` when battle turn and skill-availability
  rules must be treated as current accepted gameplay contract
- `_codex/Context/02_Current_State.md`
- `_codex/agents/docs/codebase-map.md`

Current mechanics should be filled from TArena code inspection, user
confirmation, and Unity verification. If evidence is weak, mark a mechanic as
`Unverified`.

## Code Architecture Context

Use for code navigation, cleanup planning, dependency cutting, and agent
handoffs:

- `_codex/agents/docs/codebase-map.md`
- `_codex/tasks/Analysis/001_CODEMAP-001_CodebaseContextMap_Analysis.md`

Code-map headline: apparent core game scripts live in root `Assets/*.cs`,
`Scripts/Lesisz/HexMap`, `Scripts/Lesisz/Menu`, `Scripts/Lesisz/Skills`,
`Scripts/Lesisz/PathFinding`, `Scripts/Cielu`, and `Scripts/Multiplayer`.
Photon/PUN, PlayFab, PhotonChatApi, and plugin/demo folders are heavy
legacy/vendor surfaces and should not be used as default implementation context
unless the task is specifically about dependency removal.

## Backend And Multiplayer Cleanup Context

Use when planning removal or replacement of PlayFab, PUN, Photon, chat,
networking, shop/profile backend, cloud stats, or matchmaking:

- `_codex/Context/02_Current_State.md`
- `_codex/Context/03_Production_Rules.md`
- `_codex/agents/docs/codebase-map.md`
- active cleanup task under `_codex/tasks/`

Do not delete SDK/package folders until game-code references and Unity scene
dependencies have been mapped for that task.

For future multiplayer/sync work, audit visual-only presentation workarounds
such as `Stone_Throw` renderer hiding before impact. Those local fixes should
be replaced by explicit synced reveal timing when networking returns.
