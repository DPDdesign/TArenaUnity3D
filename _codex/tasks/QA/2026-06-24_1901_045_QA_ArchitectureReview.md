# [TARENA] QA Architecture Review - PRD045

Task: `_codex/tasks/archive/045_PRD_UpfrontRewardOpportunityMaterialization.md`
Protocol: `_codex/tasks/QA/2026-06-24_1900_045_CodingAgentCompletion.md`
Date: 2026-06-24

## Verdict

Pass for the requested battle-completion vertical slice, with one documented follow-up required for no-battle `RecruitReward` node resolution.

## Reviewed Files

- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/DB Enums.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineDatabaseSchemaV1.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineMaterializedRunMapDbStore.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/022_RunBattle/OfflineRunBattleDbStore.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/RewardMapMaterializedGenerator.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/OfflineRewardMapDbStore.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/OfflineRewardOpportunityDbStore.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/OfflineDatabaseSchemaTests.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/PRD45RewardOpportunityMaterializationTests.cs`

## Findings

No blocking architecture findings for the implemented battle-reward path.

## Non-Blocking Observations

- `reward_opportunities` is correctly separate from `map_node_rewards`; unresolved rows cannot satisfy the existing `map_node_rewards.base_snapshot_id` contract until the post-battle/current reward base snapshot exists.
- `RewardMapMaterializedGenerator` exposes planning helpers and an overload without changing the existing value/parity resolution math.
- `OfflineRunBattleDbStore` now resolves from persisted plans and only falls back for older runs with no opportunity rows.
- `RecruitReward` nodes now receive upfront unresolved opportunity rows, but they still need a no-battle reward-creation hook before those nodes can open Reward Map with resolved cards. The protocol names this explicitly; document it in PRD045 as remaining work.

## Test Review

The new `PRD45RewardOpportunityMaterializationTests` cover:

- unresolved opportunity rows before battle;
- operation slot/type preservation through battle completion;
- Reward Map reload stability without new opportunity or choice rows;
- seed/version matching between `offline_runs` and opportunity rows.

Tests were not executed during QA because project rules prohibit command-line Unity, `dotnet`, build, package restore, and Git tooling in this workflow.
