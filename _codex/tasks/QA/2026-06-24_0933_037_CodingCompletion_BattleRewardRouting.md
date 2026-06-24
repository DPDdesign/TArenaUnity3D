# [TARENA] Coding Completion - PRD37 Battle Reward Routing

- Task: `_codex/tasks/037_PRD_MaterializedRunGenerationRewardsAndMapPersistence.md`
- Date: 2026-06-24
- Agent: Coding Agent
- Scope: focused PRD37 follow-up from current code map risk

## Summary

Implemented the remaining PRD37 routing fix so normal battle wins hand off to
Reward Map and therefore trigger materialized reward generation in the DB store.
Final battle wins now hand off to Summary Value, while losses continue to route
to run-loss/default handling.

## Files Changed

- `TArenaUnity3D/Assets/Scripts/RunMetagame/022_RunBattle/RunBattleService.cs`
  - `DetermineNextScreen(...)` now returns `Reward` for non-final wins,
    `FinalSummary` for final wins, and `RunLoss` for non-wins.
- `TArenaUnity3D/Assets/Scripts/RunMetagame/022_RunBattle/RunBattleTacticalResultBridge.cs`
  - routes via `completion.CompletionRecord.NextScreen` after persistence,
    instead of routing by the raw `playerWon` boolean.
- `TArenaUnity3D/Assets/Scripts/RunMetagame/GameSceneManager.cs`
  - added `ReturnFromBattle(RunBattleNextScreen nextScreen)` to map persisted
    battle destinations to existing screen methods.
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/RunBattleServiceTests.cs`
  - updated service expectations for normal win -> `Reward`, final win ->
    `FinalSummary`, and loss -> `RunLoss`.
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/OfflineRunBattleRewardDbTests.cs`
  - updated DB-backed battle/reward expectation to `Reward`.
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/PRD37MaterializedRunGenerationTests.cs`
  - added explicit assertion that battle completion routes to `Reward` before
    checking persisted materialized reward rows.

## Inspector Fields

No Inspector fields changed.

## Tests

Updated existing EditMode tests:

- `RunBattleServiceTests`
- `OfflineRunBattleRewardDbTests`
- `PRD37MaterializedRunGenerationTests`

Tests were not run automatically. Per project rules, run them manually in Unity
Test Runner:

`Window > General > Test Runner > EditMode`

Expected focused result:

- `RunBattleServiceTests` passes.
- `OfflineRunBattleRewardDbTests` passes.
- `PRD37MaterializedRunGenerationTests` passes.

## Manual Play Mode Check

1. Start an Offline run.
2. Travel to a normal battle node.
3. Complete the tactical battle with a win.
4. Expected: the battle completion persists, materialized reward rows are
   created, and the UI opens Reward Map instead of Run Map.
5. Click a legal reward.
6. Expected: the reward applies immediately and returns to Run Map.
7. Complete a final battle with a win.
8. Expected: the run routes to Summary Value.

## Notes

- No Unity scenes, prefabs, materials, controllers, `.inputactions`, `.asmdef`,
  `.asmref`, or generated Unity files were edited.
- No gameplay floats or balance values were changed.
- The old `ReturnFromBattleWon()` and `ReturnFromBattleLost()` methods remain
  for compatibility with existing callers.
