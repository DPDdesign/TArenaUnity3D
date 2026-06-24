# [TARENA] PRD 036: Start Run Slot Availability

- Status: ready-for-agent
- Type: PRD
- Area: Run Metagame, Start Run, Generated Starting Armies, Offline DB, UI
- Label: ready-for-agent
- Related:
  - `_codex/tasks/archive/020_PRD019_StartRun.md`
  - `_codex/tasks/030_DB-001_OfflineModeDatabasePersistence.md`
  - `_codex/tasks/035_PRD_RandomStartingArmiesRoutes.md`
  - `_codex/Documentation/PRD030_OfflineDatabase_Map.md`
  - `_codex/Documentation/ADR_008_PRD035_UncertainGeneratorDecisions.md`

## Problem Statement

The Start Run screen needs to show generated starting army slots even when some
slots are not available to the current player yet.

Today the Start Run flow can generate and display starting army options, but it
does not have a clear player-progress availability model per visual slot. That
creates several product problems:

- a new player should see that future starting army slots exist,
- the UI should explain why a locked slot cannot be clicked,
- unavailable slots still need generated army content underneath them,
- slot availability must come from current account progress and run history,
- locked state must not be stored as duplicated database truth,
- the number of visible slots should follow the Inspector-wired UI, not a
  hardcoded C# offer count.

The feature must preserve the PRD035 direction: starting army offers are
generated from seed/config/unit truth, while the Offline DB provides account
progress and completed-run facts. The database should not become the authored
starting-army catalog for this slice.

## Solution

Add Start Run slot availability as a calculated screen-state layer.

When the Start Run screen opens or regenerates its slots, it requests as many
generated starting army offers as there are army-card slots wired in the Unity
Inspector. The generator should produce an army offer for each requested visual
slot, including slots that are currently locked.

The screen view model marks each slot as unlocked or locked by evaluating the
current account progress:

- Slot 1 is available.
- Slot 2 requires the player to have won at least one run.
- Slot 3 requires account level 5.
- Slot 4 is unavailable in the demo.
- Slot 5 and later are coming soon by default.

Locked state is always derived at screen generation time. It is not persisted as
`IsLocked`. The database remains the source of account facts: account XP, active
run summaries, and final result records.

The Start Run UI shows the generated army card for every slot, overlays a
locked panel for locked slots, writes the lock reason into a TMP reason label,
and disables the card button so locked cards cannot be clicked or highlighted.
The Begin Run command must also reject locked selected slots so UI disablement
is not the only guard.

Unselected generated army offers are still not persisted in this PRD. PRD035
identifies generated offer history as a future analytics/data-analysis
capability, but this feature should not add offer-history persistence or a
schema migration.

## User Stories

1. As a new player, I want to see the first Start Run slot available, so that I
   can start my first run immediately.
2. As a new player, I want to see future Start Run slots on the screen, so that
   I understand there is progression beyond the first slot.
3. As a player, I want locked slots to show a clear reason, so that I know what
   to do to unlock them.
4. As a player, I want Slot 2 to require winning a run, so that the second
   starting option is tied to proving run completion.
5. As a player, I want Slot 3 to require level 5, so that account progression
   opens more starting options.
6. As a player, I want Slot 4 to explain that it is unavailable in the demo, so
   that demo limitations are explicit instead of feeling broken.
7. As a player, I want slots beyond the current product scope to say Coming
   soon, so that extra visible UI slots do not look like bugs.
8. As a player, I want a locked card to be visible but unclickable, so that I
   can inspect the future option without accidentally selecting it.
9. As a player, I want locked cards to avoid hover/highlight interaction, so
   that locked state feels firm and consistent.
10. As a player, I want every visible slot to have a generated army underneath
    it, so that a later unlock can reveal a real offer instead of a placeholder.
11. As a designer, I want the number of Start Run slots to come from the
    Inspector-wired UI, so that layout changes do not require a C# constant.
12. As a designer, I want Slot 5 and later to default to Coming soon, so that
    future slots can be shown safely before their rules are designed.
13. As a designer, I want lock reasons to be simple text values, so that the
    current UI can localize or restyle them later.
14. As a developer, I want lock state calculated from account facts, so that DB
    progress and UI state cannot drift apart.
15. As a developer, I want Start Run controllers to consume service view data,
    so that UI scripts do not query SQLite directly.
16. As a developer, I want Begin Run validation to reject locked selected slots,
    so that button disablement is not a security or correctness boundary.
17. As a developer, I want generated army count to be request-driven, so that
    `armyCards.Length` can drive the offer count without replacing the
    generator.
18. As a developer, I want current PRD035 generated offer behavior preserved,
    so that selected armies still persist only when a run begins.
19. As a developer, I want account level derived from existing XP rules, so that
    Start Run does not invent a second progression formula.
20. As a QA tester, I want deterministic tests for slot rules, so that win,
    level, demo, and coming-soon cases can be checked without Play Mode.
21. As a future analytics designer, I want the PRD to note unselected generated
    offer history, so that data-analysis persistence can be added later without
    being confused with this lock feature.

## Implementation Decisions

- Slot availability is calculated when the Start Run screen data is built or
  regenerated. It is never written as a persisted `IsLocked` field.
- The Start Run UI requests the number of generated offers from the number of
  Inspector-wired army-card views.
- The generator should be able to produce an offer for every requested visual
  slot, regardless of whether that slot is currently selectable.
- The generated-offer source remains generator-backed, using current unit truth
  and generator config. The Offline DB does not become a starting-army catalog.
- The screen model needs per-slot availability fields: visual slot index,
  locked state, lock reason, and selectable/can-start state.
- Slot numbering is visual and 1-based for product rules.
- Slot 1 has no progression lock.
- Slot 2 is locked with reason `Win A Run` until the current account has at
  least one active run summary whose final result is won.
- Slot 3 is locked with reason `Reach Level 5` until the current account reaches
  level 5.
- Account level is derived from existing account XP progression. A new account
  starts at level 1, and level 5 begins at 1000 XP under the current
  250-XP-per-level model.
- Slot 4 is locked with reason `Unavailable in DEMO` for the current demo
  product state.
- Slot 5 and later are locked with reason `Coming soon`.
- The reason text must be bound to a TMP text field, not legacy Unity text.
- The locked overlay is a serialized GameObject on the Start Run army-card
  view.
- Locked cards should disable their button component interaction and
  interactable state so click and highlight behavior are both blocked.
- UI controllers must not query SQLite directly. Any DB-backed progress facts
  should be exposed through the Start Run service/store/composition boundary.
- The Begin Run command must validate the selected slot availability and return
  a blocked-run-start result for locked selections.
- This PRD does not add generated-offer analytics persistence. That remains a
  future PRD035 offer-history/data-analysis feature with an explicit schema
  plan.
- This PRD does not rename public or serialized fields that already exist.
- This PRD does not edit prefabs, scenes, Unity assets, generated files, or
  gameplay balance values without explicit permission.

## Testing Decisions

- Tests should verify externally visible behavior: generated offer count,
  slot lock state, lock reasons, selection blocking, and begin-run rejection.
- Service-level tests should cover the slot-rule matrix without Unity scene or
  prefab setup.
- DB-backed tests should verify the facts used by slot rules:
  account XP for level calculation and active won run summaries for Slot 2.
- UI view tests should stay limited to bind behavior that can be checked without
  scene assets where practical. Full prefab wiring remains manual Unity QA.
- Prior art for tests:
  - Start Run service tests for screen view-data and begin-run validation.
  - Offline Start Run / Run Map DB tests for DB-backed run persistence.
  - Offline Summary / Saved Armies DB tests for run-summary persistence.
  - Battle Result tests for account XP progression and account DB updates.
- Unity compilation and EditMode tests are run manually by the user in the Unity
  Test Runner unless a future prompt explicitly allows command-line Unity test
  execution.

## Out of Scope

- No generated-offer analytics table or offer-history persistence.
- No schema migration solely for `IsLocked`.
- No migration of starting army definitions into the Offline DB.
- No online backend, PlayFab, Photon, PUN, cloud sync, or matchmaking work.
- No tactical battle changes.
- No unit stat, skill, cooldown, damage formula, or gameplay float changes.
- No scene, prefab, material, controller, `.inputactions`, `.asmdef`, or
  `.asmref` edits without explicit permission.
- No reroll command or reroll UI work.
- No localization system.
- No redesign of account progression beyond using the current XP-to-level rule.
- No implementation of campaign/mission unlock progression beyond the explicit
  Start Run slot rules in this PRD.

## Further Notes

- PRD035 already notes that unselected generated offers may become a
  data-analysis/offer-history feature later. This PRD intentionally keeps that
  separate.
- The current demo lock for Slot 4 is product state, not account progression.
  A future non-demo build can remove or replace that rule without changing how
  slot availability is represented.
- The slot model should remain easy to extend when Online Mode validates slot
  availability server-side.
