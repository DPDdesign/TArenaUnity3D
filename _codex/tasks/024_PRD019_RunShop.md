# [TARENA] PRD019: Run Shop

- Status: implemented-manual-test-pending
- Type: HITL Task
- Area: Run Shop, One-Currency Purchases, Shop Resolver, Shop UI
- Label: ready-for-agent
- Parent: `_codex/tasks/019_PRD_RunMetagameRewardFramework.md`
- Blocked by: `_codex/tasks/archive/021_PRD019_RunMap.md`, `_codex/tasks/023_PRD019_RewardMap.md`

## HITL Gate

Before implementation, run `/grill-me` for this task.

Confirm run-shop offer categories, one run currency rules, purchase limits,
recovery/resurrection limits, and how the run shop stays separate from the
existing metagame shop/profile UI.

Specific questions for `/grill-me`:

- Which shop offer categories are in V1 and which remain future scope?
- How many offers should a shop node show at once?
- Which offer types need target selection before purchase?
- Should offer preview always show the full army-after-purchase snapshot, or
  only the affected stack plus compact army summary?

## What To Build

Create the Run Shop slice: a shop node gives the player a slower breathing-room
decision for repair and army shaping during a run.

The project already has non-run shop/profile code. This task must not silently
reuse that economy as the run-shop economy. The run shop is a separate in-run
system using one main run currency.

## Mode Architecture - 2026-06-14

Current implementation target:

- Implement this task for `Offline Mode` only.
- Offline Mode creates offers, previews purchases, validates affordability, and
  applies purchases locally through a deterministic run-shop adapter.
- The local shop resolver is authoritative only for Offline Mode.

Future online target:

- `Online Mode` must receive or validate shop offers through a backend adapter.
- Online purchase affordability, target legality, currency spending, and army
  mutation must be backend-verified before the client updates authoritative
  state.
- Do not reuse the existing non-run shop/profile economy as Online Mode.
- Do not add real networking, PlayFab, PUN, Photon, cloud sync, or backend calls
  in this task.

Shared seam:

- Shape shop visit and purchase as mode-neutral payloads: shop visit id, offer
  ids, offer category, cost, legal target, focused preview, selected purchase,
  before/after army snapshots, run currency change, and result source.
- Offline and future Online should be separate adapters behind one shop
  interface. UI presenters must not implement purchase rules directly.

## Clarification - 2026-06-14

Feedback from the Run Shop UI mockup:

- `Your Army` preview is good, but it should be the same shared army preview
  component used by Start Run, Reward Map, Summary Value, and Saved Armies where
  possible.
- Run shop content is directionally fine, but exact offer categories and offer
  rules still need `/grill-me` before implementation.
- `Offer Preview` should work like `Preview Army After Reward`: when the player
  focuses/clicks an offer, the UI previews what the army will look like after
  buying that offer.
- The army preview should display unit skills where relevant, especially for
  skill offers or offers that change a unit's available skills.
- Do not let the shop become a full army editor. The shop previews one selected
  offer operation, then lets the player buy it or choose another offer.

## 1. Database

Prepare SQLite-ready persistence boundaries for:

- game mode and shop authority/source,
- run shop visit id,
- run id,
- route node id,
- run currency balance,
- shop offer id,
- offer category: recovery, resurrection, skill, stack, upgrade/exchange,
  economy,
- cost,
- preview data,
- focused/previewed offer id,
- army-after-purchase preview snapshot,
- affected stack preview data,
- unit skill display state for army preview,
- purchase result,
- purchased/remaining offer state.

## 2. Backend Methods

Define methods for:

- selecting the Offline run-shop adapter for current implementation,
- creating or loading offers for a shop node,
- checking affordability,
- validating legal targets,
- building offer preview data,
- building army-after-purchase preview snapshots when an offer is focused,
- exposing unit skill display data for the shared army preview component,
- applying one selected purchase,
- spending one run currency,
- returning result data for success, insufficient currency, invalid target, or
  unavailable offer,
- updating current run army state.

Backend logic must stay deterministic and testable without Unity scene objects.

## 3. Frontend Methods

Define presenter/view-model methods for:

- grouped shop offers,
- shared current army preview component data with wounds/losses and skills,
- focused offer preview state,
- selected offer preview,
- army-after-purchase preview data,
- affordability state,
- purchase result,
- run currency display,
- leave-shop transition.

Frontend methods must consume resolver output and must not implement shop
business rules.

## 4. UI Setup

Prepare UI setup requirements for the run shop screen:

- current army panel,
- shared `Your Army` preview component where possible,
- unit skills shown inside the army preview where relevant,
- grouped limited offers,
- selected offer preview with before/after data,
- click/focus offer behavior that previews `Army After Purchase`,
- run currency wallet,
- Buy and Leave commands,
- disabled/error states.

Actual Unity UI scene/prefab/asset edits require explicit permission.

## Acceptance Criteria

Done when:

- a shop node can present limited in-run offers,
- purchases use one run currency,
- shop offers can heal, resurrect, teach skills, add stacks, or upgrade/exchange
  where confirmed by grill,
- shop content categories and exact offer rules are captured as `/grill-me`
  decisions before implementation,
- the shop uses the same shared army preview component as other run/metagame
  screens where possible,
- focusing/clicking an offer can preview the army after purchase before
  confirmation,
- the army preview can show unit skills where relevant,
- selected purchases mutate current run army exactly as previewed,
- Offline Mode can preview and apply purchases without backend services,
- purchase payloads are explicit enough for future Online backend validation,
- existing non-run shop/profile behavior is not changed,
- frontend data exists for shop offer groups, preview, affordability, and result,
- no real-money economy, backend SDK, or full army optimizer is introduced.

## Implementation - 2026-06-14

### What Changed

- `RunShopModels`, `RunShopService`, `OfflineRunShopAdapter`: added Offline
  Run Shop visit/offer/preview/purchase DTOs, deterministic resolver logic,
  purchased-offer state, and mode/authority payloads. No Inspector fields.
- `RunShopScreenController`: added serialized UI setup fields:
  `runId`, `routeNodeId`, `startingRunCurrency`, `offerCards`,
  `currentArmyRows`, `previewArmyRows`, `walletText`,
  `selectedOfferTitleText`, `selectedOfferPreviewText`, `resultMessageText`,
  `buyButton`, `leaveButton`. These affect only the Run Shop mockup screen.
  `startingRunCurrency` useful range is 0+; lower values show disabled Buy
  states sooner, higher values allow more purchases. Tuning hint: keep this as
  mock/setup data until `/grill-me` confirms V1 shop economy.
- `RunShopOfferCardView` and `RunShopStackRowView`: added serialized references
  for offer cards, stack rows, state overlays, text, icon, and button binding.
  These should point to local child UI objects; missing references skip visuals.
- UI mockup assets now live under
  `Assets/Resources/UI/PRD_19/024_RunShop/`: main prefab
  `PRD_19_024_RunShop.prefab` and template prefabs under `Prefabs/`.
- Historical HTML mockup references are no longer the accepted PRD019 task
  structure.
- Removed fields: none. Existing public/serialized Unity fields were not
  renamed.

### Automatic Test

- Added `TArenaUnity3D/Assets/Scripts/Tests/EditMode/RunShopServiceTests.cs`.
- Tests check: limited offer/category generation with focused preview,
  preview-vs-purchase consistency, purchased-offer state, and insufficient
  currency rejection.
- Run manually in Unity: `Window > General > Test Runner > EditMode`, select
  `RunShopServiceTests`, click Run. Expected result: 3 passing tests.
- I did not run Unity tests automatically; user runs them inside Unity.
- Ran JS syntax checks and UI prefab YAML validation outside Unity; they passed.

### Unity Test

#### Unity Setup

- Let Unity import the new scripts in
  `Assets/Scripts/RunMetagame/024_RunShop/`.
- Open
  `Assets/Resources/UI/PRD_19/024_RunShop/PRD_19_024_RunShop.prefab`.
- Add `RunShopScreenController` to `Script_RunShopScreenController`.
- Add `RunShopOfferCardView` to offer card objects based on
  `Resources/UI/PRD_19/024_RunShop/Prefabs/PRD19_024_OfferCard.prefab`.
- Add `RunShopStackRowView` to stack row objects based on
  `Resources/UI/PRD_19/024_RunShop/Prefabs/PRD19_024_ArmyPreview_StackRow.prefab`.
- Wire `Text`, `Image`, `Button`, state GameObjects, offer card array, current
  army row array, preview row array, Buy button, and Leave button in Inspector.

#### Play Mode Test

- Open a UI test scene containing the wired `PRD_19_024_RunShop` prefab and
  press Play.
- Focus each offer and confirm `Army After Purchase` changes before buying.
- Buy an affordable offer and confirm RUN GOLD decreases, army preview updates,
  and the same offer becomes unavailable for that visit.
- Try an unaffordable offer and confirm Buy is disabled or returns
  insufficient currency.
- Click Leave and confirm the placeholder leave-shop message appears.

### QA Verdict

- Final QA verdict: Pass.
- QA report: `_codex/tasks/QA/2026-06-14_2359_024_QA_ArchitectureReview.md`.
- Actionable findings: none.
- Non-blocking observations: exact shop costs/amounts remain provisional until
  the task's `/grill-me` gate is resolved.
- Follow-up fixes applied before final QA: `Field Resurrection` now uses the
  `Resurrection` category, and shop visits preserve purchased-offer state by
  `VisitId`.
- Post-implementation `/grill-me` decision 2026-06-15: do not include a free
  Economy offer that grants RUN GOLD for no trade-off in V1 Run Shop.
  `shop-sell-salvage` currently exists in code and must be removed or hidden
  before Run Shop is treated as gameplay-approved.
- Manual-QA update 2026-06-15: `PRD_19_024_RunShop.prefab` was rebuilt with real
  `RunShopScreenController`, `RunShopOfferCardView`, `RunShopStackRowView`,
  TMP/Button/Image references, and nested offer-card / army-row subprefab
  instances. Manual integration PRD:
  `_codex/tasks/RunMetaGame_Tests/024_PRD019_RunShop_ManualIntegrationTest.md`.

### Notes

- This does not modify legacy `Assets/Shop.cs`, profile UI, PlayFab, PUN,
  Photon, current non-run economy, battle rules, unit stats, skill effects,
  scenes, materials, controllers, `.inputactions`, `.asmdef`, or `.asmref`.
- Prefabs are functional mockup assets. They were validated as YAML and rebuilt
  with serialized controller/view wiring. Manual Unity Play Mode validation is
  still required.
- Offline Mode is local-authoritative only; future Online Mode still needs a
  backend-validated adapter.
- Backend gaps: Online shop offer loading, purchase validation, and durable
  run-shop persistence: tutaj powinno byc z bazy danych.

### Next Steps

- Run `RunShopServiceTests` in Unity Test Runner EditMode.
- Run manual integration checks from
  `_codex/tasks/RunMetaGame_Tests/024_PRD019_RunShop_ManualIntegrationTest.md`.
- Resolve the task's `/grill-me` decisions before treating costs, amounts, and
  exact offer mix as final balance.

## Follow-up Fix - 2026-06-15

- Removed `shop-sell-salvage` from `RunShopService.BuildOffers()` so V1 Run
  Shop no longer grants free RUN GOLD without a trade-off.
- Updated `RunShopServiceTests` to expect no Economy offer in the current V1
  shop offer set.
- Economy remains a PRD19 reward family and future shop category, but it needs
  a real trade-off before returning to Run Shop.
