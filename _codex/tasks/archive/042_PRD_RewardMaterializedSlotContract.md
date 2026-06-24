# [TARENA] PRD 042: Reward Materialized Slot Contract

- Status: closed-implemented-pending-unity-validation
- Type: PRD
- Area: Run Metagame, Reward Generation, Reward Map
- Label: closed-implemented-pending-unity-validation
- Related:
  - `_codex/tasks/019_PRD_RunMetagameRewardFramework.md`
  - `_codex/tasks/023_PRD019_RewardMap.md`
  - `_codex/tasks/archive/037_PRD_MaterializedRunGenerationRewardsAndMapPersistence.md`
  - `_codex/tasks/archive/041_PRD_RewardValueParityScaling.md`
  - `_codex/Documentation/ADR_011_RunGenerationMaterializedUpfront.md`
  - `_codex/Documentation/ADR_012_RewardDirectionAndStackFocus.md`

## Problem Statement

PRD037 and PRD041 moved post-battle rewards toward materialized, deterministic,
value-scaled choices, but the current materialized generator still has weak
contract edges.

The player-facing 1-of-3 reward choice can silently drift from the intended
Minimal Reward Flow V1 shape:

- a single impossible normal reward slot can become `RunGold`,
- three card slots are not guaranteed to represent three different normal
  operation types,
- disabled or burned normal cards are not modeled as first-class card results,
- promote/downgrade legality depends on hardcoded unit-name faction inference,
  not on catalog identity.

These weaknesses can create bugs such as wrong reward type, wrong upgrade unit,
missing upgrade path, or economy fallback appearing when the run should show a
disabled route-consequence card.

## Solution

Deepen the materialized reward generation module so one small interface owns the
full V1 card-slot contract.

For each post-battle reward opportunity, generation should produce three
materialized normal slots from three different normal operation types:

- Add new stack,
- Increase stack,
- Promote unit,
- Downgrade unit.

If a planned normal operation type has no legal target, that slot remains a
visible disabled/burned card. It must not reroll into another operation type and
must not become `RunGold` by itself.

Emergency `RunGold` can appear only when all three normal slots are impossible.
In this task, expose it through the existing reward-choice DTO surface without
editing Unity prefabs or scenes. If the current card list is the only available
surface, keep the three normal slots deterministic and make the fallback
explicit enough for the Reward Map persistence task to store it safely.

Promote and downgrade should use catalog unit identity for same-faction and
tier-neighbor decisions. Do not rely on hardcoded unit-name lists when catalog
metadata is available.

## User Stories

1. As a player, I want every reward slot to represent the route reward type I
   earned, so that the run does not secretly reroll bad outcomes into unrelated
   cards.
2. As a player, I want an impossible reward type to stay visible as disabled, so
   that route choices feel honest.
3. As a player, I want `RunGold` only when every normal card is impossible, so
   that economy fallback does not replace army growth too eagerly.
4. As a player, I want the three cards to represent different directions, so
   that the 1-of-3 choice is not fake.
5. As a player, I want promotion and downgrade cards to choose the correct
   faction neighbor, so that upgrades do not transform into the wrong unit.
6. As a designer, I want operation-type planning to be deterministic from the
   run seed, node, slot, and seed version, so that reward bugs can be
   reproduced.
7. As a designer, I want disabled/burned cards to preserve the planned
   operation type, so that route reward direction remains inspectable.
8. As a designer, I want catalog faction and tier data to drive conversion, so
   that adding units does not require editing a hardcoded resolver.
9. As a developer, I want generation to return concrete card-slot outcomes, so
   that Reward Map UI and DB stores do not infer state from strings.
10. As a developer, I want the reward generator tests to cover whole choices,
    not only one operation found across many seeds.
11. As a QA reviewer, I want a regression test proving one impossible card does
    not create `RunGold`, so that the fallback rule cannot regress.
12. As a QA reviewer, I want a regression test proving all-normal-impossible
    creates an explicit emergency fallback, so that the player cannot dead-end.
13. As a QA reviewer, I want tests for three distinct normal operation types, so
    that duplicate operation choices cannot slip through.

## Implementation Decisions

- Primary coding ownership for this PRD:
  - materialized reward generation module,
  - reward unit definition/source models needed by the generator,
  - generator-focused EditMode tests.
- Do not edit Reward Map screen prefabs, scenes, materials, `.asmdef`, or
  generated Unity files.
- Do not change reward balance float values from PRD041.
- Do not change tactical battle code in this PRD.
- The materialized generator should select planned operation types first, then
  resolve each planned type to a legal concrete card or a disabled/burned card.
- A disabled/burned normal card should keep:
  - planned operation type,
  - stable slot index,
  - stable template/catalog entry id,
  - `Legal = false`,
  - `RewardMapError.NoLegalTarget`,
  - display text short enough for the existing card view.
- A single disabled normal slot must not trigger `RunGold`.
- Emergency `RunGold` is valid only when all planned normal slots are disabled.
- If the current DTO surface cannot represent a bottom action cleanly, represent
  the fallback as an explicit card/list entry without losing the three planned
  normal slot records. Document the temporary UI compromise in the completion
  notes.
- Add catalog faction identity to the unit definition path used by reward
  generation, or otherwise expose a small unit classification interface that
  the generator can use.
- Promote/downgrade should find exactly one tier up/down in the same catalog
  faction. If no such unit exists, the slot is disabled/burned.
- Keep seeded randomness only for deterministic tie-breaking between equally
  good legal candidates.
- Keep PRD041 value parity scoring intact for legal cards.

## Testing Decisions

- Add or update EditMode tests around the generator seam.
- Good tests should exercise the public generator output and card DTOs, not
  private helper methods.
- Add coverage that one impossible normal operation creates a disabled card and
  no `RunGold`.
- Add coverage that all three normal operations impossible exposes emergency
  `RunGold` explicitly.
- Add coverage that a generated choice contains three distinct normal operation
  types before any emergency fallback is considered.
- Add coverage that generated reward value parity from PRD041 still holds for
  legal Mass, Width, Promote, and Downgrade cards.
- Add coverage that promote/downgrade uses catalog faction/tier metadata, not
  hardcoded name inference.
- Prior art:
  - `PRD37MaterializedRunGenerationTests`,
  - `PRD41RewardValueParityTests`,
  - `RewardMapServiceTests`.

## Out of Scope

- No Reward Map prefab or scene edits.
- No database schema changes unless absolutely required for compiling the
  generator DTO shape; persistence is handled by PRD043.
- No tactical result bridge changes; stack identity is handled by PRD044.
- No full upfront reward-opportunity run generation; that is PRD045.
- No new reward families beyond Minimal Reward Flow V1.
- No gameplay balance changes.

## Further Notes

This PRD should run before PRD043 because persistence needs a stable generated
card-slot contract to store and reload.

Implementation note after integration review:

- The generated choice can contain three disabled normal slot records plus a
  fourth emergency `RunGold` fallback record when every normal slot is
  impossible.
- `RewardMapScreenController.SelectFocusedReward()` now applies the focused
  legal reward, so the emergency fallback remains reachable even if the current
  visible card array only renders the three normal slots.
- Unity EditMode/manual verification is still pending.

Suggested worker ownership:

- Owns: reward generator, unit definition/source additions required by reward
  generation, generator tests.
- Avoids: Reward Map DB store, tactical result bridge, screen controller.

## Closure - 2026-06-24

Closed after implementation and integration review.

Implemented:

- deterministic three-normal-slot planning,
- disabled normal cards preserving their planned operation type,
- emergency `RunGold` only when all normal slots are impossible,
- catalog faction metadata for promote/downgrade,
- PRD041 value parity preservation tests,
- focused Reward Map apply path for the emergency fallback.

Unity compilation, EditMode tests, and Play Mode validation remain manual.
