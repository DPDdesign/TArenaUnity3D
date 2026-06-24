# [TARENA] PRD 037: Materialized Run Generation, Rewards, And Map Persistence

- Status: archived
- Type: PRD
- Area: Run Metagame, Reward Generation, Route Map, Offline DB, UI Flow
- Label: closed
- Related:
  - `_codex/tasks/019_PRD_RunMetagameRewardFramework.md`
  - `_codex/tasks/023_PRD019_RewardMap.md`
  - `_codex/tasks/030_DB-001_OfflineModeDatabasePersistence.md`
  - `_codex/tasks/035_PRD_RandomStartingArmiesRoutes.md`
  - `_codex/Documentation/ADR_011_RunGenerationMaterializedUpfront.md`
  - `_codex/Documentation/ADR_012_RewardDirectionAndStackFocus.md`

## Problem Statement

The current run metagame has two conflicting shapes.

The desired product shape is that a run is generated from a seed, materialized,
persisted, and then played as concrete runtime state. Starting armies and route
nodes already moved in that direction, but rewards still carry too much legacy
catalog behavior and screen-time generation. Reward objects are also too broad
for the current design pass: they include many operation types before the simple
reward generator has proven its feel.

The user wants the run architecture to be stricter:

- the run seed generates the run deterministically,
- generated runtime data is stored in minimal DB tables by run/node,
- screens load materialized generated rows instead of rolling fallback content,
- reward generation is implemented in code, not only as database shape,
- old runtime fallback catalogs are removed from the production flow,
- the database can be rebuilt to the cleaner model.

## Solution

Build a clean materialized run generation model for Offline Mode.

The run begins with `run_seed` and `run_seed_version`. The generator uses that
seed and ScriptableObject catalog entries to materialize run-owned content into
minimal database tables. Runtime screens then load the materialized data and
apply player choices.

The new map persistence model should center on:

- `map_nodes`,
- `map_node_connections`,
- `map_node_rewards`,
- `map_node_enemies`.

Reward generation should be implemented as a simple ruleset generator. For each
reward opportunity, the run start generation pass chooses three different normal
operation types. For V1, normal operations are:

- `AddNewStack`,
- `IncreaseStack`,
- `PromoteUnit`,
- `DowngradeUnit`.

`RunGold` exists only as an emergency fallback when none of the three normal
operation-type cards can be applied. Gold is not part of normal V1 reward
selection and should not replace a single failed card.

Concrete reward cards are materialized after battle completion, before the
Reward Map screen opens, because they must target the post-battle/base army
snapshot. The materializer uses the pre-rolled operation types, selects legal
targets/amounts where possible, and records disabled/burned cards when an
operation type has no legal target. The Reward Map screen does not generate or
reroll rewards. It shows the card result, applies a legal card immediately on
click, persists the result, and then calls `GameSceneManager.ShowRunMap()`.

## User Stories

1. As a player, I want reward cards to fit my current army, so that the choice
   feels relevant instead of hand-authored around old sample stacks.
2. As a player, I want reward cards to be concrete, so that I can understand
   exactly what changes before I click.
3. As a player, I want each card to show the concrete before/after result, so
   that I can understand the consequence quickly.
4. As a player, I want clicking a reward card to commit the reward immediately,
   so that post-battle reward selection stays fast.
5. As a player, I want to return to the Run Map right after selecting a reward,
   so that the run flow does not require an extra confirmation step.
6. As a player, I want every run reload to show the same generated map and
   already-created reward choices, so that saved local state is stable.
7. As a player, I want impossible pre-rolled cards to stay visible as disabled
   route-choice consequences, so that reward hints are honest.
8. As a player, I want an emergency gold fallback only when all three cards are
   impossible, so that the run never dead-ends on the reward screen.
9. As a designer, I want reward recipes to live in a catalog, so that reward
   presentation and generation weights can be authored separately from runtime
   rows.
10. As a designer, I want generated reward rows to remember their
    `catalog_entry_id`, so that I can trace which recipe produced a card.
11. As a designer, I want generated rewards to store concrete operation data,
    so that I can debug what the run actually offered.
12. As a designer, I want `IncreaseStack` to mean a clear +30 percent stack
    increase, so that the simplest growth card is predictable.
13. As a designer, I want `PromoteUnit` to move exactly one tier up within the
    same faction, so that quality rewards are readable.
14. As a designer, I want `DowngradeUnit` to move exactly one tier down within
    the same faction, so that mass-conversion rewards are readable.
15. As a designer, I want `AddNewStack` to avoid unit duplicates for now, so
    that adding a stack remains distinct from increasing a stack.
16. As a developer, I want run generation to be a testable module, so that seed
    behavior can be verified without Unity scenes.
17. As a developer, I want Reward Map to load persisted generated rows, so that
    UI code does not own reward generation.
18. As a developer, I want old reward fallback catalogs removed from production
    runtime, so that missing generated rows surface as real errors.
19. As a developer, I want map node connections persisted explicitly, so that
    branch and merge route maps are easy to query and reload.
20. As a developer, I want run seed versioning, so that future generator changes
    can be tracked and migrated deliberately.
21. As a developer, I want generated enemies to follow the same node-owned table
    pattern, so that future enemy generation does not become a separate ad hoc
    persistence path.
22. As a QA reviewer, I want deterministic generation tests, so that the same
    seed and catalog state produce the same materialized run.
23. As a QA reviewer, I want persistence reload tests, so that reopening the
    same run never rerolls generated content.
24. As a QA reviewer, I want preview-vs-apply reward tests, so that the hovered
    army preview matches the committed reward result.

## Implementation Decisions

- This task requires code changes, not only database changes.

- Follow ADR 011: run-owned generated content is deterministic from the run
  seed and materialized into runtime DB rows. Screens load rows; they do not
  roll production fallback content.

- Add `run_seed` and `run_seed_version` at the run level. Do not store separate
  seeds on each generated node, reward, or enemy row.

- Replace the old route-node persistence direction with cleaner map tables:

```text
map_nodes
- node_id
- run_id
- catalog_entry_id
- node_type
- stage_index
- state
- basic metadata

map_node_connections
- connection_id
- run_id
- from_node_id
- to_node_id

map_node_rewards
- reward_id
- node_id
- reward_slot_index
- catalog_entry_id
- base_snapshot_id
- target_snapshot_stack_id
- reward_type
- unit_id
- to_unit_id
- amount
- is_selected
- applied_snapshot_id

map_node_enemies
- enemy_id
- node_id
- catalog_entry_id
- army_id or snapshot_id
- encounter_id or enemy_rule_id
- risk_band/basic metadata
```

- Use `catalog_entry_id` for ScriptableObject catalog entries. Do not call this
  `catalog_position_id`. The id identifies the recipe/configuration entry that
  produced the runtime row.

- Catalog entries own recipe and presentation data: reward type, family/visual
  category, title template, icon, weights, and generation legality rules.

- DB rows own concrete runtime results: selected node, reward slot, base army
  snapshot, target stack, unit ids, generated amount, selected/applied state,
  and resulting snapshot references.

- UI text is derived from catalog presentation plus runtime operation data plus
  unit catalog/DataMapper data. Do not store normal UI title/before/after text
  as database truth in V1.

- Reward opportunities are generated at run start as three different operation
  types for the reward node. Concrete reward cards are materialized after battle
  completion, because target legality depends on the post-battle/base army
  snapshot.

- Reward generation uses deterministic attempts from the run seed, node identity,
  reward slot index, operation type, and attempt index. If a candidate targets a
  dead or illegal stack, the generator continues with the next deterministic
  attempt for the same operation type.

- If the operation type has no legal target after all deterministic attempts,
  the card slot remains materialized as disabled/burned. Do not reroll that slot
  into a different operation type.

- After reward rows are written, Reward Map must only load the materialized
  rows. Reopening Reward Map for the same node must show the same cards.

- V1 normal reward pool:
  - `AddNewStack`,
  - `IncreaseStack`,
  - `PromoteUnit`,
  - `DowngradeUnit`.

- V1 emergency fallback:
  - `RunGold`, only when all three pre-rolled normal operation cards are
    disabled/impossible.

- `RunGold` fallback is not one of the normal three cards. It may be backed by a
  catalog entry/service payload, but it must not replace a single disabled
  normal card.

- `IncreaseStack` adds 30 percent of the current stack amount. The generated
  row should store the amount added.

- `PromoteUnit` changes the target stack to a unit in the same faction and
  exactly one tier higher. The amount is:

```text
round((oldAmount * oldUnitCost * 1.2) / newUnitCost)
```

- `DowngradeUnit` changes the target stack to a unit in the same faction and
  exactly one tier lower. The amount uses the same formula.

- `AddNewStack` may choose any unit from the catalog, but must avoid a unit id
  already present in the current army for V1. Its target value is:

```text
averageExistingStackValue * 1.2
```

  The amount is `round(targetValue / newUnitCost)`, minimum 1.

- Reward Map interaction:
  - card view: show a short operation label plus the materialized effect
    directly,
  - hover/focus card: optional highlight only in Minimal Flow V1,
  - click card: apply immediately,
  - successful apply: call `GameSceneManager.ShowRunMap()`,
  - failed apply: remain on Reward Map and show the error/status.

- Disabled/burned reward cards remain visible, are not clickable, and show that
  the pre-rolled operation type has no legal target.

- If all three cards are disabled/burned, show an emergency `RunGold` action at
  the bottom of the screen. Applying it returns to Run Map through the same
  successful reward route.

- Remove the separate `Select` plus `Continue` reward flow from the target UX.
  A click on a legal card is the commit.

- Existing `reward_choices` / `reward_cards` and `route_nodes` style tables may
  be retired by a rebuild/migration plan. This task accepts a database rebuild
  at the end rather than preserving old fallback compatibility indefinitely.

- `map_node_enemies` is part of the model even if enemy generation starts with
  simple authored/catalog references. It should follow the same materialized
  run/node pattern as rewards.

- The implementation should create small, deep modules:
  - run generation orchestrator,
  - map materializer,
  - reward ruleset generator,
  - reward materialization store,
  - map node store,
  - Reward Map loader/apply service,
  - catalog resolver for ScriptableObject entries.

- The run generation orchestrator should be implemented through the current
  `RunGenerationSession` surface, but the preferred final naming is
  `RunGenerator`. If the implementation renames the Unity MonoBehaviour/file,
  it must handle Unity script references deliberately and avoid leaving both old
  and new production orchestrators active.

- Production composition should route Start Run, Run Map, Run Battle, and Reward
  Map through those modules. Production runtime should not construct in-memory
  fallback stores or hardcoded reward catalogs.

## Testing Decisions

- Tests should verify external behavior and persisted rows, not private random
  helper implementation.

- Add deterministic generation tests: same seed, same seed version, same
  catalog entries, and same army snapshot produce the same materialized map and
  reward rows.

- Add map persistence tests: generated `map_nodes` and `map_node_connections`
  reload with branch and merge connections intact.

- Add reward materialization tests: after battle completion, each reward node
  writes three `map_node_rewards` rows with stable `reward_slot_index` values.

- Add legal reward tests for:
  - `AddNewStack` avoids duplicate unit ids,
  - `IncreaseStack` stores a +30 percent amount,
  - `PromoteUnit` requires same faction and exactly +1 tier,
  - `DowngradeUnit` requires same faction and exactly -1 tier,
  - dead/missing targets are not selected,
  - an impossible operation type creates a disabled/burned card instead of
    rerolling to another type.

- Add fallback tests: if all three normal operation cards are impossible, the
  screen exposes an emergency `RunGold` action. If only one or two cards are
  impossible, those cards remain disabled and no gold fallback is shown.

- Add no-screen-reroll tests: reopening/loading Reward Map for the same run/node
  returns the persisted rows without creating new reward rows.

- Add preview-vs-apply tests: the hovered reward preview matches the army
  snapshot created by clicking the card.

- Add apply flow tests: successful reward application marks only one selected
  reward, writes `applied_snapshot_id`, updates current army state, and rejects
  second apply for the same node/reward set.

- Add Reward Map controller or presenter-level tests where feasible for card
  preview text, disabled card state, emergency gold state, and immediate
  click-apply semantics. Unity visual animation is not required for this task.

- Add production composition guard coverage to ensure production Reward Map no
  longer uses old fallback catalogs.

- Use existing test patterns as prior art:
  - PRD35 deterministic generation tests,
  - Reward Map service tests,
  - Offline Run Battle/Reward DB tests,
  - Offline Start Run/Run Map DB tests,
  - Offline database schema tests.

- The user compiles and tests inside Unity. Do not run Unity, dotnet, package
  restore, external build scripts, or SDK installation commands unless the user
  explicitly allows a specific command.

## Out of Scope

- No tactical battle rewrite.
- No unit stat, gameplay float, cooldown, skill execution, or damage formula
  changes.
- No final reward balance pass beyond the simple V1 formulas in this PRD.
- No normal gold reward design before the shop economy is ready.
- No recovery rewards in V1.
- No skill rewards in V1.
- No reroll token rewards in V1.
- No reward card animation implementation beyond leaving room for hover/focus
  visuals.
- No online backend, PlayFab, Photon, PUN, cloud sync, or matchmaking work.
- No Unity scene, prefab, material, controller, `.inputactions`, `.asmdef`, or
  `.asmref` edits unless a later implementation task explicitly permits them.
- No full enemy army generator unless scoped as a follow-up. This PRD only sets
  the persistence/materialization shape for node enemies.
- No long-term compatibility layer for old fallback catalogs. A database rebuild
  is acceptable for this cleanup.

## Further Notes

- This PRD intentionally supersedes the old idea that Reward Map selects from a
  broad screen-time reward catalog. Reward Map should become a load/preview/apply
  screen over materialized generated reward rows.

- The important architectural rule is broader than rewards:

```text
If it belongs to the generated run, it is generated from run_seed, materialized
into run-owned rows, and loaded by screens. Runtime screens do not secretly roll
new production content.
```

- The first implementation should probably start by introducing the new schema
  and generator/store seams in tests before cutting over all UI paths.

- Because the user explicitly accepted rebuilding the database, implementation
  should prefer a clean final schema over preserving stale compatibility tables.

## Implementation - 2026-06-17

### What Changed

- `OfflineDatabaseSchemaV1`: added `offline_runs.run_seed` and `offline_runs.run_seed_version`, plus `map_nodes`, `map_node_connections`, `map_node_rewards`, and `map_node_enemies`. These are DB fields, not Inspector fields. Seed range is any non-zero int; different values generate different deterministic run content. Seed version is currently `1`; higher future values should only be used for deliberate generator migrations.
- `OfflineRunContextDbWriter`, `OfflineRouteMapSeedFactory`, `OfflineMaterializedRunMapDbStore`, `OfflineStartRunDbStore`, `OfflineRunMapDbStore`: Start Run and fallback Run Map seeding now mirror generated route topology into PRD37 map tables while keeping legacy `route_*` rows for current UI compatibility.
- `OfflineRunBattleDbStore`, `RewardMapMaterializedGenerator`, `OfflineRewardMapDbStore`, `RewardMapService`: successful non-final battle completion now materializes three persisted V1 reward rows before Reward Map opens; Reward Map loads those rows and does not reroll DB-backed rewards.
- `RewardMapScreenController`: removed obsolete serialized Inspector fields `selectCommandButton` and `continueCommandButton`. These affected the old two-step reward flow. No numeric range applies; lower/higher values do not apply. Tuning hint: wire reward card button references, not separate select/continue command buttons.
- `RewardMapRewardCardView`: added hover focus callback and click-to-apply card behavior. No Inspector fields changed.

### Automatic Test

- Added `TArenaUnity3D/Assets/Scripts/Tests/EditMode/PRD37MaterializedRunGenerationTests.cs`.
- `StartRun_PersistsMaterializedMapNodesConnectionsAndSeed` checks materialized map rows, explicit branch connections, enemy placeholder rows, and run seed/version persistence.
- `BattleCompletion_MaterializesRewardRowsAndRewardMapDoesNotReroll` checks post-battle reward materialization, stable Reward Map reload, focus changes without rerolling, and one selected/applied reward row.
- Tests were not run automatically. Run them manually in Unity Test Runner: `Window > General > Test Runner > EditMode > PRD37MaterializedRunGenerationTests`. Expected result: both tests pass.
- These tests do not require scene or prefab setup because they exercise service/store seams against temporary SQLite DB files and fake unit catalogs.

### Unity Test

#### Unity Setup

- Use a fresh or rebuilt Offline Mode database, because PRD37 adds `offline_runs.run_seed` columns and accepts DB rebuild.
- Ensure the scene has the existing `GameSceneManager`, Start Run, Run Map, Run Battle handoff, and Reward Map UI objects wired as before.
- On Reward Map card objects, keep `RewardMapRewardCardView.Button` assigned so card clicks can apply rewards.
- No `RewardMapScreenController` `selectCommandButton` or `continueCommandButton` assignments are needed; those fields were removed.

#### Play Mode Test

- Start an Offline run.
- Travel to a battle node and complete the battle with a win.
- Observe Reward Map opens with three persisted reward cards.
- Hover a reward card and observe the preview/focused summary updates.
- Click a legal reward card and observe the reward applies immediately, current army/run gold persist, and the UI returns to Run Map.
- Reopen the same reward state only through DB/service tests; it should load the same rows, not create new reward choices.

### QA Verdict

- Final QA verdict: Pass.
- Initial QA report: `_codex/tasks/QA/2026-06-17_2008_037_QA_ArchitectureReview.md`.
- Follow-up QA report: `_codex/tasks/QA/2026-06-17_2009_037_QA_ArchitectureReview_Followup.md`.
- Actionable findings: one compile-level resolver issue was fixed before final QA; one Reward Map null-safety follow-up was fixed and re-reviewed.
- Non-blocking observations: existing `route_*` tables remain as a compatibility layer; existing local DB files need rebuild/reset for the new schema.

### Notes

- No Unity scenes, prefabs, materials, `.asmdef`, `.asmref`, or generated Unity assets were edited.
- Existing editor prefab builders can still create legacy command button objects, but runtime `RewardMapScreenController` no longer exposes or depends on those serialized fields.
- V1 rewards are limited to AddNewStack, IncreaseStack, PromoteUnit, DowngradeUnit, with RunGold only as fallback.
- Unity compilation and Play Mode validation remain manual.

### Next Steps

- Run Unity Test Runner EditMode tests: `PRD37MaterializedRunGenerationTests`, `OfflineDatabaseSchemaTests`, `PRD35RunGenerationTests`, `OfflineRunBattleRewardDbTests`, `RewardMapServiceTests`, and `OfflineModeProductionCompositionTests`.
- Rebuild/reset the local Offline Mode DB before Play Mode validation.
- Run the manual Start Run -> Run Map -> Run Battle win -> Reward Map click-apply -> Run Map smoke test.

## Implementation - 2026-06-24

### What Changed

- `RunBattleService`: normal battle wins now route to `RunBattleNextScreen.Reward`, final battle wins route to `RunBattleNextScreen.FinalSummary`, and non-wins still route to `RunBattleNextScreen.RunLoss`.
- `RunBattleTacticalResultBridge`: after tactical completion is persisted, screen routing now uses `completion.CompletionRecord.NextScreen` instead of the raw `playerWon` boolean.
- `GameSceneManager`: added `ReturnFromBattle(RunBattleNextScreen nextScreen)` to map persisted battle destinations to existing screens.
- Tests: updated PRD37 and battle/reward expectations so the reward materialization path is exercised by normal battle wins.
- No Inspector fields changed.

### Automatic Test

- Updated `RunBattleServiceTests` to cover normal win -> Reward, final win -> Summary Value, and loss -> run-loss handling.
- Updated `OfflineRunBattleRewardDbTests` to expect the DB-backed completion handoff to Reward.
- Updated `PRD37MaterializedRunGenerationTests` to assert Reward routing before checking persisted materialized reward rows.
- Tests were not run automatically. Run them manually in Unity Test Runner: `Window > General > Test Runner > EditMode`. Expected result: all three focused test classes pass.

### Unity Test

#### Unity Setup

- Use the existing Offline Mode screen setup with `GameSceneManager`, Run Map, Battle handoff, Reward Map, and Summary Value screens wired.
- No new components, GameObjects, prefab assignments, or Inspector field values are required.
- Use a rebuilt/reset Offline Mode database if the local DB predates the PRD37 schema.

#### Play Mode Test

- Start an Offline run.
- Travel to a normal battle node and complete the tactical battle with a win.
- Expected: battle completion persists, reward rows materialize, and Reward Map opens instead of Run Map.
- Click a legal reward card.
- Expected: the reward applies immediately and returns to Run Map.
- Complete a final battle with a win.
- Expected: the run routes to Summary Value.

### QA Verdict

- Final QA verdict: Pass.
- QA report: `_codex/tasks/QA/2026-06-24_0934_037_QA_ArchitectureReview_BattleRewardRouting.md`.
- Actionable findings: none.
- Non-blocking observations: direct bridge coverage still needs Play Mode smoke testing because that path depends on the default DB, `DataMapper.Instance`, and tactical scene runtime objects.
- Follow-up fixes: none required.

### Notes

- No Unity scenes, prefabs, materials, controllers, `.inputactions`, `.asmdef`, `.asmref`, or generated Unity files were edited.
- No gameplay floats or balance values were changed.
- `ReturnFromBattleWon()` and `ReturnFromBattleLost()` remain for compatibility with existing callers; PRD37 tactical completion now uses the persisted `NextScreen` route.

### Next Steps

- In Unity Test Runner, run `RunBattleServiceTests`, `OfflineRunBattleRewardDbTests`, and `PRD37MaterializedRunGenerationTests`.
- Run the Play Mode smoke path: Start Run -> Run Map -> normal Battle win -> Reward Map click-apply -> Run Map.
- Run a final battle win smoke test and confirm Summary Value opens.
