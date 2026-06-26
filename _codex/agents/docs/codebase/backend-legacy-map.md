# TArenaUnity3D Backend And Legacy Dependency Codebase Map

Status: active
Last updated: 2026-06-26

## Backend, Multiplayer, Shop, Profile

Key files:

- `Assets/Scripts/Multiplayer/PlayFabControler.cs`
- `Assets/Scripts/Multiplayer/PhotonControler.cs`
- `Assets/Scripts/Multiplayer/Chat.cs`
- `Assets/Scripts/Multiplayer/InventoryObjects.cs`
- `Assets/Scripts/Multiplayer/Details.cs`
- `Assets/NetworkConnectionManager.cs`
- `Assets/NetworkConMenager.cs`
- `Assets/PlayerP.cs`
- `Assets/Shop.cs`
- `Assets/OverlayMainMenu.cs`

Responsibilities:

- PlayFab login/register/stats/catalog/inventory/purchase/cloud scripts,
- PlayFab-to-Photon authentication token setup,
- PUN connection and room creation/joining,
- chat messages for combat log,
- shop UI and owned item checks,
- profile/progression overlay.

Risks:

- this is the main removal target requested by the user,
- `NetworkConMenager.cs` appears to be an older/typo duplicate of
  `NetworkConnectionManager.cs`,
- `Shop`, `OverlayMainMenu`, `MouseControler`, and `NetworkConnectionManager`
  directly call `PlayFabControler.PFC`,
- `PlayerP` only wraps Photon instantiation/destruction and currently has an
  empty `OnPhotonSerializeView`.

## First-Order PlayFab/PUN Coupling Points

Start dependency cleanup from these game-code files:

- `Assets/Scripts/Multiplayer/PlayFabControler.cs` - central PlayFab singleton,
  stats, account, shop, inventory, cloud scripts, Photon auth.
- `Assets/Shop.cs` - store and purchase UI directly depends on
  `PlayFabControler.PFC`.
- `Assets/OverlayMainMenu.cs` - profile/currency/stats overlay depends on
  `PlayFabControler.PFC`.
- `Assets/Scripts/Lesisz/Menu/Generator.cs` - checks owned units through
  PlayFab store/inventory state.
- `Assets/Scripts/Lesisz/HexMap/MouseControler.cs` - calls PlayFab stats/cloud
  results and uses many PUN RPCs.
- `Assets/Scripts/Lesisz/HexMap/HexMap.cs` - inherits PUN callbacks, uses
  Photon room/player state and RPCs during world setup.
- `Assets/Scripts/Lesisz/Skills/CastManager.cs` - inherits PUN callbacks and
  sends skill effects/move actions through RPCs.
- `Assets/NetworkConnectionManager.cs` and `Assets/NetworkConMenager.cs` -
  Photon connection/room flow.
- `Assets/PlayerP.cs` - Photon network instantiate/destroy wrapper.
- `Assets/UICanvas.cs` - disconnect button calls `PhotonNetwork`.

## Do Not Start Here By Default

Avoid opening or editing vendor/plugin folders unless the task is specifically
about SDK removal, plugin removal, or scene reference cleanup. Some vendor
folders from older scans may already be absent from the current workspace;
verify existence before treating a path as active code.

- `Assets/Plugins/`
- `Assets/OutlineEffect/`
- `Assets/AShopExport/`
- `Assets/Scripts/Lesisz/UnityOutlineFX-master/`
