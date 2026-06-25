# [TARENA] QA Architecture Review - Task 048 Reward Generator Rule Sets

## Review Target

- Completion protocol:
  `_codex/tasks/QA/2026-06-25_0812_048_CodingAgentCompletion.md`
- Task:
  `_codex/tasks/048_PRD_RewardGeneratorRuleSets.md`

## Verdict

Pass.

The implementation follows the PRD048 ownership boundary: reward tuning is
authored in ScriptableObject assets, generated encounter difficulty resolves
both enemy and reward rules, and concrete reward materialization still happens
after battle completion using persisted/prepared battle and enemy snapshots.

## Files Reviewed

- `TArenaUnity3D/Assets/Scripts/RunMetagame/035_Generation/RewardGeneratorRuleSet.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/035_Generation/EnemyEncounterRuleCatalog.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/RewardMapMaterializedGenerator.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/022_RunBattle/OfflineRunBattleDbStore.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/030_Database/OfflineModeDatabaseComposition.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/022_RunBattle/RunBattleTacticalResultBridge.cs`
- `TArenaUnity3D/Assets/Resources/0_Data/Encounters/Mock_EnemyEncounters.asset`
- `TArenaUnity3D/Assets/Resources/0_Data/RewardRulesets/RewardRuleset_Low_Demo.asset`
- `TArenaUnity3D/Assets/Resources/0_Data/RewardRulesets/RewardRuleset_Medium_Demo.asset`
- `TArenaUnity3D/Assets/Resources/0_Data/RewardRulesets/RewardRuleset_High_Demo.asset`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/EnemyEncounterRuleCatalogTests.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/PRD40EncounterMaterializationTests.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/PRD48RewardGeneratorRuleSetTests.cs`

## Findings

No blocking or follow-up-required architecture findings.

## Checks

- Reward calculation is isolated from UI and remains in generator/domain code.
- No database schema ownership was changed; `map_node_enemies.enemy_rule_id`
  remains the bridge from materialized node difficulty to authored reward rules.
- `OfflineRunBattleDbStore` fails clearly for catalog-backed generated reward
  battles when reward rules or materialized enemy snapshot value are missing.
- Boss entries are allowed to omit reward rules, matching the current final-win
  Summary Value route.
- Reward operation planning remains separate from materialization; planned
  operation types still flow through `reward_opportunities`.
- The default encounter catalog asset now references Low/Medium/High demo
  reward rulesets.
- The tactical result bridge and production composition both resolve an
  encounter catalog for battle completion, reducing accidental fallback to the
  legacy no-ruleset reward path.
- UI/TMP rules are not implicated; this task did not touch UI text components.

## Test Coverage Review

Added/updated EditMode coverage is appropriate for the slice:

- `PRD48RewardGeneratorRuleSetTests` covers the Low/Medium/High formula,
  zero-loss clamp, enemy-value cap, pre-battle growth source, quantity/tier
  multipliers, planned operation preservation, and missing reward ruleset
  authoring failure.
- `EnemyEncounterRuleCatalogTests` now verifies generated entries resolve both
  army and reward rules.
- `PRD40EncounterMaterializationTests` fixtures now include reward rules for
  generated Low/Medium/High entries.

Tests were not run by QA because project rules reserve Unity compilation and
EditMode execution for manual Unity Editor validation.

## Residual Risk

- Unity import must validate the manually created `.asset` and `.meta` files.
- The no-ruleset materializer constructor still exists for legacy/authored
  tests and non-generated callers. This is acceptable for this slice because
  generated catalog-backed reward-producing battles now resolve rulesets and
  fail clearly when config is missing.
- Play Mode reward scale still needs visual validation across Low, Medium, and
  High battle nodes.
