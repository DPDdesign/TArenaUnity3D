# [TARENA] PRD019: Start Run

- Status: implemented-manual-test-pending
- Type: HITL Task
- Area: Run Metagame, Starting Armies, Starting Army Variants, Online-Ready Architecture
- Label: ready-for-agent
- Parent: `_codex/tasks/019_PRD_RunMetagameRewardFramework.md`
- Blocked by: None - can start immediately

## HITL Gate

Before implementation, run `/grill-me` for this task.

Confirm how the player starts a run, which weaker starting army variants are
available, and how a smaller starting army is selected without changing unit
stats, skills, cooldowns, or existing battle values.

## What To Build

Create the Start Run slice: the player chooses one weaker starting army variant
and creates a new run state. This is the first vertical slice of the run
metagame.

This task must preserve current gameplay and avoid confusing two different
concepts:

- `Starting Armies` are weaker preset or derived armies used to begin a run.
- `Saved Armies` are completed-run armies used later for offence/defence.
- The Start Run screen must not show `in use`, `defence`, or `offence` states.
- Existing saved army/build data may be inspected for adapter needs, but Start
  Run must not treat the saved-army roster as the starting-army list.
- The architecture must be online-ready, but no online framework/backend exists
  yet.

## Mode Architecture - 2026-06-14

Current implementation target:

- Implement this task for `Offline Mode` only.
- Offline Mode creates the run locally and stores the initial run army snapshot
  through a SQLite-ready local persistence adapter.
- Starting-army templates/variants may be read from local authored data or a
  temporary current-save adapter, but the Start Run domain model must not depend
  on `PlayerPrefs` or local file paths as authority.

Future online target:

- `Online Mode` must be a separate future mode. It will ask a backend for legal
  starting armies, validate the selected variant server-side, and create the
  run online.
- Do not add online login, PlayFab, PUN, Photon, cloud sync, or backend calls in
  this task.

Shared seam:

- Shape the run-start interface as a mode-neutral command/result:
  selected starting army, selected route preview/path, initial run currency
  where confirmed, validation errors, and created run id.
- Offline and future Online should be separate adapters behind that interface,
  not two sets of Start Run UI rules.

## Clarification - 2026-06-14

Feedback from the Start Run UI mockup:

- Rename the player-facing list from `Saved Armies` to `Starting Armies`.
- Starting armies are separate weaker variants, not the player's saved
  offence/defence armies.
- Do not show defence/offence usage badges on Start Run.
- Do not add an aggregate `Army Skills` gameplay concept.
- Skills belong to individual units. A unit may have its own skills locked or
  unlocked.
- The screen should show unit level/tier such as I, II, III, unit amount, stack
  combat value, and total army value.
- Route Preview is a good direction: three route choices, description,
  recommended value, and current army value.

Design decision: `Army Skills` as a shared army-level skill list is rejected for
this task. If a future ADR is created, it should record that Start Run displays
per-unit skill unlock state, not a new army-wide skill system.

## 1. Database

Prepare SQLite-ready persistence boundaries for:

- game mode: Offline now, Online as a future separate mode,
- run authority/source: local offline adapter now, backend adapter later,
- account/player id,
- starting army template id,
- starting army variant id,
- selected starting army id,
- new run id,
- initial run army snapshot,
- unit tier/level per starting stack,
- unit amount per starting stack,
- combat value per starting stack,
- total starting army value,
- per-unit skill unlock state where needed,
- route preview option id and recommended army value where needed,
- run status at creation,
- starting run currency if confirmed by grill.

The data shape should be serializable and backend-agnostic. Do not make
`PlayerPrefs` or local file paths the core model authority.

## 2. Backend Methods

Define methods for:

- selecting the Offline run-start adapter for current implementation,
- listing available starting army variants,
- reading starting army template/variant data through a clean boundary,
- validating that a starting army can start a run,
- calculating stack combat values and total army value for display,
- exposing per-unit skill locked/unlocked state without creating army-wide
  skills,
- preparing route preview options with description, recommended value, and
  current army value,
- creating a new run record,
- creating the initial current-run army state,
- returning clear errors for missing starting army, empty army, invalid army, or
  blocked run start.

Backend methods should be deterministic and testable without Unity scene
objects.

## 3. Frontend Methods

Define presenter/view-model methods for:

- starting army list,
- selected starting army details,
- unit tier/level, amount, stack combat value, and total army value,
- per-unit skill unlock indicators,
- route preview data with three route choices where available,
- recommended value versus current army value,
- start-run validation state,
- starting army value summary,
- begin-run command result.

Frontend methods must consume backend result data and must not duplicate run
start rules.

## 4. UI Setup

Prepare UI setup requirements for the Start Run screen:

- player-facing `Starting Armies` list,
- no defence/offence/in-use badges,
- selected starting army detail panel,
- stack rows showing unit tier/level, amount, stack combat value, and total
  army value,
- per-unit skill locked/unlocked indicators only,
- no aggregate `Army Skills` panel or army-wide skill feature,
- route preview with three paths, description, recommended value, and current
  army value,
- run start summary,
- Begin and Back commands,
- disabled/error states for invalid selection.

Actual Unity scene, prefab, Canvas, material, or asset edits require explicit
permission during implementation.

## Acceptance Criteria

Done when:

- a player-facing Start Run flow can select a valid weaker starting army,
- a new run state can be created from that selection,
- starting armies are represented separately from immutable saved armies and
  offence/defence armies,
- the Start Run screen does not show defence/offence/in-use states,
- stack rows can show unit tier/level, amount, stack combat value, and total
  army value,
- per-unit skill locked/unlocked state can be displayed without creating an
  army-wide skill system,
- route preview data can show three route choices, descriptions, recommended
  value, and current army value,
- Offline Mode can start a run without any backend service,
- the run-start command/result shape can later be handled by an Online adapter
  without changing Start Run UI rules,
- the design is SQLite-ready and online-ready without introducing a backend SDK,
- UI-facing data exists for the starting army list, selected army, validation,
  and start command,
- no gameplay balance, unit stats, skills, cooldowns, existing saved army files,
  or battle behavior are changed.

## Implementation - 2026-06-14

### What Changed

- `StartRunModels.cs`: added non-Inspector DTO fields for ids, route id, run id,
  authority, status, currency, stack tier/level/amount/value, skill unlock
  state, and initial army snapshot. Useful ranges: ids are stable non-empty
  strings, amount/level must be above 0, currency/value are 0+. Higher
  amount/value only changes Start Run display and created snapshot for now, not
  battle stats. Tuning hint: tune starter amounts in the catalog, not unit XML.
- `DefaultStartRunCatalog.cs`: added 3 starting army variants and 3 route
  previews. `StartingCurrency` is 0 because no currency amount was confirmed.
- `StartRunService.cs` and `OfflineStartRunAdapter.cs`: added BuildScreen and
  BeginRun paths for Offline Mode. No Inspector fields changed.
- `_codex/Gen_Im/RETSOT ONLINE/src/game/ui.js`: updated Task 20 mockup so Scene
  1 is Army + Inspector, Scene 2 is Route Preview, Scene 3 is Begin Result.
- Removed fields: none. Existing public/serialized Unity fields were not
  renamed or changed.

### Automatic Test

- Added `TArenaUnity3D/Assets/Scripts/Tests/EditMode/StartRunServiceTests.cs`.
- Tests check: Start Run view data for armies/routes/stack values, Offline run
  record creation with initial snapshot, and rejection of an illegal unit skill.
- Run manually in Unity: `Window > General > Test Runner > EditMode`, select
  `StartRunServiceTests`, click Run. Expected result: 3 passing tests.
- I did not run Unity tests automatically; user runs them inside Unity.
- Ran `node --check` for the modified HTML mockup JS; it passed.

### Unity Test

#### Unity Setup

- No scene, prefab, Canvas, material, component, or Inspector setup is required
  for this task.
- Let Unity compile the new `.cs` files, then open the Unity Test Runner
  EditMode tab.
- For product mockup review, open
  `_codex/Gen_Im/RETSOT ONLINE/index.html` and use `Menu > Task 20 mockup`.

#### Play Mode Test

- No Play Mode scene flow is wired in this task.
- In the HTML mockup, choose a Starting Army and confirm the inspector updates
  in the same scene, then choose a route, click Begin Run, and confirm the
  created-run result appears.
- In a future Unity UI hookup, call `OfflineStartRunAdapter.BuildScreen(...)`
  for display data and `BeginRun(...)` for the command result.

### QA Verdict

- Final QA verdict: Pass.
- QA report:
  `_codex/tasks/QA/2026-06-14_1935_020_QA_ArchitectureReview_Final.md`.
- Initial QA found one stale mockup anchor after merging Scene 1 and Scene 2.
  Follow-up fixed it and final QA found no remaining actionable issues.
- Non-blocking observation: the run record store is in-memory; a later task
  should replace it with a real local persistence adapter without changing the
  Start Run service boundary.
- Closure update 2026-06-15: manual Unity integration PRD created at
  `_codex/tasks/RunMetaGame_Tests/020_PRD019_StartRun_ManualIntegrationTest.md`.
  The current
  `Assets/Resources/UI/PRD_19/020_StartRun/PRD_19_020_StartRun.prefab` has
  nested army-card, route-option, and stack-row subprefabs, but manual Play
  Mode setup must confirm `backButton` and `beginButton` are assigned before
  testing.

### Notes

- Start Run is separate from saved armies, offence, defence, and in-use states.
- No gameplay balance, unit stats, skill XML, cooldowns, battle behavior,
  saved-army files, `PlayerPrefs`, PlayFab, PUN, Photon, scenes, prefabs,
  `.asmdef`, or `.asmref` were changed.
- Starting currency remains 0 until a product decision confirms a currency
  amount.

### Next Steps

- Run the 3 EditMode tests in Unity Test Runner.
- Run manual integration checks from
  `_codex/tasks/RunMetaGame_Tests/020_PRD019_StartRun_ManualIntegrationTest.md`.
- Later, wire Unity UI to `OfflineStartRunAdapter` and replace the in-memory
  store with real local persistence.
