# 049ED PRD Tactical AI Action Selection And Execution Migration

- Status: in production planning
- Type: PRD
- Area: Tactical AI, battle actions, SO-driven execution
- Owner: TBD
- Supersedes: `_codex/tasks/049D_PRD_TacticalAILegalActionAPIIntegration.md`,
  `_codex/tasks/049E_PRD_SODrivenTacticalActionExecutionMigration.md`

## Current 049ABC Handoff

049ABC is complete as the API/SO-data foundation, not as a full live runtime
migration.

Current handoff assumptions:

- Shared skill/action API and DTOs exist.
- Current skill assets have SO activation, targeting, resolution, and effect
  data.
- Live/default gameplay commit can still depend on `MouseControler` /
  `CastManager` reflection method bodies.
- Passive trigger mutation can still depend on legacy hooks such as
  `TosterHexUnit`, `SpellOverTime`, and `HexClass`.

049ED must therefore cover every current active asset-backed and unit-assigned
skill when wiring AI selection, shared validation, simulation, and execution.
Do not skip an active skill because it was listed as in-scope for 049ABC.

## Goal

Migrate Tactical AI to choose and execute shared `ValidatedTacticalAction`
candidates directly.

AI must stop generating private skill targets, stop using
`TacticalAIActionIntent` as a transition adapter, and stop falling back to
`CastManager` for migrated action API skills.

For this PRD, "migrated" means the skill's live action path no longer depends
on legacy skill target guessing or `CastManager` reflection execution, not
merely that the skill has SO data from 049ABC.

049ED combines the former AI integration and SO-driven execution migration
because AI choice is not complete unless the chosen action can run through the
same new action model.

## Core Rules

- Validator checks legality only.
- AI scoring decides tactical value.
- Executor applies the validated action effects.
- AI compares all legal actions together: skills, movement, basic attacks,
  wait, and defend.
- Skills do not get automatic priority over movement or basic attacks.
- The new AI path consumes `ValidatedTacticalAction` directly.
- Do not add a `TacticalAIActionIntent` compatibility adapter.
- Do not add a `CastManager` fallback for migrated action API skills.
- Do not add a parallel Tactical AI framework. Adapt the current PRD046/047 AI
  surfaces to consume the new action model.
- Planning reads immutable snapshot/spec data, not live Unity objects.
- Execution revalidates against current battle state before applying effects.

## Accepted Grill Decisions

- 049ED is not a limited family-by-family slice. It starts after the 049ABC
  API/SO-data foundation exists and covers the full active action set.
- Unsupported legacy-only skills are logged with `Debug.LogWarning` only when
  they have no current skill asset and no unit assignment. Every active
  asset-backed/unit-assigned skill must be handled.
- The shared legal-action API returns all legal actions by default.
- AI owns scoring, pruning, and runtime candidate limits.
- Validator/API owns deterministic ordering and stable ids.
- AI may sort defensively by stable key, but the source list must be stable.
- Skill simulation uses the 049A/049AC effect model. 049ED must not invent a
  separate hidden skill-effect model.
- Cache stays simple for now. Plans remain advisory and are revalidated against
  the current snapshot. A future definition/action-data hash can be added if AI
  performance requires heavier caching.
- If no legal skill is good or available, AI chooses from other legal actions
  from the same API: move, basic attack, wait, or defend.
- AI keeps an ordered list of best legal actions after scoring. If the top
  action fails revalidation, execution tries the next best ranked action from
  that same list. It must not jump to a hardcoded fallback type such as basic
  attack.
- 049ED starts only after the 049ABC API/SO-data foundation exists.
- 049ED covers all current active asset-backed/unit-assigned actions and skills,
  not a limited slice or subset. 049ABC asset/data coverage is not a reason to
  skip runtime migration in 049ED.
- AI uses the same command, validation, execution, and `TacticalActionResult`
  model as the player. Treat AI as another online player/client for architecture
  purposes, supporting future anti-cheat and server-side authority.
- AI may score `ValidatedTacticalAction` candidates internally, but commit uses
  the same minimal submitted intent shape as a player command. That intent is
  revalidated locally now and should map cleanly to future server validation.
- The local validator contract is the future server-side validation contract.
  049ED must not introduce a Unity-only command model that would need replacing
  for anti-cheat/server authority.
- If AI has no legal ranked actions at all, including wait/defend, this is an
  invalid battle/action state. Log `Debug.LogError` in Unity and do not bypass
  the shared API with an emergency turn-end path.
- 049ED does not delete the old `TacticalAIActionIntent` path immediately. It
  replaces runtime use with the new path, then 049F removes the old classes
  after reference audit.
- AI scoring uses the same validated preview/result consequence data as UI and
  execution. Do not create a separate AI-only predictor that can drift from the
  shared action model.
- Existing AI profiles remain as a lightweight scoring-weight/config layer for
  now. 049ED does not redesign AI personality/profile assets.
- Keep the new ScriptableObject AI profile/config module. Parameters such as
  search depth, candidate limits, and scoring weights should remain authored
  there.
- AI planning must remain asynchronous/preplanned. Previous main-thread AI
  calculation stalled the game, so 049ED should compute during the player turn
  and during action/presentation animations where possible.
- Preserve the current AI search shape, including ply-3 style planning. The old
  search worked well for non-skill decisions; 049ED's purpose is to connect it
  to the new legal action/skill system and server-like player command path, not
  redesign the whole AI brain. Any exception must be called out as a narrow
  action-model compatibility exception, not as a new planner design.
- Ply simulation uses the same pure executor/result path as real execution,
  applied to copied battle state.
- AI continues to plan from full battle-state information, matching the current
  AI behavior. 049ED does not introduce a fog-of-war or player-visibility model.
- Async preplanning should start when the next likely AI actor is known from the
  turn queue. The queue is usually known ahead of time; rare player effects that
  slow, haste, kill, or otherwise reorder units invalidate the advisory plan and
  require recalculation/revalidation.
- V1 preplanned AI rankings use a small dependency footprint for performance:
  planned actor id, ranked candidate ids/order keys, referenced units,
  referenced hexes, turn/action-state hash, and profile/scoring config id or
  hash. Fuller selective invalidation is a future optimization, not required
  scope for 049ED.

## Existing AI Surfaces To Preserve

049ED modifies the current PRD046/047 Tactical AI architecture instead of
building a replacement system.

Preserve and adapt:

- `BattleSnapshot` as the copied, immutable planning input.
- `TacticalAIProfile` as the ScriptableObject profile/config source for depth,
  candidate limits, scoring weights, behavior settings, and fallback
  preferences.
- `TacticalAIPlanCache` as the advisory cache/preplanned result holder.
- `TacticalAISearchScoring` as the current ply-3 style scoring/search surface.
- `TacticalAILiveTurnIntegrator` and `TacticalAIAsyncTurnIntegrator` as the
  enemy-turn integration and async planning surfaces.
- `MostStupidAIEver` as the current enemy-turn entry point unless a separate
  approved task moves that entry point.

Replace underneath those surfaces:

- `TacticalAICandidateGenerator` should stop producing private skill-slot
  target guesses and should consume shared legal `ValidatedTacticalAction`
  candidates instead.
- `TacticalAIExecutionBridge` should commit AI-selected actions through the
  same submitted-intent, validation, executor, and result path as player
  actions.
- `TacticalAICastManagerSkillIntentExecutor` becomes obsolete for migrated
  action API skills and must not be used by the new 049ED runtime path.
- `TacticalAIActionIntent` may remain only as legacy code until 049F reference
  audit and cleanup; the new runtime path must not consume it.

Do not introduce new AI-only action, validation, preview, execution, or result
DTOs that duplicate `SubmittedActionIntent`, `ValidatedTacticalAction`, or
`TacticalActionResult`.

## Problem Statement

Current Tactical AI can generate skill intents from available skill slots, then
expand or revalidate targets through separate logic. Execution then still leans
on live `MouseControler` and `CastManager` behavior.

That creates duplicated authority:

- validator/player path knows one set of legal actions,
- AI guesses or expands another set,
- legacy execution mutates battle state from method bodies.

049ED replaces that split for migrated action families.

## Goals

- Generate legal `ValidatedTacticalAction` candidates from battle snapshot plus
  skill/action definitions.
- Score `ValidatedTacticalAction` candidates directly.
- Execute the selected validated action through the SO-driven effect executor.
- Cover every current active asset-backed/unit-assigned action and skill using
  the shared validation, simulation, and execution model.
- Return the same `TacticalActionResult` shape for player and AI actions.
- Keep legality, scoring, and execution as separate responsibilities.
- Preserve deterministic planning and snapshot-only worker-thread rules.
- Avoid main-thread stalls by keeping expensive AI planning off the main
  gameplay flow where safe.
- Preserve current ply-3 planning behavior, replacing the candidate/action
  model underneath it.
- Preserve ScriptableObject AI profile/config authoring for search depth,
  candidate limits, and scoring weights.
- Preserve existing gameplay values unless explicitly changed.
- Keep presentation separate from gameplay mutation.

## Non-Goals

- Do not rebalance AI.
- Do not make validator judge tactical quality.
- Do not build server authority in this PRD.
- Do not remove all legacy code in this PRD.
- Do not add new behavior to `CastManager`.
- Do not build a large cache/versioning system until performance requires it.

## Desired Flow

1. Capture/copy `BattleSnapshot`.
2. Load immutable skill/action definition data.
3. Generate all legal `ValidatedTacticalAction` candidates for the actor.
4. Log and skip unsupported legacy-only methods only when they have no current
   skill asset and no unit assignment.
5. Score and prune legal candidates in AI.
6. Simulate candidate outcomes using 049A/049AC effect data, preserving the
   current ply-3 style search.
   Simulation applies the same pure executor/result path to copied state that
   real execution uses for commit.
7. Pick ordered candidate actions asynchronously where possible, preplanning
   during player turns and action/presentation animations.
8. Store the ranking with the V1 dependency footprint for local invalidation.
9. Convert the top ranked action back to the minimal submitted intent shape.
10. Revalidate that submitted intent against current battle state.
11. If revalidation fails, try the next ranked legal action from the same AI
   plan.
12. Execute effects through the new SO-driven action executor.
13. Emit gameplay result events.
14. Let presentation consume gameplay result/presentation cue data after logic
    is complete.

## Async Planning And Invalidation

Each advisory AI plan should carry a V1 dependency footprint:

- planned actor id,
- ranked candidate action ids/order keys,
- units directly referenced by ranked candidates,
- hexes directly referenced by ranked candidates,
- turn/action-state hash that can affect legality,
- profile/scoring config id or hash.

If a changed unit, hex, or turn-state field is outside this footprint, the plan
may remain advisory-valid. If a changed item intersects the footprint, discard
or refresh the plan before commit.

Do not build a broader invalidation framework in 049ED. If later profiling
shows this is insufficient, add richer selective invalidation as a follow-up.

Final submitted-intent revalidation is still required even when the advisory
plan remains valid.

If the advisory plan is invalidated immediately before the AI turn and no fresh
async plan is ready, AI may run a short synchronous emergency scorer over the
current legal actions. This fallback must be shallow and bounded: no deep search
and no main-thread stall. It still uses shared legal actions, shared
preview/result consequence data, submitted-intent commit, and revalidation.
The emergency scorer may choose wait or defend if those actions win scoring; it
must not force an active skill, attack, or movement action just because one is
legal.

## PRD49ABC Naming Map

049ED must use the PRD49ABC action API responsibilities without creating a
second naming layer.

Current naming may differ by implementation file, but the responsibilities map
as follows:

- `SubmittedActionIntent` is the player-equivalent untrusted command shape. If
  the implemented PRD49ABC name is `SkillUse`, AI commit must reduce to that
  same shape.
- `ValidatedTacticalAction` is the trusted normalized validator output. If the
  implemented PRD49ABC name is `SkillCast`, AI scoring and execution must use
  that same validation result.
- `TacticalActionResult` is the shared ordered execution/result output. If the
  implemented PRD49ABC name is `SkillResult`, AI and player execution must
  return that same result model.

Do not implement both name sets as separate runtime DTO stacks. Use the names
that exist after 049ABC, and preserve these responsibilities.

## Legal Action API Behavior

Input:

- battle snapshot,
- actor stack id,
- action definitions,
- skill/effect definitions.

Output:

- legal `ValidatedTacticalAction` candidates,
- deterministic order,
- stable action ids/order keys,
- normalized destination/impact/target/effect data,
- warning/log data for unsupported legacy-only methods that have no current
  skill asset and no unit assignment.

The API returns all legal actions by default. Technical safety guards may exist
to prevent runaway candidate explosion, but they are runtime protection, not
tactical scoring.

Legal action generation and AI scoring must be able to run from immutable
snapshot/spec data without live Unity references, so expensive planning can
stay off the main gameplay flow.

The submitted intent and validation result shape should be server-ready. Local
Unity validation is the first implementation of that contract, not a throwaway
client-only model.

## Scoring Boundary

Validation output may include structural consequences:

- destination hex,
- impact hex,
- target unit ids,
- affected unit ids,
- affected hexes,
- effect family,
- action cost.

Validation output must not include tactical judgment.

AI scoring may consider:

- damage prediction,
- unit value,
- trap placement value,
- threat and safety,
- movement position,
- objective pressure later.

Damage prediction, affected units, affected hexes, movement destinations,
statuses, trap placement, and other action consequences must come from the same
preview/result data model used by UI and execution.

## Execution Boundary

The executor consumes:

- current battle state,
- revalidated `ValidatedTacticalAction`,
- skill/action definition data,
- ordered effect data,
- stable actor/target ids.

It does not consume:

- raw UI click state,
- `hexUnderMouse`,
- reflection method names,
- `CastManager` mode flags as authority.

Core execution should be a pure state transition where possible:

- input battle state/snapshot plus validated action,
- output updated battle state/snapshot plus gameplay result events.

Runtime can then apply the returned state through the appropriate authoritative
battle-state service.

AI execution must use the same command/result path as player execution. AI can
score validated candidates, but commit goes through a player-equivalent
submitted intent and revalidation step. Do not create a separate simplified AI
execution result model.

## Execution Families

Migration should follow broad effect families:

- turn-cost and cooldown application,
- direct damage,
- area damage,
- status apply/remove,
- trap placement,
- actor movement,
- forced target movement,
- spawn/split,
- self costs and self buffs.

## Presentation Boundary

Gameplay must not depend on VFX/SFX timing.

Core execution calculates gameplay results first. Presentation runs after logic
is complete.

Core executor emits gameplay result events. A presentation layer can build cue
events from:

- the validated action,
- before/after state,
- gameplay result events,
- `SkillPresentationCatalog` data.

Existing `SkillPresentationManager` may be reused as a presentation adapter,
not as gameplay execution.

## Migration Strategy

1. Start after the 049ABC API/SO-data foundation exists.
2. Generate legal actions for all current active asset-backed/unit-assigned
   actions and skills.
3. Score those actions directly as `ValidatedTacticalAction`.
4. Execute selected actions through the SO-driven executor.
5. Compare outcomes against legacy behavior in Unity.
6. Add automated tests where logic is isolated.
7. Mark equivalent legacy paths as retired only after audit and manual Unity
   validation.

## Initial Grill Questions For 049ED

Use these when this PRD continues grilling:

1. What exact `ValidatedTacticalAction` fields are required for AI scoring and
   execution?
2. What minimum gameplay result events are needed for move, damage, status,
   trap, spawn, cooldown, and turn-cost results?
3. How should cooldowns and turn completion be applied consistently?
4. What exact state changes invalidate a preplanned AI action list, and which
   distant/no-impact changes can safely keep it advisory-valid?
5. How do we compare migrated execution against legacy behavior?
6. What is the rollback behavior if part of the full active action set fails
   manual Unity validation?
7. When is a `CastManager` method considered retired?

## Testing Requirements

- AI simulation and real pure execution must share equivalence tests: the same
  submitted intent/revalidated action on the same battle state should produce
  the same `TacticalActionResult`/gameplay result events when run for planning
  and when run for commit.
- Ply-3 planning tests should verify that simulated child states are produced
  through the shared pure executor/result path, not through an AI-only
  approximation layer.
- Async planning tests should cover advisory plan reuse, local dependency
  invalidation, revalidation before commit, and fallback to shallow emergency
  scoring when no async plan is ready.
- Server-like command path tests should verify that AI commit uses the same
  minimal submitted intent and validation result contract as player commit.
- Reference/audit tests should verify that the new 049ED planning path does not
  read `CastManager`, `MouseControler`, `Resources`, `DataMapper`, scene
  objects, or other live Unity state.
- Runtime path tests should verify that migrated AI skill execution does not
  use `TacticalAIActionIntent`, `TacticalAICastManagerSkillIntentExecutor`, or
  a private AI-only action/result DTO.

## Acceptance Criteria

Done when:

- Tactical AI no longer generates skill targets from slot availability alone.
- Tactical AI consumes legal `ValidatedTacticalAction` candidates directly.
- All current active asset-backed/unit-assigned actions and skills are
  available to the new AI selection/execution path.
- Player and AI actions share the same `TacticalActionResult` output model.
- AI commit uses the same minimal submitted intent and validation path as player
  commit.
- Submitted intent and validation result contracts are suitable for future
  server-side authority.
- AI scoring operates after legality generation.
- AI compares skills, movement, basic attacks, wait, and defend together.
- Unsupported legacy-only methods are skipped and logged with warnings only
  when they have no current skill asset and no unit assignment.
- No new `TacticalAIActionIntent` adapter is introduced for the new path.
- Existing `TacticalAIActionIntent` legacy code remains only until 049F cleanup
  and is not used by the new 049ED runtime path.
- No new AI-only action, validation, preview, execution, or result DTOs are
  introduced to duplicate the PRD49ABC command/action/result model.
- No migrated action API skill executes through `CastManager`.
- Migrated AI skill execution does not use
  `TacticalAICastManagerSkillIntentExecutor`.
- AI scoring uses shared preview/result consequence data, not a separate
  AI-only consequence predictor.
- Existing AI profiles still provide scoring weights/config where useful; no
  full profile-system rewrite is required.
- Existing `TacticalAIProfile` or its direct evolution remains the source for
  search depth, candidate limits, scoring weights, behavior settings, and
  fallback preferences; do not move those values to hardcoded constants.
- AI planning/preplanning remains async and must avoid main-thread stalls.
- Selected actions are revalidated before execution.
- Failed revalidation advances to the next best AI-ranked action, not a
  hardcoded fallback action type.
- Empty AI ranked action list logs a Unity `Debug.LogError` and is treated as
  invalid state.
- The full current active asset-backed/unit-assigned action set executes
  through the SO-driven executor.
- Effect values come from skill/action definition data.
- Cooldown and turn-cost behavior matches current rules.
- Manual Unity scenarios confirm parity for migrated actions.
- No AI planning path reads `CastManager`, `MouseControler`, or live Unity
  objects.
- New 049ED runtime work modifies the existing Tactical AI integration/search
  surfaces instead of adding a replacement AI framework beside them.

## Implementation - 2026-06-25

### What Changed

- `TacticalAIActionIntent`: added `ValidatedSkillCast` and `PreviewResult` so skill candidates can carry shared PRD49ABC `SkillCast`/`SkillResult` data.
- `TacticalAIAsyncDecisionPipeline`: copied skill metadata now also exposes skill definition lookup to async planning.
- `TacticalAISearchScoring`: skill expansion now uses `SkillRules.GetTargets(...)` and `SkillRules.Validate(...)`; snapshot simulation consumes `SkillResult` preview events for damage, move, trap, hp cost, stack delta, and turn/cooldown effects.
- `TacticalAIIntentRevalidator`: skill revalidation now rebuilds a live `SkillUse` and validates it into a fresh live `SkillCast` before execution.
- `TacticalAIExecutionBridge`: default skill executor changed from `TacticalAICastManagerSkillIntentExecutor` to `TacticalAISkillRulesExecutor`; ranked plans no longer append fresh fallback actions when a ranked plan exists; no-legal-action now logs `Debug.LogError`.
- `TacticalAISkillRulesExecutor`: added shared `SkillRules.Apply(...)` live runtime adapter for migrated AI skills.
- `SkillCast` / `SkillRules`: added and populated `RepeatableInTurn` so runtime and simulation do not infer repeatability from effect shape.
- Tests: updated/added focused EditMode coverage for validated skill candidates and ranked-plan-only execution queues.
- No Inspector fields changed.

### Automatic Test

- Not run automatically. Per project rules, Unity compilation and Unity Test Runner execution are manual.
- Lightweight source brace-balance check passed for the changed source/test files.
- Added/updated EditMode tests:
  - `TacticalAISearchScoringTests.SearchCandidateExpansion_EmitsValidatedSkillCastAndPreview`
  - `TacticalAIExecutionBridgeTests.FallbackPlanner_UsesRankedPlanOnlyWhenPlanExists`
  - `TacticalAIExecutionBridgeTests` now uses test skill definitions for shared skill revalidation.
- Run in Unity: `Window > General > Test Runner > EditMode`, then run `TacticalAISearchScoringTests` and `TacticalAIExecutionBridgeTests`. Expected result: selected tests pass.

### Unity Test

#### Unity Setup

- No new scene, prefab, or Inspector setup is required.
- Existing `DataMapper` must still resolve the skill catalog because live AI execution resolves `SkillDefinitionAsset` by skill id.

#### Play Mode Test

- Start a tactical battle where an enemy AI unit owns an active catalog skill.
- Let the enemy turn run through `MostStupidAIEver` / `TacticalAIAsyncTurnIntegrator`.
- Confirm Console shows tactical AI async planning logs.
- Confirm AI can rank skills alongside movement, basic attacks, wait, and defend.
- For skill actions, confirm there are no `TacticalAICastManagerSkillIntentExecutor` execution logs and that effects apply through the new shared rules executor path.
- If all ranked actions fail revalidation, confirm Unity logs an error instead of forcing a hardcoded fallback action type.

### QA Verdict

- Final QA status: follow-up required.
- Initial QA report: `_codex/tasks/QA/2026-06-25_1715_049ED_QA_ArchitectureReview.md`
- Follow-up protocol: `_codex/tasks/QA/2026-06-25_1716_049ED_CodingAgentFollowup.md`
- Final QA report: `_codex/tasks/QA/2026-06-25_1716_049ED_QA_FinalArchitectureReview.md`
- Follow-up fix applied: `SkillCast.RepeatableInTurn` now carries activation repeatability into simulation and live runtime.
- Remaining actionable findings:
  - migrated runtime still routes through the legacy `TacticalAIActionIntent` shell,
  - async copied planning still carries `SkillDefinitionAsset` references instead of plain immutable skill specs,
  - full active-skill parity is incomplete for spawn, concrete status modifiers, detailed damage/effect matching, and representative parity tests.

### Notes

- This is a significant PRD049ED architecture slice, not full PRD049ED closure.
- `TacticalAICastManagerSkillIntentExecutor` remains in the project for PRD049F cleanup but is no longer the default AI skill runtime path.
- `TacticalAISkillRuntime` currently handles core shared result event families and logs unsupported spawn events instead of using reflection fallback.
- New `.cs` file metadata was not hand-authored; Unity can generate `.meta` on import.

### Next Steps

- Run the listed EditMode tests manually in Unity Test Runner.
- Run the Play Mode enemy-turn scenario above.
- Open a follow-up implementation task for the remaining QA findings before closing PRD049ED as complete.

## Implementation Follow-Up - 2026-06-25

### What Changed

- Added `TacticalAIPlannedAction` and moved the live/async migrated execution path to `TacticalAISearchPlan.OrderedActions`.
- Skill planned actions now carry `SkillUse`, `SkillCast`, and `SkillResult`; they do not carry a consumed legacy `TacticalAIActionIntent`.
- Added `SkillDefinitionSpec` and changed async copied planning to capture specs instead of `SkillDefinitionAsset` references.
- Updated `SkillContext` / `SkillRules` so validation, preview, simulation, and execution can use copied specs.
- Expanded `SkillEffect` with status modifier fields.
- Updated `TacticalAISkillRuntime` to apply status modifier data and live `UnitSpawned` events.
- Updated live/async integrators and tests to use `BestAction`, `OrderedActions`, and `ExecutedAction`.

### Automatic Test

- Not run automatically. Unity compile and Unity Test Runner execution remain manual.
- Lightweight source brace-balance checks passed for changed files.
- Added/updated focused tests:
  - `TacticalAISearchScoringTests.SearchPlan_SkillActionDoesNotCarryLegacyIntent`
  - `TacticalAIAsyncDecisionPipelineTests.CopiedSkillMetadataProvider_CapturesMetadataWithoutLiveReadsAfterCapture`
  - `TacticalAILiveTurnIntegrationTests` planned-action execution assertions.

### Unity Test

#### Unity Setup

- No new scene or Inspector setup is required.
- Existing skill catalog and unit catalog references must remain wired through `DataMapper`.

#### Play Mode Test

- Run an enemy AI turn with an active skill.
- Confirm skill planned actions execute through `TacticalAIPlannedAction` / `SkillRules` and do not invoke the CastManager AI bridge.
- Specifically validate `Stone_Throw` because half-stack split parity is still the highest-risk dynamic-value edge.

### QA Verdict

- Final follow-up QA status: pass with residual manual-validation risk.
- Follow-up protocol: `_codex/tasks/QA/2026-06-25_1727_049ED_CodingAgentFollowup2.md`
- Final follow-up QA report: `_codex/tasks/QA/2026-06-25_1727_049ED_QA_FinalFollowupReview.md`
- Previous required findings are addressed at code-structure level:
  - migrated skill path no longer consumes `TacticalAIActionIntent`,
  - async copied planning no longer stores `SkillDefinitionAsset`,
  - status/spawn runtime families are no longer warning-only.

### Notes

- `TacticalAIActionIntent` remains for legacy non-skill compatibility and PRD049F cleanup.
- `Stone_Throw` half-stack parity may still need a narrow dynamic stack-fraction effect expression if Unity validation shows drift.

### Next Steps

- Run Unity compile.
- Run `TacticalAISearchScoringTests`, `TacticalAIAsyncDecisionPipelineTests`, `TacticalAILiveTurnIntegrationTests`, and `TacticalAIExecutionBridgeTests` in EditMode.
- Run Play Mode validation for enemy AI skill use, with special attention to `Stone_Throw`.
