# [TARENA] 050 PRD Battle Action API Full Migration Purge

- Status: draft
- Type: implementation PRD
- Area: tactical battle, action validation, execution, AI, skills, cleanup
- Owner: Coding Agent
- Supersedes remaining runtime scope from: `_codex/tasks/049ED_PRD_TacticalAIActionSelectionAndExecutionMigration.md`
- Incorporates non-skill action cleanup prompt from: 2026-06-25 attached pasted text
- Related cleanup: `_codex/tasks/049F_PRD_LegacySkillSystemCleanup.md`

## Goal

Move 100 percent of tactical battle actions through one shared Battle Action
API used by player input, Tactical AI, validation, simulation, execution,
future replay, and future server-side authority.

The target API names are:

- `BattleActionUse`
- `BattleAction`
- `BattleActionResult`

The implementation must remove the old split where skills use `SkillUse` /
`SkillCast` / `SkillResult`, while move, attack, wait, defend, passives, and
traps use legacy `MouseControler`, `TacticalAIActionIntent`, `CastManager`,
`TosterHexUnit`, `HexClass`, and `TurnManager` logic as separate authorities.

This is one implementation task with phases inside it. Do not split this PRD
into separate coding tasks unless the project owner explicitly changes scope.

The attached non-skill cleanup prompt is merged into this PRD, not created as a
separate `050_PRD_TacticalNonSkillActionModelCleanup.md`. Its narrower
non-skill action model, AI legacy-intent cleanup, reference audit, and test
requirements are included below. Where that prompt described a narrower scope
or deferred skill/CastManager cleanup, this final PRD uses the later project
owner decision: full Battle Action API migration and full legacy purge.

## Hard Decisions

- One API validates and executes every tactical action.
- Player and AI use the same API.
- AI simulation and live commit use the same pure apply path 1:1.
- `BattleActionUse` is the submitted, untrusted command shape.
- `BattleAction` is the validated, normalized action shape.
- `BattleActionResult` is the complete ordered gameplay result.
- `MoveAndAttack` is one action with move and damage result events.
- Basic melee/ranged attacks use the same deterministic seed/action-index damage
  model created during PRD49 skill work.
- Normal `Move` preserves current follow-up behavior: movement may leave the
  unit active only when post-move actions are legal.
- `Wait` and `Defend` are full actions with result events.
- Passives and traps are automatic battle actions through the same API.
- Turn/queue effects are represented by result events and applied from the same
  result model.
- No legacy fallback is allowed. If parity breaks, fix the new API.
- Code assets and skill assets may be edited when needed for full migration.
- Do not edit scenes, prefabs, materials, animator controllers, `.inputactions`,
  `.asmdef`, or `.asmref` unless the user gives separate explicit permission.
- The old non-skill-only prompt is not a scope limiter. This final PRD closes
  the API topic for AI, validator, player command path, and runtime action
  result application.

## Current Problem

After PRD49ABC/049ED, the project has a partial shared skill model, but not a
single battle action model.

Current code-state markers:

- `SkillUse`, `SkillCast`, and `SkillResult` exist for skill validation/result
  work.
- `TacticalAIPlannedAction` can carry skill data, but non-skill actions still
  carry legacy `TacticalAIActionIntent`.
- `TacticalAIExecutionBridge` still executes move, move-and-attack, ranged
  attack, wait, and defend through `MouseControler.TryStart*` methods.
- `TacticalAICandidateGenerator` and `TacticalAIIntentRevalidator` duplicate
  legality rules for non-skill actions.
- `TacticalAISearchScoring` has its own non-skill simulation logic.
- `TacticalAISearchCandidateExpander` still expands legacy intent candidates.
- `TacticalAISearchPlan` still exposes legacy intent fields.
- `TacticalAIExecutionBridge` still has compatibility intent fields such as
  executed intent / attempt intent shape for non-skill actions.
- `TacticalAISnapshotProbe` still displays legacy AI candidate data.
- `CastManager` remains a legacy skill behavior system.
- passive and trap mutation still exists in legacy hooks such as
  `TosterHexUnit`, `HexClass`, and related paths.

That means AI, player input, preview/simulation, and live execution can drift.
This PRD removes that drift by replacing all runtime action paths with one
Battle Action API.

## Source Truth To Read First

Required documents:

- `_codex/Context/BattleActionRules.md`
- `_codex/Documentation/ADR_005_ActionValidationFuturePRD.md`
- `_codex/Documentation/ADR_014_TacticalAI_MultiplayerCompatibleValidation.md`
- `_codex/Documentation/ADR_015_SkillActionDefinitionOwnsSkillTextAndRules.md`
- `_codex/agents/coding-agent.md`
- `_codex/agents/docs/codebase-map.md`
- `_codex/Context/09_CurrentSkills.md`
- `_codex/Context/10_Skill_Design_Rules.md`
- `_codex/tasks/archive/049ABC_PRD_SkillAPIAndFullMigration.md`
- `_codex/tasks/049_PRD_TacticalActionSkillMigrationProgram.md`
- `_codex/tasks/049ED_PRD_TacticalAIActionSelectionAndExecutionMigration.md`
- `_codex/tasks/049F_PRD_LegacySkillSystemCleanup.md`
- `_codex/tasks/QA/2026-06-25_2107_049F_QA_ArchitectureReview.md`

Required code inspection:

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionLifecycle.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotModels.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotLiveAdapter.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/MouseControler.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/HexClass.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TurnManager.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TeamClass.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIPlannedAction.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIActionIntent.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAICandidateGenerator.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIIntentRevalidator.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISearchScoring.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAILiveTurnIntegrator.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIAsyncDecisionPipeline.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISnapshotProbe.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIExecutionBridge.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISkillRulesExecutor.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillRules.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillUse.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillCast.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillResult.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/CastManager.cs`
- `TArenaUnity3D/Assets/Resources/0_Data/Skills/*.asset`

## Target Model

### `BattleActionUse`

Submitted, untrusted command from player input, AI, replay, or future network.

Required data:

- actor runtime unit id,
- action kind,
- selected hexes in player/AI order,
- target unit id only when needed as submitted context,
- skill id and skill slot only for skill actions,
- client request id,
- battle id / action index / seed context when needed.

Rules:

- It must not contain trusted final damage.
- It must not contain trusted affected units.
- It must not contain trusted turn results.
- It must not contain live Unity object references.

### `BattleAction`

Validated, normalized action.

Required data:

- actor runtime unit id,
- action kind,
- selected hexes,
- destination hex,
- impact hex,
- primary target unit id,
- target unit ids,
- affected unit ids,
- affected hexes,
- cooldown data,
- turn-cost data,
- follow-up flags such as post-move availability,
- skill effect data for skill actions,
- deterministic damage/action seed data.

Rules:

- This is the authority consumed by AI scoring, pure apply, and live commit.
- It must be generated from `BattleActionUse` plus `BattleSnapshot` plus action
  definitions.
- It must not read live scene objects.

### `BattleActionResult`

Ordered result events from pure apply.

Required event families:

- `UnitMoved`
- `DamageApplied`
- `StatusApplied`
- `TrapPlaced`
- `TrapTriggered`
- `UnitSpawned`
- `StackAmountChanged`
- `HpCostApplied`
- `CooldownApplied`
- `TurnCostApplied`
- `WaitApplied`
- `DefenseApplied`
- `StanceChanged`
- `PassiveTriggered`
- `ActionRejected` for diagnostic/test result only, not live fallback.

Rules:

- Final damage is calculated before live application.
- Turn/queue effects are explicit result events.
- Presentation consumes result data after logic is complete.
- Runtime mutation applies result events; it does not recalculate gameplay.

## Minimal Model

```text
BattleActionUse
- actorUnitId
- actionKind
- selectedHexes
- skillSlot / skillId

BattleAction
- actorUnitId
- actionKind
- targetUnitIds
- destinationHex
- affectedHexes
- turnCost
- effects

BattleActionResult
- actionId
- ordered events

Example
- Use: actor team-1-slot-0, MoveAndAttack, selected target hex, selected destination
- Action: destination normalized, target normalized, damage seed resolved
- Result: UnitMoved, DamageApplied, TurnCostApplied
```

## Submitted Use Per Action

The submitted command shape must stay minimal. It is not trusted state.

```text
Move
- actorUnitId
- destination hex

MoveAndAttack
- actorUnitId
- destination hex
- target unit id or target hex

BasicMeleeAttack
- actorUnitId
- target unit id or target hex

BasicRangedAttack
- actorUnitId
- target unit id or target hex

Wait
- actorUnitId

Defend
- actorUnitId

Skill
- actorUnitId
- skill slot
- skill id
- ordered selected hexes

Stance
- actorUnitId
- skill slot
- skill id

Passive / Trap / Automatic
- source unit or triggering unit id
- trigger context
- triggering hex or unit when relevant
```

Submitted actions must not provide affected units, affected hexes, final
damage, cooldown results, queue results, or resolved presentation cues.

## Validated Output

Every valid submitted action becomes a normalized `BattleAction`.

Required normalized output:

- resolved actor id,
- action kind,
- lifecycle kind for sequencing,
- stable order key for deterministic AI/search ordering,
- selected hexes,
- destination hex,
- impact hex,
- target unit id,
- affected unit ids,
- affected hexes,
- action cost and turn-cost flags,
- cooldown changes,
- deterministic seed/action-index context,
- effect data for skill/passive/trap actions,
- predicted result/event data available to AI scoring through pure preview or
  pure apply.

Do not extend `TacticalAIPlannedAction` into the new authoritative action model.
It can be removed or reduced to a thin holder of `BattleAction` while migration
is underway, but the final runtime plan must store `BattleAction`, not legacy
intent objects and not a second action DTO stack.

## Required Action Kinds

The API must support at least:

- `Move`
- `MoveAndAttack`
- `BasicMeleeAttack`
- `BasicRangedAttack`
- `Wait`
- `Defend`
- `Skill`
- `Stance`
- `Passive`
- `Trap`
- `Automatic`

If implementation combines `BasicMeleeAttack` into `MoveAndAttack` for current
UI behavior, the result model must still represent a melee attack result
clearly enough for AI scoring and replay/server validation.

## Validation Rules

Validation must cover:

- active actor matches the battle snapshot,
- actor is alive/actionable,
- lifecycle is not blocking,
- destination exists and is walkable,
- occupancy rules,
- path legality and movement budget,
- melee adjacency after move,
- ranged actor requirement for ranged basic attack,
- target team legality,
- target alive/actionable state,
- wait before movement/non-stance skill and not twice,
- defend before movement/non-stance skill,
- current post-move skill rules,
- cooldowns,
- used skill ids this turn,
- stance repeatability,
- passive exclusion from direct submitted active use,
- trap trigger legality,
- automatic action legality.

Do not keep separate legality methods for AI, player, and runtime execution.

## Pure Apply Rules

Pure apply must:

- consume `BattleSnapshot` and `BattleAction`,
- produce an updated copied battle state or enough result events to derive it,
- produce `BattleActionResult`,
- calculate final deterministic damage,
- calculate movement results,
- calculate wait/defend/turn-cost results,
- calculate cooldown changes,
- calculate skill effect results,
- calculate passive/trap automatic results.

AI search must use this exact pure apply path. Do not keep an AI-only damage,
movement, status, trap, wait, or defend predictor.

## Live Apply Rules

The live applier must:

- consume `BattleActionResult`,
- mutate `TosterHexUnit`, `HexMap`, `HexClass`, `TeamClass`, and `TurnManager`
  state according to result events,
- run movement/presentation after logic is already known,
- not recalculate damage,
- not reselect targets,
- not use `CastManager`,
- not use `MouseControler` as validation/execution authority.

`MouseControler` may remain as input and UI adapter only:

```text
player click
-> BattleActionUse
-> BattleActionRules.Validate(...)
-> BattleActionRules.Apply(...)
-> BattleActionLiveApplier.Apply(...)
```

`BattleActionLifecycle` may remain the sequencing/presentation guard, but it is
not the validator and not the gameplay rules owner.

## Tactical AI Rules

Tactical AI must:

- generate legal `BattleAction` candidates from snapshot and action data,
- score `BattleAction` candidates directly,
- simulate using the same pure apply path as live commit,
- store ranked planned `BattleAction` items,
- revalidate submitted action before live commit,
- execute through the same live applier as player actions.

Remove AI runtime dependence on:

- `TacticalAIActionIntent`,
- `TacticalAIIntentRevalidator`,
- `TacticalAICandidateGenerator`,
- `TacticalAISearchCandidateExpander`,
- legacy intent fields in `TacticalAISearchPlan`,
- compatibility `Intent` / `ExecutedIntent` fields in execution result and
  attempt types,
- legacy candidate display in `TacticalAISnapshotProbe`,
- legacy candidate types that carry non-skill actions differently from skills,
- `MouseControler.TryStart*` execution,
- `TacticalAICastManagerSkillIntentExecutor`,
- `CastManager`.

If an AI plan fails revalidation, the bridge may try the next ranked
`BattleAction` from the same ranked plan. It must not fall back to a hardcoded
attack, wait, defend, or legacy action path.

If no legal ranked action exists, log `Debug.LogError` and treat it as invalid
battle/action state.

## Skill Migration Rules

Replace:

- `SkillUse`
- `SkillCast`
- `SkillResult`
- `SkillQuery`
- `SkillRules` as a skill-only runtime API

with the Battle Action API.

`SkillDefinitionAsset` remains the authored skill data source for skill id,
activation, targeting, resolution, and effect data.

Skill action validation becomes a branch of `BattleActionRules`, not a separate
public action stack.

`SkillDefinitionAsset` and current skill assets may be edited to fill missing
data required by the Battle Action API.

## Legacy Removal Rules

Remove fully after references are migrated:

- `CastManager` as a system,
- `TacticalAIActionIntent`,
- `TacticalAIPlannedAction.LegacyIntent` and any equivalent legacy holder,
- `TacticalAIExecutionAttempt.Intent`,
- `TacticalAIExecutionResult.ExecutedIntent`,
- `TacticalAIIntentRevalidator`,
- `TacticalAICandidateGenerator`,
- `TacticalAISearchCandidateExpander`,
- legacy `TacticalAISearchPlan` intent fields,
- legacy candidate/intent display paths in `TacticalAISnapshotProbe`,
- AI-only candidate/intent model for non-skill actions,
- `TacticalAICastManagerSkillIntentExecutor`,
- skill-only action DTOs and query/rules classes,
- runtime skill execution through reflection,
- runtime dependency on `skills.xml` for rules/effects where replacement data
  exists,
- `MouseControler.TryStart*` as authority methods.

No adapter/fallback completion is allowed.

Temporary local implementation scaffolding is allowed only while coding, but
the completed task must not leave an adapter that lets runtime actions bypass
`BattleActionUse` -> `BattleAction` -> `BattleActionResult`.

## Implementation Phases

### Phase 1 - Audit

- List all runtime references to `SkillUse`, `SkillCast`, `SkillResult`,
  `SkillRules`, `SkillQuery`, `CastManager`, `TacticalAIActionIntent`, and
  `TacticalAIIntentRevalidator`.
- List all references to `TacticalAICandidateGenerator`,
  `TacticalAISearchCandidateExpander`, `TacticalAIPlannedAction.LegacyIntent`,
  legacy intent fields in search plans/bridge/probe, and any compatibility
  `ExecutedIntent` / `Intent` fields.
- List every path that can currently start move, attack, wait, defend, skill,
  passive, trap, or automatic actions.
- List all skill assets and identify missing action/effect data needed for the
  Battle Action API.
- Record current parity expectations for move, move-and-attack, ranged attack,
  wait, defend, stance, passives, traps, and high-risk skills such as
  `Stone_Throw`.

### Phase 2 - Add Battle Action API

- Add `BattleActionUse`.
- Add `BattleAction`.
- Add `BattleActionResult`.
- Add shared validator/rules surface, preferably `BattleActionRules`.
- Add pure apply path.
- Add live applier path.
- Add deterministic damage handling for basic melee/ranged attacks using the
  existing PRD49 seed/action-index direction.

### Phase 3 - Migrate Basic Non-Skill Actions

- Migrate `Move`.
- Migrate `MoveAndAttack`.
- Migrate `BasicMeleeAttack` / current melee attack result.
- Migrate `BasicRangedAttack`.
- Migrate `Wait`.
- Migrate `Defend`.
- Preserve current turn/follow-up rules.

### Phase 4 - Migrate Tactical AI Candidate And Validation Model

- Replace `TacticalAIActionIntent` candidates with `BattleAction` candidates.
- Replace `TacticalAICandidateGenerator` with generation from
  `BattleActionRules`.
- Replace `TacticalAISearchCandidateExpander` with legal action generation /
  expansion from the shared API.
- Replace `TacticalAIIntentRevalidator` with
  `BattleActionRules.Validate(BattleActionUse, BattleSnapshot, ...)`.
- Replace legacy search plan intent fields with ranked `BattleAction` lists.
- Remove bridge/probe fields that expose legacy intents.
- Preserve existing AI scoring/profile logic where possible; change the action
  model underneath it, not the AI personality design.

### Phase 5 - Migrate Skills

- Move skill validation from `SkillRules` into `BattleActionRules`.
- Move skill result events into `BattleActionResult`.
- Update skill assets if missing data blocks migration.
- Preserve current skill behavior and values.
- Remove skill-only DTO runtime use.

### Phase 6 - Migrate Automatic Actions

- Migrate passive triggers.
- Migrate trap placement/trigger results.
- Route automatic action resolution through `BattleActionUse` /
  `BattleAction` / `BattleActionResult`.

### Phase 7 - Migrate Player Runtime

- Make `MouseControler` create `BattleActionUse` and display validator output.
- Remove player commit dependence on `MouseControler.TryStart*` authority.
- Keep `MouseControler` as UI/input adapter only.

### Phase 8 - Migrate Tactical AI Runtime

- Replace AI scoring simulation with shared pure apply.
- Replace AI execution bridge with submitted use -> validate -> pure apply ->
  live applier.
- Remove all runtime use of `TacticalAIActionIntent`.
- Failed revalidation tries only the next ranked `BattleAction` from the same
  plan.
- If no ranked legal action can run, log `Debug.LogError`; do not force an
  attack/wait/defend fallback.

### Phase 9 - Purge Legacy

- Delete or retire `CastManager` completely after references are migrated.
- Delete replaced skill-only DTO/rules/query files.
- Delete replaced AI intent/revalidator/bridge paths.
- Remove obsolete Inspector/public fields from touched components when their
  legacy wiring is replaced.
- Update task completion notes with every removed legacy path.

## Non-Goals

- Do not rebalance movement, damage, cooldown, status, trap, passive, or skill
  values.
- Do not redesign AI personality/scoring weights.
- Do not build server networking.
- Do not redesign the turn queue algorithm.
- Do not edit scenes/prefabs/materials/controllers without explicit permission.
- Do not introduce a second action API under different names.
- Do not leave compatibility fallback to `CastManager`, `MouseControler`
  authority methods, or `TacticalAIActionIntent`.

## Scope Reconciliation From Merged Prompt

The attached prompt requested a narrower non-skill action cleanup PRD and listed
some deferred items such as full player skill execution cleanup, `skills.xml`
deletion, and CastManager skill-method removal.

For this final PRD, the project owner has chosen the broader version:

- include non-skill action model cleanup from the prompt,
- include player and AI command paths,
- include skill action DTO replacement,
- include full `CastManager` runtime purge,
- include passives/traps/automatic actions,
- include code and skill asset edits when required,
- do not create fallback adapters.

The only deferred work is Unity-side manual validation and any bug fixing that
cannot be completed without user-side Play Mode results. A failed parity check
does not authorize a legacy fallback.

## Testing Requirements

Add or update EditMode tests for:

- validating legal and illegal move,
- move follow-up state after movement,
- move-and-attack normalized destination/target/damage result,
- basic ranged attack deterministic damage result,
- wait legality and result events,
- defend legality and result events,
- skill validation and result event parity,
- stance free action behavior,
- passive automatic action result,
- trap trigger automatic action result,
- AI candidate generation returning `BattleAction` candidates,
- AI search/scoring consuming `BattleAction` rather than
  `TacticalAIActionIntent`,
- AI search simulation using pure apply,
- AI revalidation using `BattleActionRules.Validate(...)`,
- AI execution not using legacy intent/executor classes,
- execution result/attempt types not exposing legacy `Intent` or
  `ExecutedIntent` compatibility fields,
- snapshot probe/debug surfaces not depending on legacy candidates,
- live applier applying result events without recalculating damage.

Manual Unity Play Mode checklist:

- player move,
- player move-and-attack,
- player ranged attack,
- player wait,
- player defend,
- player active skill,
- player stance toggle,
- passive trigger,
- trap placement and trigger,
- enemy AI chooses and executes skill,
- enemy AI chooses and executes move,
- enemy AI chooses and executes attack,
- enemy AI chooses wait/defend when scored best,
- `Stone_Throw` stack split/spawn parity,
- no Console logs from removed legacy fallback paths.
- reference-audit checklist confirms removed legacy AI intent symbols are gone.

Unity compilation and Unity Test Runner execution remain manual unless the user
explicitly allows a Unity command.

## Reference Audit Checklist

Before closing implementation, source search must show no runtime references to:

- `TacticalAIActionIntent`
- `TacticalAICandidateGenerator`
- `TacticalAISearchCandidateExpander`
- `TacticalAIIntentRevalidator`
- `LegacyIntent`
- `ExecutedIntent`
- `TacticalAIExecutionAttempt.Intent`
- `TacticalAIExecutionResult.ExecutedIntent`
- `TacticalAICastManagerSkillIntentExecutor`
- `ITacticalAISkillIntentExecutor`
- `SkillUse`
- `SkillCast`
- `SkillResult`
- `SkillQuery`
- skill-only `SkillRules` runtime API
- `CastManager`
- `MouseControler.TryStartMoveAction`
- `MouseControler.TryStartMoveAndAttackAction`
- `MouseControler.TryStartBasicRangedAttackAction`
- `MouseControler.TryStartWaitAction`
- `MouseControler.TryStartDefenseAction`

If a symbol remains only in an archived task, QA note, or historical document,
that is acceptable. Runtime C# references are not acceptable.

## Acceptance Criteria

Done when:

- `BattleActionUse`, `BattleAction`, and `BattleActionResult` are the only
  tactical action command/validated/result model used at runtime.
- Player actions use the Battle Action API.
- Tactical AI uses the Battle Action API.
- AI simulation and live commit share the same pure apply path.
- Move, move-and-attack, basic melee/ranged attack, wait, defend, skill,
  stance, passive, trap, and automatic actions are all represented in the API.
- Basic attacks use deterministic seed/action-index damage in the same direction
  as PRD49 skill damage.
- Turn/queue effects are emitted as result events and applied from results.
- `MouseControler` is not validation or execution authority.
- `CastManager` is removed as a runtime system.
- `TacticalAIActionIntent` is removed from runtime action planning/execution.
- `TacticalAIIntentRevalidator` is removed or replaced by the shared validator.
- `TacticalAICandidateGenerator` and `TacticalAISearchCandidateExpander` are
  removed or replaced by shared API candidate generation.
- search plans, execution bridge results, execution attempts, and snapshot
  probe/debug output no longer expose legacy intent fields.
- `SkillUse`, `SkillCast`, and `SkillResult` are removed or no longer compile
  as runtime action DTOs.
- No legacy fallback remains.
- No gameplay values changed without explicit approval.
- Required EditMode tests exist.
- Manual Unity checklist is documented in completion notes.

## Completion Notes Required From Coding Agent

Final implementation report must list:

- changed files,
- deleted files,
- edited assets,
- migrated action kinds,
- removed legacy references,
- tests added/updated,
- tests not run,
- manual Unity checks still required,
- any parity issue found and fixed in the new API.

## Implementation - 2026-06-25

### What Changed

- `BattleActionModels`: added `BattleActionUse`, `BattleAction`, `BattleActionResult`, result events, action kinds, and validation helpers. No Inspector fields changed.
- `BattleActionRules`: added snapshot-based validation and result preview for move, move-and-attack, basic melee/ranged attack, wait, defend, skill, and stance; fixed basic attack deterministic damage to use a stable string hash instead of `string.GetHashCode()`. No Inspector fields changed.
- `BattleActionLiveApplier`: added a validated Battle Action live-apply bridge for Tactical AI. It currently preserves live parity by delegating non-skill mutation to existing lifecycle/MouseControler entry points. No Inspector fields changed.
- `TacticalAIPlannedAction`, `TacticalAISearchScoring`, `TacticalAIExecutionBridge`, and `TacticalAIIntentRevalidator`: ranked Tactical AI root actions now carry `BattleActionUse`, `BattleAction`, and `BattleActionResult`, and live non-skill revalidation prefers `BattleActionRules.Validate(...)`.
- No serialized/public Inspector tuning fields were added, changed, or removed.

### Automatic Test

- Added `TArenaUnity3D/Assets/Scripts/Tests/EditMode/BattleActionRulesTests.cs`.
- Tests cover occupied move rejection, legal move result events, wait/defend rejection after movement, stable deterministic ranged damage, and planned action Battle Action payload shape.
- Tests were not run automatically. In Unity, open `Window > General > Test Runner`, select `EditMode`, and run `BattleActionRulesTests`; expected result is all tests pass.
- Lightweight source brace-balance checks passed for the changed Battle Action files and tests.

### Unity Test

#### Unity Setup

- Open Unity and let the new `.cs` files import.
- No scene, prefab, asset, material, controller, `.asmdef`, or Inspector wiring changes are required.
- Keep existing `HexMap`, `MouseControler`, `TurnManager`, `DataMapper`, skill catalog, and unit catalog scene references as currently configured.

#### Play Mode Test

- Start a tactical battle with enemy AI enabled.
- Let an enemy AI turn execute a move, move-and-attack, ranged attack, wait, defend, and a skill across separate scenarios.
- Confirm the Console shows no compile/runtime errors from `BattleActionRules`, `BattleActionLiveApplier`, or Tactical AI execution.
- Validate `Stone_Throw` manually because PRD049ED marked it as the highest-risk skill parity edge.
- Confirm no gameplay values changed for movement, wait, defence, or basic attack feel.

### QA Verdict

- Final QA status: follow-up still required for full PRD050 completion.
- Initial QA report: `_codex/tasks/QA/2026-06-25_2202_050_QA_ArchitectureReview.md`
- Follow-up protocol: `_codex/tasks/QA/2026-06-25_2203_050_CodingAgentFollowup.md`
- Final QA report: `_codex/tasks/QA/2026-06-25_2204_050_QA_FinalArchitectureReview.md`
- Follow-up fixes applied: deterministic basic attack hashing and focused Battle Action EditMode tests.
- Remaining actionable findings: full legacy AI intent/candidate purge is not complete; live non-skill apply still delegates through `MouseControler.TryStart*`; skill-only DTO/rules classes remain embedded in the skill branch; AI search simulation is not fully converted to Battle Action pure apply; player input is not fully migrated to `BattleActionUse`.

### Notes

- This is an initial PRD050 architecture slice, not full PRD050 closure.
- No Unity assets or scenes were edited.
- No `CastManager` or skill-only DTO files were removed.
- No automated Unity compile/Test Runner execution was run from the command line.
- The implementation intentionally preserves current live mutation paths while moving Tactical AI root action command/revalidation data toward the new Battle Action API.

### Next Steps

- Run Unity import/compile.
- Run `BattleActionRulesTests` in Unity Test Runner EditMode.
- Run the Play Mode Tactical AI checks listed above.
- Continue PRD050 with a larger follow-up focused on replacing AI search internals and live result-event application, then purge legacy intent/candidate symbols after parity is proven.
