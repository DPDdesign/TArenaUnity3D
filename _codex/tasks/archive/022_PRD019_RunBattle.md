# [TARENA] PRD019: Run Battle

- Status: implemented-backend-qa-pass
- Type: HITL Task
- Area: Run Battle Bridge, Battle Completion, Loss Tracking, Online-Ready Architecture
- Label: ready-for-agent
- Parent: `_codex/tasks/019_PRD_RunMetagameRewardFramework.md`
- Blocked by: None for completed backend bridge; full route integration still
  depends on `_codex/tasks/021_PRD019_RunMap.md`

## HITL Gate

Before implementation, run `/grill-me` for this task.

Confirm how a run node launches an existing battle, what battle completion data
is needed, and how losses are recorded without changing existing battle rules.

Specific questions for `/grill-me`:

- What is the smallest safe adapter from run battle node data into the current
  battle launch path?
- Should V1 route battles use existing saved/build file ids, generated temporary
  build records, or an in-memory bridge into the existing spawn system?
- What exact completion signal should return from battle to run state: win/loss
  only, or win/loss plus surviving stack counts and losses?
- How should the run battle bridge avoid making `PlayerPrefs` the long-term
  authority while still respecting the current battle launcher?

## What To Build

Create the Run Battle slice: a battle node launches the existing battle setup,
then returns a battle result to the run. This task is a bridge/adapter around
current gameplay, not a battle system task.

Do not change unit stats, skills, cooldowns, damage, movement, targeting, SFX,
VFX, turn rules, or battle feel values.

## Mode Architecture - 2026-06-14

Current implementation target:

- Implement this task for `Offline Mode` only.
- Offline Mode uses a local run-battle adapter around the existing battle
  launch/spawn path. Local battle completion is authoritative only for Offline
  Mode.
- The adapter may bridge to current local build/spawn mechanisms, but
  `PlayerPrefs`, local build files, and compatibility RPC helpers are adapter
  surfaces, not the run-battle domain authority.

Future online target:

- `Online Mode` must be a separate backend-verified battle mode.
- Online battle start, submitted action/result payloads, final outcome, losses,
  and saved-army eligibility must be validated by the backend.
- Do not let future Online trust an offline client-side battle result as final
  authority.
- Do not add online transport, matchmaking, PlayFab, PUN, Photon, cloud sync, or
  backend calls in this task.

Shared seam:

- Shape battle launch and completion as explicit mode-neutral payloads:
  run battle id, run id, route node id, current army snapshot id, encounter id,
  enemy goal, battle result, surviving stacks, losses, and result source.
- Offline and future Online should be separate adapters behind that interface.
  The run flow should not know whether completion came from the local adapter or
  a future backend-verified adapter.

## Clarification - 2026-06-14

The battle already exists. TArena already has mechanics for preparing armies,
spawning teams, and running the tactical battle. This task should not rebuild or
redesign battle spawning.

Scope correction:

- Implement only the connection between a run battle node and the existing
  battle launch/spawn mechanism.
- Treat current battle setup as a legacy/current adapter surface.
- The task should prepare the run's current army and selected enemy encounter
  so the existing battle can start.
- After battle completion, the task should collect the result needed by the run:
  win/loss and, where available, surviving stack/loss data for rewards and
  shops.
- Do not add a new battle mode, new spawn rules, new tactical HUD, or new battle
  mechanics in this task.

Known current-code direction to inspect during implementation:

- menu selection currently uses `PlayerPrefs` keys such as `YourArmy` and
  `EnemyArmy`,
- army builds use `PanelArmii.BuildG`,
- battle setup/spawn paths are around `HexMap` and `TeamClass.GenerateTeam(...)`.

These paths are evidence for an adapter boundary, not permission to make
`PlayerPrefs` or local files the final online-ready run battle model.

## 1. Database

Prepare SQLite-ready persistence boundaries for:

- game mode and battle-result authority/source,
- run battle id,
- run id,
- route node id,
- encounter id,
- current run army snapshot id,
- enemy encounter army source id,
- temporary battle launch record id if needed,
- player army snapshot before battle,
- player army snapshot after battle,
- enemy goal metadata such as "try to win" or "deal maximum losses",
- battle result: win, loss, escaped/cancelled if later confirmed,
- losses and recovered/living stack counts needed by rewards and shops,
- explicit battle-completion payload for future backend verification,
- adapter metadata that links run battle data to the current battle launch path
  without making that adapter the long-term data authority.

## 2. Backend Methods

Define methods for:

- selecting the Offline run-battle adapter for current implementation,
- preparing a run battle from a route node,
- mapping current run army data into the existing battle setup/spawn boundary,
- mapping selected enemy encounter data into the existing enemy army boundary,
- launching the current battle path through a narrow adapter,
- receiving battle completion data,
- recording win/loss and stack losses,
- updating run state after battle,
- deciding whether the next screen is reward, run loss, or final summary.

Backend methods should wrap current battle behavior rather than replacing it.
The bridge should be narrow enough that a future online-ready battle launch
model can replace the legacy adapter without rewriting run flow.

## 3. Frontend Methods

Define presenter/view-model methods for:

- run battle header data,
- current run stage and route context,
- compact army value/loss preview,
- battle launch command state,
- battle-complete transition result,
- target/result preview data only if it can be read from existing battle
  systems without gameplay changes.

Frontend methods must not own battle rules, spawn rules, or tactical result
calculation.

## 4. UI Setup

Prepare UI setup requirements for run battle context only:

- small run-stage header,
- current battle goal label,
- current army value/loss summary,
- route context,
- transition handling when battle completes.

This task does not require a new battle HUD. Actual battle HUD changes, Canvas
edits, scene edits, or prefab edits require explicit permission.

## Mockup Workflow

No Unity UI mockup is required for this task unless the task is explicitly
rescoped to include a player-facing Run Battle context screen. The current task
is a backend/domain adapter bridge around battle launch and completion payloads.

## Acceptance Criteria

Done when:

- a battle node can start a battle through a clear run-battle boundary,
- the implementation uses the existing battle launch/spawn mechanism through an
  adapter instead of rebuilding battle spawning,
- battle completion can update run state with win/loss and losses,
- current army state after battle is available for rewards and shops,
- enemy goal vocabulary can be represented without changing AI yet,
- frontend data exists for run battle context, launch state, and completion
  transition,
- Offline Mode can complete a battle locally without backend services,
- battle completion records include enough explicit payload data for a future
  Online adapter to submit and receive backend-verified results,
- `PlayerPrefs`, local build files, and current spawn paths are treated as
  adapter surfaces, not the long-term online-ready authority,
- no current battle gameplay values, skills, animations, VFX/SFX, or turn rules
  are changed.

## Implementation - 2026-06-15

### What Changed

- `RunBattleModels.cs`: added non-Inspector DTO/enums for Offline/Online mode,
  authority, node type, enemy goal, battle outcome, next screen, errors, launch
  payload, launch record, army snapshots, stack loss records, and completion
  records. Useful ranges: ids must be stable non-empty strings, amounts/losses
  are 0+, level is 1+, values are 0+. Higher `Amount`/`CombatValue` only affects
  payload/view data and tests here, not tactical battle stats. Tuning hint:
  tune real encounter/army values in route/encounter data, not in this bridge.
- `RunBattleContracts.cs`: added `IRunBattleEncounterSource`,
  `IRunBattleLaunchAdapter`, `IRunBattleStore`, and `InMemoryRunBattleStore`.
  No Inspector fields changed.
- `DefaultRunBattleEncounterCatalog.cs`: added a tiny V1 local encounter catalog
  with `enc-iron-border-clash`, `enc-iron-hill-ambush`, and `enc-final-proof`.
  No gameplay floats or battle rules changed.
- `OfflineRunBattleLaunchAdapter.cs` and `OfflineRunBattleAdapter.cs`: added
  Offline Mode adapter entry points. Adapter metadata labels current
  `HexMap`/`TeamClass`/PlayerPrefs/local-file paths as launch surfaces only.
- `RunBattleService.cs`: added `PrepareBattle(...)` and `CompleteBattle(...)`.
  It now requires explicit `EncounterId`, builds launch payloads, records
  completion payloads, calculates losses from before/after snapshots, and routes
  normal wins to `Reward`, final wins to `FinalSummary`, and non-wins to
  `RunLoss`.
- No Run Battle UI mockup is required for this backend/adapter slice.
- Removed fields: none. Existing public/serialized Unity fields were not
  renamed or changed.

### Automatic Test

- Added `TArenaUnity3D/Assets/Scripts/Tests/EditMode/RunBattleServiceTests.cs`.
- Tests check: Offline launch payload and legacy adapter record, win completion
  with stack losses and reward transition, final-win versus loss transition, and
  rejection of missing army or missing encounter id.
- Run manually in Unity: `Window > General > Test Runner > EditMode`, select
  `RunBattleServiceTests`, click Run. Expected result: 5 passing tests.
- I did not run Unity tests automatically; user runs them inside Unity.
- Browser/prototype-page validation is not part of the accepted PRD019 workflow
  for this backend/adapter task.

### Unity Test

#### Unity Setup

- Let Unity import the new `.cs` files, then open the Unity Test Runner EditMode
  tab.
- No scene, prefab, Canvas, material, component, or Inspector setup is required
  for this backend/adapter task.

#### Play Mode Test

- No Play Mode battle scene launch is wired in this task.
- In a future Unity UI hookup, call `OfflineRunBattleAdapter.PrepareBattle(...)`
  before battle launch and `CompleteBattle(...)` when the current battle returns
  an outcome.

### QA Verdict

- Final QA verdict: Pass.
- QA report: `_codex/tasks/QA/2026-06-15_0125_022_QA_ArchitectureReview_Final.md`.
- Initial QA found mockup/code drift in encounter ids and a missing explicit
  `EncounterId` requirement. Follow-up fixed both.
- Workflow correction 2026-06-15: this Pass applies to the Run Battle code and
  payload alignment only. Because this task is backend/adapter-only, it does
  not require a Unity UI mockup deliverable.
- Non-blocking observation: the default encounter catalog is intentionally tiny
  and should later be replaced or fed by Run Map authored/generated data.

### Notes

- This task does not launch a Unity battle scene, generate `PanelArmii.BuildG`
  records, set `PlayerPrefs`, or alter current tactical battle behavior.
- No gameplay balance, unit stats, skills, cooldowns, movement, targeting,
  animations, VFX/SFX, scenes, prefabs, `.asmdef`, `.asmref`, PlayFab, PUN, or
  Photon paths were changed.
- The obsolete UI mockup created for this backend task has been removed from
  `Assets` and archived under
  Historical PRD019 mockup-builder artifacts were removed from
  `_codex/tasks/archive/PRD19_ObsoleteMockups/022_RunBattle/`.

### Next Steps

- Run `RunBattleServiceTests` in Unity Test Runner EditMode.
- Later, implement/fix Run Map so it supplies explicit encounter ids into this
  bridge, then wire a Unity scene adapter for the existing battle launch path.

## Backend-Only Mockup Cleanup - 2026-06-15

- `PRD_19_22_RunBattleMockup.prefab`, its generated repeated prefabs, the
  RunBattle mockup view scripts, and the mockup builder were archived under
  Historical PRD019 mockup-builder artifacts were removed from
  `_codex/tasks/archive/PRD19_ObsoleteMockups/022_RunBattle/`.
- Active Run Battle scripts now live under
  `Assets/Scripts/RunMetagame/022_RunBattle/`.
- Do not regenerate a Run Battle prefab unless a later task explicitly adds a
  real player-facing Run Battle UI screen.
