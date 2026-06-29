# [TARENA] PRD019: Battle Result

- Status: implemented-manual-test-pending
- Type: HITL Task
- Area: Async Offence Defence, Ranking, Account Experience, Result UI
- Label: ready-for-agent
- Parent: `_codex/tasks/019_PRD_RunMetagameRewardFramework.md`
- Blocked by: `_codex/tasks/026_PRD019_SavedArmies.md`

## HITL Gate

Before implementation, run `/grill-me` for this task.

Confirm first ranking/account XP rules, offence/defence result vocabulary, and
how much async behavior is simulated locally before a real online framework
exists.

## What To Build

Create the Battle Result slice: after an offence or defence battle, the game
records and presents ranking and account progress results. Offence must not
steal units or destroy another player's army.

This task is online-ready but local-framework-first. Do not add real networking,
matchmaking, PlayFab, Photon, PUN, cloud storage, or live backend calls.

## Mode Architecture - 2026-06-14

Current implementation target:

- Implement this task for `Offline Mode` only if it is implemented before a
  real Online Mode exists.
- Offline Mode may simulate offence/defence results locally for product flow
  validation. Those results are authoritative only inside Offline Mode.
- Offline simulated opponents must not pretend to be real online player records.

Future online target:

- `Online Mode` must be a separate backend-authoritative async offence/defence
  mode.
- Online result recording, ELO-like rank delta, account XP, unlock progress,
  and no-steal/no-destroy preservation must be validated and stored by the
  backend.
- The client may display a result payload but must not locally author the final
  online rank/account outcome.
- Do not add online matchmaking, PlayFab, PUN, Photon, cloud sync, or backend
  calls in this task.

Shared seam:

- Shape async results as mode-neutral payloads: attacker saved army id, defender
  saved army id, opponent metadata, result, rank before/after, rank delta,
  account XP, unlock progress, preservation record, and result source.
- Offline and future Online should be separate adapters behind the same result
  interface. UI presenters must not implement ranking rules directly.

## Clarification - 2026-06-14

Feedback from the Async Battle Result / Account Progress UI mockup:

- This screen direction is accepted.
- ELO-like ranking/rank delta is a good fit here.
- Account XP/progress and next unlock preview are good fits here.
- This is the right screen for rank/progression information, not the Saved
  Armies roster screen.
- Keep the no-steal/no-destroy saved army rule visible enough for clarity, but
  do not turn it into a warning-heavy screen.

## 1. Database

Prepare SQLite-ready persistence boundaries for:

- game mode and result authority/source,
- async battle result id,
- attacker saved army id,
- defender saved army id,
- result: offence win/loss, defence win/loss,
- opponent strength/ranking metadata,
- rank before/after,
- rank delta,
- account XP gained,
- unlock progress,
- no-steal/no-destroy saved army preservation record.

## 2. Backend Methods

Define methods for:

- selecting the Offline battle-result adapter for current implementation,
- recording an offence/defence result,
- calculating ELO-like ranking delta,
- awarding account experience,
- updating unlock progress for future runs,
- preserving both attacker and defender saved armies,
- returning result data for UI,
- supporting future server validation through explicit ids and result payloads.

Backend methods should be deterministic and testable without online services.

## 3. Frontend Methods

Define presenter/view-model methods for:

- battle result summary,
- attacker and defender army summary,
- rank change display,
- account XP display,
- next unlock preview,
- no-army-lost messaging,
- continue/view-armies commands.

Frontend methods must not implement ranking rules directly.

## 4. UI Setup

Prepare UI setup requirements for the Battle Result screen:

- result header,
- attacker versus defender summary,
- ranking delta panel,
- account XP/unlock progress panel,
- no army stolen/destroyed indicator,
- Continue and View Armies commands.

Actual Unity UI setup or scene/prefab edits require explicit permission.

## Mockup Workflow

If a mockup is requested for this task, use
`_codex/skills/make-ui-mockup/SKILL.md`. The accepted output is a Unity UGUI
prefab/prototype with visible components, repeated prefab templates, `Script_*`
owners, and serialized field wiring. Browser/prototype-page mockups do not
satisfy this task's mockup requirement.

## Acceptance Criteria

Done when:

- offence/defence result records can be represented without online services,
- Offline Mode can simulate/store result records without backend services,
- future Online result payloads are explicit enough for backend-authoritative
  validation and storage,
- ranking behaves in an ELO-like way so stronger/equal opponents are more
  rewarding than farming weak opponents,
- account XP and future unlock progress can be updated,
- attacker and defender saved armies are not stolen, destroyed, or edited by
  the result,
- frontend data exists for a readable battle result screen,
- result data is online-ready through stable ids and explicit payloads,
- no backend SDK, live multiplayer, or old saved army mutation is introduced.

## Implementation - 2026-06-15

### What Changed

- `BattleResultModels`, `BattleResultContracts`, `BattleResultService`,
  `OfflineBattleResultAdapter`: added Offline async offence/defence result
  payloads, attacker/defender saved army summaries, simulated-opponent metadata,
  ELO-like rank delta, account XP, next unlock preview, result store, and
  no-steal/no-destroy preservation record. No Inspector fields changed.
- `BattleResultScreenController`,
  `BattleResultArmySummaryCardView`, `BattleResultRankDeltaPanelView`,
  `BattleResultXpProgressPanelView`, and `BattleResultCommandButtonView`:
  added Battle Result UI runtime/view code. Historical PRD019 prefab builders
  for this screen have been removed and must not be regenerated without current
  path-specific permission. The controller builds a sample async result through
  `OfflineBattleResultAdapter -> BattleResultService`, renders the returned
  `BattleResultViewData`, and wires army focus, Continue, and View Armies
  interactions.
- Removed fields: none. Existing public/serialized Unity fields were not
  renamed.

### Automatic Test

- Added
  `TArenaUnity3D/Assets/Scripts/Tests/EditMode/BattleResultServiceTests.cs`.
- Tests check: offence win against a stronger opponent gives positive rank/XP,
  weak-opponent farming gives less rank than equal opponent, both saved armies
  are preserved, and missing attacker is rejected.
- Run manually in Unity: `Window > General > Test Runner > EditMode`, select
  `BattleResultServiceTests`, click Run. Expected result: 3 passing tests.
- I did not run Unity tests automatically; user runs them inside Unity.
- Ran a local `py -3` brace-balance scan for RunMetagame/EditMode C# files.

### Unity Test

#### Unity Setup

- Let Unity import scripts under `Assets/Scripts/RunMetagame/027_BattleResult/`.
- Do not run or recreate historical PRD019 prefab builders. Existing
  `Assets/Resources/UI/PRD_19/027_BattleResult/` prefabs are read-only unless
  the current task gives path-specific permission.

#### Play Mode Test

- Inspect the prefab for attacker/defender summary, ranking delta, account XP,
  no-army-lost messaging, Continue, and View Armies.
- Enter Play Mode with the prefab under a Canvas. Click attacker/defender army
  cards to focus details, Continue to update the flow state, and View Armies to
  open the saved-armies roster preview.
- The prototype uses the current Offline adapter and service locally. Durable
  Online/database persistence remains a future backend responsibility.

### QA Verdict

- Final QA verdict: Pass with manual Unity import pending.
- QA report: `_codex/tasks/QA/2026-06-15_0242_027_QA_ArchitectureReview.md`.
- Actionable findings: none.
- Non-blocking observations: ELO-like formula is deterministic local V1, not
  final balance; future Online Mode must validate result authority server-side.

### Notes

- This task does not add real matchmaking, backend SDK, PlayFab, PUN, Photon,
  live multiplayer, or saved-army mutation.
- Backend gaps: backend-authoritative Online result storage/ranking/account XP:
  tutaj powinno byc z bazy danych.

### Next Steps

- Run `BattleResultServiceTests` in Unity Test Runner EditMode.
- Inspect the existing Battle Result prefab manually if needed; do not
  regenerate PRD019 prefab assets.
