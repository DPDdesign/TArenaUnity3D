# [TARENA] Coding Agent Completion - Task 048 Reward Generator Rule Sets

## Task

- `_codex/tasks/048_PRD_RewardGeneratorRuleSets.md`

## Scope

Implemented the first PRD048 slice for generated post-battle reward value tuning:

- added a `RewardGeneratorRuleSet` ScriptableObject,
- wired generated Low/Medium/High encounter rules to reward rules,
- made battle reward materialization use battle loss value, enemy army value,
  and pre-battle army value when a catalog-backed reward ruleset is present,
- created demo reward ruleset assets for Low, Medium, and High,
- added focused EditMode coverage for formula behavior and missing ruleset
  authoring failure.

## Files Changed

- `TArenaUnity3D/Assets/Scripts/RunMetagame/035_Generation/RewardGeneratorRuleSet.cs`
  - new ScriptableObject with serialized tuning fields:
    `recoveryRateFromLoss`, `recoveryRateFromEnemy`, `growthRate`,
    `quantityMultiplier`, `tierMultiplier`.
  - exposes pure calculation methods for `armyGrowthReward` and
    family-adjusted reward values.
- `TArenaUnity3D/Assets/Scripts/RunMetagame/035_Generation/EnemyEncounterRuleCatalog.cs`
  - added `RewardGeneratorRuleSet` to generated encounter rules.
  - added `MissingRewardGeneratorRuleSet` validation for generated non-Boss
    difficulties.
  - preserved predefined enemy behavior and Boss no-reward-ruleset allowance.
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/RewardMapMaterializedGenerator.cs`
  - added optional `RewardGeneratorRuleSet` plus `RewardGeneratorValueContext`.
  - normal card target values now use the PRD048 budget when the ruleset/context
    are present.
  - emergency run gold fallback derives from `armyGrowthReward` when the
    ruleset/context are present.
  - retained the legacy PRD41 ratio path only for no-ruleset legacy/test callers.
- `TArenaUnity3D/Assets/Scripts/RunMetagame/022_RunBattle/OfflineRunBattleDbStore.cs`
  - added optional enemy encounter catalog dependency.
  - resolves the reward ruleset from `map_node_enemies.enemy_rule_id` at battle
    completion.
  - calculates pre-battle army value, post-battle army value, battle loss value,
    and materialized enemy army value for reward materialization.
  - fails clearly if a catalog-backed generated reward-producing battle has no
    reward ruleset or no materialized enemy snapshot value.
- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineModeDatabaseComposition.cs`
  - resolves the active `RunGenerationSession.EnemyEncounterRuleCatalog` or the
    default `Resources/0_Data/Encounters/Mock_EnemyEncounters` catalog.
  - passes that catalog into the DB-backed run battle store.
- `TArenaUnity3D/Assets/Scripts/RunMetagame/022_RunBattle/RunBattleTacticalResultBridge.cs`
  - passes the composition-resolved encounter catalog into tactical battle
    completion persistence.
- `TArenaUnity3D/Assets/Resources/0_Data/RewardRulesets/RewardRuleset_Low_Demo.asset`
  - new demo asset: loss 1.00, enemy cap 0.45, growth 0.12, quantity 1.20,
    tier 1.00.
- `TArenaUnity3D/Assets/Resources/0_Data/RewardRulesets/RewardRuleset_Medium_Demo.asset`
  - new demo asset: loss 1.00, enemy cap 0.55, growth 0.18, quantity 1.20,
    tier 1.00.
- `TArenaUnity3D/Assets/Resources/0_Data/RewardRulesets/RewardRuleset_High_Demo.asset`
  - new demo asset: loss 0.80, enemy cap 0.65, growth 0.25, quantity 1.20,
    tier 1.00.
- `TArenaUnity3D/Assets/Resources/0_Data/Encounters/Mock_EnemyEncounters.asset`
  - assigned Low/Medium/High demo reward rulesets to the matching generated
    encounter entries.
  - Boss remains without a reward ruleset by design.
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/EnemyEncounterRuleCatalogTests.cs`
  - updated generated test rules to include reward rulesets.
  - verifies resolved generated entries expose both army and reward rules.
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/PRD40EncounterMaterializationTests.cs`
  - updated generated enemy catalog fixtures to include reward rulesets for
    Low/Medium/High.
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/PRD48RewardGeneratorRuleSetTests.cs`
  - new EditMode tests for Low/Medium/High formula output, loss clamping,
    enemy-value recovery cap, growth-from-pre-battle value, family multipliers,
    planned operation preservation, and missing reward-ruleset failure.

## Automatic Tests

Not run automatically. Project rules say Unity compilation and EditMode tests
are run manually by the user in Unity.

Recommended Unity Test Runner EditMode classes:

- `PRD48RewardGeneratorRuleSetTests`
- `EnemyEncounterRuleCatalogTests`
- `PRD40EncounterMaterializationTests`
- `PRD41RewardValueParityTests`
- `PRD42RewardMaterializedSlotContractTests`
- `PRD45RewardOpportunityMaterializationTests`
- `OfflineRunBattleRewardDbTests`

## Manual Unity Setup

1. Let Unity import the new `RewardGeneratorRuleSet.cs` script and the three
   new reward ruleset assets.
2. Open `Assets/Resources/0_Data/Encounters/Mock_EnemyEncounters.asset`.
3. Confirm Low, Medium, and High entries have their matching
   `RewardRuleset_*_Demo` asset assigned.
4. Confirm the Boss entry has no reward ruleset unless a future Boss reward
   flow is intentionally added.

## Play Mode Validation

1. Start an Offline run using the generator-backed run setup.
2. Travel to a Low, Medium, and High battle node in separate runs or controlled
   test seeds.
3. Win the battle and let battle completion route to Reward Map.
4. Confirm the Reward Map loads persisted concrete cards and does not reroll
   operation types at screen time.
5. Compare reward sizes: Medium should be larger than Low, High should be the
   largest mathematical budget, and heavy High losses should still be capped by
   enemy value plus growth.

## Known Limits

- No database schema change was made; reward rules are resolved from the
  authored encounter catalog by the persisted `enemy_rule_id` difficulty.
- The old no-ruleset materializer path remains only for legacy/authored tests
  and callers that do not have generated encounter catalog context.
- Boss reward rules remain out of scope because final wins currently route to
  Summary Value.
- Unity compilation, asset import, and Play Mode smoke testing remain manual.
