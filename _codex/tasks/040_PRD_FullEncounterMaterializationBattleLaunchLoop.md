# [TARENA] PRD 040: Full Encounter Materialization And Battle Launch Loop

Status: ready-for-agent
Type: PRD
Area: Run Metagame, Enemy Encounters, Run Battle, Offline DB, Battle Scene Bridge
Labels: ready-for-agent
Related: PRD019, PRD022, PRD030, PRD035, PRD037, PRD039

## Problem Statement

The run map can represent battle nodes, and PRD039 introduced the enemy encounter rule catalog contract, but the run does not yet fully materialize enemy armies for battle nodes or launch battle from a clicked encounter node.

At the gameplay level, a generated run should already know what enemy army is attached to every battle node on the map. Clicking a battle node should advance the run position, prepare the battle payload, bridge the selected player and enemy army snapshots into the current battle scene input format, enter battle, and return to the run metagame after battle completion.

The current system still has gaps:

- battle nodes are not guaranteed to have generated enemy army snapshots at run start;
- enemy generation is not wired through the same generation method as starting armies;
- battle node difficulty is not yet an explicit node field;
- `GameSceneManager` can enter battle, but does not receive a fully prepared battle context;
- the battle scene still expects legacy army input surfaces, while the run metagame source of truth should be runtime army snapshots;
- the full win/loss return loop from battle back to Run Map, Reward, or Summary needs to be treated as one end-to-end encounter flow.

## Solution

Implement full encounter materialization and battle launch for battle nodes.

At run creation time, materialize an enemy army snapshot for every battle and final boss node on the generated map. This includes unchosen branches. Enemy armies use the exact same army generation path as starting armies, but with the rule set selected by the enemy encounter catalog instead of the starting army rule set.

Each battle-capable node must carry an explicit difficulty value. That difficulty chooses the catalog entry for the node. The catalog can either provide a generated army rule set or a predefined enemy id. For this PRD, generated rule sets are the primary path. Predefined ids remain part of the contract, but because army definitions do not exist yet, they should either be reserved without runtime use or fail clearly if encountered.

When the player clicks a battle node in Run Map, the flow must run in this order:

1. validate and persist travel to the clicked node;
2. prepare battle using the already materialized player and enemy army snapshots;
3. bridge the snapshot payload into the current battle scene input format;
4. call `GameSceneManager.EnterBattle()`;
5. after battle completion, call the existing run battle completion path;
6. route back to the correct run metagame screen.

`GameSceneManager` should only switch UI/scene state. It must not generate enemies, select encounter definitions, or own run progression decisions.

## User Stories

- As a player, when I start a run, every battle node on the generated map already has a deterministic enemy army attached to it.
- As a player, when I choose a branch later, the enemy army on that branch is the same one that was generated at run start.
- As a player, when I click a battle node, the run first moves me to that node and then starts the correct battle.
- As a player, when I win a normal battle, I return to the reward flow and then can continue the run map loop.
- As a player, when I win the final boss battle, I go to the run summary victory flow.
- As a player, when I lose a battle, I go to the run loss or summary loss flow.
- As a designer, I can set battle node difficulty explicitly on the node data without relying on parsing an encounter id string.
- As a designer, I can map node difficulty to an enemy `ArmyGeneratorRuleSet`.
- As a developer, I can keep the current battle scene alive through a bridge while moving the run metagame source of truth to army snapshots.

## Implementation Decisions

- The whole generated map is materialized at run start.
- Enemy armies are generated for all battle and final boss nodes at run start, including branches the player may never choose.
- Enemy army generation must call the same generation method used by starting armies. The only difference is the selected `ArmyGeneratorRuleSet`.
- Enemy generation uses the full unit/skill pool allowed by the selected rule set. It must not be filtered by player account unlocks.
- Player and enemy armies use the same army snapshot model. A boss is still technically an army, even if future data gives it special composition.
- Generated enemy snapshots are persisted and linked from `map_node_enemies.army_snapshot_id`.
- Battle node difficulty is an explicit node field. Do not infer primary behavior from `encounterId`. Legacy `encounterId` parsing may exist only as migration or compatibility fallback.
- `EnemyEncounterRuleCatalog` remains the rule lookup boundary from node difficulty to either:
  - generated `ArmyGeneratorRuleSet`;
  - or `PredefinedEnemyId`.
- There is no separate generated/predefined mode field. If `PredefinedEnemyId` is present, it wins. If it is absent, use the generator rule set.
- Because army definitions are not implemented yet, predefined encounter ids should not silently succeed. They should either be unreachable in seeded data for this PRD or return a clear unsupported/predefined-not-resolved result.
- Run Map click flow must do travel first, then battle preparation. Entering the battle scene before persisted travel is not allowed.
- `GameSceneManager` is only an entry/scene transition coordinator. Battle context preparation belongs in run metagame services/adapters.
- Add an ADR documenting the transitional `army_snapshot -> legacy battle input` bridge. The ADR should state that PlayerPrefs/build-file-style input is an adapter surface for the current battle scene, not the run metagame source of truth.
- The bridge should be replaceable later by direct runtime snapshot consumption in the battle scene.
- Battle completion should continue through the existing `RunBattleService.CompleteBattle` style path where possible, so reward/summary routing remains owned by the run metagame.

## Functional Requirements

- Materialized run creation creates enemy army snapshots for every `Battle` and `FinalBoss` node.
- Materialized run creation stores the generated snapshot id in `map_node_enemies.army_snapshot_id`.
- Materialized run creation stores enough node difficulty data to reproduce which catalog rule was selected.
- The enemy catalog lookup accepts explicit node difficulty.
- The generated army path is deterministic for the same run seed, node identity, difficulty, and rule set.
- The generated enemy army path does not depend on player unlock state.
- Run Map node click validates that the clicked node is reachable before preparing battle.
- Run Map node click persists current node travel before preparing battle.
- Battle preparation reads the enemy army snapshot attached to the current node, not a newly generated enemy.
- Battle preparation reads the current player army snapshot.
- Battle launch bridge writes or exposes both player and enemy armies to the existing battle scene input format.
- `GameSceneManager.EnterBattle()` is called only after travel and battle preparation succeed.
- Battle completion reports result back into the run metagame.
- Normal battle victory routes to reward.
- Final boss victory routes to summary victory.
- Battle loss routes to run loss or summary loss according to existing run metagame conventions.
- Reloading a materialized run preserves battle node enemy assignments.

## Non-Functional Requirements

- Keep generation deterministic and testable without loading Unity scenes.
- Keep the new encounter flow behind service/adapter seams already used by the run metagame.
- Avoid direct battle-scene dependencies inside map generation.
- Avoid adding ScriptableObject node collections in this PRD. The explicit node field should be compatible with that future direction.
- Avoid large rewrites of the battle scene. The bridge is an intentional transitional layer.

## Testing Decisions

- Add edit-mode coverage for deterministic enemy army materialization across all battle/final nodes.
- Test that unchosen branch battle nodes still receive enemy army snapshots at run start.
- Test that `map_node_enemies.army_snapshot_id` is populated for battle/final nodes and not required for non-battle nodes.
- Test that explicit node difficulty selects the expected `ArmyGeneratorRuleSet`.
- Test that enemy generation does not use player unlock filtering.
- Test that predefined ids currently fail clearly or remain unreachable, depending on seeded data choice.
- Test Run Map battle click ordering: travel succeeds first, then prepare battle, then enter battle.
- Test that prepare battle uses the persisted node enemy snapshot instead of generating a new one.
- Test normal battle win routing to reward.
- Test final boss win routing to summary victory.
- Test battle loss routing to loss/summary loss.
- Test reload behavior: a saved run keeps the same enemy snapshot ids for its nodes.
- Add a manual Unity validation checklist for the full loop: start run, click battle node, enter battle, resolve battle, return to metagame, continue.

## Out Of Scope

- Authoring real predefined boss army definitions.
- Creating node ScriptableObject collections.
- Replacing the battle scene internals with direct runtime snapshot consumption.
- Redesigning battle gameplay, combat resolution, or enemy AI.
- Adding account unlock filtering for enemies.
- Changing gameplay balance values inside existing rule sets unless explicitly approved.
- Editing Unity scenes, prefabs, assets, or ScriptableObjects unless a later implementation task explicitly permits it.

## Further Notes

- This PRD intentionally treats encounter implementation as a full loop, not only as catalog lookup or scene entry.
- The catalog from PRD039 is the intended rule lookup boundary, but the materialized node should own explicit difficulty.
- Future boss support can reuse `PredefinedEnemyId` once army definitions exist. Until then, generated final boss encounters may use a boss/high difficulty rule set or a clearly unsupported predefined path, depending on implementation task scope.
- The bridge ADR is important because it prevents legacy battle scene input mechanics from becoming the long-term architecture.

## Implementation - 2026-06-18

### What Changed

- `RunGenerationSession`: added serialized `enemyEncounterRuleCatalog`. This affects Start Run enemy materialization. Useful value is one assigned `EnemyEncounterRuleCatalog` with `Low`, `Medium`, `High`, and `Boss` generated entries; unassigned fails Start Run clearly in the production path. This is not numeric, so there is no lower/higher Play Mode tuning; tune by assigning different rule sets per difficulty.
- `RunMapNodeDefinition` / seed models: added explicit `EncounterDifficulty` for battle nodes. Generated route nodes now set Low/Medium/High/Boss directly instead of relying on encounter id parsing.
- `EnemyEncounterArmyMaterializer`: added enemy army snapshot generation using the same `DeterministicRunGenerationCatalog` path as starting armies, with no account unlock filtering.
- `OfflineMaterializedRunMapDbStore`: battle/final nodes now persist generated enemy snapshots and write `map_node_enemies.army_snapshot_id`.
- `OfflineStartRunDbStore` and schema: restored/seeded `route_maps`, `route_paths`, and `route_nodes` alongside `map_nodes` so current Run Map persistence keeps working with aligned node ids.
- `OfflineRunBattleEncounterCatalog`: added DB-backed encounter source that returns the materialized `snapshot-*` as enemy army source.
- `RunMapController`: battle/final click now performs travel first, prepares battle second, then calls `GameSceneManager.EnterBattle()`.
- ADR added: `_codex/Documentation/ADR_040_ArmySnapshotToLegacyBattleInputBridge.md`.

### Automatic Test

- Added `TArenaUnity3D/Assets/Scripts/Tests/EditMode/PRD40EncounterMaterializationTests.cs`.
- Tests cover: all battle/final nodes receive enemy snapshot ids at Start Run; DB-backed encounter source returns `snapshot-*`; missing enemy catalog fails clearly when an enemy unit source is provided.
- Tests are deterministic DB/service tests and do not require scene or prefab setup.
- Tests were not run automatically. Run them manually in Unity Test Runner: `Window > General > Test Runner > EditMode > PRD40EncounterMaterializationTests`. Expected result: all tests pass.

### Unity Test

#### Unity Setup

- On the scene object with `RunGenerationSession`, assign `Starting Army Rule Set`.
- On the same `RunGenerationSession`, assign `Enemy Encounter Rule Catalog`.
- The enemy catalog must contain generated entries for `Low`, `Medium`, `High`, and `Boss`; each entry should reference an `ArmyGeneratorRuleSet`.
- Ensure existing Run Map UI still has its `GameSceneManager` and `RunMapController` references wired as before.

#### Play Mode Test

- Start Offline Mode and begin a run.
- Open Run Map and click an available battle node.
- Expected: current node is persisted first, battle is prepared, then battle scene entry is triggered.
- Complete battle through the existing completion path.
- Expected: normal win routes to Reward, final win routes to Summary, loss routes to run loss/summary loss according to existing Run Battle behavior.

### QA Verdict

- QA status: pass-with-notes.
- QA report: `_codex/tasks/QA/2026-06-18_040_QA_ArchitectureReview.md`.
- Actionable findings: none.
- Follow-up fixes applied: none after QA; no blocking findings were reported.
- Non-blocking observations: direct legacy constructors still create placeholder enemy rows unless the new enemy catalog/unit-source constructor path is used.

### Notes

- No Unity scenes, prefabs, assets, materials, `.asmdef`, or `.asmref` files were edited.
- Unity compilation and Play Mode validation remain manual.
- Predefined enemy ids are still reserved only; if encountered during materialization, they fail clearly because army definitions do not exist yet.
- The tactical battle scene still consumes legacy adapter labels; ADR 040 documents that this is transitional.

### Next Steps

- Run Unity EditMode tests for `PRD40EncounterMaterializationTests`.
- Assign the enemy catalog on `RunGenerationSession`.
- Run one manual Play Mode smoke test: Start Run -> Run Map battle node -> Battle -> Completion -> Reward/Summary/Loss routing.
