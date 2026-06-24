# [TARENA] PRD 045: Upfront Reward Opportunity Materialization

- Status: closed-implemented-pending-unity-validation
- Type: PRD
- Area: Run Generation, Reward Hints, Offline DB
- Label: closed-implemented-pending-unity-validation
- Unblocked by closed implementation:
  - `_codex/tasks/archive/042_PRD_RewardMaterializedSlotContract.md`
  - `_codex/tasks/archive/043_PRD_RewardPersistencePreviewApplyContract.md`
- Related:
  - `_codex/tasks/archive/037_PRD_MaterializedRunGenerationRewardsAndMapPersistence.md`
  - `_codex/Documentation/ADR_011_RunGenerationMaterializedUpfront.md`
  - `_codex/Documentation/ADR_012_RewardDirectionAndStackFocus.md`
  - `_codex/tasks/021_PRD019_RunMap.md`

## Problem Statement

ADR011 says run-owned generated content should be deterministic from the run
seed and materialized upfront. PRD037 accepts that concrete reward target and
amount must resolve after battle because the post-battle army snapshot is not
known yet.

The current implementation only creates reward rows after battle completion.
That is workable for the current Reward Map, but it leaves route-level reward
opportunity truth weak:

- Run Map cannot reliably show persisted reward direction hints,
- reward operation types are not visible until after the battle,
- changing generator code before battle completion can change what a run would
  offer,
- debugging cannot inspect the full generated run's reward plan.

## Solution

Split reward materialization into two phases.

At Start Run / run generation time, persist reward opportunities for reward
producing nodes:

- node id,
- slot index,
- planned normal operation type,
- catalog/ruleset entry id,
- run seed version,
- unresolved state.

After battle completion, resolve each preplanned opportunity against the
post-battle army snapshot:

- target stack/unit,
- amount,
- disabled/burned state,
- fallback state if all normal slots are impossible,
- preview/apply payload.

Reward Map continues to load persisted rows. It must not roll screen-time
fallback content.

## User Stories

1. As a player, I want Run Map reward hints to reflect the run's generated
   reward direction, so that route choices are meaningful.
2. As a player, I want reward cards after battle to match the route opportunity
   I chose before battle, so that the run feels honest.
3. As a designer, I want reward operation opportunities visible in persisted
   run data, so that I can debug generated routes.
4. As a developer, I want reward opportunity planning separate from
   post-battle resolution, so that each module has clear locality.
5. As a developer, I want generator changes to be versioned through seed
   version/opportunity rows, so that old runs do not silently mutate.
6. As a QA reviewer, I want tests proving route generation creates reward
   opportunity rows before battle, so that ADR011 stays enforced.

## Implementation Decisions

- PRD042 and PRD043 have landed with Unity verification still pending; this
  PRD must build on their current contracts rather than redesign them.
- Add or reuse a reward opportunity persistence model that can store unresolved
  planned slots independently from resolved card payloads.
- Run generation should choose planned operation types deterministically from
  run seed, node id, slot index, and seed version.
- Battle completion should resolve existing opportunity rows instead of rolling
  operation types from scratch.
- Reward Map should continue loading persisted concrete rows after resolution.
- Run Map hint UI can remain minimal; if no UI task is authorized, only expose
  backend/view data for future binding.
- Keep authored reward catalog/ruleset data out of SQLite except for stable
  catalog entry references and runtime generated rows.
- Keep PRD042 card generation rules intact:
  - three planned normal slots,
  - disabled normal cards preserve operation type,
  - emergency `RunGold` only when all normal slots are impossible.
- Keep PRD043 persistence identity intact:
  - reward id,
  - reward slot index,
  - legal/error state,
  - fallback state,
  - selected/applied state.
- Do not change tactical duplicate-stack reconciliation from PRD044.

## Testing Decisions

- Add EditMode tests at the Start Run / run generation DB seam.
- Add tests that reward-producing nodes have three unresolved opportunity rows
  before battle.
- Add tests that battle completion resolves those exact slots without changing
  operation types.
- Add tests that Reward Map reload never creates new opportunities or rerolls
  operation type.
- Add tests that seed/version changes are explicit.

## Out of Scope

- No implementation before PRD042 and PRD043 land.
- No Reward Map UI prefab changes.
- No full reward hint visual design.
- No online/backend generation authority.
- No new reward families or balance changes.

## Further Notes

This PRD records the remaining architecture gap between current PRD37
implementation and ADR011's full upfront materialization target. It is ready
for a second implementation wave after PRD042 and PRD043.

Suggested worker ownership:

- Owns: unresolved reward opportunity planning/persistence, Start Run or run
  generation DB seam, battle-completion resolution hook, focused tests.
- Avoids: reward generator value/parity rules, Reward Map DB identity contract,
  tactical duplicate-stack reconciliation, UI prefab builders/assets.

## Implementation - 2026-06-24

### What Changed

- No Inspector fields changed.
- Added `reward_opportunities` persistence with `DBRewardOpportunityStateId` so run generation stores node id, slot index, planned operation type, catalog id, run seed, seed version, and unresolved/resolved state.
- `OfflineMaterializedRunMapDbStore` now writes three unresolved planned normal reward slots for battle and recruit-reward nodes during route materialization.
- `RewardMapMaterializedGenerator` now exposes deterministic planning helpers and a `BuildChoice` overload that resolves cards from persisted operation plans without changing PRD041/PRD042 value or fallback logic.
- `OfflineRunBattleDbStore` now loads the existing opportunity plan before materializing reward cards after battle completion.
- `OfflineRewardMapDbStore` now marks matching opportunity rows resolved after concrete PRD042/PRD043 reward cards are saved.
- Added `OfflineRewardOpportunityDbStore` as the reward-opportunity DB helper.

### Automatic Test

- Added `PRD45RewardOpportunityMaterializationTests` covering unresolved rows before battle, exact slot/type preservation during battle resolution, Reward Map reload without rerolling, and explicit run seed/version persistence.
- Updated `OfflineDatabaseSchemaTests` for the new table, seed/version columns, and stable opportunity state ids.
- Tests are manual in Unity: open Unity Test Runner, select EditMode, then run `PRD45RewardOpportunityMaterializationTests` and `OfflineDatabaseSchemaTests`.
- Expected result: all selected EditMode tests pass.

### Unity Test

#### Unity Setup

- No new Inspector, prefab, scene, material, controller, `.inputactions`, `.asmdef`, or `.asmref` setup is required.
- Use the existing Offline Mode run-metagame setup.

#### Play Mode Test

- Start an Offline Mode run, travel to a normal battle node, complete the battle as a win, and confirm Reward Map opens with the persisted reward cards.
- Reload/refocus Reward Map and confirm the card slot/order/type does not change.
- Apply one reward and confirm the flow returns to Run Map with the reward applied once.

### QA Verdict

- QA verdict: pass for the battle-completion vertical slice.
- QA report: `_codex/tasks/QA/2026-06-24_1901_045_QA_ArchitectureReview.md`.
- Blocking findings: none.
- Non-blocking observation: `RecruitReward` nodes receive unresolved plans but still need a no-battle reward-creation hook before that direct Reward Map path can resolve cards.
- Follow-up fixes applied: none; the remaining no-battle hook is documented as out-of-slice remaining work.

### Notes

- Unity, `dotnet`, Git, external build scripts, package restore, SDK install, and Unity build/test tooling were not run.
- Existing PRD042 card slot/fallback behavior and PRD043 reward identity/selected/applied persistence are preserved.
- Older runs without opportunity rows still use the previous deterministic generator fallback during battle reward materialization.
- Remaining work: resolve no-battle `RecruitReward` opportunity rows when Run Map routes directly to Reward Map.

### Next Steps

- Run the new EditMode tests manually in Unity Test Runner.
- Run the battle-win Reward Map smoke test in Play Mode.
- Add the no-battle `RecruitReward` resolution hook in a focused follow-up task before treating all reward-producing node types as fully complete.

## Closure - 2026-06-24

Closed as a completed battle-reward vertical slice with one documented
follow-up.

Implemented:

- `reward_opportunities` table and state enum,
- unresolved reward opportunity planning during run map materialization,
- battle-completion resolution from persisted planned slots,
- generator overload for resolving an existing planned operation sequence,
- reward opportunity rows linked back to resolved reward cards,
- tests for unresolved rows, slot/type preservation, reload stability, and
  seed/version persistence.

Remaining follow-up:

- no-battle `RecruitReward` nodes receive unresolved plans but still need a
  direct Reward Map resolution hook.

Unity compilation, EditMode tests, and Play Mode validation remain manual.
