# [TARENA] Coding Agent Completion Protocol - PRD045

Task: `_codex/tasks/archive/045_PRD_UpfrontRewardOpportunityMaterialization.md`
Date: 2026-06-24

## Summary

Implemented a PRD045 vertical slice for upfront reward opportunity planning and post-battle resolution:

- Start Run route seeding now writes unresolved reward opportunity rows for reward-producing nodes.
- Battle completion loads those existing opportunity rows and resolves them into the existing PRD042/PRD043 reward card persistence contract.
- Reward Map reload continues to load persisted resolved reward choices/cards and does not reroll opportunities.
- Focused EditMode tests were added for unresolved rows, preserved slots/types, reload stability, and explicit run seed/version columns.

## Files Changed

- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/DB Enums.cs`
  - Added `DBRewardOpportunityStateId`.
- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineDatabaseSchemaV1.cs`
  - Added `reward_opportunities` table and unique active run/node/slot index.
- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineMaterializedRunMapDbStore.cs`
  - Saves unresolved plans for battle and recruit-reward nodes during route materialization.
- `TArenaUnity3D/Assets/Scripts/RunMetagame/022_RunBattle/OfflineRunBattleDbStore.cs`
  - Loads planned operation types before reward choice resolution.
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/RewardMapMaterializedGenerator.cs`
  - Exposes deterministic operation planning and catalog id helpers.
  - Adds a `BuildChoice` overload accepting persisted planned operation types.
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/OfflineRewardMapDbStore.cs`
  - Marks matching opportunities resolved after concrete reward cards are saved.
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/OfflineRewardOpportunityDbStore.cs`
  - New helper for writing/loading/resolving opportunity rows.
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/OfflineDatabaseSchemaTests.cs`
  - Added schema and enum assertions for reward opportunities.
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/PRD45RewardOpportunityMaterializationTests.cs`
  - New focused EditMode tests.

## Scope Boundaries

- No Reward Map UI files, prefabs, scenes, materials, controllers, `.inputactions`, `.asmdef`, `.asmref`, or generated Unity files changed.
- No gameplay float values changed.
- Existing PRD042 slot/fallback and PRD043 identity/selected/applied contracts are preserved.
- PRD044 tactical files were not changed.

## Verification

Automatic execution was not run because project rules prohibit Unity, `dotnet`, Git, external build scripts, package restore, and SDK installation commands in this workflow.

Manual Unity EditMode tests to run:

- `OfflineDatabaseSchemaTests`
- `PRD45RewardOpportunityMaterializationTests`
- Existing adjacent coverage:
  - `OfflineRunBattleRewardDbTests`
  - `PRD37MaterializedRunGenerationTests`
  - `PRD42RewardMaterializedSlotContractTests`

## Notes For QA

- `reward_opportunities` is intentionally separate from `map_node_rewards` because unresolved rows do not yet have a post-battle base snapshot, while `map_node_rewards.base_snapshot_id` is required and represents resolved reward card state.
- `RecruitReward` nodes receive unresolved opportunity plans at run generation, but this implementation only resolves through the battle-completion hook requested by PRD045.
- Older runs without opportunity rows still use the previous deterministic generator fallback path during battle reward materialization.
