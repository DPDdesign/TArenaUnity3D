# TArenaUnity3D Codebase Scan And Navigation Map

Status: active
Last updated: 2026-06-26

Unity project root:

- `D:\Unity\Projects\TArenaUnity3D\TArenaUnity3D`

Assets root:

- `D:\Unity\Projects\TArenaUnity3D\TArenaUnity3D\Assets`

## Current Recovery Goal

TArenaUnity3D is in legacy recovery mode. The current code work is about:

- excavating and documenting legacy code,
- cutting broken PlayFab, PUN, Photon, and multiplayer paths,
- replacing assets where needed,
- improving architecture in small, Unity-safe steps.

Do not treat vendor SDK folders as normal implementation context unless the
task is specifically about dependency removal.

## Scan Summary

Read-only scan date: 2026-06-10.

Scope: all `.cs` files under `TArenaUnity3D/Assets`.

Total `.cs` files found: 443.

| Area | Files | Lines | Classification |
| --- | ---: | ---: | --- |
| Historical scan: Photon vendor package | 190 | 31050 | Vendor PUN/Photon package and demos; verify current folder existence before routing |
| Historical scan: PlayFab SDK | 96 | 53111 | Vendor PlayFab SDK; verify current folder existence before routing |
| Historical scan: PlayFab editor extension | 28 | 5965 | Vendor PlayFab editor extension; verify current folder existence before routing |
| Historical scan: Photon Chat copy | 12 | 2199 | Legacy/vendor Photon Chat copy; verify current folder existence before routing |
| `Assets/Plugins/AsyncAwaitUtil` | 10 | 828 | Plugin |
| Historical scan: WebSocket plugin | 2 | 367 | Plugin / Photon support; verify current folder existence before routing |
| `Assets/OutlineEffect` | 7 | 634 | Plugin |
| Root `Assets/*.cs` game scripts | 25 | 2196 | Legacy game/UI/network scripts |
| `Scripts/Lesisz/HexMap` | 19 | 5189 | Main tactics gameplay core |
| `Scripts/Lesisz/Skills` | 4 | 1751 | Skill execution and skill base prototypes |
| `Scripts/Lesisz/PathFinding` | 16 | 2462 | A* and priority queue pathfinding |
| `Scripts/Lesisz/Menu` | 11 | 851 | Army/menu/build selection |
| `Scripts/Multiplayer` | 5 | 617 | Custom PlayFab/PUN/chat/store wrappers |
| `Scripts/Cielu` | 8 | 231 | Combat/toster prototypes |
| `Scripts/Lesisz` misc | 4 | 274 | AI and misc prototypes |
| `Resources` scripts | 2 | 32 | Data marker scripts |
| `AShopExport` | 2 | 122 | Demo/plugin-like shop visual helpers |
| `Scripts/Lesisz/UnityOutlineFX-master` | 2 | 267 | Copied outline plugin |

## Default Navigation

For normal gameplay/code tasks, inspect these first:

- `Assets/Scripts/Lesisz/HexMap/`
- `Assets/Scripts/Lesisz/Skills/`
- `Assets/Scripts/Lesisz/Menu/`
- `Assets/Scripts/Lesisz/PathFinding/`
- root `Assets/*.cs` scripts such as `UICanvas.cs`, `Shop.cs`,
  `OverlayMainMenu.cs`, `NetworkConnectionManager.cs`, `PlayerP.cs`
- `Assets/Scripts/Multiplayer/` only when dealing with PlayFab/PUN/shop/chat
  removal or backend replacement

For dependency-removal tasks, inspect direct game-code references before opening
vendor SDK internals. Most vendor code is not project-specific truth.
