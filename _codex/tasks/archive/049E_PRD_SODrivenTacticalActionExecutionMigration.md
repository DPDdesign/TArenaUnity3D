# 049E PRD SO Driven Tactical Action Execution Migration

Superseded by:
`_codex/tasks/049ED_PRD_TacticalAIActionSelectionAndExecutionMigration.md`

049E is no longer an active standalone PRD. AI legal action selection and
SO-driven execution are now planned together in 049ED so the new AI path can
choose and execute `ValidatedTacticalAction` without a transition adapter or
`CastManager` fallback.

- Status: superseded
- Type: PRD
- Area: battle execution, skills, ScriptableObjects, legacy migration
- Owner: TBD

## Goal

Migrate tactical action execution from legacy `CastManager` skill methods toward
an executor that consumes `ValidatedTacticalAction` plus
`SkillActionDefinition` data.

049E is where the project starts replacing runtime skill behavior, not just
validating or previewing it.

## Problem Statement

049A creates a validator and UI targeting path. 049AC defines target and effect
data. 049D
integrates AI with legal action generation.

But if execution still depends on `CastManager` method bodies as source of
truth, the project still has duplicated behavior:

- SO data says what should happen,
- legacy methods actually mutate battle state.

049E defines how execution moves to the SO-driven model.

## Goals

- Execute actions from `ValidatedTacticalAction`.
- Read effect data from `SkillActionDefinition`.
- Apply movement, damage, statuses, traps, spawn/split, cooldowns, and turn
  costs through explicit code paths.
- Keep presentation separate from gameplay mutation.
- Migrate execution family by family.
- Preserve existing gameplay values unless explicitly changed.

## Non-Goals

- Do not rebalance skills.
- Do not redesign AI scoring.
- Do not build server networking.
- Do not remove all legacy code in the first execution slice.
- Do not add new behavior to `CastManager`.

## Execution Inputs

The future executor consumes:

- current battle state,
- `ValidatedTacticalAction`,
- `SkillActionDefinition`,
- effect data,
- actor/target stable ids.

It does not consume:

- raw UI click state,
- `hexUnderMouse`,
- reflection method names,
- `CastManager` mode flags as authority.

The target execution architecture is a new system, not a live legacy adapter.
Do not design `CastManager` fallback execution for migrated actions.

The core executor should be a pure state transition where possible:

- input battle state/snapshot plus validated action,
- output updated battle state/snapshot plus gameplay result events.

Runtime can then persist/apply the returned state through the appropriate
authoritative battle-state service.

## Execution Families

Execution migration should follow broad effect families:

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

Core execution calculates the complete gameplay result first. Presentation runs
after logic is complete.

Core execution should emit gameplay result events, not Unity presentation
events. A separate presentation planner/layer can build presentation cue events
from:

- the validated action,
- the before/after state,
- gameplay result events,
- skill presentation data.

Use C# events or direct C# presentation orchestration where needed. Do not use
UnityEvent as the domain event model.

Gameplay must not depend on VFX/SFX timing. Projectile travel, cast cues, impact
cues, animations, and sound should present already-computed results.

Open issue:

- Decide whether the first executor uses existing `SkillPresentationManager`
  directly or emits a neutral action result event consumed by presentation.

Updated direction:

- Core executor emits `GameplayResultEvents`.
- Presentation layer creates/consumes `PresentationCueEvents`.
- Existing `SkillPresentationManager` may be reused as a presentation adapter,
  not as gameplay execution.

## Migration Strategy

Recommended approach:

1. Start with one simple family that already passed validator/UI migration.
2. Execute from `ValidatedTacticalAction`.
3. Compare outcome against legacy behavior in Unity.
4. Add automated tests where logic is isolated.
5. Mark the equivalent `CastManager` method as legacy-retired after migration.

## Initial Grill Questions For 049E

Use these when this PRD is grilled:

1. What is the first execution family to migrate?
2. Does the executor mutate live Unity objects directly, or operate through a
   battle state service first?
3. How should presentation be triggered?
4. How are cooldowns and turn completion applied consistently?
5. How do we compare migrated execution against legacy behavior?
6. What is the rollback path if one migrated skill fails?
7. When is a `CastManager` method considered retired?
8. What is the exact gameplay event vocabulary needed for move, damage, status,
   trap, spawn, cooldown, and turn-cost results?

## Acceptance Criteria

Done when:

- At least one migrated skill family executes without relying on
  `CastManager` method logic.
- Execution consumes `ValidatedTacticalAction`.
- Effect values come from `SkillActionDefinition`.
- Cooldown and turn-cost behavior matches current rules.
- Manual Unity scenarios confirm parity for migrated skills.
- Legacy execution remains only for not-yet-migrated skills.
