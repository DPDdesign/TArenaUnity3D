# 049ED PRD Tactical AI Action Selection And Execution Migration

- Status: draft
- Type: PRD
- Area: Tactical AI, battle actions, SO-driven execution
- Owner: TBD
- Supersedes: `_codex/tasks/049D_PRD_TacticalAILegalActionAPIIntegration.md`,
  `_codex/tasks/049E_PRD_SODrivenTacticalActionExecutionMigration.md`

## Goal

Migrate Tactical AI to choose and execute shared `ValidatedTacticalAction`
candidates directly.

AI must stop generating private skill targets, stop using
`TacticalAIActionIntent` as a transition adapter, and stop falling back to
`CastManager` for migrated action API skills.

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
- Planning reads immutable snapshot/spec data, not live Unity objects.
- Execution revalidates against current battle state before applying effects.

## Accepted Grill Decisions

- Migrate family by family. Skills without complete action data are skipped.
- Unsupported legacy-only skills are logged with `Debug.LogWarning`.
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
- 049ED starts only after 049ABC is implemented for all active skills.
- 049ED covers all active actions and skills described by 049ABC, not a limited
  slice or subset.
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
- Cover every active action and skill once 049ABC has completed their data and
  validation model.
- Return the same `TacticalActionResult` shape for player and AI actions.
- Keep legality, scoring, and execution as separate responsibilities.
- Preserve deterministic planning and snapshot-only worker-thread rules.
- Migrate action families incrementally.
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
4. Log and skip unsupported legacy-only skills.
5. Score and prune legal candidates in AI.
6. Simulate candidate outcomes using 049A/049AC effect data.
7. Pick ordered candidate actions.
8. Convert the top ranked action back to the minimal submitted intent shape.
9. Revalidate that submitted intent against current battle state.
10. If revalidation fails, try the next ranked legal action from the same AI
   plan.
11. Execute effects through the new SO-driven action executor.
12. Emit gameplay result events.
13. Let presentation consume gameplay result/presentation cue data after logic
    is complete.

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
- warning/log data for unsupported legacy-only skills.

The API returns all legal actions by default. Technical safety guards may exist
to prevent runaway candidate explosion, but they are runtime protection, not
tactical scoring.

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

1. Start after 049ABC is implemented for all active skills.
2. Generate legal actions for all active actions and skills covered by 049ABC.
3. Score those actions directly as `ValidatedTacticalAction`.
4. Execute selected actions through the SO-driven executor.
5. Compare outcomes against legacy behavior in Unity.
6. Add automated tests where logic is isolated.
7. Mark equivalent legacy paths as retired only after audit and manual Unity
   validation.

## Initial Grill Questions For 049ED

Use these when this PRD continues grilling:

1. What is the first action family to integrate end-to-end?
2. Does the first executor mutate live Unity objects directly, or apply through
   a battle-state service first?
3. What exact `ValidatedTacticalAction` fields are required for AI scoring and
   execution?
4. What minimum gameplay result events are needed for move, damage, status,
   trap, spawn, cooldown, and turn-cost results?
5. How should cooldowns and turn completion be applied consistently?
6. How do we compare migrated execution against legacy behavior?
7. What is the rollback behavior if one migrated action family fails manual
   Unity validation?
8. When is a `CastManager` method considered retired?

## Acceptance Criteria

Done when:

- Tactical AI no longer generates skill targets from slot availability alone.
- Tactical AI consumes legal `ValidatedTacticalAction` candidates directly.
- All active actions and skills covered by 049ABC are available to the new AI
  selection/execution path.
- Player and AI actions share the same `TacticalActionResult` output model.
- AI commit uses the same minimal submitted intent and validation path as player
  commit.
- Submitted intent and validation result contracts are suitable for future
  server-side authority.
- AI scoring operates after legality generation.
- AI compares skills, movement, basic attacks, wait, and defend together.
- Unsupported legacy-only skills are skipped and logged with warnings.
- No new `TacticalAIActionIntent` adapter is introduced for the new path.
- Existing `TacticalAIActionIntent` legacy code remains only until 049F cleanup
  and is not used by the new 049ED runtime path.
- No migrated action API skill executes through `CastManager`.
- AI scoring uses shared preview/result consequence data, not a separate
  AI-only consequence predictor.
- Existing AI profiles still provide scoring weights/config where useful; no
  full profile-system rewrite is required.
- Selected actions are revalidated before execution.
- Failed revalidation advances to the next best AI-ranked action, not a
  hardcoded fallback action type.
- Empty AI ranked action list logs a Unity `Debug.LogError` and is treated as
  invalid state.
- At least one migrated action family executes through the SO-driven executor.
- Effect values come from skill/action definition data.
- Cooldown and turn-cost behavior matches current rules.
- Manual Unity scenarios confirm parity for migrated actions.
- No AI planning path reads `CastManager`, `MouseControler`, or live Unity
  objects.
