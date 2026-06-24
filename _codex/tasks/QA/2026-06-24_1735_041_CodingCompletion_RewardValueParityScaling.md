# [TARENA] Coding Completion - PRD041 Reward Value Parity Scaling

- Task: `_codex/tasks/041_PRD_RewardValueParityScaling.md`
- Date: 2026-06-24
- Agent: Coding Agent
- Scope: focused generator-side implementation before QA review

## Summary

Implemented the PRD041 reward value policy in the materialized reward generator
so legal Mass, Width, Promote, and Downgrade cards scale from the current
average live stack value instead of using fixed or overly broad formulas.

## Changed Files

- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/RewardMapMaterializedGenerator.cs`
  - Added private reward value constants:
    - base reward gain ratio: 20 percent of average live stack value,
    - raw growth multiplier: 1.2 for More Units and Add Stack,
    - army-shape multiplier: 1.0 for Promote and Downgrade.
  - Changed `BuildIncreaseStack(...)` so it evaluates all live stacks and
    chooses the amount/target closest to the raw-growth target gain instead of
    always adding 30 percent of a random stack amount.
  - Changed `BuildAddNewStack(...)` so it evaluates legal non-duplicate units
    and chooses the new stack value closest to the raw-growth target gain
    instead of using average stack value times 1.2 as the whole new-stack value.
  - Changed `BuildPromoteOrDowngrade(...)` so the converted whole-stack amount
    targets `oldStackValue + targetGain` and chooses the closest legal target.
  - Replaced random target-stack selection helpers with closest-candidate
    scoring and seeded tie-breaking.

## Inspector / Serialized Fields

No Inspector fields changed.

## Architecture Notes

- The change stays inside the materialized generation layer.
- Reward Map UI remains a load/preview/apply screen.
- No SQLite schema, persistence table, screen controller, prefab, scene, unit
  stat, unit cost, skill, cooldown, or battle behavior was changed.
- The existing seeded `Random` is still used only to choose between equally
  close candidates.

## Tests

Per `/implement` workflow, focused EditMode tests are intentionally added after
QA review. No tests were authored in this pre-QA implementation pass.

## Manual Checks Performed

- Text scan confirmed old random target helpers are no longer referenced.
- Brace count check passed for `RewardMapMaterializedGenerator.cs`.

## Known Follow-Up For Test Pass

Add focused EditMode tests for:

- high-value stacks around 5000 value,
- More Units target gain,
- Add Stack target gain and duplicate avoidance,
- Promote final stack value,
- Downgrade final stack value,
- deterministic closest-candidate behavior.
