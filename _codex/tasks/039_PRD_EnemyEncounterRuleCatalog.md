# [TARENA] PRD 039: Enemy Encounter Rule Catalog

- Status: ready-for-agent
- Type: PRD
- Area: Run Metagame, Enemy Encounters, Run Generation, Offline DB
- Label: ready-for-agent
- Related:
  - `_codex/tasks/019_PRD_RunMetagameRewardFramework.md`
  - `_codex/tasks/archive/022_PRD019_RunBattle.md`
  - `_codex/tasks/030_DB-001_OfflineModeDatabasePersistence.md`
  - `_codex/tasks/035_PRD_RandomStartingArmiesRoutes.md`
  - `_codex/tasks/037_PRD_MaterializedRunGenerationRewardsAndMapPersistence.md`

## Problem Statement

The run generator can already create battle nodes with encounter identifiers
and risk bands such as low, medium, high, and final. The database can also store
materialized enemy placeholders for generated map nodes. However, the project
does not yet have a clear authored catalog that decides which enemy army
generation rules belong to each encounter difficulty.

The current `ArmyGeneratorRuleSet` asset is used for starting army generation.
It should not become overloaded with node-type knowledge or enemy-specific
selection rules. Battle nodes should remain simple route-map data. A battle node
does not need to know which generator asset to use; it only needs a difficulty
or risk band. A separate catalog should translate that difficulty into either a
generated enemy ruleset or a predefined enemy reference.

Without this catalog, future enemy generation risks becoming ad hoc string
parsing, duplicated switch logic, or hardcoded boss handling spread across run
generation, battle preparation, and persistence code.

## Solution

Introduce an authored enemy encounter rule catalog for Offline Mode run
generation.

The catalog maps an encounter difficulty to one enemy source rule:

- if `predefinedEnemyId` is assigned, use that predefined enemy id;
- if `predefinedEnemyId` is empty, use the assigned `ArmyGeneratorRuleSet`.

The encounter difficulty set is:

1. `Low`
2. `Medium`
3. `High`
4. `Boss`

Normal battle nodes use `Low`, `Medium`, or `High`. Boss encounters can use
`predefinedEnemyId`, which overrides any ruleset and allows the ruleset
reference to be null. The predefined enemy id is intentionally one string. The
implementation does not need to distinguish whether the id points to a boss, a
boss army, or a special authored encounter; technically it resolves to an enemy
army source.

The catalog should be a Unity `ScriptableObject` so designers can assign
entries in the Inspector. It should be separate from the starting army
generation ruleset and should not depend on `RunMapNodeType`.

## User Stories

1. As a designer, I want battle encounter difficulty to be separate from node
   type, so that route shape and enemy balancing remain independent.
2. As a designer, I want low battle nodes to use a low enemy generation ruleset,
   so that early or safer fights can be tuned separately.
3. As a designer, I want medium battle nodes to use a medium enemy generation
   ruleset, so that normal fights can occupy their own balance band.
4. As a designer, I want high battle nodes to use a high enemy generation
   ruleset, so that risky route choices can produce meaningfully harder enemy
   armies.
5. As a designer, I want boss encounters to support predefined enemy ids, so
   that important encounters can be authored deliberately instead of generated
   from the normal pool.
6. As a designer, I want predefined boss entries to allow a null generator
   ruleset, so that the Inspector does not require irrelevant data.
7. As a designer, I want predefined enemy ids to be one generic string, so that
   the same field can represent a boss, special enemy army, or special encounter
   source.
8. As a designer, I want the starting army ruleset to stay dedicated to starting
   army generation, so that changes to player starts do not accidentally affect
   enemy generation.
9. As a designer, I want the enemy catalog to be a ScriptableObject, so that I
   can tune assignments without changing code.
10. As a developer, I want a small enemy difficulty enum, so that code does not
    depend on parsing arbitrary risk text everywhere.
11. As a developer, I want a single catalog lookup to resolve difficulty into an
    enemy rule, so that run generation and battle preparation do not duplicate
    selection logic.
12. As a developer, I want predefined enemy ids to override generator rulesets
    explicitly, so that boss override behavior is visible and testable.
13. As a developer, I want the catalog to avoid `RunMapNodeType` dependency, so
    that `Battle` and `FinalBoss` remain map semantics rather than enemy
    generator semantics.
14. As a developer, I want existing generated encounter ids and risk bands to
    remain compatible, so that the current run generator can evolve without a
    large route-map rewrite.
15. As a developer, I want missing or invalid catalog entries to fail clearly,
    so that bad tuning data does not silently create invalid enemy encounters.
16. As a QA reviewer, I want tests for each difficulty lookup, so that low,
    medium, high, and boss mappings are all verified.
17. As a QA reviewer, I want predefined entries tested with null rulesets, so
    that boss-style entries do not regress into requiring generated ruleset
    data.
18. As a QA reviewer, I want generated entries tested with required rulesets, so
    that ordinary battle generation cannot proceed without configuration.
19. As a future content author, I want this catalog to support more authored
    boss entries later, so that special battles can grow without changing the
    basic difficulty model.
20. As a future systems designer, I want enemy army generation to build on this
    catalog later, so that generated enemy snapshots can be materialized from a
    stable authored source.

## Implementation Decisions

- Build a dedicated enemy encounter rule catalog. Do not extend the starting
  army ruleset to know about enemies or route nodes.
- Add an `EnemyEncounterDifficulty` concept with four values: `Low`, `Medium`,
  `High`, and `Boss`.
- A rule with a non-empty predefined enemy id uses the predefined enemy id.
- A rule with an empty predefined enemy id uses its `ArmyGeneratorRuleSet`.
- A predefined enemy id overrides the generator ruleset. Its generator ruleset
  may be null and should be ignored if assigned.
- A rule with no predefined enemy id requires an `ArmyGeneratorRuleSet`.
- Use one generic predefined enemy id string. Do not split it into separate boss
  id, army id, or encounter id fields in this PRD.
- Store the catalog as a Unity `ScriptableObject` that can be authored in the
  Inspector.
- The catalog should expose a small lookup surface: given an encounter
  difficulty, return the matching rule.
- The catalog should not use `RunMapNodeType` as a key. Node type remains
  route-map semantics; encounter difficulty remains enemy-selection semantics.
- The current generated risk bands should map naturally into the new
  difficulty values: low to `Low`, medium to `Medium`, high to `High`, and final
  or boss-like encounters to `Boss`.
- Existing battle encounter data may continue to expose enemy goal and
  encounter id. This catalog is only responsible for resolving the enemy source
  rule.
- The catalog should be designed so later enemy snapshot materialization can
  consume it without changing the public authoring model.
- Do not migrate starting army generation to this catalog.
- Do not use the mock starting army ruleset as an enemy ruleset by default.
- Do not implement full enemy army generation as part of this PRD unless a child
  task explicitly expands the scope.

## Testing Decisions

- Tests should cover catalog behavior through its public lookup and validation
  behavior, not private list iteration details.
- Test that `Low`, `Medium`, and `High` generated entries return their assigned
  `ArmyGeneratorRuleSet`.
- Test that `Boss` can return a predefined entry with a null ruleset and a
  non-empty predefined enemy id.
- Test that a non-empty predefined enemy id ignores any assigned ruleset.
- Test that an empty predefined enemy id uses the assigned ruleset.
- Test that a rule with neither predefined enemy id nor ruleset reports invalid
  configuration.
- Test that missing difficulty entries are handled with a clear failure state.
- Use existing ScriptableObject catalog tests as prior art for authoring-time
  lookup behavior.
- Use existing run-map and run-battle service tests as prior art for validating
  externally visible run-metagame behavior.
- Manual Unity validation should confirm that the catalog asset can be created,
  entries can be edited in the Inspector, generated entries can reference
  `ArmyGeneratorRuleSet` assets, and predefined entries can leave the ruleset
  empty.

## Out of Scope

- Full enemy army generation.
- Materializing enemy army snapshots into the database.
- Tactical battle changes.
- Boss battle gameplay implementation.
- New boss art, skills, or balance data.
- Changing existing starting army generation.
- Changing route node type definitions.
- Changing gameplay float values or existing power bands.
- Editing Unity scenes or prefabs unless a child task explicitly requests asset
  setup.
- Replacing existing encounter id generation.
- Database schema changes beyond what is necessary for a later explicit enemy
  materialization task.

## Further Notes

- The desired mental model is: route generation decides where battles are and
  how difficult they are; the enemy encounter rule catalog decides how that
  difficulty resolves into an enemy source.
- `PredefinedEnemyId` is not a difficulty. It is a predefined source reference
  that overrides generated enemy rules for a difficulty entry, especially
  `Boss`.
- Bosses are still technically armies. The single predefined enemy id should be
  broad enough to point to whatever future boss-army source is chosen.
- This PRD intentionally keeps the authoring shape small so it can support the
  next enemy-generation step without committing to the final boss data model too
  early.

## Implementation - 2026-06-18

### What Changed

- `EnemyEncounterRuleCatalog.cs` / `EnemyEncounterRuleCatalog`: added a
  ScriptableObject catalog with `entries`. This affects enemy encounter
  authoring only; value range is 0-N entries. Lower/missing entries make lookup
  fail clearly, higher duplicated entries for the same difficulty fail as
  duplicates. Tuning hint: keep exactly one entry for each active difficulty.
- `EnemyEncounterRuleCatalog.cs` / `EnemyEncounterRule`: added Inspector fields
  `Difficulty`, `ArmyGeneratorRuleSet`, and `PredefinedEnemyId`. `Difficulty`
  selects `Low`, `Medium`, `High`, or `Boss`; non-empty `PredefinedEnemyId`
  overrides the ruleset; empty `PredefinedEnemyId` uses the assigned ruleset.
  Tuning hint: set only rulesets for Low/Medium/High and set only
  `PredefinedEnemyId` for authored Boss entries.
- `EnemyEncounterRuleCatalog.cs`: added lookup/result enums and result object:
  `EnemyEncounterDifficulty`, `EnemyEncounterRuleLookupError`, and
  `EnemyEncounterRuleLookupResult`. These affect code consumers, not Inspector
  wiring.
- Removed fields: `Mode` / `EnemyEncounterResolutionMode`.

### Automatic Test

- Added `EnemyEncounterRuleCatalogTests`.
- Tests check Low/Medium/High rules resolving assigned rulesets, Boss resolving
  from a non-empty predefined id with null ruleset, predefined ids ignoring
  assigned rulesets, empty predefined ids falling back to rulesets, entries with
  neither predefined id nor ruleset failing, and missing/duplicate entries
  failing clearly.
- These tests create ScriptableObjects in memory and do not require scene,
  prefab, Resources, or database setup.
- Run manually in Unity: `Window > General > Test Runner > EditMode`, then run
  `EnemyEncounterRuleCatalogTests`. Expected result: all tests pass.
- I did not run Unity tests automatically per project rule.

### Unity Test

#### Unity Setup

- Let Unity recompile/import scripts.
- Create a catalog asset from `Assets > Create > TArena > Run Metagame > Enemy
  Encounter Rule Catalog`.
- Add entries for `Low`, `Medium`, `High`, and `Boss`.
- For Low/Medium/High, leave `PredefinedEnemyId` empty and assign an
  `ArmyGeneratorRuleSet`.
- For Boss, leave `ArmyGeneratorRuleSet` empty and set `PredefinedEnemyId` to a
  non-empty enemy id string.

#### Play Mode Test

- No Play Mode runtime behavior is expected yet, because this task only adds
  the catalog contract and does not wire it into run generation or battle
  preparation.
- After pressing Play, existing Start Run, Run Map, and Run Battle behavior
  should remain unchanged.

### QA Verdict

- Final QA verdict: Pass.
- QA report: `_codex/tasks/QA/2026-06-18_039_QA_ArchitectureReview.md`.
- Actionable findings: none.
- Non-blocking observations: a later integration task should map existing
  risk-band strings (`low`, `medium`, `high`, `final`) to
  `EnemyEncounterDifficulty`; a later asset-authoring task may create real
  catalog assets.
- Follow-up fixes applied: none needed.

### Notes

- No Unity assets, prefabs, scenes, `.asmdef`, `.asmref`, or database schema
  files were edited.
- The catalog is intentionally not wired into enemy army generation,
  `map_node_enemies`, or `RunBattleService` yet.
- `Mock_ArmyGeneratorRuleSet` remains a starting-army mock asset and was not
  reused as an enemy ruleset by default.

### Next Steps

- Run `EnemyEncounterRuleCatalogTests` manually in Unity EditMode Test Runner.
- Create a manual catalog asset in Unity and verify Inspector authoring.
- Use a future task to consume the catalog from run generation and materialize
  enemy army data.

## Fix - 2026-06-18

- Removed the explicit generated/predefined mode field after user feedback.
- New rule: non-empty `PredefinedEnemyId` wins; empty `PredefinedEnemyId` means
  use `ArmyGeneratorRuleSet`.
- Updated `EnemyEncounterRuleCatalogTests` to cover the simplified contract.
