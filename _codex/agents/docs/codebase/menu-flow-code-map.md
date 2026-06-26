# TArenaUnity3D Menu, Army, And Match Flow Codebase Map

Status: active
Last updated: 2026-06-26

## Menu And Army Selection

Key files:

- `Assets/Scripts/Lesisz/Menu/PlayButton.cs`
- `Assets/Scripts/Lesisz/Menu/SelectMenu.cs`
- `Assets/Scripts/Lesisz/Menu/PanelArmii.cs`
- `Assets/Scripts/Lesisz/Menu/Generator.cs`
- `Assets/Scripts/Lesisz/Menu/MainMenu.cs`
- root `OverlayMainMenu.cs`

Responsibilities:

- select local/multi/AI mode with `PlayerPrefs`,
- choose `YourArmy` and `EnemyArmy`,
- create/edit saved army builds,
- load unit data through `DataMapper` from the unit `ScriptableObject` catalog,
- drive menu scene transitions.

Important persistence:

- `Application.persistentDataPath + "/buildN.d"` serialized with
  `BinaryFormatter`,
- `PlayerPrefs` keys such as `YourArmy`, `EnemyArmy`, `AI`, `Multi`,
  `BuildNumber`, `NazwaBohatera`.

Risks:

- menu and build selection are tied to scene object names, `PlayerPrefs`, and
  `BinaryFormatter`,
- `Generator.cs` also reads PlayFab inventory/store state for unit ownership,
- build data structure `PanelArmii.BuildG` is nested inside a UI class but is
  used by gameplay classes.

## Match Startup And World Generation

Key files:

- `Assets/Scripts/Lesisz/HexMap/HexMap.cs`
- `Assets/Scripts/Lesisz/HexMap/TeamClass.cs`
- `Assets/Scripts/Lesisz/HexMap/HexClass.cs`
- `Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
- `Assets/Scripts/Lesisz/HexMap/TosterView.cs`
- `Assets/Scripts/Lesisz/HexMap/TurnManager.cs`

Responsibilities:

- branch between `PlayerPrefs.GetInt("Multi")` paths,
- load saved armies,
- generate the hex grid and hex GameObjects,
- generate teams and toster units,
- maintain hex-to-object and toster-to-object maps,
- choose turn order and advance turns.

Current structure:

- `HexMap` is the central scene controller and is also a
  `MonoBehaviourPunCallbacks`.
- `TeamClass` loads `PanelArmii.BuildG` from disk and instantiates team units.
- `TosterHexUnit` contains unit stats, movement/pathing, damage, death,
  cooldowns, status flags, and spell effect management.
- `TurnManager` chooses active units by initiative, waiting state, speed, and
  team order.

Risks:

- `HexMap.Awake()` redirects to `MainMenu_Scene` when `PhotonNetwork` is not
  connected, which can block clean local/single-player startup.
- multiplayer setup, build exchange RPCs, local map generation, and UI state are
  mixed in `HexMap`.
- `TosterHexUnit` is a large model/service hybrid and directly sends chat/UI
  messages.
- `HexClass.CostToMoveToTile()` throws `NotImplementedException`, though
  pathing appears to use `AggregateCostToEnter()`.

## Player Input, Turn Actions, And AI

Key files:

- `Assets/Scripts/Lesisz/HexMap/MouseControler.cs`
- `Assets/Scripts/Lesisz/MostStupidAIEver.cs`
- `Assets/Scripts/Lesisz/HexMap/BattleSnapshotModels.cs`
- `Assets/Scripts/Lesisz/HexMap/BattleSnapshotBuilder.cs`
- `Assets/Scripts/Lesisz/HexMap/BattleSnapshotLiveAdapter.cs`
- `Assets/Scripts/Lesisz/HexMap/TacticalAIExecutionBridge.cs`
- `Assets/Scripts/Lesisz/HexMap/TacticalAISearchScoring.cs`
- `Assets/Scripts/Lesisz/HexMap/TacticalAIProfile.cs`
- `Assets/Scripts/Lesisz/HexMap/TacticalAILiveTurnIntegrator.cs`
- `Assets/Scripts/Lesisz/HexMap/TacticalAIAsyncDecisionPipeline.cs`
- `Assets/UICanvas.cs`
- `Assets/OutlineM.cs`
- `Assets/UnityOutlineManagerMainToster.cs`
- `Assets/UnityOutlineFXMainToster.cs`

Responsibilities:

- state-machine style mouse/keyboard input,
- unit selection, movement previews, attacks, waiting, defense,
- skill targeting,
- camera drag/scroll helper calls,
- outline/highlight integration,
- UI button callbacks,
- Tactical Battle AI planning through pure snapshots, profile-budgeted
  search/scoring, ordered fallback planning, and async worker planning,
- AI live execution through the same move/attack/skill/lifecycle paths as
  player actions after bridge revalidation,
- legacy AI fallback through `MostStupidAIEver`.

Risks:

- `MouseControler` is a large monolith and also a `MonoBehaviourPunCallbacks`.
- many actions are routed through `[PunRPC]` even when the intended future target
  is local play.
- `MouseControler.Start()` calls `PlayFabControler.PFC.GetStats()`.
- two early overloads throw `NotImplementedException`, while later overloads
  implement similarly named move/attack coroutines.
- Tactical AI planning is now deliberately separated from live execution; do not
  bypass `TacticalAIExecutionBridge` or mutate live Unity objects from pure
  planner/search code.
- PRD047 async planning must stay worker-safe: use copied immutable snapshot,
  profile, and skill metadata; live execution and logging stay on the main
  thread.
