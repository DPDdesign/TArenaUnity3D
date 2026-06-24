# [TARENA] PRD019: Summary Value

- Status: implemented-manual-test-pending
- Type: HITL Task
- Area: Final Encounter, Run Summary, Army Value, Saved Army Snapshot
- Label: ready-for-agent
- Parent: `_codex/tasks/019_PRD_RunMetagameRewardFramework.md`
- Blocked by: `_codex/tasks/archive/022_PRD019_RunBattle.md`, `_codex/tasks/023_PRD019_RewardMap.md`
- Implemented prerequisite pending manual QA: `_codex/tasks/024_PRD019_RunShop.md`

## HITL Gate

Before implementation, run `/grill-me` for this task.

Confirm what "army value" means for summary purposes, when the pre-final
snapshot is captured, and what final encounter result is required before saving
an army.

Specific questions for `/grill-me`:

- What exact data appears in the run summary timeline for each stage?
- What account progress is awarded on final victory?
- What is the overwrite confirmation behavior when the player selects a taken
  army slot?
- Should the UI show 8 physical compatibility slots with only the unlocked
  slots usable, or should `/grill-me` explicitly override the PRD 19 target of
  2 starting unlocked saved-army slots?
- Should an overwritten army be archived, replaced directly, or blocked unless
  the player confirms a second time?

## What To Build

Create the Summary Value slice: at the end of a run, the game summarizes army
growth, losses, rewards, shop purchases, final encounter result, and the saved
army candidate.

This task preserves PRD 19's key rule: the final PvE encounter proves the army,
but the saved army is based on the pre-final snapshot so winning the final does
not make the reward worse.

## Mode Architecture - 2026-06-14

Current implementation target:

- Implement this task for `Offline Mode` only.
- Offline Mode captures the pre-final snapshot locally, validates the local
  final result, produces a saved-army candidate, and saves it into a local
  roster adapter.
- The local saved-army candidate is authoritative only for Offline Mode.

Future online target:

- `Online Mode` must have backend-verified final completion, pre-final snapshot
  validation, account progress awards, saved-army candidate creation, and slot
  save/overwrite commands.
- Online Mode must not trust a client-created saved-army candidate as final
  authority.
- Do not add online account storage, PlayFab, PUN, Photon, cloud sync, or
  backend calls in this task.

Shared seam:

- Shape summary and save-army flow as mode-neutral payloads: run id, pre-final
  snapshot id, final result, account progress reward data, saved-army candidate
  id, slot state, selected slot, save/overwrite command, confirmation state, and
  result source.
- Offline and future Online should be separate adapters behind the same summary
  and saved-army save interface.

## Clarification - 2026-06-14

Feedback from the Final Win / Save Army UI mockup:

- `Run Summary` is good, especially if it shows what was received at each run
  stage.
- `Account Progress` is a strong screen element and should be included.
- `Save This Army` with a full army preview is good.
- The 8-slot save selector is good. It should clearly show which slots are
  locked, empty, and already taken.
- The 8 slots are physical/UI compatibility capacity. Gameplay availability
  must come from an unlocked saved-army slot count. PRD 19's current target is
  2 starting unlocked slots unless `/grill-me` explicitly changes it.
- A locked slot cannot be selected for saving or overwriting.
- If the player clicks an empty slot, the primary action should be `Save`.
- If the player clicks a taken slot, the primary action should become
  `Overwrite` and should require clear confirmation behavior decided in
  `/grill-me`.
- An `Immutable` warning or heavy `Final Encounter Reward` info panel is not
  needed as a prominent UI element on this screen.

The army is still immutable as data after saving. The clarification only means
the UI should not spend prominent screen space warning about immutability unless
later user testing proves it is needed.

## 1. Database

Prepare SQLite-ready persistence boundaries for:

- game mode and saved-army authority/source,
- run summary id,
- run id,
- start army snapshot,
- pre-final army snapshot,
- post-final battle result,
- reward history,
- shop purchase history,
- currency history,
- stage-by-stage run summary entries,
- account progress reward data,
- army value summary,
- saved army candidate id,
- save slot states for 8 physical slots: locked, empty, taken,
- unlocked saved-army slot count,
- selected save slot id,
- save action mode: save or overwrite,
- overwrite confirmation state,
- run win/loss state.

## 2. Backend Methods

Define methods for:

- capturing the pre-final snapshot,
- validating final encounter completion,
- calculating summary value from confirmed data,
- building stage-by-stage run summary entries,
- calculating account progress rewards for final victory where confirmed,
- producing a saved army candidate after a won final,
- rejecting saved army creation after a failed run,
- preserving immutability of the saved army candidate,
- listing 8 save target slots with locked/empty/taken state,
- selecting a save target slot,
- rejecting locked slots as save targets,
- returning `Save` action state for empty slots,
- returning `Overwrite` action state for taken slots,
- applying overwrite only after confirmed behavior is approved,
- returning summary result data.

Backend methods must not change battle rules or rebalance values.

## 3. Frontend Methods

Define presenter/view-model methods for:

- run summary timeline,
- stage-by-stage received rewards/gains summary,
- account progress display,
- starting army versus final army comparison,
- army value display,
- losses/recovery/reward/shop history summary,
- saved army candidate display,
- saved army preview component data,
- 8-slot save target list,
- selected slot state: locked, empty, or taken,
- primary action label: `Save` or `Overwrite`,
- disabled action state for locked slots,
- overwrite confirmation state,
- save-army command state.

Frontend methods should make the result readable without exposing raw internal
budget math unless explicitly approved.

## 4. UI Setup

Prepare UI setup requirements for the Summary Value screen:

- final result header,
- start-to-final summary,
- run summary with stage-by-stage received rewards/gains,
- account progress panel,
- saved army candidate panel,
- full `Save This Army` preview,
- army value and skill/unit summary,
- 8 save slots with locked/empty/taken state,
- locked slots visibly unavailable unless `/grill-me` changes the starting slot
  rule,
- selected empty slot shows `Save`,
- selected taken slot shows `Overwrite`,
- Save Army and Return commands.

Actual Unity UI setup or scene/prefab edits require explicit permission.
Do not make an immutable warning or final-encounter reward explanation a
prominent required UI panel for this task.

## Mockup Workflow

If a mockup is requested for this task, use
`_codex/skills/make-ui-mockup/SKILL.md`. The accepted output is a Unity UGUI
prefab/prototype with visible components, repeated prefab templates, `Script_*`
owners, and serialized field wiring. Browser/prototype-page mockups do not
satisfy this task's mockup requirement.

## Acceptance Criteria

Done when:

- the run can capture a pre-final army snapshot,
- a won final can produce a saved army candidate from that snapshot,
- a failed run cannot produce a saved army candidate,
- summary data includes army value, losses, rewards, shop purchases, and final
  result,
- run summary can show what was received at each relevant stage,
- account progress reward data can be displayed,
- the player can see 8 physical save slots,
- only unlocked slots can be selected for save/overwrite,
- empty slots resolve to a `Save` action,
- taken slots resolve to an `Overwrite` action with confirmation behavior
  captured by `/grill-me`,
- locked slots cannot save or overwrite,
- frontend data exists for a readable end-of-run summary,
- saved army candidate data is immutable and online-ready,
- immutability remains a data rule but is not required as a prominent warning
  panel on this screen,
- no gameplay balance, battle behavior, or old saved army data is changed.

## Implementation - 2026-06-15

### What Changed

- `SummaryValueModels`, `SummaryValueContracts`, `SummaryValueService`,
  `OfflineSummaryValueAdapter`: added Offline run summary payload, pre-final
  saved-army candidate creation, account progress reward display, timeline
  entries, 8 physical save slots, unlocked-slot gating, Save/Overwrite action
  mode, locked-slot rejection, and overwrite confirmation. No Inspector fields
  changed.
- `SummaryValueScreenController`, timeline/stack/slot/command button view
  components, and `PRD19_025_SummaryValuePrefabBuilder`: added a
  task-specific Unity UGUI prototype for Summary Value under
  `Assets/Resources/UI/PRD_19/025_SummaryValue/` after Unity imports scripts.
  The controller renders `SummaryValueScreenViewData` through
  `OfflineSummaryValueAdapter` and sends Save/Overwrite through
  `SummaryValueService`.
- Removed fields: none. Existing public/serialized Unity fields were not
  renamed.

### Automatic Test

- Added
  `TArenaUnity3D/Assets/Scripts/Tests/EditMode/SummaryValueServiceTests.cs`.
- Tests check: won final creates a saved-army candidate from pre-final snapshot,
  lost final creates no candidate, locked slot rejection, save into empty slot,
  and overwrite confirmation.
- Run manually in Unity: `Window > General > Test Runner > EditMode`, select
  `SummaryValueServiceTests`, click Run. Expected result: 3 passing tests.
- I did not run Unity tests automatically; user runs them inside Unity.
- Ran a local `py -3` brace-balance scan for RunMetagame/EditMode C# files.

### Unity Test

#### Unity Setup

- Let Unity import scripts under `Assets/Scripts/RunMetagame/025_SummaryValue/`.
- Unity should run the 025 mockup builder automatically once after compile, or
  use `TArena > Mockups > Rebuild PRD 19 025 Summary Value Prefabs`.
- Open
  `Assets/Resources/UI/PRD_19/025_SummaryValue/PRD_19_025_SummaryValue.prefab`.

#### Play Mode Test

- Inspect the prefab for timeline, account progress, saved army candidate,
  8 save slots, Save/Overwrite, and Return.
- Enter Play Mode with the prefab under a Canvas. Click save slots to select
  empty/taken/locked states, click Save/Overwrite to call
  `OfflineSummaryValueAdapter.Save(...)`, and click Return to update the
  prototype flow state.

### QA Verdict

- Final QA verdict: Pass with manual Unity import pending.
- QA report: `_codex/tasks/QA/2026-06-15_0242_025_QA_ArchitectureReview.md`.
- Actionable findings: none.
- Non-blocking observations: in-memory roster store should later become durable
  local persistence; UI should avoid rebuilding while overwrite confirmation is
  open.

### Notes

- This task preserves the PRD rule that the saved army candidate comes from the
  pre-final snapshot, not the post-final losses.
- This task does not change battle behavior, old saved army data, scenes,
  materials, controllers, `.inputactions`, `.asmdef`, or `.asmref`.
- Backend gaps: durable roster persistence and future backend-verified save
  command: tutaj powinno byc z bazy danych.

## Prototype Audit - 2026-06-15

- `SummaryValueScreenController` is the 025 UI owner. It creates sample run
  payloads as `SummaryValue` models, calls `OfflineSummaryValueAdapter.BuildSummary(...)`,
  renders returned timeline/candidate/slot data, and sends Save/Overwrite
  through `OfflineSummaryValueAdapter.Save(...)`.
- Slot buttons bind `SummaryValueSaveSlotViewData`. Empty unlocked slots select
  `Save`, taken unlocked slots select `Overwrite`, locked slots update status
  and do not become save targets.
- Overwrite is confirmation-like: first click on a taken slot returns the
  service `MissingConfirmation` result and changes the primary label to
  `Confirm Overwrite`; second click calls the same save path with confirmation.
- `PRD19_025_SummaryValuePrefabBuilder` is now the task-specific Unity Editor
  builder for the 025 prefab. It generates nested prefabs for timeline entries,
  saved army stack rows, save slots, and command buttons under
  `Assets/Resources/UI/PRD_19/025_SummaryValue/Prefabs/`.
- The checked-in prefab YAML can remain stale until Unity imports the new 025
  scripts and generates `.meta` GUIDs. Do not hand-author script GUIDs; let
  Unity import, then run `TArena > Mockups > Rebuild PRD 19 025 Summary Value
  Prefabs`.

### Next Steps

- Run `SummaryValueServiceTests` in Unity Test Runner EditMode.
- Let Unity generate/open the Summary Value mockup prefab and inspect its
  hierarchy.
