# TArenaUnity3D Codebase Map

Status: active
Last updated: 2026-06-24

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

Do not treat vendor SDK folders as normal implementation context unless the task
is specifically about dependency removal.

## Scan Summary

Read-only scan date: 2026-06-10.

Scope: all `.cs` files under `TArenaUnity3D/Assets`.

Total `.cs` files found: 443.

| Area | Files | Lines | Classification |
| --- | ---: | ---: | --- |
| `Assets/Photon` | 190 | 31050 | Vendor PUN/Photon package and demos |
| `Assets/PlayFabSdk` | 96 | 53111 | Vendor PlayFab SDK |
| `Assets/PlayFabEditorExtensions` | 28 | 5965 | Vendor PlayFab editor extension |
| `Assets/PhotonChatApi` | 12 | 2199 | Legacy/vendor Photon Chat copy |
| `Assets/Plugins/AsyncAwaitUtil` | 10 | 828 | Plugin |
| `Assets/Plugins/WebSocket` | 2 | 367 | Plugin / Photon support |
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

For PRD019 Run Metagame and PRD030 Offline Database tasks, inspect these first:

- `_codex/agents/docs/PRD019_PRD030_RunMetagame_Code_Map.md`
- `_codex/Documentation/PRD030_OfflineDatabase_Map.md`
- `Assets/Scripts/RunMetagame/`
- `Assets/Scripts/RunMetagame/030_Database/OfflineModeDatabaseComposition.cs`
- `Assets/Scripts/RunMetagame/030_Database/OfflineDatabaseSchemaV1.cs`
- `Assets/Scripts/RunMetagame/030_Database/OfflineRunContextDbReader.cs`
- `Assets/Scripts/RunMetagame/030_Database/OfflineRunContextDbWriter.cs`

Current reward-generation implementation notes:

- `023_RewardMap/RewardMapMaterializedGenerator.cs` is the active PRD37/PRD41
  materialized reward generator.
- PRD042 closed the V1 slot contract: each generated choice plans three
  distinct normal operation types, disabled normal slots stay visible with
  `RewardMapError.NoLegalTarget`, and emergency `RunGold` appears only when all
  three normal slots are impossible.
- PRD043 closed the Reward Map persistence contract: cards now carry explicit
  reward id, reward slot index, legal/error state, fallback state, and
  selected/applied state. Do not infer disabled state from preview text or
  identify focused cards by `template_id` alone.
- PRD045 added upfront unresolved reward opportunities in
  `023_RewardMap/OfflineRewardOpportunityDbStore.cs` and the
  `reward_opportunities` table. Start Run/run map materialization writes the
  planned operation slots; battle completion resolves those slots into concrete
  Reward Map cards.
- Normal battle wins route to Reward Map through persisted
  `RunBattleNextScreen.Reward`; final wins route to Summary Value.
- Reward Map production flow loads persisted reward rows and must not reroll
  screen-time fallback rewards for DB-backed runs.
- PRD41 value parity scales materialized reward amounts from average live stack
  value: Mass/Width target higher raw point gain, while Promote/Downgrade target
  army-shape gain.
- PRD044 reduced the duplicate same-unit stack risk during tactical
  battle-to-reward handoff by reconciling post-battle stacks by stack id, then
  battle-input order, then explicit legacy unit-id fallback.
- Closed reward-flow PRDs are archived under `_codex/tasks/archive/`:
  `042_PRD_RewardMaterializedSlotContract.md`,
  `043_PRD_RewardPersistencePreviewApplyContract.md`,
  `044_PRD_TacticalRewardStackIdentity.md`, and
  `045_PRD_UpfrontRewardOpportunityMaterialization.md`.
- Remaining reward-flow follow-up: no-battle `RecruitReward` nodes receive
  unresolved opportunity rows but still need a direct Reward Map resolution
  hook before that node type is complete.

Current run-metagame DB architecture:

- `OfflineRunContextDbReader` is the shared read side for active run context,
  screen-specific `next_screen` lookup, persisted summary lookup, latest battle
  result lookup, Start Run record reload, and snapshot conversion to screen DTOs.
- `OfflineRunContextDbWriter` is the only runtime code surface that should
  insert or update `offline_runs`.
- Start Run, Run Map, Run Battle, Reward Map, Run Shop, and Summary Value must
  update run context through the writer instead of duplicating
  `INSERT/UPDATE offline_runs` SQL in slice-specific stores.
- UI controllers should read run state through adapters/services and the shared
  reader, not by direct SQLite access or serialized placeholder ids.
- Reward planning DB ownership now spans `reward_opportunities`,
  `reward_choices`, `reward_cards`, and `map_node_rewards`. The first table is
  unresolved run-generation truth; the latter three are resolved concrete
  Reward Map truth.

For dependency-removal tasks, inspect direct game-code references before opening
vendor SDK internals. Most vendor code is not project-specific truth.

## Main Gameplay Flow

### Menu And Army Selection

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

### Match Startup And World Generation

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

- `HexMap` is the central scene controller and is also a `MonoBehaviourPunCallbacks`.
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
- `HexClass.CostToMoveToTile()` throws `NotImplementedException`, though pathing
  appears to use `AggregateCostToEnter()`.

### Player Input, Turn Actions, And AI

Key files:

- `Assets/Scripts/Lesisz/HexMap/MouseControler.cs`
- `Assets/Scripts/Lesisz/MostStupidAIEver.cs`
- `Assets/Scripts/Lesisz/HexMap/BattleSnapshot.cs`
- `Assets/Scripts/Lesisz/HexMap/TacticalAIActionIntent.cs`
- `Assets/Scripts/Lesisz/HexMap/TacticalAICandidateGenerator.cs`
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
- Tactical Battle AI V1 planning through pure snapshots, action intents,
  profile-budgeted search/scoring, ordered fallback intents, and async worker
  planning,
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
- Tactical AI planning is now deliberately separated from live execution; do
  not bypass `TacticalAIExecutionBridge` or mutate live Unity objects from pure
  planner/search code.
- PRD047 async planning must stay worker-safe: use copied immutable snapshot,
  profile, and skill metadata; live execution and logging stay on the main
  thread.

### Skills And Effects

Key files:

- `Assets/Scripts/Lesisz/Skills/CastManager.cs`
- `Assets/Scripts/Lesisz/HexMap/SpellOverTime.cs`
- `Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
- `Assets/Scripts/Lesisz/HexMap/MouseControler.cs`
- `Assets/Scripts/Lesisz/Skills/SkillsDefault.cs`
- `Assets/Scripts/Lesisz/Skills/Skill1.cs`
- `Assets/UICanvas.cs`
- `Assets/RightClickInfo.cs`
- `Assets/RightClickInfoSkill.cs`
- `Assets/Resources/0_Data/UnitCatalog.asset`
- `Assets/Resources/0_Data/Units/*.asset`
- `Assets/Resources/Data/skills.xml`

Responsibilities:

- load unit skill assignment from the unit `ScriptableObject` catalog into
  `TosterHexUnit.skillstrings`,
- display skill buttons and icons from `SelectedToster.skillstrings`,
- load skill metadata/right-click info from Resources XML,
- route selected skill slots through `MouseControler.SelectedSpellid`,
- resolve skill modes by reflection (`spellID + "M"`),
- apply skill targeting flags,
- cast individual named skills,
- apply temporary status/stat modifiers through `SpellOverTime`,
- load skill metadata and icons from Resources XML/sprite paths.

Current structure:

- `UnitCatalog.asset` is the current source for unit tier and which skill
  strings each unit can legally have.
- run, reward, shop, and future player progression state may lock or unlock
  those legal skills for a specific stack.
- `TosterHexUnit.InitateType(name)` loads those strings into
  `TosterHexUnit.skillstrings`.
- `UICanvas` uses the same skill strings to load icons from
  `Resources/Sprites/Skill_Icons/{SkillName}`.
- `MouseControler.CastSkillBooleans(...)` selects a skill slot and asks
  `CastManager.getMode(...)` to configure targeting.
- `CastManager.getMode(spellID, ST)` invokes `{SkillName}M` by reflection.
- `MouseControler.startSpell(...)` commits the clicked target and calls
  `CastManager.startSpell(...)`.
- `CastManager.startSpell(spellID, hex)` invokes `{SkillName}` by reflection.
- Some skills directly play Animator states such as `Skill1`, `Skill2`, or
  `"Skill" + (SelectedSpellid + 1)`.
- Some skills directly instantiate projectiles from `CastManager.Projectiles` or
  store a prefab on `TosterHexUnit.Projectile`.

Risks:

- `CastManager` is over 2000 lines and mixes targeting mode, skill definitions,
  animation/projectile setup, RPCs, and cooldown changes.
- skill names are string-based and must match Resources XML, method names, and
  icon paths.
- several passive skills are represented as empty cast bodies plus mode methods.
- future skill VFX/SFX data on unit models can drift from unit catalog skill assignment
  unless both use the exact skill string as the join key.
- `CastManager.Projectiles[index]`, `Axe(...)`, and `FireBall(...)` are legacy
  projectile paths; planned skill VFX/SFX work should replace them with
  catalog-driven projectile VFX moved by code, without Rigidbody physics.

### Pathfinding

Key files:

- `Assets/Scripts/Lesisz/PathFinding/HPath.cs`
- `Assets/Scripts/Lesisz/PathFinding/IPathTile.cs`
- `Assets/Scripts/Lesisz/PathFinding/IQPathUnit.cs`
- `Assets/Scripts/Lesisz/PathFinding/IQPathWorld.cs`
- `Assets/Scripts/Lesisz/PathFinding/IQPath_AStar.cs`
- `Assets/Scripts/Lesisz/PathFinding/PathfindingPriorityQueue.cs`
- `Assets/Scripts/Lesisz/PathFinding/Priority Queue/*`

Responsibilities:

- A* path resolution over `IPathTile`,
- movement-cost calculation delegated to units and hexes,
- priority queue support.

Risks:

- generic priority queue code may be third-party or copied utility code,
- gameplay rules for blocking and movement cost live mostly in `TosterHexUnit`
  and `HexClass`, not in a separate rules service.

### Backend, Multiplayer, Shop, Profile

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

Avoid opening or editing these folders unless the task is specifically about
SDK removal, plugin removal, or scene reference cleanup:

- `Assets/Photon/`
- `Assets/PlayFabSdk/`
- `Assets/PlayFabEditorExtensions/`
- `Assets/PhotonChatApi/`
- `Assets/Plugins/`
- `Assets/OutlineEffect/`
- `Assets/AShopExport/`
- `Assets/Scripts/Lesisz/UnityOutlineFX-master/`

## Largest Game-Code Files

These are the main architecture hotspots:

- `Scripts/Lesisz/HexMap/MouseControler.cs` - input, turn action state machine,
  RPC wrappers, skill targeting, movement, attacks.
- `Scripts/Lesisz/Skills/CastManager.cs` - skill mode and skill execution
  monolith.
- `Scripts/Lesisz/HexMap/TosterHexUnit.cs` - unit stats, combat, pathing,
  statuses, messages.
- `Scripts/Lesisz/HexMap/HexMap.cs` - map generation, teams, PUN startup path,
  highlighting.
- `Scripts/Multiplayer/PlayFabControler.cs` - backend account/stats/shop/cloud.
- `Symulator.cs` - simulation UI/prototype; contains `NotImplementedException`
  methods.
- `Scripts/Lesisz/HexMap/TurnManager.cs` - initiative and queue logic.
- `Scripts/Lesisz/HexMap/TeamClass.cs` - team build load/generation.

## Known Runtime Risk Markers

Game-code markers found during the scan:

- `NotImplementedException` in `Symulator.cs`, `HexClass.cs`,
  `TosterHexUnit.cs`, and early overloads in `MouseControler.cs`.
- hard-coded old product text in `PlayFabControler.cs`: "Retsot account".
- `BinaryFormatter` used for local build files in menu/team/selection code.
- `Resources.Load` string paths for units, skills, icons, and toster models.
- skill assignment, skill UI, and skill execution are coupled by string names
  across the unit catalog, icon paths, info XML, and `CastManager` method names.
- many public fields used by Unity Inspector; do not rename without permission.
- root and `Scripts` files are mostly in the global namespace, with only small
  pockets such as `HPath`, `Priority_Queue`, `TimeSpells`, `Traps`, `Tostery`,
  and outline/plugin namespaces.

## Recommended Next Tasks

1. Create a dedicated cleanup task to design a local-only game session path that
   bypasses Photon connection requirements without deleting SDKs.
2. Create a PlayFab replacement/stub task for account, inventory, shop, and
   profile reads used by `Shop`, `OverlayMainMenu`, `Generator`, and
   `MouseControler`.
3. Create a small task to decide whether `NetworkConMenager.cs` is unused and
   can be removed after Unity scene reference checks.
4. Create an architecture task to extract `PanelArmii.BuildG` into a plain data
   type only after serialized compatibility is understood.
5. Create a skill-system mapping task before refactoring `CastManager`.
6. When adding skill VFX/SFX, keep `UnitCatalog.asset` as the skill ownership source and
   use the existing skill string as the join key for any model-local
   presentation data.
