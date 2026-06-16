# [TARENA] PRD019 021/023/025/027 - UI Prototype Rework Audit

## Scope

Second-pass audit after user feedback that the previous PRD19 mockups looked
identical, overflowed their scenes, and had non-functional buttons.

This audit covers:

- `021_PRD019_RunMap.md`
- `023_PRD019_RewardMap.md`
- `025_PRD019_SummaryValue.md`
- `027_PRD019_BattleResult.md`

Mockups 020 and 024 were intentionally left out of this rework pass.

## Verdict

Implementation checkpoint reached for a working offline prototype. Full closure
still requires Unity import and Unity Test Runner execution inside the Editor.

## Code-to-UI Binding

- 021 Run Map: `PRD19_021_RunMapMockupController` renders data from
  `OfflineRunMapAdapter` and sends travel commands through `RunMapService`.
- 023 Reward Map: `RewardMapScreenController` renders
  `RewardMapChoiceViewData` from `OfflineRewardMapAdapter` and commits rewards
  through `RewardMapService`.
- 025 Summary Value: `SummaryValueScreenController` renders
  `SummaryValueScreenViewData` from `OfflineSummaryValueAdapter` and sends
  Save/Overwrite through `SummaryValueService`.
- 027 Battle Result: `BattleResultScreenController` records and renders
  `BattleResultViewData` through `OfflineBattleResultAdapter` and
  `BattleResultService`.

## UI Prototype Actions

- 021 route node buttons focus real route nodes; Travel validates and advances
  route state; Back and View Army update the prototype state.
- 023 reward card buttons focus reward choices; Select applies the focused
  reward and locks the choice; Continue is blocked until a selection exists.
- 025 save slot buttons select empty/taken/locked slots; Save/Overwrite uses the
  service path, including overwrite confirmation; Return updates flow state.
- 027 army cards focus attacker/defender details; Continue checks stored result
  flow; View Armies opens the saved-armies roster preview.

## Builder Split

The old shared-looking PRD19 mockup builder has been replaced with
task-specific builders:

- `PRD19_021_RunMapMockupBuilder`
- `PRD19_023_RewardMapPrefabBuilder`
- `PRD19_025_SummaryValuePrefabBuilder`
- `PRD19_027_BattleResultPrefabBuilder`

Each builder owns its own nested prefab templates and menu command.

## Local Validation

- `rg` scan found no inert button-select listeners, Unity object/name lookup
  shortcuts, stale shared builder command, legacy shared mockup screen view,
  `TODO`, or `FIXME` in the 021/023/025/027 C# scope.
- `py -3` brace-balance scan passed for RunMetagame C# files in
  021/023/025/027 scope.
- `py -3` duplicate type declaration scan passed for RunMetagame and EditMode
  tests in 021/023/025/027 scope.
- No Unity, dotnet, git, package restore, or build command was run.

## Remaining Gap

Only durable database/backend persistence remains unresolved:

- route persistence and Online route validation,
- reward choice persistence and inventory source,
- saved-army roster/save-slot persistence,
- backend-authoritative async result/ranking/account XP.

Current prototypes use in-memory/offline stores by design.
