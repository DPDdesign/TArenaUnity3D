# 001 CODEMAP-001 Analysis

Date: 2026-06-10
Task: `_codex/tasks/001_CODEMAP-001_CodebaseContextMap_Coding.md`
Agent role: Coding Agent

## Scope

Read-only analysis of all `.cs` files under `TArenaUnity3D/Assets`.

No runtime `.cs`, Unity asset, prefab, scene, material, controller,
`.inputactions`, generated Unity file, `.asmdef`, or `.asmref` edits were made.

## Method

- Listed all `.cs` files under `TArenaUnity3D/Assets`.
- Grouped files by path bucket and line count.
- Extracted class/interface/enum/struct declarations from non-vendor game code.
- Searched for first-order coupling markers:
  - `PlayFabControler.PFC`
  - `PlayFabClientAPI`
  - `PlayFabSettings`
  - `PhotonNetwork`
  - `MonoBehaviourPun`
  - `MonoBehaviourPunCallbacks`
  - `[PunRPC]`
  - `IPunObservable`
- Searched for persistence and resource markers:
  - `BinaryFormatter`
  - `Application.persistentDataPath`
  - `PlayerPrefs`
  - `Resources.Load`
  - `TextAsset`
  - `SceneManager.LoadScene`
- Searched for incomplete/legacy markers:
  - `NotImplementedException`
  - `TODO`
  - `OLD`
  - `UNUSED`
  - `Retsot`

## Inventory

Total `.cs` files: 443.

Large external/vendor surfaces:

- `Assets/Photon`: 190 files, about 31050 lines.
- `Assets/PlayFabSdk`: 96 files, about 53111 lines.
- `Assets/PlayFabEditorExtensions`: 28 files, about 5965 lines.
- `Assets/PhotonChatApi`: 12 files, about 2199 lines.
- Plugins/copies: AsyncAwaitUtil, WebSocket, OutlineEffect, AShopExport,
  UnityOutlineFX.

Likely game/legacy surface:

- Root `Assets/*.cs`: 25 files.
- `Scripts/Lesisz/HexMap`: 19 files.
- `Scripts/Lesisz/Skills`: 4 files.
- `Scripts/Lesisz/PathFinding`: 16 files.
- `Scripts/Lesisz/Menu`: 11 files.
- `Scripts/Multiplayer`: 5 files.
- `Scripts/Cielu`: 8 files.
- `Scripts/Lesisz` misc: 4 files.
- `Resources` scripts: 2 files.

## Key Findings

The game core is a hex tactics prototype. The central runtime loop appears to be
menu/build selection, `HexMap.CreateWorld()`, `TeamClass.GenerateTeam()`, turn
selection through `TurnManager`, player input through `MouseControler`, unit
state/combat through `TosterHexUnit`, skill handling through `CastManager`, and
temporary effects through `SpellOverTime`.

The code is not cleanly local-only. Important gameplay classes inherit from PUN
callbacks or use Photon RPCs:

- `HexMap : MonoBehaviourPunCallbacks`
- `MouseControler : MonoBehaviourPunCallbacks`
- `CastManager : MonoBehaviourPunCallbacks`
- `NetworkConnectionManager : MonoBehaviourPunCallbacks`
- `NetworkConne22ctionManager : MonoBehaviourPunCallbacks`
- `PlayerP : MonoBehaviourPun, IPunObservable`

PlayFab is not isolated to login. It appears in shop, overlay/profile, menu unit
ownership, combat result reporting, and Photon authentication.

The largest architecture hotspots are:

- `MouseControler.cs`
- `CastManager.cs`
- `TosterHexUnit.cs`
- `HexMap.cs`
- `PlayFabControler.cs`

## Current Context Map

For future agents:

- code navigation starts at `_codex/agents/docs/codebase-map.md`,
- production intent starts at `_codex/Context/02_Current_State.md`,
- safety rules start at `_codex/Context/03_Production_Rules.md`,
- milestone sequencing starts at `_codex/Context/04_Milestones.md`,
- backend/multiplayer cleanup should not start by deleting SDK folders.

## Main Risks

- Removing Photon/PUN directly will likely break `HexMap`, `MouseControler`,
  `CastManager`, `PlayerP`, `UICanvas`, and network manager scripts.
- Removing PlayFab directly will likely break `PlayFabControler`, `Shop`,
  `OverlayMainMenu`, `Generator`, `MouseControler`, and
  `NetworkConnectionManager`.
- `HexMap.Awake()` checks `PhotonNetwork.IsConnected` and redirects to
  `MainMenu_Scene`, which is suspicious for a local-only target.
- `PanelArmii.BuildG` is a nested UI data type but is also serialized and loaded
  by gameplay code.
- `BinaryFormatter` is used for saved army builds.
- String-based Resources paths and skill names create hidden coupling between
  code, XML, sprites, and prefabs.
- Public/serialized fields are widespread; renaming is high-risk.

## Recommended Follow-Up Tasks

1. Local-only startup path:
   - Map scene flow and make a small plan to let `HexMap` create local worlds
     without needing Photon connection.
2. PlayFab facade/stub:
   - Define local data returned by account/profile/shop/inventory calls before
     removing SDK references.
3. PUN/RPC isolation:
   - Introduce a local action path for movement, attack, wait, defense, and
     skill execution before removing `[PunRPC]` calls.
4. Network duplicate cleanup:
   - Verify Unity scene references for `NetworkConMenager.cs` versus
     `NetworkConnectionManager.cs`.
5. Skill system map:
   - List skill strings, XML entries, icons, and `CastManager` methods before
     refactoring skills.

## Files Updated By This Task

- `_codex/Context/02_Current_State.md`
- `_codex/Context/03_Production_Rules.md`
- `_codex/Context/04_Milestones.md`
- `_codex/Context/CONTEXT-MAP.md`
- `_codex/agents/docs/codebase-map.md`
- `_codex/tasks/001_CODEMAP-001_CodebaseContextMap_Coding.md`
- `_codex/tasks/Analysis/001_CODEMAP-001_CodebaseContextMap_Analysis.md`
- `_codex/tasks/QA/001_CODEMAP-001_CodebaseContextMap_Completion.md`
- `_codex/tasks/QA/001_CODEMAP-001_CodebaseContextMap_QAReview.md`
