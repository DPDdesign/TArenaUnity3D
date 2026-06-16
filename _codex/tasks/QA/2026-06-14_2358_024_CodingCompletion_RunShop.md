# [TARENA] Coding Completion: PRD019 Run Shop

- Task: `_codex/tasks/archive/024_PRD019_RunShop.md`
- Date: 2026-06-14
- Agent: Coding Agent
- Scope: Offline Mode Run Shop domain slice plus UI mockup assets.

## Files Changed

- `TArenaUnity3D/Assets/Scripts/RunMetagame/RunShop/RunShopModels.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/RunShop/RunShopContracts.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/RunShop/RunShopService.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/RunShop/DataMapperRunShopUnitSource.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/RunShop/OfflineRunShopAdapter.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/RunShop/RunShopScreenController.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/RunShop/RunShopOfferCardView.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/RunShop/RunShopStackRowView.cs`
- `TArenaUnity3D/Assets/Resources/UI/PRD_19_21.prefab`
- `TArenaUnity3D/Assets/Resources/UI/PRD19_21/PRD19_21_OfferCard.prefab`
- `TArenaUnity3D/Assets/Resources/UI/PRD19_21/PRD19_21_ArmyPreview_StackRow.prefab`
- `TArenaUnity3D/Assets/Resources/UI/PRD19_21/PRD19_21_CommandButton.prefab`
- `TArenaUnity3D/Assets/Resources/UI/PRD19_21/PRD19_21_WalletSummary.prefab`
- `_codex/Gen_Im/RETSOT ONLINE/src/game/renderer.js`
- `_codex/Gen_Im/RETSOT ONLINE/src/game/ui.js`

## What Changed

- Added a new `RunMetagame/RunShop` C# folder for PRD019 Run Shop.
- Added Offline Mode run-shop DTOs for visit, offer, operation, preview,
  purchase command/result, army snapshots, stack snapshots, skill display state,
  mode, authority, category, operation type, and deterministic errors.
- Added `RunShopService` that:
  - builds limited shop offers from current run army and RUN GOLD,
  - supports recovery/reinforcement, skill, stack, upgrade/exchange, and economy
    offer categories,
  - previews Army After Purchase before buy,
  - validates affordability and legal targets,
  - applies one selected purchase,
  - stores purchased offer state per shop visit,
  - keeps Offline authority explicit through `LocalOfflineAdapter`.
- Added `OfflineRunShopAdapter` and `DataMapperRunShopUnitSource`.
- Added thin UGUI view/controller scripts for future Unity wiring:
  `RunShopScreenController`, `RunShopOfferCardView`, and
  `RunShopStackRowView`.
- Added main Unity UI prefab skeleton:
  `TArenaUnity3D/Assets/Resources/UI/PRD_19_21.prefab`.
- Added smaller template prefabs under:
  `TArenaUnity3D/Assets/Resources/UI/PRD19_21/`.
- Added a separate `Task 21 mockup` page to the HTML/JS PRD019 prototype menu.

## Scope Boundaries

- Did not edit legacy `Assets/Shop.cs`, profile UI, PlayFab, PUN, Photon, or
  current non-run economy.
- Did not change gameplay float values, unit stats, skill effects, battle
  behavior, scenes, materials, controllers, `.inputactions`, `.asmdef`, or
  `.asmref`.
- The exact shop balance and offer catalog remain provisional because the task
  still has a `/grill-me` gate.
- Prefab YAML intentionally does not include manually guessed MonoScript GUIDs
  for the new scripts. Unity import must create script `.meta` files before
  final script-component wiring.

## Verification Already Run

- Ran UI prefab YAML validation:
  `py _codex/skills/make-ui-mockup/scripts/validate_ui_prefab.py ...`
- Result: 5 prefabs checked, 0 warnings, 0 errors.
- Ran JavaScript syntax checks:
  - `node --check _codex/Gen_Im/RETSOT ONLINE/src/game/ui.js`
  - `node --check _codex/Gen_Im/RETSOT ONLINE/src/game/renderer.js`
- Result: both passed.

## QA Focus

- Check whether `RunShopService` keeps shop business rules out of UI presenters.
- Check whether preview and purchase paths can drift.
- Check whether purchased-offer state is represented at the right boundary.
- Check whether the provisional offer generation is acceptable under the
  task's `ready-for-grill` status.
- Check whether prefab placement/naming matches the user's explicit path:
  main prefab `Resources/UI/PRD_19_21`, smaller prefabs in
  `Resources/UI/PRD19_21/`.
- Check whether the UI mockup work creates unacceptable manual Unity YAML risk
  or needs a follow-up setup note.

## Known Manual Unity Setup

- After Unity imports the new scripts, add `RunShopScreenController` to
  `Script_RunShopScreenController`.
- Add `RunShopOfferCardView` to `Script_RunShopOfferCardView` in offer-card
  instances.
- Add `RunShopStackRowView` to `Script_RunShopStackRowView` in stack-row
  instances.
- Wire serialized fields on those scripts to actual `Text`, `Image`, `Button`,
  and state GameObjects after adding visual UGUI children or replacing the
  skeletons with styled project art.

## Final Prefab Wiring Addendum - 2026-06-15

The manual add-script and wire-in-Inspector steps above are superseded for
`PRD_19_21.prefab`. The prefab was rebuilt with real controller/view scripts,
serialized TMP/Button/Image references, and nested offer-card / army-row
subprefab instances.

Use `_codex/tasks/RunMetaGame_Tests/024_PRD019_RunShop_ManualIntegrationTest.md`
as the current manual Unity validation and integration checklist.
