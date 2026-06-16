# [TARENA] PRD019 Start Run - Manual Integration Test

- Status: ready-for-manual-test
- Type: Manual QA / Integration PRD
- Area: Run Metagame, Start Run, Starting Armies, Route Preview
- Source task: `_codex/tasks/020_PRD019_StartRun.md`
- Unity prefab under test:
  `TArenaUnity3D/Assets/Resources/UI/PRD_19/020_StartRun/PRD_19_020_StartRun.prefab`
- Subprefabs under test:
  `TArenaUnity3D/Assets/Resources/UI/PRD_19/020_StartRun/Prefabs/`

## Goal

Verify that PRD019 Start Run can present legal weaker starting armies, preview a
route choice, and create an offline run record from the selected starting army
and route. This document also explains what must be wired in Unity before the
screen is treated as usable in Play Mode.

## Scope

This test covers:

- offline Start Run screen data;
- selection of one starting army variant;
- route preview selection;
- Begin Run command result;
- Unity prefab wiring for `PRD_19_020_StartRun`;
- separation from saved armies, offence, defence, and in-use states.

This test does not cover:

- final starting-army balance;
- online/backend validation;
- durable database persistence;
- Run Map generation after the route is chosen;
- legacy saved-army management.

## Assets And Scripts

Scripts:

- `Assets/Scripts/RunMetagame/020_StartRun/StartRunModels.cs`
- `Assets/Scripts/RunMetagame/020_StartRun/StartRunContracts.cs`
- `Assets/Scripts/RunMetagame/020_StartRun/DefaultStartRunCatalog.cs`
- `Assets/Scripts/RunMetagame/020_StartRun/DataMapperStartRunUnitSource.cs`
- `Assets/Scripts/RunMetagame/020_StartRun/OfflineStartRunAdapter.cs`
- `Assets/Scripts/RunMetagame/020_StartRun/StartRunService.cs`
- `Assets/Scripts/RunMetagame/020_StartRun/StartRunScreenController.cs`
- `Assets/Scripts/RunMetagame/020_StartRun/StartRunArmyCardView.cs`
- `Assets/Scripts/RunMetagame/020_StartRun/StartRunRouteOptionView.cs`
- `Assets/Scripts/RunMetagame/020_StartRun/StartRunStackRowView.cs`
- `Assets/Scripts/RunMetagame/020_StartRun/StartRunUiSpriteResolver.cs`

Prefabs:

- `Assets/Resources/UI/PRD_19/020_StartRun/PRD_19_020_StartRun.prefab`
- `Assets/Resources/UI/PRD_19/020_StartRun/Prefabs/PRD19_020_ArmyCard.prefab`
- `Assets/Resources/UI/PRD_19/020_StartRun/Prefabs/PRD19_020_ArmyCard_UnitFrame.prefab`
- `Assets/Resources/UI/PRD_19/020_StartRun/Prefabs/PRD19_020_ArmyPreview_StackRow.prefab`
- `Assets/Resources/UI/PRD_19/020_StartRun/Prefabs/PRD19_020_RouteOption.prefab`
- `Assets/Resources/UI/PRD_19/020_StartRun/Prefabs/PRD19_020_RouteNode.prefab`
- `Assets/Resources/UI/PRD_19/020_StartRun/Prefabs/PRD19_020_RouteEdge.prefab`

Tests:

- `Assets/Scripts/Tests/EditMode/StartRunServiceTests.cs`

## Required Prefab Wiring

Open `Assets/Resources/UI/PRD_19/020_StartRun/PRD_19_020_StartRun.prefab`
in Prefab Mode.

The root must contain `StartRunScreenController` with:

- `defaultAccountPlayerId` set to a stable local player id, for example
  `offline-player`;
- `armyCards` array length 3;
- `routeOptions` array length 3;
- `stackRows` array length 4;
- non-null `leaderIcon`;
- non-null `armyPreviewName`;
- non-null `armyPreviewValue`;
- non-null `routeSummaryText`;
- non-null `runtimeMessageText`;
- non-null `backButton`;
- non-null `beginButton`.

Important current wiring check:

- if `backButton` or `beginButton` shows `None` in the Inspector, assign
  `Button_Back` and `Button_BeginRun` before Play Mode testing.

The main prefab must use concrete nested prefab instances:

- 3 army cards sourced from `Prefabs/PRD19_020_ArmyCard.prefab`;
- 4 selected-army rows sourced from
  `Prefabs/PRD19_020_ArmyPreview_StackRow.prefab`;
- 3 route options sourced from
  `Prefabs/PRD19_020_RouteOption.prefab`.

Each army card subprefab instance must have `StartRunArmyCardView` with:

- `Button`;
- background `Image`;
- name, value, and status `Text`;
- 4 unit icon `Image` references.

Each route option subprefab instance must have `StartRunRouteOptionView` with:

- `Button`;
- background `Image`;
- label `Text`.

Each stack row subprefab instance must have `StartRunStackRowView` with:

- unit icon `Image`;
- tier, name, role, skill pips, count, and value `Text` fields.

This screen currently uses `UnityEngine.UI.Text`, not TextMeshPro. Do not wire
TMP fields into these view scripts unless the scripts are intentionally migrated.

## Required Test Scene Setup

Use a temporary UI test scene or the current Run Metagame test scene.

Scene requirements:

- one `Canvas`;
- one `EventSystem`;
- `GraphicRaycaster` on the Canvas;
- `PRD_19_020_StartRun.prefab` instantiated under the Canvas;
- Canvas Scaler set to a UI reference size close to 1600 x 900 or another
  project-standard scalable UI setup.

For full icon validation, the scene should include the existing project
`DataMapper` setup used by unit UI. If `DataMapper.Instance` is missing, the
screen should still bind text and buttons, but unit icons may be empty.

## Manual Test Cases

### 1. Import And Compile

Steps:

- Let Unity finish script import.
- Confirm the Console has no compile errors for `StartRun*` scripts.
- Open `PRD_19_020_StartRun.prefab`.

Pass:

- prefab opens without missing script warnings;
- root controller and child view scripts are present;
- required serialized references are assigned, including Back and Begin.

### 2. Nested Prefab Regression

Steps:

- Open `PRD19_020_ArmyCard.prefab`.
- Change a harmless visual property, for example a background color.
- Save, then reopen `PRD_19_020_StartRun.prefab`.

Pass:

- all three army cards in the main prefab inherit the subprefab change unless
  intentionally overridden.

Repeat for:

- `PRD19_020_ArmyPreview_StackRow.prefab`;
- `PRD19_020_RouteOption.prefab`.

Pass:

- all four stack rows and all three route options inherit their subprefab
  changes.

### 3. First Play Mode Render

Steps:

- Press Play in the test scene.

Pass:

- three starting armies are visible:
  `Barbarian Starter`, `Lizard Breakout`, `Stone Spark`;
- three route previews are visible:
  `Iron Line`, `Relic Trail`, `Risk Road`;
- selected army panel shows unit rows with tier, level, amount, skill pips, and
  stack value;
- route summary shows route description, recommended value, and current army
  value;
- runtime message shows a validation/success-ready state;
- no defence, offence, or in-use badges are shown.

### 4. Starting Army Selection

Steps:

- Click each starting army card.

Pass:

- selected visual state moves to the clicked card;
- selected army name and total value update;
- stack rows update to that army's units and amounts;
- per-unit skills appear as locked/unlocked pips or labels;
- no saved-army, defence, offence, or in-use state appears.

### 5. Route Selection

Steps:

- Click each route option.

Pass:

- selected visual state moves to the clicked route;
- route summary text updates;
- recommended army value changes to the selected route;
- current army value remains based on the selected starting army.

### 6. Begin Run

Steps:

- Select a valid starting army.
- Select a valid route.
- Click Begin.

Pass:

- Begin is interactable only when both selections are valid;
- runtime message reports successful run creation;
- created run id is shown in the result message;
- no exception appears in the Console.

### 7. Back

Steps:

- Click Back.

Pass:

- current implementation hides the Start Run prefab root;
- no exception appears in the Console.

Future integration should replace or wrap this with a real navigation event.

### 8. EditMode Service Tests

Steps:

- Open `Window > General > Test Runner`.
- Choose EditMode.
- Run `StartRunServiceTests`.

Pass:

- all 3 tests pass.

## Integration Contract

The current `StartRunScreenController` uses `OfflineStartRunAdapter` and local
catalog data so the screen can run without backend services.

Minimum data produced by Start Run for the next Run Metagame step:

- `runId`;
- game mode `Offline`;
- authority source `LocalOfflineAdapter`;
- account/player id;
- selected starting army template id;
- selected starting army variant id;
- selected starting army id;
- selected route id;
- initial run currency;
- initial army stack snapshot;
- created run status.

Recommended connections:

- Start Run opens first in the Run Metagame flow.
- Begin Run creates the run record and passes `runId`, selected route id, and
  initial army snapshot to Run Map.
- Run Map uses the selected route id to build or select the three-path route
  structure.
- Battle Result and Reward Map later mutate the same current-run state.
- Run Shop receives the current army and currency from the run state created
  here, not from saved armies.
- Summary Value consumes the final run state and should be able to trace back
  to the starting snapshot from this step.

Persistence still needs a real local run-state store. The current in-memory
record store is acceptable for this slice only. A future SQLite/database-backed
adapter should save run records and initial army snapshots without moving rules
into UI: tutaj powinno byc z bazy danych.

Future Online Mode must use a separate backend adapter to list legal starting
armies, validate the selected route/army pair, and create the authoritative run.
Do not mix PlayFab/PUN/Photon or saved-army profile state into this offline
Start Run UI.

## What To Plug In Later

Replace or extend the current mock/offline screen with:

- a real navigation callback for Back;
- a Begin Run callback that opens Run Map with the created `runId`;
- a persistent `IStartRunRecordStore` implementation;
- authored starting-army data or database-backed starting-army templates;
- route preview data from the Run Map generation layer once that exists;
- an online adapter that shares the command/result shape but validates on the
  backend.

The UI views should stay thin. Start Run rules should remain in
`StartRunService` or future domain services, not inside button handlers.

## Pass / Fail Criteria

Pass when:

- main prefab uses nested army-card, stack-row, and route-option subprefabs;
- all required scripts and serialized references are assigned;
- Back and Begin are wired to real `Button` components;
- Play Mode selection, route preview, Begin, and Back flows work without
  exceptions;
- `StartRunServiceTests` pass in EditMode;
- no saved-army, offence, defence, in-use, PlayerPrefs, PlayFab, PUN, Photon,
  or legacy shop/profile state is mutated.

Fail when:

- `backButton` or `beginButton` remains unassigned;
- child view scripts or UI fields are missing;
- starting armies are presented as saved armies;
- defence/offence/in-use badges appear on the Start Run screen;
- Begin creates a run without a valid starting army and route;
- route selection does not change the selected route summary;
- created run data cannot be handed to Run Map.
