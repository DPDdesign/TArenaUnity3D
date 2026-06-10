# 002 DEP-CUT-001 Coding Agent Completion

## Task

`_codex/tasks/archive/002_DEP-CUT-001_RemoveOutlinePhotonPlayFabScriptDependencies.md`

## Files Changed

- `TArenaUnity3D/Assets/Scripts/Multiplayer/LocalLegacyRuntime.cs`
- `TArenaUnity3D/Assets/Scripts/Multiplayer/PlayFabControler.cs`
- `TArenaUnity3D/Assets/Scripts/Multiplayer/PhotonControler.cs`
- `TArenaUnity3D/Assets/NetworkConnectionManager.cs`
- `TArenaUnity3D/Assets/NetworkConMenager.cs`
- `TArenaUnity3D/Assets/PlayerP.cs`
- `TArenaUnity3D/Assets/UICanvas.cs`
- `TArenaUnity3D/Assets/OutlineM.cs`
- `TArenaUnity3D/Assets/OverlayMainMenu.cs`
- `TArenaUnity3D/Assets/Shop.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Menu/Generator.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Menu/PlayButton.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/HexMap.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/MouseControler.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/CastManager.cs`
- `TArenaUnity3D/Assets/OutlineEffect/OutlineEffect.cs`
- `_codex/tasks/002_DEP-CUT-001_RemoveOutlinePhotonPlayFabScriptDependencies.md`

## Systems Touched

- Legacy backend/login/profile/shop compatibility.
- Legacy PUN RPC call sites in map, input, and skill execution.
- Network UI entry points for future custom multiplayer.
- Outline/highlight compatibility.
- Package and scene removal notes.

## Behavior Or Setup Summary

Game scripts no longer depend on PlayFab SDK, Photon/PUN namespaces, or the
external `Outline` type. Former PlayFab calls now go through a local
`PlayFabControler` compatibility component that provides local profile stats,
currency, store, and owned-unit data from `Resources/Data/Units`.

Outline remains in the project because highlighting still uses it. The only
Outline change is removal of the obsolete `UnityEngine.VR` import from
`OutlineEffect`.

Former PUN behaviours now inherit from `LocalNetworkBehaviour`. The old
`photonView.RPC(...)` calls execute locally for `RpcTarget.All` and no-op for
`RpcTarget.Others`. This keeps turn, movement, attack, and skill code callable
without PUN while leaving a narrow replacement point for a future custom
multiplayer transport.

The multiplayer UI and `PlayerPrefs.Multi` choice are preserved. Custom
multiplayer is not implemented in this task; `LocalGameSession.ShouldRunNetworkGameplay`
currently returns `false` so old PUN synchronization paths do not run.

## Unity Checks

Not run. Project rules say the user compiles and tests inside Unity unless a
specific Unity test command is allowed.

Recommended manual checks in Unity:

- Open the project and confirm the previous `UnityEngine.VR` OutlineEffect
  compile error is gone.
- Open `MainMenu_Scene`, choose local battle settings, and start `TestArea2`.
- In `TestArea2`, select a unit, move, attack, cast at least one skill, wait,
  defend, and end a turn.
- Open profile/shop/generator UI and confirm local stats, currency, unit
  unlocks, and owned buttons do not require login.
- Toggle multiplayer UI and confirm the UI still stores the choice, while no
  Photon/PUN connection is attempted.

## Intentionally Not Included

- No Unity scene, prefab, material, controller, `.inputactions`, `.asmdef`, or
  `.asmref` edits.
- No deletion of Photon, PlayFab, Outline, or package folders.
- No custom multiplayer transport implementation.
- No Git, `dotnet`, Unity build, package restore, or SDK installation commands.

## Removal Candidates

Unity Package Manager candidates after Unity validation:

- `com.unity.ads`
- `com.unity.analytics`
- `com.unity.purchasing`
- `com.unity.multiplayer.center`
- `com.unity.ai.navigation`
- `com.unity.timeline`
- `com.unity.xr.legacyinputhelpers`
- `com.unity.collab-proxy` if Unity Version Control is unused
- IDE packages not used by the user: `com.unity.ide.rider`,
  `com.unity.ide.visualstudio`, `com.unity.ide.vscode`
- optional if no Unity tests are kept: `com.unity.test-framework` and
  `com.unity.ext.nunit`

Asset folder candidates after scene/prefab cleanup:

- `Assets/Photon/`
- `Assets/PhotonChatApi/`
- `Assets/Plugins/WebSocket/`
- `Assets/PlayFabSdk/`
- `Assets/PlayFabEditorExtensions/`
- `Assets/MobileDependencyResolver/`
Do not remove `Assets/OutlineEffect/` unless the Outline/highlight path is
replaced first.

Scene cleanup candidates:

- Build Settings disabled scenes: `Assets/Scenes/Battle_Scene.unity`,
  `Assets/Scenes/Log.unity`, `Assets/Scenes/TestArea.unity`,
  `Assets/Scenes/Master.unity`, and Photon demo scenes.
- `Assets/Scenes/LogIn.unity`: old `PhotonControler` object; old `PlayFab`
  object after login UI is replaced or rewired.
- `Assets/Scenes/TestArea2.unity`: inactive `OutlineManager` object and legacy
  UnityOutlineFX components only after confirming they are not part of the
  current Outline/highlight path.
- `Assets/Resources/PlayerP*.prefab`: legacy UnityOutlineFX components only if
  those prefabs are not using them for visible highlighting.
