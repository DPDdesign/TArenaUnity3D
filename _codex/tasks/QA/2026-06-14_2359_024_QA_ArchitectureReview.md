# [TARENA] QA Architecture Review: PRD019 Run Shop

- Task: `_codex/tasks/archive/024_PRD019_RunShop.md`
- Protocol: `_codex/tasks/QA/2026-06-14_2358_024_CodingCompletion_RunShop.md`
- Date: 2026-06-14
- Reviewer: QA Architecture Review Agent
- Verdict: Pass

## Findings

No blocking architecture findings.

## Review Notes

- `RunShopService` owns offer generation, affordability validation, target
  legality, preview building, purchase application, and purchased-offer state.
  `RunShopScreenController`, `RunShopOfferCardView`, and `RunShopStackRowView`
  remain thin UI bridges and do not duplicate resolver rules.
- Preview and purchase both route through `PreviewOffer(...)` and
  `ApplyOperation(...)`, which reduces preview-vs-actual drift. Focused offer
  previews are built from cloned army snapshots.
- Purchased offer state is tracked at the visit-store boundary through
  `IRunShopVisitStore.HasPurchasedOffer(...)` and re-applied when a visit is
  rebuilt with the same `VisitId`.
- Offline/future Online separation is explicit through mode and authority
  fields. No PlayFab, PUN, Photon, or legacy profile/shop dependency was added.
- The implementation stays in the user-requested script location:
  `TArenaUnity3D/Assets/Scripts/RunMetagame/RunShop/`.
- The main prefab and template prefabs are in the user-requested locations:
  `TArenaUnity3D/Assets/Resources/UI/PRD_19_21.prefab` and
  `TArenaUnity3D/Assets/Resources/UI/PRD19_21/`.
- UI prefab YAML validation passed for all five prefabs with 0 warnings and 0
  errors.
- The HTML/JS `Task 21 mockup` is separate from Task 20 and keeps the Run Shop
  preview flow visible in the existing prototype menu.

## Non-Blocking Observations

- The task still has a `/grill-me` gate. The current offer amounts, costs,
  upgrade ratios, and exact V1 shop content should remain provisional until the
  product decision is made.
- The Unity prefabs are structural mockup assets rather than fully wired final
  prefabs. This is intentional because the new C# scripts do not have Unity
  `.meta` GUIDs until Unity imports them; guessing GUIDs by hand would create
  higher YAML risk. After import, the user should add the new script components
  and wire serialized fields in the Inspector.
- The in-memory visit store is correct for Offline prototype behavior, but a
  later task should replace it with a local persistence adapter without moving
  purchase rules into UI.

## Required Follow-Up

None for this task before tests.

## Suggested Manual Unity Checks

- Let Unity import the new RunShop scripts and prefab assets.
- Open `Resources/UI/PRD_19_21.prefab` and confirm the hierarchy imports.
- Add `RunShopScreenController`, `RunShopOfferCardView`, and
  `RunShopStackRowView` to the named `Script_*` GameObjects or final styled
  replacements.
- Wire actual `Text`, `Image`, `Button`, and state GameObjects in the Inspector.
- Enter Play Mode in a test scene with the prefab and verify focus, preview,
  Buy, purchased state, and Leave Shop message behavior.

## Final Prefab Wiring Addendum - 2026-06-15

The manual add-script and wire-in-Inspector steps above are superseded for
`PRD_19_21.prefab`. The prefab was rebuilt with real controller/view scripts,
serialized TMP/Button/Image references, and nested offer-card / army-row
subprefab instances.

Use `_codex/tasks/RunMetaGame_Tests/024_PRD019_RunShop_ManualIntegrationTest.md`
as the current manual Unity validation and integration checklist.
