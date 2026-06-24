# [TARENA] PRD 041: Reward Value Parity Scaling

- Status: archived
- Type: PRD
- Area: Run Metagame, Reward Generation, Reward Balance, Offline Mode
- Label: closed
- Related:
  - `019_PRD_RunMetagameRewardFramework`
  - `023_PRD019_RewardMap`
  - `037_PRD_MaterializedRunGenerationRewardsAndMapPersistence`
  - `Reward_Design`
  - `ADR_011_RunGenerationMaterializedUpfront`
  - `ADR_012_RewardDirectionAndStackFocus`

## Problem Statement

Post-battle rewards are materialized and persisted, but their offered value can
be wildly uneven. A late-run army can have stacks worth about 5000 points each,
while one reward card meaningfully upgrades a stack by about 1000 points and
another card adds a new stack worth only about 50 points.

From the player perspective, that makes the 1-of-3 reward choice fake. The
player is not choosing between different army directions; they are choosing the
one card that has real value and ignoring the under-scaled cards.

The current reward flow already has the right product shape: concrete cards,
materialized rows, no screen-time reroll, and immediate click-to-apply. The
missing piece is value parity across legal reward cards.

## Solution

Reward generation should calculate a shared target value from the current army
and then translate that value into different strategic reward forms.

For Minimal Reward Flow V1:

```text
averageLiveStackValue = average CombatValue of live stacks in the current army
baseRewardGain = averageLiveStackValue * 0.20
```

Legal reward cards should land around their target gain, with an accepted
rounding band of target gain plus or minus 20 percent.

Reward families use small V1 multipliers:

```text
More units / Mass: baseRewardGain * 1.2
Add stack / Width: baseRewardGain * 1.2
Promote / Quality: baseRewardGain * 1.0
Downgrade / Mass conversion: baseRewardGain * 1.0
```

This preserves strategic distinction:

- Mass and Width are better raw point growth.
- Promote and Downgrade are lower raw point growth but reshape the army.
- All legal cards remain close enough in value that the player can choose a
  direction, not just the biggest number.

Example:

```text
average live stack value = 5000
base reward gain = 1000

More units target gain = 1200, acceptable roughly 960-1440
Add stack target gain = 1200, acceptable roughly 960-1440
Promote target gain = 1000, acceptable roughly 800-1200
Downgrade target gain = 1000, acceptable roughly 800-1200
```

## User Stories

1. As a player, I want all three post-battle reward cards to feel valuable, so
   that the 1-of-3 choice is meaningful.
2. As a player, I want reward value to scale with my current army, so that late
   run rewards do not feel like early run leftovers.
3. As a player, I want a new-stack reward to add a stack worth real army value,
   so that Width is not a trap option.
4. As a player, I want a more-units reward to visibly grow my army, so that Mass
   feels like the strongest raw growth choice.
5. As a player, I want Promote to improve quality without hugely outscaling
   other cards, so that upgrades are strategic rather than mandatory.
6. As a player, I want Downgrade to create more bodies without becoming a free
   oversized power spike, so that mass conversion stays readable.
7. As a player, I want two raw-growth rewards to be worth more points than a
   Promote plus Downgrade pair, so that "more army" really means more points.
8. As a player, I want card choice to be about army direction, so that I can
   pick mass, width, or quality based on my run plan.
9. As a player, I want reward cards to show concrete before/after results, so
   that I do not need to calculate the value formula.
10. As a player, I want rewards after hard battles to still grow my surviving
    army, so that a winning run can keep momentum.
11. As a player, I want very small or invalid reward results to be avoided, so
    that I do not feel punished by generator math.
12. As a player, I want legal cards to stay comparable after rounding to whole
    unit counts, so that unit cost granularity does not break the choice.
13. As a designer, I want one shared reward-value target, so that reward
    families can be compared against the same baseline.
14. As a designer, I want Mass and Width to have a higher raw-value multiplier,
    so that their identity is clear.
15. As a designer, I want Promote and Downgrade to use the same base value
    target, so that quality and conversion are strategic alternatives.
16. As a designer, I want target selection to prefer the closest legal result,
    so that random selection does not create obviously bad cards.
17. As a designer, I want randomness to remain only as a tie-breaker between
    similarly good legal targets, so that generated runs still vary without
    sacrificing reward quality.
18. As a designer, I want the reward formulas to use current stack values, so
    that future army stat changes do not require rewriting static reward
    amounts.
19. As a developer, I want reward value calculations isolated from UI code, so
    that screens only display materialized card results.
20. As a developer, I want reward value calculations tested at the generator
    seam, so that UI and database persistence tests do not hide balance drift.
21. As a developer, I want no fallback screen-time reroll to fix bad values, so
    that materialized rows remain deterministic and debuggable.
22. As a developer, I want materialized card data to preserve concrete amount
    and target values, so that bad generated rewards can be inspected.
23. As a QA reviewer, I want a regression test for the 5000-value-stack case,
    so that a new stack worth 50 points cannot return.
24. As a QA reviewer, I want tests for rounding bands, so that expensive units
    and cheap units both remain acceptable.
25. As a future online-mode developer, I want the client-facing contract to stay
    materialized and deterministic, so that backend-authoritative rewards can
    validate the same target-value model later.

## Implementation Decisions

- Keep PRD037 materialization rules: generated reward rows are loaded by Reward
  Map, not rolled by the screen.
- Do not change unit stats, unit costs, skill effects, battle damage,
  cooldowns, or tactical behavior.
- Introduce a reward value policy inside reward generation. This policy owns:
  average live stack value, base reward gain, family multiplier, target gain,
  acceptance band, and distance-to-target scoring.
- The average should include only live current-army stacks with amount greater
  than zero.
- The default base reward gain is 20 percent of the average live stack value.
- The default acceptance band is target gain plus or minus 20 percent after
  unit-count rounding.
- Mass / More Units uses a 1.2 multiplier.
- Width / Add Stack uses a 1.2 multiplier.
- Promote uses a 1.0 multiplier.
- Downgrade uses a 1.0 multiplier.
- More Units should add enough units to the selected stack that the value gain
  is closest to the Mass target gain.
- Add Stack should choose a legal non-duplicate unit and amount whose new stack
  value is closest to the Width target gain.
- Add Stack still uses the first free formation slot in V1. If no free slot
  exists, the card is illegal and remains disabled/burned.
- Promote should convert the whole selected stack to the same-faction tier +1
  unit, with amount calculated so the final stack value is closest to:

```text
oldStackValue + targetGain
```

- Downgrade should convert the whole selected stack to the same-faction tier -1
  unit, with amount calculated so the final stack value is closest to:

```text
oldStackValue + targetGain
```

- Candidate selection should evaluate legal candidates and pick the one closest
  to the target gain after rounding to whole units.
- Randomness should only break ties, or choose between candidates that are
  equivalently close to the target.
- A candidate outside the acceptance band may still be used only if no legal
  candidate can land inside the band. In that case, choose the closest legal
  result rather than falling back to a tiny reward.
- A card should be disabled/burned only when its operation type has no legal
  target or no legal unit conversion, not merely because the first random
  candidate was poor.
- Emergency RunGold remains out of normal reward selection and appears only
  when all three normal cards are impossible.
- UI should keep showing concrete before/after text and should not expose the
  raw budget formula as normal player-facing text.
- Existing materialized persistence can continue storing concrete operation
  amount, target stack, unit ids, selected state, and applied snapshot state.
  New debug/value metadata is optional unless needed for tests or QA.
- The implementation should remain a small generator-side fix plus tests. Do
  not build a full reward economy, rarity system, shop rebalance, or army
  optimizer under this PRD.

## Testing Decisions

- Good tests should verify generated reward behavior from public service/store
  seams, not private helper internals.
- Add generator or materialization tests for an army whose live stacks average
  about 5000 value. Assert that legal cards land near the target value instead
  of producing a tiny new stack.
- Add tests for More Units: amount added should produce a value gain close to
  `averageLiveStackValue * 0.20 * 1.2`.
- Add tests for Add Stack: the new stack value should be close to
  `averageLiveStackValue * 0.20 * 1.2` and should avoid duplicate unit ids.
- Add tests for Promote: converted final stack value should be close to
  `oldStackValue + averageLiveStackValue * 0.20`.
- Add tests for Downgrade: converted final stack value should be close to
  `oldStackValue + averageLiveStackValue * 0.20`.
- Add tests that rounding to whole units stays inside the plus/minus 20 percent
  band when possible.
- Add tests that target selection chooses the closest legal candidate rather
  than a random poor candidate.
- Add tests that randomness is deterministic for a seed when two candidates are
  tied or equivalently close.
- Add tests that disabled/burned cards still happen when an operation type has
  no legal target.
- Add tests that emergency RunGold still appears only when all three normal
  operation cards are impossible.
- Existing prior art includes materialized run generation tests, Reward Map
  service tests, and offline run battle/reward database tests.
- Unity Play Mode validation should confirm the visible Reward Map card values
  match the generated operation amounts after a normal battle win.

## Out of Scope

- No changes to unit stats, costs, tiers, skills, cooldowns, damage formulas, or
  tactical battle rules.
- No broad reward family expansion beyond the current Minimal Reward Flow V1
  operation types.
- No skill rewards, recovery rewards, reroll-token rewards, or normal gold
  reward design.
- No shop economy rebalance.
- No rarity or stage scaling pass beyond preserving space for future
  multipliers.
- No full army editor or target-picking UI.
- No Unity scene, prefab, material, controller, generated asset, `.asmdef`, or
  `.asmref` edits unless a later implementation task explicitly authorizes it.
- No online backend, PlayFab, Photon, PUN, cloud sync, or matchmaking work.
- No database rebuild requirement unless a later implementation discovers that
  persisted value metadata is required.

## Further Notes

This PRD refines PRD037 rather than replacing it. PRD037 established that
rewards are materialized, persisted, and loaded by Reward Map. PRD041 defines
the missing reward-value policy so the materialized cards are fair enough to be
real choices.

The design intent is:

```text
Similar value, different strategy.
Mass and Width win on raw points.
Promote and Downgrade win on army shape.
```

The motivating bug is the late-run case where stacks are worth around 5000
points each, one card gives about +1000 value, and another card gives about
+50 value. The implementation should make that impossible under normal legal
reward generation.

## Implementation - 2026-06-24

### What Changed

- `RewardMapMaterializedGenerator`
  - Added private `BaseRewardGainRatio = 0.20f`. This affects all materialized
    reward target values. Useful range is roughly `0.10-0.35`; lower values make
    post-battle growth smaller, higher values make runs snowball faster. Tuning
    hint: keep this at `0.20` until full run length and enemy scaling are tuned.
  - Added private `RawGrowthRewardMultiplier = 1.20f`. This affects More Units
    and Add Stack rewards. Useful range is roughly `1.0-1.5`; lower values make
    raw growth closer to Promote/Downgrade, higher values make mass and width
    more point-efficient. Tuning hint: keep it above the army-shape multiplier
    so "more army" stays the best raw-value option.
  - Added private `ArmyShapeRewardMultiplier = 1.00f`. This affects Promote and
    Downgrade rewards. Useful range is roughly `0.8-1.2`; lower values make
    shape rewards more strategic and less point-efficient, higher values make
    conversions more dominant. Tuning hint: keep it at or below raw growth.
  - Changed More Units to evaluate all live stacks and add an amount closest to
    the raw-growth target instead of adding a fixed 30 percent of a random
    stack.
  - Changed Add Stack to evaluate legal non-duplicate units and pick the new
    stack amount closest to the raw-growth target.
  - Changed Promote/Downgrade to convert the whole stack while targeting
    `oldStackValue + targetGain`.
  - Replaced random target selection with closest-candidate scoring; seeded
    randomness remains only for ties.

No Inspector fields changed. No public or serialized fields were renamed or
removed.

### Automatic Test

- Added `PRD41RewardValueParityTests`.
  - `MoreUnitsAndAddStack_ScaleFromAverageLiveStackValue` checks that Mass and
    Width rewards generated from a 5000-value average army land near the 1200
    raw-growth target and that Add Stack avoids duplicate units.
  - `PromoteAndDowngrade_AddTargetValueToConvertedStack` checks that converted
    whole-stack rewards land near the 1000 army-shape target.
  - `AddStack_DoesNotReturnTinyLateRunReward` prevents the regression where a
    late-run Add Stack reward can be worth about 50 points.

Run manually in Unity: `Window > General > Test Runner > EditMode`, select
`PRD41RewardValueParityTests`, click Run. Expected result: 3 passing tests.
These tests use DTOs and a fake unit catalog only; they do not require scene or
prefab setup. Tests were not run automatically.

### Unity Test

#### Unity Setup

- No new components, GameObjects, prefabs, scenes, or Inspector assignments are
  required.
- Use the existing Offline Mode setup with Start Run, Run Map, Run Battle, and
  Reward Map wired as before.
- Use a rebuilt/reset Offline Mode database if the local DB predates PRD37.

#### Play Mode Test

- Start an Offline run.
- Grow or use a run state where current stacks are high value.
- Complete a normal battle with a win.
- Expected: Reward Map opens with materialized cards whose Add Stack and More
  Units rewards are comparable to the current average stack value target, not
  tiny early-run values.
- Click a legal reward.
- Expected: the reward applies immediately and returns to Run Map, with the
  army value change matching the visible card amount.

### QA Verdict

- Final QA verdict: Pass.
- QA report: `_codex/tasks/QA/2026-06-24_1737_041_QA_ArchitectureReview.md`.
- Actionable findings: none.
- Non-blocking observations: existing PRD37 disabled-card and RunGold fallback
  behavior was not redesigned by this focused value-parity task; preview/apply
  parity still depends on using the same unit source in generation and reward
  application.
- Follow-up fixes: none required.

### Notes

- No Unity assets, prefabs, scenes, materials, controllers, `.inputactions`,
  `.asmdef`, `.asmref`, generated Unity files, DB schema, unit stats, unit
  costs, skill effects, cooldowns, damage formulas, or tactical rules were
  changed.
- The implementation changes materialized reward operation amounts only.
- The old broad PRD37 behavior around disabled/burned cards remains a separate
  future cleanup if needed.

### Next Steps

- Run `PRD41RewardValueParityTests` in Unity Test Runner EditMode.
- Run the Play Mode smoke path: Start Run -> Run Map -> normal Battle win ->
  Reward Map -> click legal reward -> Run Map.
- During the smoke test, compare the visible reward deltas for Add Stack, More
  Units, Promote, and Downgrade on a high-value army.
