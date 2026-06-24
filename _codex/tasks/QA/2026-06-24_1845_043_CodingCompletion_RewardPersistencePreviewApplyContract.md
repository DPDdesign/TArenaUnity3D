# [TARENA] Coding Completion - PRD043 Reward Persistence Preview Apply Contract

- Task: `_codex/tasks/archive/043_PRD_RewardPersistencePreviewApplyContract.md`
- Date: 2026-06-24
- Agent: Coding Agent
- Scope: focused reward DB persistence, schema compatibility, and EditMode test coverage

## Summary

Implemented explicit persisted reward card identity/state for DB-backed Reward
Map choices. Reload no longer infers disabled state from preview display text,
and apply/focus resolution now uses persisted reward card identity plus reward
slot instead of template id alone.

## Changed Files

- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/RewardMapModels.cs`
  - Added runtime card fields:
    - `RewardSlotIndex` for concrete materialized reward slot identity.
    - `IsFallback` for emergency fallback card state.
  - No Inspector or serialized Unity fields changed.
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/OfflineRewardMapDbStore.cs`
  - Saves and reloads explicit card state in `reward_cards`: reward id, reward
    slot index, affected stack/slot, operation type/payload, legal state,
    error id, selected state, fallback state, preview snapshot, and applied
    snapshot.
  - Saves matching node reward identity/state in `map_node_rewards`: reward
    choice id, card reward id, reward slot, legal/error state, selected state,
    applied snapshot, and fallback state.
  - Saves focused and selected reward slot indexes in `reward_choices`.
  - Loads disabled state from `legal`/`error_id` instead of comparing
    `preview_text_after` with `"No legal target"`.
  - Keeps disabled operation payloads unchanged on reload; stack-id
    normalization only runs for legal cards.
  - Resolves focused preview rows and applied rows through persisted card
    identity, preferring `reward_id + reward_slot_index`; template id is only a
    legacy fallback.
  - Updates exactly one `reward_cards` row and one matching
    `map_node_rewards` row during apply.
- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineDatabaseSchemaV1.cs`
  - Added V1 table columns for PRD043 reward identity/state.
- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineDatabaseModule.cs`
  - Added V1 compatibility `ALTER TABLE` guards for existing local databases.
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/OfflineRunBattleRewardDbTests.cs`
  - Added disabled/fallback reload coverage.
  - Added duplicate-template focus/apply coverage and selected/applied row
    consistency assertions.
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/OfflineDatabaseSchemaTests.cs`
  - Added schema/compatibility assertions for the new PRD043 columns.

## Inspector / Serialized Fields

No Inspector fields changed.

## Architecture Notes

- The implementation stays in the reward DB/store boundary plus plain model
  fields needed to carry persisted runtime identity.
- Reward generation, tactical result bridge, launch adapter, reconciler,
  Reward Map screen controller, prefabs, scenes, `.asmdef`, and `.asmref` were
  not edited.
- Preview/apply remains service/resolver-owned. Persistence no longer
  recalculates state just to discover whether a card is disabled.
- `offline_runs` writes remain delegated through `OfflineRunContextDbWriter`.

## Tests

Added/updated focused EditMode tests:

- `RewardChoicePersistence_ReloadsDisabledNormalsAndEmergencyFallbackFromCardState`
  - Persists three disabled normal cards plus one emergency RunGold fallback.
  - Reloads disabled state from DB fields even when after text is not
    `"No legal target"`.
  - Verifies fallback state is present in both `reward_cards` and
    `map_node_rewards`.
- `RewardChoicePersistence_DuplicateTemplateIdsFocusAndApplyByRewardSlot`
  - Persists two cards with the same template/catalog id in different reward
    slots.
  - Focuses/applies the second card by reward id/slot identity.
  - Verifies exactly one selected/applied card row and one matching
    `map_node_rewards` row.
- `OfflineDatabaseSchemaTests`
  - Verifies new PRD043 columns are included in schema creation and V1
    compatibility.

## Manual Checks Performed

- Source inspection for remaining template-only update paths.
- Source inspection of `reward_cards` and `map_node_rewards` insert/update
  column/value shapes.

## Commands Not Run

- Unity Test Runner was not run.
- `dotnet` was not run.
- Git commands were not run.
- Unity build or external build tooling was not run.
