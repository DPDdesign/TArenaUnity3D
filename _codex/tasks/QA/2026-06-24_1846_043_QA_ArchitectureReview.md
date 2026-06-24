# [TARENA] QA Architecture Review - PRD043 Reward Persistence Preview Apply Contract

- Task: `_codex/tasks/archive/043_PRD_RewardPersistencePreviewApplyContract.md`
- Protocol: `_codex/tasks/QA/2026-06-24_1845_043_CodingCompletion_RewardPersistencePreviewApplyContract.md`
- Date: 2026-06-24
- Reviewer: QA Architecture Review Agent
- Verdict: Pass

## Sources Reviewed

- `AGENTS.md`
- `_codex/agents/qa-architecture-review-agent.md`
- `_codex/skills/qa-review/SKILL.md`
- `_codex/tasks/archive/043_PRD_RewardPersistencePreviewApplyContract.md`
- `_codex/tasks/QA/2026-06-24_1845_043_CodingCompletion_RewardPersistencePreviewApplyContract.md`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/RewardMapModels.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/OfflineRewardMapDbStore.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/RewardMapService.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineDatabaseSchemaV1.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineDatabaseModule.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/OfflineRunBattleRewardDbTests.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/OfflineDatabaseSchemaTests.cs`

## Findings

No actionable findings.

## Architecture Review

- Ownership is correct for PRD043. The changes are limited to the reward DB
  persistence seam, schema compatibility helpers, plain reward DTO state, and
  DB-focused tests.
- The implementation avoids the PRD042/PRD044-owned files named by the user:
  reward generation, tactical result bridge, tactical stack reconciler, launch
  adapter, Reward Map screen controller, prefab builders, and assets were not
  edited.
- Persisted reward card state is explicit. `reward_cards` now carries reward
  id, slot identity, affected stack/slot, operation type/payload, legal/error
  state, selected/applied state, and fallback state. `map_node_rewards` carries
  matching choice/card identity and selected/fallback state.
- Disabled reload no longer uses display text as domain truth. The store reads
  `legal`/`error_id`, and disabled operation payloads are no longer normalized
  on reload.
- Focus/apply identity is slot-safe for new rows. Focused preview and apply
  resolution prefer persisted `reward_id + reward_slot_index`, with template id
  retained only as a legacy fallback.
- Preview/apply authority remains in the Reward Map service/resolver. The DB
  store preserves materialized card state and applies the resolver's accepted
  result rather than owning reward operation rules.
- `offline_runs` updates remain delegated through `OfflineRunContextDbWriter`,
  matching PRD030 ownership rules.
- No Unity assets, prefabs, scenes, materials, `.inputactions`, generated Unity
  files, `.asmdef`, or `.asmref` were edited.
- No gameplay float or balance value was changed.

## Test Review

- `RewardChoicePersistence_ReloadsDisabledNormalsAndEmergencyFallbackFromCardState`
  directly covers the PRD042 all-impossible shape: three disabled normal cards
  plus emergency RunGold fallback, with disabled state surviving reload without
  the `"No legal target"` text sentinel.
- `RewardChoicePersistence_DuplicateTemplateIdsFocusAndApplyByRewardSlot`
  covers duplicate template/catalog ids across slots and verifies exactly one
  selected/applied `reward_cards` row plus one matching `map_node_rewards` row.
- `OfflineDatabaseSchemaTests` covers both schema creation fragments and V1
  compatibility columns.
- Tests are deterministic EditMode DB/service tests and do not require scene or
  prefab setup.

## Non-Blocking Observations

- `RewardMapService` still recalculates legal-card previews when focusing or
  applying. That is consistent with the task requirement that the resolver stay
  the final authority. Disabled cards skip recalculation because persisted
  `Legal/Error` state is now explicit.
- The template-id identity query remains only as a final legacy fallback for
  old rows without explicit PRD043 identity columns.

## Final Verdict

Pass. No follow-up fixes required before manual Unity EditMode validation.
