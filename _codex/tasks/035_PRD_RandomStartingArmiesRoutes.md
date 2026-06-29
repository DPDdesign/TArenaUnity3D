# [TARENA] PRD 035: Random Starting Armies And Route Maps

- Status: ready-for-agent
- Type: PRD
- Area: Run Metagame, Starting Armies, Route Maps, Offline DB
- Label: ready-for-agent
- Related:
  - `_codex/tasks/019_PRD_RunMetagameRewardFramework.md`
  - `_codex/tasks/archive/020_PRD019_StartRun.md`
  - `_codex/tasks/archive/021_PRD019_RunMap.md`
  - `_codex/tasks/030_DB-001_OfflineModeDatabasePersistence.md`
  - `_codex/tasks/035_PRD_RandomStartingArmiesRoutes_Grill.html`
  - `_codex/Documentation/ADR_008_PRD035_UncertainGeneratorDecisions.md`

## Problem Statement

Start Run and Run Map currently depend on small authored catalogs:

- three manually authored starting armies,
- three route preview choices,
- a simple route map with three paths and a shared final node.

That is useful for the first vertical slice, but it does not support the
intended TArena run structure. The player should start a campaign/mission run
from generated-but-balanced starting army options, then play a predictable
Mewgenics/Darkest Dungeon-like mission map with meaningful branches. The run
should be deterministic by seed, reloadable through the Offline DB, and
compatible with future account unlocks and future online authority.

The current hand-authored route shape is also too close to a simple Slay the
Spire path choice. TArena V1 should move toward predictable mission maps with
branching, fixed event points, and light variations. The player should mostly
solve "how do I build the best army from what this campaign offers?" rather
than depend on heavy route RNG.

## Solution

Build a deterministic Offline Mode generator layer for:

- starting army offers,
- starting run assets,
- mission route maps,
- generated route node metadata,
- generated encounter preset references.

The generator must feed the existing Start Run and Run Map service boundaries
instead of rewriting UI flow. Start Run UI should still consume starting army
view data, route/mission preview data, validation state, and begin-run results.
Run Map UI should consume persisted route node state and travel commands.

The generator should use current unit truth from `DataMapper/UnitCatalog`:
unit cost, tier, and legal skills. It must not duplicate unit stats or legal
skill lists in generator-owned data.

For V1, a starting army is balanced around:

- four stacks by default,
- total value target 1600-1700 for the initial implementation,
- a hard validation band 1450-1750 unless later tuning changes it,
- stack values roughly even, with each stack within about 20 percent of the
ideal per-stack value,
- one unlocked skill per stack,
- account unlock filtering,
- fallback early-unit pool when unlocks are too narrow.

Routes should move from the current three-path prototype toward a campaign and
mission model:

1. Player chooses a campaign such as forest, desert, or castle.
2. Each campaign contains three missions.
3. Missions must be completed in order.
4. Each mission owns its own branching route map.
5. V1 mission maps have fixed branch structure and fixed event points, with
   only light deterministic variation.

## User Stories

1. As a player, I want starting armies to feel varied, so that starting a run
   does not always mean choosing from the same three authored presets.
2. As a player, I want starting armies to be balanced, so that one generated
   option is not obviously stronger than the rest.
3. As a player, I want each starting army to contain four stacks by default,
   so that the army reads clearly in the current Start Run UI.
4. As a player, I want duplicate unit stacks to be legal, so that the generator
   can build valid armies even from a small unlock pool.
5. As a player, I want the start army to stay within a fair value range, so
   that the run challenge starts from a predictable baseline.
6. As a player, I want stack values to be roughly even, so that the army is not
   secretly just one power stack unless a future campaign explicitly allows it.
7. As a player, I want starting armies to respect my account unlocks, so that
   locked units and skills do not appear as legal choices.
8. As a player, I want all available starting army offers visible, so that the
   frontend can explain which ones are selectable at my level and which are
   blocked.
9. As a player, I want one starting reroll, so that I have a small amount of
   agency before committing to a run.
10. As a player, I want the run to start with 150 run gold and no battle skip
    tokens, so that the starting economy is explicit.
11. As a player, I want each stack to start with one unlocked skill, so that
    the army is playable without front-loading full skill progression.
12. As a player, I want generated army names to be readable, so that options
    like "Stone Spark" or "Wisp Screen" communicate an archetype.
13. As a player, I want the same seed to produce the same army and route
    options, so that an offline seeded run can be replayed predictably.
14. As a player, I want offline seeded runs to be distinguishable from normal
    progression runs, so that future saved-army offence/defence rules can
    block seeded-run exploits if needed.
15. As a player, I want campaign selection before the mission map, so that
    forest/desert/castle-style campaign identity affects the run.
16. As a player, I want campaign missions to unlock in order, so that campaign
    progress has structure.
17. As a player, I want a mission route map with branches, so that I can choose
    between safer and riskier paths.
18. As a player, I want predictable fixed event points in V1, so that planning
    matters more than route RNG.
19. As a player, I want battle nodes to point at encounter presets, so that
    the mission map can launch battles before full enemy-army generation
    exists.
20. As a player, I want harder branches to promise better rewards, so that risk
    and reward are readable.
21. As a designer, I want generator config to be campaign-driven, so that later
    campaigns can allow three stacks, power-stack starts, different tier mixes,
    or different route structures.
22. As a designer, I want unit role tags available in the unit catalog, so that
    the generator can reason about frontline/ranged/support/control roles.
23. As a designer, I want fallback behavior when unlocks are narrow, so that
    the Start Run screen never breaks because too few units are available.
24. As a designer, I want offer history to be a future DB capability, so that
    generated options can later be analyzed for balance and data science.
25. As a developer, I want generator output to flow through existing services,
    so that Start Run and Run Map UI do not need direct generator knowledge.
26. As a developer, I want generated runtime state persisted to the Offline DB,
    so that reloads do not reroll armies or maps.
27. As a developer, I want deterministic EditMode tests by seed, so that
    generator behavior can be changed safely.
28. As a developer, I want schema changes isolated and explicit, so that PRD030
    DB rules remain understandable.

## Implementation Decisions

- Do not change tactical unit stats, gameplay floats, cooldowns, skill
  execution, battle behavior, legacy battle scene flow, PlayFab, Photon, PUN,
  or online backend behavior.

- The first implementation should focus on domain logic, persistence seams, and
  existing UI contracts. It should not replace the visual UI layout unless that
  becomes necessary to expose the generated choices.

- The generator must use `DataMapper/UnitCatalog` as the source of truth for
  unit ids, cost, tier, and legal skills. Generator-owned data may define rules,
  budgets, archetypes, race limits, role tags, and selection constraints, but it
  must not fork unit stat truth.

- Unit role/category data should live on the current Unit Catalog entry model.
  The first implementation may derive an initial category from stats, skills,
  and name, then the user can manually correct it later. This requires explicit
  permission when the implementation edits Unity assets or serialized
  ScriptableObject data.

- Starting army V1 uses four stacks. Duplicate unit stacks are legal. The
  generator must keep duplicate-stack identity risks in mind because current
  persisted snapshots identify stacks through snapshot rows, formation slots,
  and unit ids.

- Starting army value should start with a 1600-1700 target and a hard
  validation band of 1450-1750. Stack values should be distributed roughly
  evenly across four stacks, with a target tolerance of about plus/minus 20
  percent from the ideal stack value.

- The existing `Symulator` balancing logic is relevant prior art. It can compare
  two unit stacks by simulating repeated attacks in both initiative orders. PRD
  035 should treat it as a balancing reference, not blindly depend on the
  current MonoBehaviour UI shape. If used for production generation, extract or
  wrap only a testable non-UI balancing service.

- Legal tier compositions for the default start are:
  - 4x T1,
  - 3x T1 + 1x T2,
  - 2x T1 + 1x T2 + 1x T3,
  - 2x T1 + 2x T2,
  - 1x T1 + 2x T2 + 1x T3,
  - 1x T1 + 3x T2.

- Starting army V1 must not include more than two races. Race identity needs a
  generator-readable source in unit metadata or generator rules.

- Each starting stack has one unlocked legal skill in V1. Future versions may
  allow generator rules to vary unlocked skills per unit, but that is not part
  of this first PRD.

- Account unlocks must be respected. If account unlocks leave too few legal
  units, the generator should use an explicit early fallback pool:
  Wisp, Rusher, Trapper, Thrower, Healer, StoneGolem.

- If fallback still cannot produce four distinct useful choices, duplicate T1
  or T2 units are legal. The result should carry a warning/debug flag, but
  Start Run should not be blocked.

- The frontend should be able to show all generated starting army options. It
  should block selection when account level/unlocks make an option unavailable.

- Starting assets for V1:
  - one starting reroll token,
  - 150 run gold,
  - zero battle skip tokens.

- A single initial seed should generate the complete deterministic run offer:
  starting armies, reroll options, route map, and encounter preset references.
  Rerolls should be stable variations of the same seed. Replaying the same seed
  offline should reproduce the same rolls.

- Future online mode should not allow arbitrary known-seed starts unless the
  backend explicitly validates them. Offline mode may allow known-seed starts.
  A future rule may prevent known-seed offline runs from producing final saved
  offence/defence armies. ADR 008 allows this rule to be implemented now as
  provisional behavior if the implementation clearly marks the uncertainty and
  revision trigger.

- Only the selected starting army is persisted to `army_snapshots` at Begin
  Run. Unselected offers remain generated screen output in V1.

- Offer history is a valuable future DB feature for balance analysis and data
  science. It may be implemented in this PRD if the schema/migration is
  explicit and the implementation records the decision as provisional under
  ADR 008.

- Route generation must move toward Mewgenics/Darkest Dungeon-like campaign
  missions rather than Slay the Spire-style route randomness. The player should
  plan around mostly predictable maps with fixed event points and light
  deterministic variation.

- V1 campaign flow:
  - player chooses a campaign such as forest, desert, or castle,
  - each campaign contains three missions,
  - mission 1 must be completed before mission 2,
  - mission 2 must be completed before mission 3,
  - completing the third mission can unlock the next map/campaign tier.

- V1 mission map shape:
  - node 1: battle + reward,
  - node 2: random event,
  - node 3: battle + reward,
  - branch 1 node 4A: battle + reward,
  - branch 1 node 5A: random event,
  - branch 1 node 6A: battle + reward,
  - branch 1 node 7A: empty,
  - branch 1 node 8A: empty,
  - branch 2 node 4B: harder battle + better reward,
  - branch 2 node 5B: random event,
  - branch 2 node 6B: battle + reward,
  - branch 2 node 7B: random event,
  - branch 2 node 8B: battle + reward,
  - merge node 9: shop,
  - node 10: final battle.

- Current `RunMapNodeType` does not include random event or empty nodes.
  Implementation must either extend the enum/schema mapping through an explicit
  migration/enum plan or represent these nodes through an approved existing node
  type only if that remains semantically clear. Prefer an explicit model if the
  scope allows it.

- Battle nodes use authored encounter presets in V1. The future direction is a
  generator that creates encounter presets, but full enemy army generation is a
  separate PRD.

- Encounter ids should be derived from the same run seed and selected by risk
  band/preset table. If the encounter preset catalog is small, repeats are
  acceptable, but battle/final nodes must have non-empty encounter ids.

- Risk/reward hints should be partial information. They can come from template
  bias, branch role, and encounter preset risk band. Do not reveal exact enemy
  armies in V1.

- Seed persistence likely needs schema work. Preferred direction from the grill:
  store one seed on the run record and let dependent tables refer to the run id.
  Current `offline_runs` has no explicit seed column, so implementation must
  analyze whether to add a migration or temporarily encode seed metadata in
  existing text fields. Do not silently overload fields if a schema migration is
  the cleaner long-term choice.

- Generator architecture should be analyzed before coding. The likely shape is:
  new generator-backed sources behind existing Start Run and Run Map
  interfaces, but the final module boundary should respect DB composition and
  avoid production `InMemory...Store` construction.

- Minimal model candidates:
  - `RunGenerationSeed`,
  - `CampaignDefinition`,
  - `MissionDefinition`,
  - `StartingArmyGeneratorConfig`,
  - `GeneratedStartingArmyOffer`,
  - `StartingArmyCandidate`,
  - `GeneratedStackCandidate`,
  - `UnitRoleCategory`,
  - `RouteGeneratorConfig`,
  - `GeneratedRouteMap`,
  - `GeneratedRouteNode`,
  - `EncounterPresetBand`.

- A deep module opportunity is a deterministic generation service with a simple
  input/output contract. It should encapsulate random source handling, account
  unlock filtering, budget validation, tier/race/role constraints, reroll
  variation, and route topology generation. It should be testable without Unity
  scene objects.

- UI hookup is part of acceptance. The generated data must be visible through
  the existing Start Run and Run Map UI paths, not only through backend tests.

## Testing Decisions

- Tests should verify external behavior and persisted results, not private
  implementation details of the random selection algorithm.

- Add or update EditMode tests for deterministic generation by seed. The same
  seed and same account unlock state must produce the same offers, route map,
  reroll options, and encounter preset references.

- Add budget-bound tests. Generated starting armies must stay in the hard
  accepted value band and stack values should satisfy the configured per-stack
  distribution tolerance.

- Add legal unit/skill tests. Generated stacks must use units from
  DataMapper/UnitCatalog truth and only legal skills for that unit. V1 should
  unlock exactly one skill per stack.

- Add account unlock tests. Locked units/skills must not appear unless fallback
  is explicitly activated.

- Add fallback tests. If the account unlock pool is too narrow, the generator
  should use the early fallback pool and may duplicate T1/T2 units without
  blocking Start Run.

- Add tier composition tests. Generated four-stack armies must match one of the
  approved tier compositions.

- Add race limit tests. A starting army must not use more than two races.

- Add route topology tests. A generated mission map must include the required
  fixed node sequence, branch nodes, merge shop node, and final battle node.

- Add route node type tests. Battle, reward, random event, empty, shop, and
  final semantics must be represented clearly, either through explicit new node
  types or an approved migration-compatible mapping.

- Add encounter reference tests. Battle and final nodes must have non-empty
  encounter ids derived deterministically from the seed/risk band.

- Add DB reload tests. Begin Run should persist the selected starting army and
  generated route map, recreate adapters/services, reload by persisted run id,
  and return identical runtime state without rerolling.

- Add reroll determinism tests. The one starting reroll must be a stable
  variation of the same seed and should cost/consume the configured start
  resource according to the final command contract.

- Add production composition guard coverage. Runtime RunMetagame code should
  continue to avoid constructing `new InMemory...Store()` in production paths.

- Use existing test patterns as prior art:
  - `StartRunServiceTests`,
  - `RunMapServiceTests`,
  - `OfflineStartRunRunMapDbTests`,
  - `OfflineModeProductionCompositionTests`,
  - schema/persistence tests from PRD030.

- The user compiles and tests inside Unity. Do not run Unity, dotnet, package
  restore, external build scripts, or SDK installation commands unless the user
  explicitly allows a specific command.

## Out of Scope

- No tactical battle rewrite.
- No unit stat, float, cooldown, skill execution, or damage formula changes.
- No balancing changes to current unit assets unless explicitly approved.
- No scene, prefab, material, controller, `.inputactions`, `.asmdef`, or
  `.asmref` edits without explicit permission.
- No online backend, PlayFab, Photon, PUN, cloud sync, or matchmaking work.
- No full enemy army generator in this PRD.
- Final saved-army offence/defence eligibility for known-seed runs may be
  implemented only as ADR 008 provisional behavior. If not implemented, defer
  it explicitly in task completion notes.
- Offer history/data science persistence may be implemented only with an
  explicit schema/migration plan and ADR 008 provisional note.
- No broad account progression redesign.
- No heavy run RNG; V1 maps are predictable mission maps with light seeded
  variation.

## Further Notes

- No second follow-up grill is needed before implementation. ADR 008 accepts
  the remaining uncertain items as implementation-allowed provisional
  decisions:
  - whether seed storage requires an explicit schema migration,
  - exact generator-vs-catalog class boundary,
  - whether route generation should later adapt strongly to selected army
    composition,
  - known-seed saved-army eligibility rules for offline mode,
  - offer history as a future analytics feature.

- For each uncertain item implemented under ADR 008, task completion notes
  should state what was implemented, why it was needed now, and what would
  trigger revision.

- The current route schema can persist paths and nodes, but it does not yet
  clearly model campaigns, missions, random events, empty nodes, or run seed.
  Implementation should start with a short schema/domain analysis before code.

- The current Start Run models already expose starting gold, reroll tokens, and
  battle skip tokens. PRD 035 should set them to 150, 1, and 0 respectively for
  the generated Start Run flow.

- The current persisted army snapshot does not store display name, tier,
  combat value, level, or locked-skill UI state as DB truth. Keep following
  PRD030: persist runtime ids, unit ids, amounts, formation slots, and active
  skill ids; derive display data from catalogs/services.

- `Symulator` currently contains useful balancing ideas but is a MonoBehaviour
  with legacy UI dependencies and `NotImplementedException` methods. Treat it
  as prior art for extracting a future testable combat-value/balance helper,
  not as a ready production generator dependency.

## Implementation - 2026-06-16

### What Changed

- `DeterministicRunGenerationCatalog`: added deterministic PRD035 generator for starting army offers, campaign route previews, mission route maps, branch topology, random event/empty nodes, and generated encounter ids. No Inspector fields changed.
- `StartRunModels` / `StartRunService`: Start Run options now carry selected starting assets: 150 run gold, 1 reroll token, 0 battle skip tokens. Lower/higher values affect visible run-start resources only; tune through generator config, not UI.
- `RunMapModels` / `RunMapService` / DB route stores: route nodes now support explicit branch links plus `RandomEvent` and `Empty` node types. These affect which mission nodes unlock in Play Mode; tune topology in the generator.
- `OfflineModeDatabaseComposition`: production Start Run and Run Map now use the generated catalog through existing service/store seams.
- `PRD19_035_RandomStartRoutesMockupController`: mockup-only serialized references for offer cards, route nodes, command buttons, and TMP labels. Historical PRD019 mockup builders have been removed; do not recreate or run prefab-generation tooling for this UI without current path-specific user permission.

### Automatic Test

- Added `PRD35RunGenerationTests` under `Assets/Scripts/Tests/EditMode/`.
- Tests cover deterministic generated offers, 1450-1750 budget band, one unlocked legal skill per stack, two-race limit, fallback pool behavior, fixed mission topology, generated encounter ids, branch opening, and DB reload of generated route state.
- Tests were not run automatically. In Unity, open Test Runner -> EditMode -> run `PRD35RunGenerationTests`; expected result is all green.

### Unity Test

#### Unity Setup

- Open Unity and let scripts compile.
- Do not regenerate PRD019 mockup prefabs. Inspect existing assets only, unless the current task grants path-specific permission to edit or rebuild them.
- Open `PRD_19_35_RandomStartRoutes_Polished.prefab` and inspect `Script_PRD19_035_RandomStartRoutesMockupController`; offer, route node, button, and TMP references should be assigned.
- For runtime flow, open the existing Start Run/Campaign Selection and Run Map setup that uses `OfflineModeDatabaseComposition`.

#### Play Mode Test

- In Start Run, verify generated armies show 4 stacks and starting assets show `150 RUN GOLD`, `1 ROLL TOKENS`, `0 BATTLE SKIP TOKENS`.
- Begin a run and open Run Map; verify the generated mission route includes battle, random event, branch, shop, and final nodes.
- Travel through nodes 1 -> 2 -> 3; both safe and risk branch starts should become available.
- Travel to shop, then final; final should only open through the explicit route sequence.
- In the PRD35 mockup prefab, click offer cards, route nodes, REROLL, and BEGIN; labels and preview message should update.

### QA Verdict

- Final QA: pass-manual-test-pending.
- QA report: `_codex/tasks/QA/2026-06-16_1450_035_QA_ArchitectureReview_Followup.md`.
- First QA found a possible third-race generator path; follow-up fix applied by selecting a one/two-race pool before tier composition.
- Non-blocking observations: explicit run seed column and real reroll command remain future work.

### Notes

- Unity compilation, EditMode tests, and Play Mode were not run automatically.
- No existing `PRD_19_021_RunMap` or `PRD_19_020/CampaingSelection` prefabs were edited.
- No scenes, materials, controllers, `.asmdef`, `.asmref`, or generated Unity files were edited.
- Historical PRD019 mockup prefab output is deprecated. Existing PRD019 runtime mockups under `Assets/Resources/UI/PRD_19` are read-only by default.

### Next Steps

- Run Unity compile/import only. Do not run or recreate PRD019 prefab rebuild menus.
- Run Unity Test Runner -> EditMode -> `PRD35RunGenerationTests`.
- Perform the Start Run -> Run Map Play Mode smoke test above.
