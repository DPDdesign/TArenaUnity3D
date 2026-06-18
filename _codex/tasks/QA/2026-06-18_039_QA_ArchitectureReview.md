# [TARENA] QA Architecture Review: PRD 039 Enemy Encounter Rule Catalog

- Task: `_codex/tasks/039_PRD_EnemyEncounterRuleCatalog.md`
- Protocol: `_codex/tasks/QA/2026-06-18_039_CodingCompletion_EnemyEncounterRuleCatalog.md`
- Reviewed File:
  - `TArenaUnity3D/Assets/Scripts/RunMetagame/035_Generation/EnemyEncounterRuleCatalog.cs`
- Verdict: Pass
- Follow-up Required: No

## Summary

The implementation matches PRD 039's intended scope. It adds a dedicated
ScriptableObject catalog for resolving enemy encounter difficulty into either a
generated ruleset or a predefined enemy id. It does not wire the catalog into
run generation, battle preparation, database persistence, scenes, prefabs, or
Unity assets, which is correct for this PRD.

## Architecture Review

### Ownership

The new catalog lives in `RunMetagame/035_Generation`, next to the current
starting army and route generation code. This is the correct ownership area for
an authored run-generation support catalog.

The implementation does not modify `ArmyGeneratorRuleSet`, preserving the
starting-army ruleset as a reusable configuration object rather than making it
aware of node types or enemy selection.

### Separation From Node Types

The new model uses `EnemyEncounterDifficulty` as the lookup key and does not
reference `RunMapNodeType`. This matches the user decision that battle nodes
should carry difficulty/risk semantics, while the catalog decides which enemy
source rule to use.

### Generated And Predefined Resolution

The model resolves source by data presence:

- non-empty `PredefinedEnemyId` uses predefined enemy source,
- empty `PredefinedEnemyId` uses `ArmyGeneratorRuleSet`.

`EnemyEncounterRule.ResolvedArmyGeneratorRuleSet` returns null when
`PredefinedEnemyId` is non-empty, so predefined source correctly overrides
generation even if a ruleset is assigned in the Inspector.

### Failure States

The lookup result exposes clear failure states for:

- missing entries,
- duplicate entries,
- entries missing both predefined id and ruleset.

Duplicate difficulty entries failing resolution is a useful authoring guard and
does not conflict with the PRD.

### Scope Control

The implementation correctly avoids:

- enemy army generation,
- enemy snapshot materialization,
- schema changes,
- Start Run or Run Battle wiring,
- prefab/scene/asset edits.

## Findings

No actionable findings.

## Non-Blocking Observations

- A later integration task will need a small adapter or helper that translates
  existing risk-band strings such as `low`, `medium`, `high`, and `final` into
  `EnemyEncounterDifficulty`. Keeping that out of this PRD is acceptable because
  this task only introduces the catalog contract.
- A later asset-authoring task may create real Low/Medium/High/Boss catalog
  assets, but this implementation correctly avoids Unity asset edits without
  explicit permission.

## Test Recommendations

Post-QA EditMode tests should cover:

- generated Low/Medium/High rules resolving assigned rulesets,
- predefined Boss resolving with null ruleset and non-empty id,
- predefined id ignoring assigned rulesets,
- empty predefined id using assigned rulesets,
- entries with neither predefined id nor ruleset failing,
- missing and duplicate difficulty entries failing clearly.
