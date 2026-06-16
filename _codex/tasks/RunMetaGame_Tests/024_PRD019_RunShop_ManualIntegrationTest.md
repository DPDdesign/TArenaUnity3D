# [TARENA] PRD019 Run Shop - Manual Integration Test

- Status: ready-for-manual-test
- Type: Manual QA / Integration PRD
- Area: Run Metagame, Run Shop, UI Prefab Wiring
- Source task: `_codex/tasks/024_PRD019_RunShop.md`
- Unity prefab under test:
  `TArenaUnity3D/Assets/Resources/UI/PRD_19/024_RunShop/PRD_19_024_RunShop.prefab`
- Subprefabs under test:
  `TArenaUnity3D/Assets/Resources/UI/PRD_19/024_RunShop/Prefabs/`

## Goal

Verify that PRD019 Run Shop is a working offline run-shop slice and that the UI
prefab is wired with real components, buttons, TMP text fields, and nested
subprefabs. This document also explains how to connect the slice later to the
rest of Run Metagame.

## Scope

This test covers:

- offline Run Shop offer generation, preview, affordability, purchase, and
  purchased-offer locking;
- `PRD_19_024_RunShop.prefab` as the main screen prefab in the task folder;
- concrete subprefab usage for offer cards and army rows;
- manual Unity checks after import.

This test does not cover:

- final shop balance;
- backend or online authority;
- final database persistence;
- legacy non-run shop/profile economy;
- full route transition implementation after pressing Leave.

## Assets And Scripts

Scripts:

- `Assets/Scripts/RunMetagame/024_RunShop/RunShopModels.cs`
- `Assets/Scripts/RunMetagame/024_RunShop/RunShopContracts.cs`
- `Assets/Scripts/RunMetagame/024_RunShop/RunShopService.cs`
- `Assets/Scripts/RunMetagame/024_RunShop/DataMapperRunShopUnitSource.cs`
- `Assets/Scripts/RunMetagame/024_RunShop/OfflineRunShopAdapter.cs`
- `Assets/Scripts/RunMetagame/024_RunShop/RunShopScreenController.cs`
- `Assets/Scripts/RunMetagame/024_RunShop/RunShopOfferCardView.cs`
- `Assets/Scripts/RunMetagame/024_RunShop/RunShopStackRowView.cs`

Prefabs:

- `Assets/Resources/UI/PRD_19/024_RunShop/PRD_19_024_RunShop.prefab`
- `Assets/Resources/UI/PRD_19/024_RunShop/Prefabs/PRD19_024_OfferCard.prefab`
- `Assets/Resources/UI/PRD_19/024_RunShop/Prefabs/PRD19_024_ArmyPreview_StackRow.prefab`
- `Assets/Resources/UI/PRD_19/024_RunShop/Prefabs/PRD19_024_CommandButton.prefab`
- `Assets/Resources/UI/PRD_19/024_RunShop/Prefabs/PRD19_024_WalletSummary.prefab`

Tests:

- `Assets/Scripts/Tests/EditMode/RunShopServiceTests.cs`

Editor rebuild tool:

- `Assets/Scripts/RunMetagame/024_RunShop/Editor/PRD19_024_RunShopPrefabBuilder.cs`
- Unity menu: `TArena > Mockups > Rebuild PRD 19 024 Run Shop Prefabs`

## Required Prefab Wiring

Open `Assets/Resources/UI/PRD_19/024_RunShop/PRD_19_024_RunShop.prefab` in
Prefab Mode.

The main prefab must contain:

- `Script_RunShopScreenController` with `RunShopScreenController`;
- `offerCards` array length 6;
- `currentArmyRows` array length 4;
- `previewArmyRows` array length 4;
- non-null TMP references for wallet, selected offer title, selected offer
  preview, and result message;
- non-null `buyButton` and `leaveButton`.

The main prefab must use concrete nested prefab instances:

- 6 offer card instances sourced from
  `Prefabs/PRD19_024_OfferCard.prefab`;
- 4 current-army row instances sourced from
  `Prefabs/PRD19_024_ArmyPreview_StackRow.prefab`;
- 4 preview-army row instances sourced from
  `Prefabs/PRD19_024_ArmyPreview_StackRow.prefab`.

Each offer card subprefab instance must have:

- `RunShopOfferCardView`;
- a `Button`;
- title, category, cost, description, preview, disabled-reason TMP fields;
- selected, disabled, and purchased state objects assigned.

Each army row subprefab instance must have:

- `RunShopStackRowView`;
- unit icon `Image`;
- name, tier, amount, value, and skills TMP fields.

If a prefab opens with empty GameObjects, missing scripts, missing text fields,
or unpacked row/card objects, run:

`TArena > Mockups > Rebuild PRD 19 024 Run Shop Prefabs`

Then reopen `PRD_19_024_RunShop.prefab` and repeat this section.

## Required Test Scene Setup

Use any temporary UI test scene or existing Run Metagame test scene.

Scene requirements:

- one `Canvas`;
- one `EventSystem`;
- `GraphicRaycaster` on the Canvas;
- `PRD_19_024_RunShop.prefab` instantiated under the Canvas;
- Canvas Scaler set to a UI reference size close to 1600 x 900 or another
  project-standard scalable UI setup.

For full icon validation, the scene should include the existing project
`DataMapper` setup used by other UI. If `DataMapper.Instance` is not present,
the Run Shop should still show text and buttons, but unit icons may be empty.

## Manual Test Cases

### 1. Import And Compile

Steps:

- Let Unity finish script import.
- Confirm the Console has no compile errors for `RunShop*` scripts.
- Open `PRD_19_024_RunShop.prefab`.

Pass:

- prefab opens without missing script warnings;
- controller, offer cards, and row views show real serialized references.

### 2. Nested Prefab Regression

Steps:

- In Project view, open `PRD19_024_OfferCard.prefab`.
- Change a harmless visual property, for example a label color.
- Save, reopen `PRD_19_024_RunShop.prefab`.

Pass:

- all six offer cards in the main prefab inherit the subprefab change unless
  intentionally overridden.

Repeat the same with `PRD19_024_ArmyPreview_StackRow.prefab`.

Pass:

- all current and preview army rows inherit the row subprefab change.

### 3. First Play Mode Render

Steps:

- Press Play in the test scene.

Pass:

- wallet shows `120 RUN GOLD`;
- six offer cards are visible;
- current army shows the mock starting army:
  `Rusher`, `Thrower`, `Healer`, `Wisp`;
- selected-offer preview panel is populated;
- result message starts as a neutral preview/ready message.

### 4. Offer Focus And Preview

Steps:

- Click each offer card once.

Pass:

- selected state moves to the clicked card;
- selected-offer title changes;
- preview description changes;
- army-after-purchase rows update before buying;
- Buy button state matches affordability and purchase availability.

### 5. Successful Purchase

Steps:

- Select an affordable offer.
- Click Buy.

Pass:

- wallet changes by the offer cost or reward value;
- current army updates to match the previewed result;
- result message reports purchase success;
- the bought offer becomes purchased/unavailable for the same visit;
- clicking the same offer again does not apply a duplicate purchase.

### 6. Insufficient Currency

Steps:

- Stop Play Mode.
- On `RunShopScreenController`, set `startingRunCurrency` to a low value such
  as 0 or 10.
- Press Play.
- Select an expensive offer.

Pass:

- Buy is disabled or purchase fails cleanly with insufficient currency;
- wallet does not go below zero;
- current army is not mutated by a failed purchase.

### 7. Leave Shop Placeholder

Steps:

- Press Leave.

Pass:

- current implementation shows the leave-shop placeholder message;
- no exception is thrown.

Future integration must replace this placeholder with the real route transition.

### 8. EditMode Service Tests

Steps:

- Open `Window > General > Test Runner`.
- Choose EditMode.
- Run `RunShopServiceTests`.

Pass:

- all 3 tests pass.

## Integration Contract

The current `RunShopScreenController` is wired for an offline mock screen. It
builds a mock army and mock run-shop visit so the prefab can be tested without
the full Run Metagame route state.

When connecting this to the real Run Metagame, replace the mock entry data with
an explicit input object from the run state.

Minimum data needed by Run Shop:

- `runId`;
- `routeNodeId`;
- current run currency;
- current army stack snapshot;
- already purchased offers for this shop visit, if the visit is being reopened;
- allowed offer categories for this shop node;
- optional shop seed for deterministic offer generation.

Recommended connections:

- Run Map / route resolver opens `PRD_19_024_RunShop` when the selected node type is
  Shop.
- Battle Result passes post-battle army state and currency into the next route
  step.
- Reward Map applies its reward first, then Run Shop receives the updated run
  state.
- Leave Shop emits a route-complete event and returns to Run Map or advances to
  the next route node.
- The offline adapter remains local-authoritative only.
- Future online mode must validate offers, target selection, currency spend,
  and army mutation on the backend.

Persistence still needs a real run-state store. Durable shop visit state,
currency spend, purchased offers, and army mutations should be saved through
the future database-backed run state layer: tutaj powinno byc z bazy danych.

## What To Plug In Later

Replace or extend the current mock controller entry points with:

- a method that accepts a real Run Shop visit request;
- a run-state provider for current currency and army stacks;
- a route transition callback for Leave;
- a persistence adapter for purchased offers and resolved purchases;
- a backend adapter for online mode.

The UI views should stay thin. Business rules should remain in
`RunShopService` or a future domain service, not inside button handlers.

## Pass / Fail Criteria

Pass when:

- main prefab uses nested offer-card and army-row subprefabs;
- all required scripts and serialized references are assigned;
- Play Mode offer focus, preview, purchase, insufficient-currency, and leave
  flows work without exceptions;
- `RunShopServiceTests` pass in EditMode;
- no legacy non-run shop/profile economy is mutated.

Fail when:

- the prefab contains empty placeholder GameObjects instead of wired views;
- offer cards or rows are not prefab instances;
- controller arrays or button/TMP references are null;
- Buy applies a result different from the preview;
- a purchased offer can be bought twice in one visit;
- insufficient currency mutates wallet or army;
- Leave cannot be replaced by a real route transition later.
