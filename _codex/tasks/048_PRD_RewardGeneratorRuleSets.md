# [TARENA] PRD 048: Reward Generator Rule Sets

- Status: ready-for-agent
- Type: PRD
- Area: Run Metagame, Reward Generation, Enemy Encounters, Offline Mode
- Label: ready-for-agent
- Related:
  - `019_PRD_RunMetagameRewardFramework`
  - `023_PRD019_RewardMap`
  - `035_PRD_RandomStartingArmiesRoutes`
  - `039_PRD_EnemyEncounterRuleCatalog`
  - `040_PRD_FullEncounterMaterializationBattleLaunchLoop`
  - `041_PRD_RewardValueParityScaling`
  - `Reward_Design`
  - `18_Game_Difficulty`

## Problem Statement

Post-battle reward value is currently hardcoded inside the materialized reward
generator. The current value policy uses a small percentage of average live
stack value, which does not fit TArena's run attrition model.

In TArena, army value is the run's HP-like attrition resource. A battle reduces
the same resource that also represents the player's build and final saved army
candidate. In games such as Slay the Spire and Monster Train, combat damage
mostly hits a separate HP-like resource while rewards improve the deck or run
engine. TArena needs reward math that rebuilds battle losses and still grows
the army.

The player-facing problem is that winning a battle can still feel like the army
shrank, because the reward is too small compared to actual losses. The demo
slice needs the player to feel that "the army grows" without weakening tactical
AI.

## Solution

Introduce a `RewardGeneratorRuleSet` ScriptableObject that controls post-battle
reward value by encounter difficulty.

Each generated entry in the enemy encounter rules should be able to point to:

- the enemy `ArmyGeneratorRuleSet`,
- the reward `RewardGeneratorRuleSet`.

The node still owns `EncounterDifficulty`. The catalog resolves that difficulty
into the enemy generation rules and the reward generation rules.

Reward operation types remain planned at run generation time through existing
reward opportunities. The new rule set only changes the reward value used when
those planned operations are materialized after battle completion.

The reward value formula is:

```text
armyGrowthReward =
    min(battleLossValue * recoveryRateFromLoss,
        enemyArmyValue * recoveryRateFromEnemy)
    + armyValueBeforeBattle * growthRate
```

Field meanings:

- `armyGrowthReward`: base value budget for the materialized reward.
- `battleLossValue`: value lost by the player in this battle.
- `recoveryRateFromLoss`: how much of the actual loss can be recovered.
- `enemyArmyValue`: actual value of the materialized enemy army.
- `recoveryRateFromEnemy`: cap for recovery, based on enemy army value.
- `armyValueBeforeBattle`: player army value immediately before this battle.
- `growthRate`: additional growth based on the pre-battle player army value.

Reward family multipliers are then applied:

```text
Quantity rewards:
finalRewardValue = armyGrowthReward * quantityMultiplier

Tier rewards:
finalRewardValue = armyGrowthReward * tierMultiplier
```

Quantity rewards are:

- `AddUnits`
- `AddStack`

Tier rewards are:

- `PromoteStack`
- `DowngradeStack`

Gold fallback should be derived from `armyGrowthReward` converted to run gold.
It should not be a separate fixed fallback amount in the new rule set.

Create initial demo assets under:

```text
Assets/Resources/0_Data/RewardRulesets/
```

Initial assets:

- `RewardRuleset_Low_Demo`
- `RewardRuleset_Medium_Demo`
- `RewardRuleset_High_Demo`

Initial values:

```text
Low:
recoveryRateFromLoss = 1.00
recoveryRateFromEnemy = 0.45
growthRate = 0.12
quantityMultiplier = 1.20
tierMultiplier = 1.00

Medium:
recoveryRateFromLoss = 1.00
recoveryRateFromEnemy = 0.55
growthRate = 0.18
quantityMultiplier = 1.20
tierMultiplier = 1.00

High:
recoveryRateFromLoss = 0.80
recoveryRateFromEnemy = 0.65
growthRate = 0.25
quantityMultiplier = 1.20
tierMultiplier = 1.00
```

Boss reward rules are not part of the required first slice because final battle
wins currently route to summary, not Reward Map. If a future generated Boss
entry produces a normal Reward Map choice, it must receive a reward rule set at
that time.

## User Stories

1. As a player, I want my army to feel larger after a won battle and reward, so
   that the run has visible momentum.
2. As a player, I want a reward to account for units lost in the battle, so that
   winning does not feel like losing my build.
3. As a player, I want Low fights to usually leave my army clearly ahead, so
   that safe battles feel like reliable growth.
4. As a player, I want Medium fights to give stronger rewards than Low fights,
   so that normal route pressure has a meaningful payoff.
5. As a player, I want High fights to give the largest mathematical reward, so
   that risky route choices can be worth taking.
6. As a player, I want High fights to still hurt when losses are large, so that
   high-risk nodes do not become free farming.
7. As a player, I want reward cards to remain concrete before/after outcomes,
   so that I do not need to understand the hidden formula.
8. As a player, I want AddUnits and AddStack rewards to visibly add army mass,
   so that quantity rewards sell the army-growth fantasy.
9. As a player, I want Promote and Downgrade rewards to reshape army value, so
   that tier rewards feel distinct from raw quantity.
10. As a designer, I want reward growth values in ScriptableObject assets, so
    that demo tuning does not require code edits.
11. As a designer, I want reward rules attached to encounter difficulty, so
    that Low, Medium, and High nodes can be tuned together with enemy armies.
12. As a designer, I want one formula across difficulties, so that balance is
    readable and comparable.
13. As a designer, I want recovery to be capped from enemy value, so that
    extreme player losses do not create unlimited reward spikes.
14. As a designer, I want growth to scale from pre-battle army value, so that
    late-run rewards stay meaningful.
15. As a designer, I want reward operation types to stay planned at run
    generation, so that Reward Map does not reroll card type identity.
16. As a developer, I want the enemy encounter catalog to resolve reward rule
    sets, so that node difficulty remains the single difficulty input.
17. As a developer, I want missing reward rule configuration to fail clearly,
    so that the game does not silently fall back to hardcoded tuning.
18. As a developer, I want the reward calculation isolated from UI, so that
    Reward Map only displays materialized results.
19. As a developer, I want the formula to use persisted/prepared battle
    snapshots, so that reloads and DB-backed flows stay deterministic.
20. As a QA reviewer, I want focused tests for Low, Medium, and High formulas,
    so that demo reward balance cannot drift unnoticed.
21. As a QA reviewer, I want tests for missing reward rule sets, so that bad
    catalog authoring fails before Play Mode balance testing.
22. As a future online-mode developer, I want reward value to remain
    materialized after battle completion, so that server-authoritative reward
    generation can validate the same inputs later.

## Implementation Decisions

- Add a `RewardGeneratorRuleSet` ScriptableObject for reward value tuning.
- The rule set owns exactly these tuning fields for the first slice:
  `recoveryRateFromLoss`, `recoveryRateFromEnemy`, `growthRate`,
  `quantityMultiplier`, and `tierMultiplier`.
- Do not add a fixed `fallbackGoldAmount`. Gold fallback derives from
  `armyGrowthReward` converted to run gold.
- Extend enemy encounter rule entries so a generated difficulty entry can
  reference a reward rule set alongside the enemy army rule set.
- For the first slice, reward rule lookup is 1:1 with encounter difficulty:
  Low uses the Low reward rule set, Medium uses Medium, and High uses High.
- The current route node remains the difficulty authority through
  `EncounterDifficulty`.
- Run generation continues to choose and persist planned reward operation types.
  The new reward rule set must not reroll operation types.
- Battle completion is the point where concrete reward value is calculated.
- `battleLossValue` is calculated as the non-negative difference between the
  player's pre-battle army value and post-battle army value.
- `armyValueBeforeBattle` is the player's army snapshot value immediately
  before the battle, not the starting army value from the beginning of the run.
- `enemyArmyValue` is calculated from the actual materialized enemy snapshot for
  the node, not from the difficulty name or ruleset target.
- Quantity operations use `quantityMultiplier`.
- Tier operations use `tierMultiplier`.
- Whole-unit rounding and nearest-legal-candidate selection should continue to
  follow the existing materialized reward generator behavior.
- Missing reward rule configuration for a generated reward-producing battle
  entry should fail clearly instead of falling back to the old hardcoded reward
  ratios.
- Create initial demo ScriptableObject assets in
  `Assets/Resources/0_Data/RewardRulesets`.
- The PRD authorizes creating the new reward ruleset assets and the minimum
  ScriptableObject type needed for them. It does not authorize unrelated Unity
  asset, prefab, scene, material, controller, `.asmdef`, or `.asmref` edits.
- Preserve PRD030 persistence ownership: Reward Map loads materialized rows and
  should not generate new fallback rewards at screen time.

## Testing Decisions

- Good tests should verify observable reward generation behavior through the
  generator/service/store seams, not private helper internals.
- Add a pure formula test for the Low demo values.
- Add a pure formula test for the Medium demo values.
- Add a pure formula test for the High demo values.
- Add a test that `battleLossValue` clamps to zero when post-battle value is not
  lower than pre-battle value.
- Add a test that recovery is capped by `enemyArmyValue * recoveryRateFromEnemy`.
- Add a test that growth uses pre-battle army value, not run-start army value.
- Add tests that Quantity reward operations apply `quantityMultiplier`.
- Add tests that Tier reward operations apply `tierMultiplier`.
- Add a test that generated reward-producing entries without reward rule sets
  fail clearly.
- Add a test that planned reward operation types are still respected after the
  reward rule set changes the value budget.
- Add or update materialized reward tests so existing AddUnits, AddStack,
  PromoteStack, and DowngradeStack candidates still round to legal whole-unit
  results.
- Prior art includes `PRD41RewardValueParityTests`,
  `PRD42RewardMaterializedSlotContractTests`,
  `PRD45RewardOpportunityMaterializationTests`,
  `EnemyEncounterRuleCatalogTests`, and
  `PRD40EncounterMaterializationTests`.
- Unity Play Mode validation should confirm Start Run -> Run Map -> battle win
  -> Reward Map produces visible reward values in the intended Low/Medium/High
  scale.

## Out of Scope

- No tactical AI weakening or battle rule changes.
- No unit stat, unit cost, tier, skill, cooldown, damage formula, or combat
  float changes.
- No route topology redesign.
- No reward operation type rerolling at Reward Map screen time.
- No recovery reward family implementation beyond the mathematical recovery
  component inside reward value.
- No skill reward, rarity, stage scaling, shop economy, or account progression
  redesign.
- No full army editor or manual reward-target selection UI.
- No database schema changes unless implementation proves persisted reward-rule
  identity is required for reload/debug correctness.
- No Boss reward ruleset requirement in this first slice unless Boss starts
  producing normal Reward Map rewards.
- No online backend, PlayFab, Photon, PUN, cloud sync, or matchmaking work.

## Further Notes

This PRD supersedes the hardcoded reward growth ratios from PRD041 for the
current demo balance pass. PRD041 remains valid for the idea that materialized
cards should use comparable value budgets and nearest legal candidate selection.

The core design statement is:

```text
In TArena run battles, current army value is the run's HP-like attrition
resource. Post-battle rewards must recover part of that attrition and then add
growth.
```

Reference mental model:

```text
Slay the Spire:
HP loss + deck/card/relic reward

Monster Train:
Pyre HP loss + deck/unit/card upgrades

TArena:
army value loss + army reward
```

The first demo tuning target is:

```text
Low = safe visible growth
Medium = stronger growth
High = biggest mathematical reward, but not full forgiveness for heavy losses
```

Manual setup after implementation should include assigning the Low, Medium, and
High reward ruleset assets to the matching generated entries in the enemy
encounter rule catalog used by `RunGenerationSession`.

## Implementation - 2026-06-25

### What Changed

- `RewardGeneratorRuleSet`: added a ScriptableObject with `recoveryRateFromLoss`, `recoveryRateFromEnemy`, `growthRate`, `quantityMultiplier`, and `tierMultiplier`. These affect post-battle reward value budgets; useful values are `0+`, lower values reduce recovery/growth, higher values increase reward size, and tuning should compare Low/Medium/High against real battle losses.
- `EnemyEncounterRuleCatalog` / `EnemyEncounterRule`: added `RewardGeneratorRuleSet` per generated encounter entry. Low/Medium/High generated entries must assign one; null fails clearly, while Boss may stay null because final wins route to Summary Value.
- `RewardMapMaterializedGenerator`: uses the PRD048 budget when a ruleset/context is provided, while preserving planned operation types and whole-unit candidate selection.
- `OfflineRunBattleDbStore`: resolves reward rules from materialized encounter difficulty, computes battle loss from pre/post snapshots, reads actual materialized enemy value, and passes that context into reward materialization.
- `OfflineModeDatabaseComposition` and `RunBattleTacticalResultBridge`: pass the active or default encounter catalog into battle completion.
- Assets: added `RewardRuleset_Low_Demo`, `RewardRuleset_Medium_Demo`, and `RewardRuleset_High_Demo`; assigned them to `Mock_EnemyEncounters`. No fields were removed or renamed.

### Automatic Test

- Added `PRD48RewardGeneratorRuleSetTests` for Low/Medium/High formula output, zero-loss clamp, enemy-value cap, pre-battle growth source, quantity/tier multipliers, planned operation preservation, and missing reward-ruleset failure.
- Updated `EnemyEncounterRuleCatalogTests` and `PRD40EncounterMaterializationTests` fixtures for reward rulesets.
- Tests were not run automatically; run them manually in Unity Test Runner under EditMode. Expected result: the listed PRD48, encounter catalog, PRD40, PRD41, PRD42, PRD45, and `OfflineRunBattleRewardDbTests` classes pass.

### Unity Test

#### Unity Setup

- Let Unity import `RewardGeneratorRuleSet.cs` and the three new assets under `Assets/Resources/0_Data/RewardRulesets/`.
- Open `Assets/Resources/0_Data/Encounters/Mock_EnemyEncounters.asset`.
- Confirm Low uses `RewardRuleset_Low_Demo`, Medium uses `RewardRuleset_Medium_Demo`, High uses `RewardRuleset_High_Demo`, and Boss remains unassigned.
- Open Unity Test Runner, select EditMode, and select `PRD48RewardGeneratorRuleSetTests`, `EnemyEncounterRuleCatalogTests`, `PRD40EncounterMaterializationTests`, `PRD41RewardValueParityTests`, `PRD42RewardMaterializedSlotContractTests`, `PRD45RewardOpportunityMaterializationTests`, and `OfflineRunBattleRewardDbTests`.

#### Play Mode Test

- Start an Offline generator-backed run, travel to a Low battle, win, and confirm Reward Map shows persisted concrete cards.
- Repeat for Medium and High battle nodes.
- Expected: Reward Map does not reroll card operation types at screen time; Medium rewards are larger than Low, High has the largest mathematical budget, and heavy High losses remain capped by enemy value plus growth.

### QA Verdict

- Pass.
- QA report: `_codex/tasks/QA/2026-06-25_0813_048_QA_ArchitectureReview.md`.
- Actionable findings: none.
- Non-blocking observations: Unity import and Play Mode reward-scale validation are still manual; legacy no-ruleset materializer path remains for non-generated callers.
- Follow-up fixes applied: one pre-QA fix added default encounter catalog resolution when no active `RunGenerationSession` is present.

### Notes

- No database schema change was made; `map_node_enemies.enemy_rule_id` remains the difficulty bridge to authored reward rules.
- Gold fallback now derives from `armyGrowthReward` when ruleset context is present.
- Boss reward rules stay out of scope for this slice.
- Unity compilation, asset import, EditMode tests, and Play Mode smoke tests were not run here.

### Next Steps

- In Unity, run the listed EditMode test classes.
- In Play Mode, validate Start Run -> Run Map -> battle win -> Reward Map for Low, Medium, and High nodes.
- If Unity reports missing script/asset references on the new reward rulesets, reimport the `RewardRulesets` folder and re-check `Mock_EnemyEncounters`.
