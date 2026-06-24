# [TARENA] QA Architecture Review - PRD37 Battle Reward Routing

- Task: `_codex/tasks/037_PRD_MaterializedRunGenerationRewardsAndMapPersistence.md`
- Protocol: `_codex/tasks/QA/2026-06-24_0933_037_CodingCompletion_BattleRewardRouting.md`
- Date: 2026-06-24
- Reviewer: QA Architecture Review Agent
- Verdict: Pass

## Sources Reviewed

- `_codex/agents/qa-architecture-review-agent.md`
- `_codex/tasks/QA/2026-06-24_0933_037_CodingCompletion_BattleRewardRouting.md`
- `_codex/agents/docs/PRD019_PRD030_RunMetagame_Code_Map.md`
- `_codex/Documentation/PRD030_OfflineDatabase_Map.md`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/022_RunBattle/RunBattleService.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/022_RunBattle/RunBattleTacticalResultBridge.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/022_RunBattle/OfflineRunBattleDbStore.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/GameSceneManager.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/RewardMapMaterializedGenerator.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/RunBattleServiceTests.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/OfflineRunBattleRewardDbTests.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/PRD37MaterializedRunGenerationTests.cs`

## Findings

No actionable findings.

## Architecture Review

- `RunBattleService` is the correct owner for choosing `RunBattleNextScreen`.
  The change keeps destination selection out of UI code and aligns with the
  existing `RunBattleCompletionRecord` contract.
- `OfflineRunBattleDbStore` already owns the post-completion persistence and
  reward materialization trigger. Routing normal wins to `Reward` now activates
  that existing store behavior without duplicating reward generation in the
  bridge or UI controller.
- `RunBattleTacticalResultBridge` now uses the persisted completion result for
  screen routing after a successful save. This removes the previous hidden
  coupling where UI routing depended on the raw win boolean instead of the DB
  handoff state.
- `GameSceneManager` only maps a run-battle destination enum to existing screen
  methods. No screen controller takes on direct SQLite or reward-generation
  responsibility.
- Existing compatibility methods `ReturnFromBattleWon()` and
  `ReturnFromBattleLost()` remain available for current callers.

## Test Review

- `RunBattleServiceTests` now covers normal win -> `Reward`, final win ->
  `FinalSummary`, and loss -> `RunLoss`.
- `OfflineRunBattleRewardDbTests` now expects DB-backed battle completion to
  hand off to `Reward`.
- `PRD37MaterializedRunGenerationTests` now asserts reward routing before
  checking materialized reward rows and no-reroll behavior.

Tests were not run automatically, per project policy.

## Non-Blocking Observations

- There is still no direct EditMode test for
  `RunBattleTacticalResultBridge.ReportBattleFinished(...)`, because that path
  depends on the default DB, `DataMapper.Instance`, and tactical scene runtime
  objects. The service/store tests cover the deterministic routing decision and
  materialization behavior; Play Mode should still smoke-test the bridge path.
- `GameSceneManager.ReturnFromBattleWon()` still routes to Run Map for legacy
  callers. That is acceptable as compatibility, but PRD37 tactical battle
  completion should use `ReturnFromBattle(RunBattleNextScreen)` as implemented.

## Final Verdict

Pass. No follow-up code fixes required.
