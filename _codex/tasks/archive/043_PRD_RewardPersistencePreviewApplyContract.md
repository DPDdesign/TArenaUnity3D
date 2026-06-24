# [TARENA] PRD 043: Reward Persistence Preview Apply Contract

- Status: closed-implemented-pending-unity-validation
- Type: PRD
- Area: Reward Map, Offline DB, Preview/Apply Consistency
- Label: closed-implemented-pending-unity-validation
- Depends on:
  - `_codex/tasks/archive/042_PRD_RewardMaterializedSlotContract.md`
- Related:
  - `_codex/tasks/030_6_PRD030_RunBattle_Reward_DBIntegration.md`
  - `_codex/tasks/archive/037_PRD_MaterializedRunGenerationRewardsAndMapPersistence.md`
  - `_codex/tasks/archive/041_PRD_RewardValueParityScaling.md`
  - `_codex/Documentation/PRD030_OfflineDatabase_Map.md`

## Problem Statement

Reward Map currently persists materialized card rows, but some important reward
state is still inferred indirectly.

The DB-backed Reward Map load path can treat display text as domain truth:

- legal state is inferred from `preview_text_after == "No legal target"`,
- focused preview lookup uses `template_id` without slot identity,
- only the focused card receives a saved preview snapshot,
- card reload then asks the reward service to recalculate card state again.

This creates weak locality. A bug in one place can surface as mismatched card
text, wrong hover preview, wrong applied snapshot, or a second interpretation of
the same operation payload.

## Solution

Deepen the persisted reward card module so persisted rows carry explicit card
identity and card state.

Reward persistence should store and reload the materialized card as a concrete
slot result. The card interface should not require callers to know which UI text
means disabled, which template id is unique enough, or whether preview must be
recomputed to discover legality.

The DB-backed Reward Map service should still preview/apply through the reward
resolver, but persisted card loading should preserve materialized identity:

- reward choice id,
- reward slot index,
- reward id,
- template/catalog entry id,
- planned operation type,
- operation payload,
- legal/disabled state,
- fallback flag,
- selected/applied state,
- focused card identity including slot index.

PRD042 integration detail: a fully impossible normal reward choice may contain
three disabled normal slot records plus a fourth emergency `RunGold` fallback
record. Persistence must preserve both the normal slot identity and the fallback
card identity; it must not assume card list index alone is equivalent to normal
slot index.

## User Stories

1. As a player, I want hovering a reward card to preview that exact card, so
   that the UI does not show another slot's result.
2. As a player, I want disabled cards to stay disabled after reload, so that
   saved state is stable.
3. As a player, I want the applied reward to match the previewed card, so that
   I can trust before/after text.
4. As a developer, I want DB rows to store card state explicitly, so that UI
   text is never domain state.
5. As a developer, I want focused preview lookup to use slot identity, so that
   duplicate template ids cannot collide.
6. As a developer, I want Reward Map reload tests to verify no reroll and no
   card-state drift, so that bugs stay local to persistence.
7. As a QA reviewer, I want tests for duplicate template ids across slots, so
   that focus/apply cannot pick the wrong row.
8. As a QA reviewer, I want tests for disabled card reload, so that `No legal
   target` text does not remain the only persisted signal.

## Implementation Decisions

- Primary coding ownership for this PRD:
  - Reward Map DB store,
  - reward card persistence model/schema/migration if needed,
  - DB-focused reward tests.
- Do not edit reward generation logic except to consume the stable output from
  PRD042.
- Do not edit tactical result bridge code.
- Do not edit Unity prefabs, scenes, `.asmdef`, or generated Unity files.
- Add explicit persisted state for reward cards if the current schema cannot
  represent it safely.
- If schema changes are needed, update schema tests and keep the change as a
  V1-compatible local DB rebuild/migration path consistent with PRD030 rules.
- Focused card lookup must include slot identity or reward id, not template id
  alone.
- Selection/apply updates should identify the selected card by choice id plus
  slot identity/reward id. Template id alone is not enough.
- Reload should preserve disabled/burned and fallback state without comparing
  display text.
- Preview/apply should still use the reward resolver for the final operation,
  but persisted state should not be recalculated just to decide whether a card
  was disabled when materialized.
- Keep `offline_runs` updates through `OfflineRunContextDbWriter`.

## Testing Decisions

- Add or update EditMode tests around the DB store seam.
- Good tests should persist a materialized reward choice, reload it, focus each
  card, apply one card, and assert stored rows.
- Add a test that disabled cards reload as disabled without relying on preview
  text.
- Add a test that duplicate template/catalog ids in different slots do not
  break focus preview or apply.
- Add a test that exactly one card is selected and one applied snapshot is
  written.
- Add a test that `map_node_rewards` and `reward_cards` agree on selected slot.
- Prior art:
  - `OfflineRunBattleRewardDbTests`,
  - `PRD37MaterializedRunGenerationTests`,
  - `OfflineDatabaseSchemaTests`.

## Out of Scope

- No generator operation-type redesign; PRD042 owns that.
- No tactical duplicate-stack fix; PRD044 owns that.
- No UI prefab work.
- No online/backend reward authority work.
- No gameplay balance changes.

## Further Notes

This PRD runs after PRD042. PRD042 is implemented with Unity verification still
pending, and its generated card-slot contract is stable enough for the DB store
work.

Implementation note after integration review:

- Reward cards now carry persisted `RewardSlotIndex` and `IsFallback` runtime
  state.
- Reward DB rows store explicit reward id, slot identity, legal/error state,
  fallback state, operation payload/type, selected/applied state, and affected
  slot identity.
- Existing V1 databases get the PRD043 compatibility columns, including
  `map_node_rewards.is_fallback`.
- Unity EditMode/manual verification is still pending.

Suggested worker ownership:

- Owns: reward DB store, schema if required, DB tests.
- Avoids: materialized generator, tactical battle bridge, UI prefab builders.

## Implementation - 2026-06-24

### What Changed

- `RewardMapModels.cs` / `RewardMapCardViewData`: added runtime card identity
  fields `RewardSlotIndex` and `IsFallback` so persisted cards can carry slot
  and fallback state after reload.
- `OfflineRewardMapDbStore.cs`: persists/reloads reward id, reward slot,
  affected slot, operation type/payload, legal/error state, fallback state,
  selected state, preview snapshot, and applied snapshot. Disabled state now
  reloads from DB columns instead of `preview_text_after == "No legal target"`.
- `OfflineRewardMapDbStore.cs`: focus/apply lookup now prefers
  `reward_id + reward_slot_index`; template id remains only as a legacy
  fallback.
- `OfflineDatabaseSchemaV1.cs` and `OfflineDatabaseModule.cs`: added V1 schema
  and compatibility columns for PRD043 reward card state.
- No Inspector fields changed.

### Automatic Test

- Added EditMode coverage in `OfflineRunBattleRewardDbTests`:
  - disabled normal cards plus emergency RunGold fallback reload from persisted
    card state;
  - duplicate template ids focus/apply by reward id and slot;
  - exactly one `reward_cards` row and one `map_node_rewards` row are selected
    and applied.
- Updated `OfflineDatabaseSchemaTests` for new PRD043 schema and compatibility
  columns.
- Tests were not run automatically. Run them manually in Unity Test Runner:
  `Window > General > Test Runner > EditMode`, then run
  `OfflineRunBattleRewardDbTests` and `OfflineDatabaseSchemaTests`. Expected:
  all tests pass.

### Unity Test

#### Unity Setup

- No scene, prefab, material, controller, input, `.asmdef`, or `.asmref` setup
  is required for the new EditMode tests.
- Use the existing Unity project and Test Runner EditMode tab.

#### Play Mode Test

- Start an Offline run and reach a Reward Map node.
- Hover different reward cards, including cards with duplicate catalog/template
  ids if generated/test data exposes them.
- Apply one legal card.
- Expected: the focused card preview matches the hovered slot, disabled cards
  remain disabled after returning/reloading, one card is applied, and the flow
  returns to Run Map.

### QA Verdict

- QA Architecture Review: Pass.
- QA report:
  `_codex/tasks/QA/2026-06-24_1846_043_QA_ArchitectureReview.md`.
- Actionable findings: none.
- Follow-up fixes applied: none required.

### Notes

- Did not edit PRD042/PRD044-owned generator, tactical bridge, stack
  reconciler, launch adapter, Reward Map controller, prefab builders, or Unity
  assets.
- Did not run Unity, dotnet, git, build tooling, package restore, SDK install,
  or external build scripts.
- Preview/apply remains resolver-owned; persistence now preserves materialized
  card identity and disabled/fallback state.

### Next Steps

- Run the focused EditMode tests manually in Unity Test Runner.
- Run a Play Mode reward flow smoke check after Unity compilation succeeds.

## Closure - 2026-06-24

Closed after implementation, integration review, and one follow-up migration
fix for existing V1 databases.

Implemented:

- persisted reward id and reward slot identity,
- explicit legal/error/fallback state,
- selected/applied state by reward id plus slot,
- duplicate template id-safe focus/apply,
- disabled card reload without text-as-domain-state,
- `map_node_rewards.is_fallback` compatibility migration.

Unity compilation, EditMode tests, and Play Mode validation remain manual.
