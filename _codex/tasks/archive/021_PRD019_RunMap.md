# [TARENA] PRD019: Run Map

- Status: implemented-manual-test-pending
- Type: HITL Task
- Area: Run Route, Node Map, Encounter Selection, Online-Ready Architecture
- Label: ready-for-agent
- Parent: `_codex/tasks/019_PRD_RunMetagameRewardFramework.md`
- Blocked by: None
- Implemented prerequisite pending manual QA: `_codex/tasks/020_PRD019_StartRun.md`

## HITL Gate

Before implementation, run `/grill-me` for this task.

Confirm first-scope node types, route length, route visibility, encounter
preview rules, and what route data should be authored versus generated.

Specific questions for `/grill-me`:

- How should the three route choices from Start Run become the actual run map?
- Is the first implementation mostly authored/preset paths, generated paths, or
  authored templates with light variation?
- Are battle armies preset in V1? This is likely acceptable first, but the risk
  is that players will memorize them.
- How much should `Expected Risk` reveal? The desired direction is partial
  information: the player can infer danger, but does not know exactly what will
  happen.

## What To Build

Create the Run Map slice: after starting a run, the player sees a node-route
map and chooses the next node. The map is node-based, inspired by Mewgenics and
Slay the Spire, not a spatial Heroes-style overworld.

This task must not finalize exact encounter tables, route maps, enemy rosters,
or balance values unless explicitly confirmed during grill.

## Mode Architecture - 2026-06-14

Current implementation target:

- Implement this task for `Offline Mode` only.
- Offline Mode owns route-map state locally: selected route choice, available
  nodes, completed nodes, route progress, run gold, and stage progress.
- Route generation/loading can use authored local route data first. Do not make
  local file paths or `PlayerPrefs` the route authority.

Future online target:

- `Online Mode` must load and validate route state through a backend adapter.
- The online client may preview available nodes, but travel must be confirmed by
  the backend before the route state changes.
- Do not add online matchmaking, cloud sync, PlayFab, PUN, Photon, or backend
  calls in this task.

Shared seam:

- Shape route display and travel as mode-neutral view data plus a travel
  command/result.
- Offline and future Online should be separate adapters behind the same route
  map interface so UI does not duplicate route rules.

## Clarification - 2026-06-14

Feedback from the Run Map UI mockup:

- `Your Army` summary is good, but unit classification shown in the mockup is
  future scope. TArena does not currently have that classification model. Keep
  it as a future ADR candidate, not a V1 requirement.
- `Run Essence` is interesting, but not for this task. Keep it as a future ADR
  candidate.
- Any currency label on this screen should say `RUN GOLD`, not generic `GOLD`.
- `Stage Progress` is a good screen element and should remain in the Run Map
  data/UI contract.
- The generated mockup's route layout is not correct enough as a product
  direction. The Run Map must reflect the three route choices introduced at
  Start Run, so the whole route/path generation module needs `/grill-me`.
- `Battle - Reward Bias` is a good direction, but the player-facing label
  should be closer to `Possible Rewards` or reward hints.
- `Expected Risk` is a good direction if it stays uncertain. The player should
  get a read on danger, not exact full knowledge of the enemy army or outcome.

## Future ADR Candidates

Do not implement these in this task unless a later ADR/task explicitly approves
them:

- Unit classification displayed in `Your Army`.
- `Run Essence` or similar run-wide resource/system.
- Long-term route generation rules beyond the first grilled V1 route model.

## 1. Database

Prepare SQLite-ready persistence boundaries for:

- game mode and route authority/source,
- run id,
- route map id,
- route node ids,
- node type: battle, shop, recruit/reward, final/boss,
- node state: locked, available, completed, selected,
- current node,
- route progress,
- stage progress,
- selected route choice id from Start Run,
- route path id for each of the three route choices,
- route/path bias metadata for rewards, units, skills, or encounter types,
- possible reward hint metadata for player preview,
- expected risk hint metadata that reveals danger level without exact enemy
  knowledge,
- run gold balance for display.

Event nodes remain optional future scope.

## 2. Backend Methods

Define methods for:

- selecting the Offline route-map adapter for current implementation,
- creating or loading a route map for a run,
- creating or loading the three route paths selected from Start Run route
  preview data,
- listing available next nodes,
- validating travel to a node,
- marking route progress,
- exposing node preview data with possible reward hints,
- exposing expected risk as a partial-information hint,
- exposing run gold and stage progress,
- gating final/boss node access.

Backend methods should represent route state without depending on Unity scene
objects.

## 3. Frontend Methods

Define presenter/view-model methods for:

- route node graph display data,
- three route/path choice display data,
- current node and available path state,
- selected node details,
- compact current army summary,
- stage progress display,
- run gold display,
- possible reward hints,
- expected risk hint text or icons without exact enemy disclosure,
- travel command result.

Frontend methods must show meaningful previews without exposing raw generator
math.

## 4. UI Setup

Prepare UI setup requirements for the Run Map screen:

- central node-route map,
- route layout that clearly reflects the three route choices from Start Run,
- node icons for battle, shop, recruit/reward, and final/boss,
- left or bottom `Your Army` summary without future unit classification unless
  later approved,
- selected node details,
- `Stage Progress`,
- `RUN GOLD`,
- selected battle/reward node details with `Possible Rewards` or reward hints,
- `Expected Risk` as an uncertain danger hint, not exact enemy knowledge,
- Travel and Back commands.

Actual Unity UI asset or scene edits require explicit permission.

## Mockup Workflow

If a mockup is requested for this task, use
`_codex/skills/make-ui-mockup/SKILL.md`. The accepted output is a Unity UGUI
prefab/prototype with visible components, repeated prefab templates, `Script_*`
owners, and serialized field wiring. Browser/prototype-page mockups do not
satisfy this task's mockup requirement.

## Acceptance Criteria

Done when:

- the run can present a node-route map after Start Run,
- the route map reflects the three route choices from Start Run,
- available next nodes can be selected and validated,
- route progress can be persisted in SQLite-ready data,
- stage progress and run gold can be displayed,
- node previews can communicate possible reward hints at a high level,
- expected risk can be displayed as partial information without revealing exact
  enemy contents,
- V1 battle army preset/generated rules are captured as a `/grill-me` decision,
- final/boss access can be gated by route progress,
- Offline Mode can persist and restore route progress without backend services,
- the route/travel command result can later be handled by an Online adapter
  without changing route UI rules,
- unit classification and `Run Essence` are not implemented in this task,
- no spatial overworld, new battle gameplay, encounter balance, or backend SDK
  is introduced.

## Implementation - 2026-06-15

### What Changed

- `RunMapModels`, `RunMapContracts`, `DefaultRunMapPathCatalog`,
  `RunMapService`, `OfflineRunMapAdapter`: added Offline Run Map route state,
  three first-scope route paths, node states, possible reward hints, uncertain
  expected-risk hints, stage progress, RUN GOLD display data, travel
  validation, and final/boss gating. No Inspector fields changed.
- `PRD19_021_RunMapMockupController`: added 021-specific prototype behavior
  for the Run Map prefab. Historical Unity Editor-side prefab generation for
  `Assets/Resources/UI/PRD_19/021_RunMap/` has been removed and must not be
  recreated without current path-specific user permission.
- Removed fields: none. Existing public/serialized Unity fields were not
  renamed.

### Automatic Test

- Added `TArenaUnity3D/Assets/Scripts/Tests/EditMode/RunMapServiceTests.cs`.
- Tests check: three route choices plus final gate, travel persistence/progress,
  and rejection of locked final travel.
- Run manually in Unity: `Window > General > Test Runner > EditMode`, select
  `RunMapServiceTests`, click Run. Expected result: 3 passing tests.
- I did not run Unity tests automatically; user runs them inside Unity.
- Ran a local `py -3` brace-balance scan for RunMetagame/EditMode C# files.

### Unity Test

#### Unity Setup

- Let Unity import scripts under `Assets/Scripts/RunMetagame/021_RunMap/`.
- Do not run or recreate PRD019 mockup builders. Inspect existing prefabs only
  unless the current task grants path-specific permission.
- Open `Assets/Resources/UI/PRD_19/021_RunMap/PRD_19_021_RunMap.prefab`.

#### Play Mode Test

- No scene route flow is wired in this task.
- Inspect the prefab for `Script_PRD_19_021_RunMapMockupController`, route
  nodes/edges, selected node details, `Stage Progress`, `RUN GOLD`, Travel,
  View Army, and Back.
- In a future UI hookup, call `OfflineRunMapAdapter.CreateOrLoad(...)` and
  `Travel(...)`.

### QA Verdict

- Final QA verdict: Pass with manual Unity import pending.
- QA report: `_codex/tasks/QA/2026-06-15_0242_021_QA_ArchitectureReview.md`.
- Actionable findings: none.
- Non-blocking observations: path catalog is intentionally tiny and local;
  production UI should bind directly to `OfflineRunMapAdapter`.

### Notes

- This task does not add a spatial overworld, backend SDK, PlayFab, PUN,
  Photon, encounter balance, battle gameplay, scenes, materials, controllers,
  `.inputactions`, `.asmdef`, or `.asmref`.
- Backend gaps: durable route persistence and future Online route validation:
  tutaj powinno byc z bazy danych.

### Next Steps

- Run `RunMapServiceTests` in Unity Test Runner EditMode.
- Let Unity generate/open the Run Map mockup prefab and inspect its hierarchy.

## Prototype UI Fix - 2026-06-15

- Historical PRD019 mockup builders were removed and must not be recreated
  without current path-specific user permission.
- Added `PRD19_021_RunMapMockupController` as the task-specific UI owner.
  It creates sample run data through `OfflineRunMapAdapter`, renders
  `RunMapScreenViewData`, focuses route nodes, and sends Travel through
  `RunMapService.Travel`.
- Back now opens a route-choice summary state, View Army opens an army summary
  state, route nodes focus real node view data, and Travel is enabled only for
  currently travelable nodes.
- Removed stale generated prefab output from the old shared builder. Do not
  regenerate `Assets/Resources/UI/PRD_19/021_RunMap/PRD_19_021_RunMap.prefab`
  or nested prefabs unless the current task grants path-specific permission.
- Remaining unresolved production gap: durable database/backend persistence for
  route state; current prototype uses `InMemoryRunMapStore`.
