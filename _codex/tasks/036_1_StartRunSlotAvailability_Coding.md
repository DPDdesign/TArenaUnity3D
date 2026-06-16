# [TARENA] 036.1 Start Run Slot Availability Coding Task

- Status: implemented-manual-test-pending
- Type: Coding Task
- Area: Run Metagame, Start Run, Generated Starting Armies, Offline DB, UI
- Label: ready-for-agent
- Parent: `_codex/tasks/036_PRD_StartRunSlotAvailability.md`
- Related:
  - `_codex/tasks/020_PRD019_StartRun.md`
  - `_codex/tasks/030_DB-001_OfflineModeDatabasePersistence.md`
  - `_codex/tasks/035_PRD_RandomStartingArmiesRoutes.md`
  - `_codex/agents/docs/PRD019_PRD030_RunMetagame_Code_Map.md`
  - `_codex/Documentation/PRD030_OfflineDatabase_Map.md`

## Goal

Implement calculated Start Run slot availability for generated starting army
offers.

The Start Run screen must generate one army offer per Inspector-wired army card,
show locked slots with a TMP reason label, block locked-card interaction, and
reject Begin Run for locked selections. Lock state must be calculated from the
current screen/account facts and must not be stored as `IsLocked` in the
database.

## Product Rules

- Slot numbering is visual and 1-based.
- Slot count comes from the Start Run screen's Inspector-wired army-card array.
- The generated Start Run offer source must generate an army for each requested
  slot, including locked slots.
- Slot 1 is unlocked.
- Slot 2 is locked with reason `Win A Run` until the current account has an
  active won run summary.
- Slot 3 is locked with reason `Reach Level 5` until the current account level
  is at least 5.
- Slot 4 is locked with reason `Unavailable in DEMO`.
- Slot 5 and later are locked with reason `Coming soon`.
- A new player starts at level 1.
- Under the current account progression rule, level 5 begins at 1000 XP.
- Slot 2 should check the DB-backed run summary state every time Start Run
  screen data is built; do not rely on a stale cached value for this task.
- Locked cards remain visible and keep their generated army data underneath the
  locked overlay.
- Locked cards cannot be clicked, highlighted, or started through Begin Run.

## Scope

Do:

- Extend Start Run screen data to carry visual slot index, calculated locked
  state, and lock reason.
- Add a Start Run availability/progress boundary that can read:
  - account XP,
  - whether the account has an active won run summary.
- Wire production Start Run composition so the availability boundary is
  DB-backed.
- Keep UI controllers on adapter/service APIs; do not query SQLite from UI.
- Let the Start Run UI pass requested slot count from the Inspector-wired army
  card array.
- Let the generated offer source produce the requested number of offers.
- Preserve generated army data for locked slots.
- Add serialized fields to the Start Run army-card view for:
  - locked overlay GameObject,
  - locked reason TMP text.
- Bind the locked overlay and reason text from view data.
- Disable locked card button interaction so locked cards do not click or
  highlight.
- Validate Begin Run against calculated slot availability and return blocked
  run-start behavior for locked selections.
- Add focused EditMode tests after QA review according to the implement-task
  workflow.

Do not:

- Do not persist `IsLocked`.
- Do not add generated-offer analytics/offer-history persistence.
- Do not move starting army definitions into SQLite.
- Do not change unit stats, skill data, cooldowns, battle behavior, or gameplay
  float values.
- Do not edit Unity scenes, prefabs, materials, controllers, `.inputactions`,
  `.asmdef`, `.asmref`, generated Unity files, or binary assets unless the user
  explicitly permits it during implementation.
- Do not introduce legacy `UnityEngine.UI.Text`; use TMP types only.
- Do not run Git, `dotnet`, Unity builds, package restore, external build
  scripts, or SDK installation commands.

## Implementation Notes

- Start from the Start Run service, adapter, DB store/composition, generated
  catalog, Start Run screen controller, and army-card view.
- Existing production composition already uses the PRD035 deterministic
  generator-backed source for Start Run.
- Prefer a small tested domain object or service for visual slot rules so slot
  behavior can be tested without Unity scene setup.
- Keep account fact reads behind a Start Run persistence/progress interface.
- Slot 2 can use the existing run-summary table and final-result id for won
  summaries.
- Slot 3 can use the existing account XP value and the current XP-to-level rule.
- If the generated source has an offer-count config, make the requested slot
  count explicit rather than hardcoding four offers.
- If no generated offer exists for a visible slot because of an unexpected
  generator failure, the card should still fail closed as locked `Coming soon`
  rather than becoming clickable.

## Acceptance Criteria

Done when:

- Start Run screen data contains one option per requested Inspector slot count.
- Every option has a visual slot index.
- Every requested slot receives generated army data when the generator can
  produce it.
- Slot 1 is selectable for a new account.
- Slot 2 is locked with `Win A Run` for a new account.
- Slot 2 becomes selectable when DB state contains an active won run summary for
  the account.
- Slot 3 is locked with `Reach Level 5` for a level 1 account.
- Slot 3 becomes selectable when account XP reaches level 5 under the current
  XP rule.
- Slot 4 is locked with `Unavailable in DEMO` while still showing its generated
  army.
- Slot 5 and later are locked with `Coming soon`.
- Locked card views activate the locked overlay and write the reason into the
  TMP reason label.
- Unlocked card views hide the locked overlay and clear or ignore the reason
  text.
- Locked card buttons cannot be clicked or highlighted.
- Begin Run rejects locked selections even if called directly.
- UI code does not query SQLite directly.
- No `IsLocked` state is written to the database.
- No generated-offer analytics persistence is added.
- Focused tests cover slot rule behavior and DB-backed progress facts.
- Unity compilation and Play Mode validation remain manual for the user.

## Suggested Verification

- Run EditMode tests manually in Unity Test Runner after implementation.
- Manual Start Run screen smoke:
  - new account: Slot 1 available, Slot 2 `Win A Run`, Slot 3 `Reach Level 5`,
    Slot 4 `Unavailable in DEMO`;
  - test account with a won run summary: Slot 2 available;
  - test account with XP for level 5: Slot 3 available;
  - extra Inspector card beyond Slot 4: `Coming soon`;
  - locked slots show generated army visuals but cannot be clicked.

## Notes

- Generated offer history for data analysis remains a future PRD035 follow-up.
- This task is UI-facing. During the full implement-task workflow, ask before
  creating or updating a Unity UI mockup prefab unless the user explicitly
  permits prefab/mockup edits.

## Implementation - 2026-06-16

### What Changed

- `StartRunModels.cs` / `StartingArmyOptionViewData`: added non-Inspector slot
  metadata: `VisualSlotIndex`, `IsLocked`, `LockedReason`. Useful range:
  `VisualSlotIndex` is 0+, `IsLocked` is true/false, reason is short UI text.
  Lower/higher slot index changes which visual rule applies; tuning hint: add
  future slot rules in one rule module, not in UI.
- `StartRunModels.cs` / `StartRunCommand`: added `RequestedSlotCount` so Begin
  Run validates the same visual slot set the UI displayed. Useful range: 0+
  where 0 keeps legacy/default offer count; higher values request more
  generated offers.
- `StartRunContracts.cs`: added request-count offer source and slot
  availability abstractions. No Inspector fields changed.
- `DeterministicRunGenerationCatalog.cs`: implemented requested offer count so
  the generator can produce one army per Start Run card.
- `StartRunService.cs` and `OfflineStartRunAdapter.cs`: BuildScreen and
  BeginRun now accept requested slot count/account context, calculate lock
  state, and reject locked Begin Run calls.
- `OfflineStartRunSlotAvailabilitySource.cs`: added DB-backed account progress
  reader for account XP level and active won run summaries.
- `OfflineModeDatabaseComposition.cs`: production Start Run now uses the
  DB-backed availability source.
- `StartRunScreenController.cs`: passes `armyCards.Length` into screen build
  and Begin Run; locked cards no longer get click listeners.
- `CampaingSelectionScreenController.cs`: passes account id into BuildScreen
  while preserving old slot-count behavior.
- `StartRunArmyCardView.cs`: added Inspector fields `locked` and
  `lockedReasonText`. `locked` should reference the card's locked overlay
  GameObject; null means no overlay appears. `lockedReasonText` should reference
  a TMP text under the overlay; null means no reason text appears. These are
  object references, not numeric tuning fields.

### Automatic Test

- Added `StartRunSlotAvailabilityTests.cs`.
- Tests check requested generated slot count, default lock reasons, Slot 2/3
  unlock behavior from progress context, direct Begin Run rejection for a
  locked slot, and DB-backed XP/won-run reads.
- Tests were not run automatically. Run manually in Unity:
  `Window > General > Test Runner > EditMode`, select
  `StartRunSlotAvailabilityTests`, click Run. Expected result: 4 passing tests.

### Unity Test

#### Unity Setup

- On every Start Run army card, assign `Locked` to the locked overlay
  GameObject.
- On every Start Run army card, assign `Locked Reason Text` to the TMP reason
  text inside that overlay.
- Keep existing `Button`, `Background`, `Name Text`, `Value Text`, `Status
  Text`, and stack representation references assigned.
- Make sure the Start Run screen controller's `Army Cards` array contains every
  visual slot that should be generated.

#### Play Mode Test

- Press Play and open Start Run with a new account.
- Expected: Slot 1 is clickable; Slot 2 shows `Win A Run`; Slot 3 shows
  `Reach Level 5`; Slot 4 shows `Unavailable in DEMO`; Slot 5+ shows
  `Coming soon`.
- Click locked cards. Expected: no selection, no highlight, no Begin Run.
- Create/test a DB state with a won run summary. Expected: Slot 2 becomes
  clickable.
- Set account XP to at least 1000. Expected: Slot 3 becomes clickable.

### QA Verdict

- Final QA verdict: Pass.
- QA report:
  `_codex/tasks/QA/2026-06-16_1337_036_1_QA_ArchitectureReview.md`.
- Actionable findings: none.
- Non-blocking observations: new UI fields require manual prefab/Inspector
  wiring; generated-offer analytics remains future scope.
- Follow-up fixes after QA: none required.

### Notes

- Created PRD:
  `_codex/tasks/036_PRD_StartRunSlotAvailability.md`.
- Created/implemented coding task:
  `_codex/tasks/036_1_StartRunSlotAvailability_Coding.md`.
- No `IsLocked` state is persisted.
- No generated-offer analytics or offer-history persistence was added.
- No Unity prefab, scene, material, generated file, `.asmdef`, or `.asmref`
  was edited.
- Unity compilation and Play Mode validation remain manual.

### Next Steps

- Run `StartRunSlotAvailabilityTests` in Unity EditMode Test Runner.
- Wire `Locked` and `Locked Reason Text` on Start Run army cards.
- Run the Play Mode smoke test above.
- Czy chcesz, zebym wygenerowal Unity UI mockup prefab dla tego taska?
