# TArenaUnity3D Codebase Map Index

Status: active
Last updated: 2026-06-26

Unity project root:

- `D:\Unity\Projects\TArenaUnity3D\TArenaUnity3D`

Assets root:

- `D:\Unity\Projects\TArenaUnity3D\TArenaUnity3D\Assets`

## Purpose

This file is only the codebase routing index. Keep it short.

Do not scan the whole project by default. Pick the smallest matching codebase
map, then inspect only the files needed for the current task.

When the user asks to "add a codebase map", create a new focused map file under
`_codex/agents/docs/codebase/`. Do not add another large domain section to this
index.

## Domain Codebase Maps

- Recovery goal, scan summary, default navigation:
  `_codex/agents/docs/codebase/scan-navigation-map.md`
- Menu, army selection, match startup, player input, and turn actions:
  `_codex/agents/docs/codebase/menu-flow-code-map.md`
- Combat API, battle readiness, battle action validation, and battle-flow risks:
  `_codex/agents/docs/codebase/battle-action-code-map.md`
- Skills, effects, skill data/UI coupling, and pathfinding:
  `_codex/agents/docs/codebase/skills-effects-code-map.md`
- Broad gameplay/combat compatibility router:
  `_codex/agents/docs/codebase/gameplay-combat-code-map.md`
- Explicit PRD019/PRD030 run-metagame and offline database code routing:
  `_codex/agents/docs/codebase/run-metagame-code-map.md`
- PlayFab, Photon, PUN, chat, shop/profile backend, vendor/legacy avoidance:
  `_codex/agents/docs/codebase/backend-legacy-map.md`
- Largest game-code files, runtime risk markers, recommended next tasks:
  `_codex/agents/docs/codebase/hotspots-risks-map.md`

## PRD Loading Rule

PRD-specific code maps are not default code navigation. Load
`_codex/agents/docs/codebase/run-metagame-code-map.md`,
`_codex/agents/docs/PRD019_PRD030_RunMetagame_Code_Map.md`, or
`_codex/Documentation/PRD030_OfflineDatabase_Map.md` only when the current
prompt, task, or brief explicitly requires that PRD/run-metagame/offline-DB
scope.

## Default Navigation Rule

For normal gameplay/code tasks, start with
`_codex/agents/docs/codebase/scan-navigation-map.md`, then route to one focused
domain map. Vendor SDK folders and copied plugin/demo folders are explicit-only
unless the task is specifically about dependency removal, plugin removal, or
scene reference cleanup.
