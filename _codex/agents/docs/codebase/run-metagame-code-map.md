# TArenaUnity3D Run Metagame Codebase Map

Status: active
Last updated: 2026-06-26

## Use When

Use this map only when the current prompt, task, or brief explicitly touches
PRD019/PRD030 run metagame, Offline Mode, run database state, reward flow, run
screens, or screen data movement.

## Task-Scoped Sources

- `_codex/agents/docs/PRD019_PRD030_RunMetagame_Code_Map.md`
- `_codex/Documentation/PRD030_OfflineDatabase_Map.md`
- `Assets/Scripts/RunMetagame/`
- `Assets/Scripts/RunMetagame/030_Database/OfflineModeDatabaseComposition.cs`
- `Assets/Scripts/RunMetagame/030_Database/OfflineDatabaseSchemaV1.cs`
- `Assets/Scripts/RunMetagame/030_Database/OfflineRunContextDbReader.cs`
- `Assets/Scripts/RunMetagame/030_Database/OfflineRunContextDbWriter.cs`

## Current Reward-Generation Implementation Notes

- `023_RewardMap/RewardMapMaterializedGenerator.cs` is the active PRD37/PRD41
  materialized reward generator.
- PRD042 closed the V1 slot contract: each generated choice plans three
  distinct normal operation types, disabled normal slots stay visible with
  `RewardMapError.NoLegalTarget`, and emergency `RunGold` appears only when all
  three normal slots are impossible.
- PRD043 closed the Reward Map persistence contract: cards now carry explicit
  reward id, reward slot index, legal/error state, fallback state, and
  selected/applied state. Do not infer disabled state from preview text or
  identify focused cards by `template_id` alone.
- PRD045 added upfront unresolved reward opportunities in
  `023_RewardMap/OfflineRewardOpportunityDbStore.cs` and the
  `reward_opportunities` table. Start Run/run map materialization writes the
  planned operation slots; battle completion resolves those slots into concrete
  Reward Map cards.
- Normal battle wins route to Reward Map through persisted
  `RunBattleNextScreen.Reward`; final wins route to Summary Value.
- Reward Map production flow loads persisted reward rows and must not reroll
  screen-time fallback rewards for DB-backed runs.
- PRD41 value parity scales materialized reward amounts from average live stack
  value: Mass/Width target higher raw point gain, while Promote/Downgrade target
  army-shape gain.
- PRD044 reduced the duplicate same-unit stack risk during tactical
  battle-to-reward handoff by reconciling post-battle stacks by stack id, then
  battle-input order, then explicit legacy unit-id fallback.
- Closed reward-flow PRDs are archived under `_codex/tasks/archive/`:
  `042_PRD_RewardMaterializedSlotContract.md`,
  `043_PRD_RewardPersistencePreviewApplyContract.md`,
  `044_PRD_TacticalRewardStackIdentity.md`, and
  `045_PRD_UpfrontRewardOpportunityMaterialization.md`.
- Remaining reward-flow follow-up: no-battle `RecruitReward` nodes receive
  unresolved opportunity rows but still need a direct Reward Map resolution hook
  before that node type is complete.

## Current Run-Metagame DB Architecture

- `OfflineRunContextDbReader` is the shared read side for active run context,
  screen-specific `next_screen` lookup, persisted summary lookup, latest battle
  result lookup, Start Run record reload, and snapshot conversion to screen DTOs.
- `OfflineRunContextDbWriter` is the only runtime code surface that should
  insert or update `offline_runs`.
- Start Run, Run Map, Run Battle, Reward Map, Run Shop, and Summary Value must
  update run context through the writer instead of duplicating
  `INSERT/UPDATE offline_runs` SQL in slice-specific stores.
- UI controllers should read run state through adapters/services and the shared
  reader, not by direct SQLite access or serialized placeholder ids.
- Reward planning DB ownership now spans `reward_opportunities`,
  `reward_choices`, `reward_cards`, and `map_node_rewards`. The first table is
  unresolved run-generation truth; the latter three are resolved concrete Reward
  Map truth.
