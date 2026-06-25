# 049C PRD SkillActionDefinition Effect Data Model

- Status: draft
- Type: PRD
- Area: skill data, ScriptableObjects, effect parameters, migration
- Owner: TBD

## Goal

Define the complete effect-data side of `SkillActionDefinition` so every active
skill can eventually be described by SO data instead of hidden `CastManager`
logic, XML flags, or hardcoded method values.

049C does not rebalance skills. Existing values should be copied unless a later
design decision explicitly changes them.

## Relationship To 049A And 049B

049A defines the first validator/UI vertical slice.

049B maps skills by broad target family:

- `Self`
- `UnitTarget`
- `HexTarget`
- `Movement`

049C adds the missing effect-data model:

- damage,
- status,
- heal,
- cleanse,
- trap,
- movement,
- forced movement,
- spawn/split,
- HP/resource cost,
- cooldown,
- turn cost,
- effect timing data needed by execution later.

## Problem Statement

Target legality alone is not enough to replace the legacy skill system.

The project also needs a complete SO description of what a skill does after it
has been validated. Today those effect values and behaviors are distributed
across `CastManager` methods, XML flags, unit runtime state, and presentation
side effects.

If effect data is not migrated into SO definitions, future server-side
validation, AI simulation, and SO-driven execution will still depend on legacy
code as source of truth.

## Goals

- Define effect data categories for all active skill families.
- Keep fields enum-driven and simple.
- Support full active skill migration later.
- Preserve current gameplay values by default.
- Preserve intended skill side effects during migration, but resolve detailed
  per-skill side-effect questions during family-specific 049B grills and
  skill-by-skill 049C mapping.
- Record open questions rather than guessing exact balance.
- Prepare data needed by 049E execution migration.

## Non-Goals

- Do not implement code in this PRD.
- Do not rebalance skill values.
- Do not decide final AI scoring.
- Do not remove `CastManager`.
- Do not design visual polish or VFX timing in detail unless required by effect
  execution data.

## Draft Effect Data Families

## Effect Model Decisions

Each `SkillActionDefinition` should contain an ordered `effects[]` list.

Each effect entry uses:

- `effectType` enum,
- simple serialized fields,
- one target source,
- optional simple fields specific to the selected effect type.

Do not create custom per-skill effect scripts as the default model.

Effects execute in list order. This order is authoring data and should not be
silently resorted by the executor.

Cooldown and turn-cost rules belong in activation/cost rules. HP cost, self
damage, stack modification, movement, damage, status, trap placement, and spawn
are effects.

Effect target selection should use a clear target-source enum that references
validated action output. Examples:

- Actor
- PrimaryUnit
- SelectedUnits
- AffectedUnits
- SelectedHexes
- AffectedHexes
- DestinationHex
- TriggeringUnit

One effect has one target source. If a skill affects actor and enemies, use two
effect entries.

Missing target behavior must be explicit. Default is fail/reject. An effect can
opt into simple `skipIfNoTarget`, for cases such as no-hit Rush where movement
still happens but damage has no primary target.

Preview is a future task. 049C must keep data compatible with future preview,
but does not implement preview.

### DamageEffectData

Potential fields:

- damage mode: real modes discovered from existing skills, using domain names
  rather than legacy method names,
- damage value or modifier,
- defence interaction,
- effect target source,
- repeat count,
- self damage modifier if needed.

Open questions:

- Which legacy skills use temporary `SpecialDMGModificator`?
- Which damage values are fixed and which derive from unit stats?

Direction:

- Damage effects should calculate damage outcome, then apply resulting
  HP/amount changes through the shared StackModification domain API where
  possible.
- Damage should not grow a separate stack mutation model that diverges from
  reward and server-side stack modification.
- Damage modes should be added from real skill migration needs, not invented in
  advance.
- Normal-attack-like skill damage should be represented as a damage mode, not
  as a separate "perform full basic attack" effect unless a real skill requires
  that later.

### StatusEffectData

Potential fields:

- status id,
- effect target source,
- duration,
- stack behavior,
- stat modifiers,
- removable by cleanse,
- source skill id.

Open questions:

- Which existing `AddNewTimeSpell` calls represent reusable status definitions?
- Does each status become its own SO later, or is it embedded in skill data for
  first migration?

Direction:

- Statuses should become separate `StatusDefinition` ScriptableObjects.
- Status SOs migrate together with the first skill that needs them.
- Skill effects should use `ApplyStatus` pointing at a `StatusDefinition`.
- Status definitions should use enum/simple fields and stat modifiers by
  default, not custom scripts.

### HealOrCleanseEffectData

Potential fields:

- heal amount or mode,
- cleanse mode: all debuffs, specific status, all statuses, or other explicit
  enum values discovered from real skills,
- cleanse all negative statuses,
- effect target source.

Open questions:

- Which skills cleanse only negative effects versus specific statuses?

### TrapEffectData

Potential fields:

- trap id,
- duration,
- trigger: on enter,
- target filter,
- triggered effect,
- whether existing trap blocks placement,
- whether trap owner/team matters.

Open questions:

- Can traps stack on a hex?
- Can friendly units trigger own traps?

Direction:

- Trap placement should use `PlaceTrap` pointing at a separate
  `TrapDefinition`.
- Trap definitions migrate together with the skill that places them.
- Trap trigger effects should use the same `EffectEntry` model as skills.
- A triggered trap behaves like applying effects to `TriggeringUnit`, while
  retaining trap owner/source identity for credit and team rules.

### MovementEffectData

Potential fields:

- actor move,
- target move,
- forced move,
- teleport,
- pull,
- line rush,
- movement destination source,
- path required,
- blockers,
- no-hit behavior.

Open questions:

- Which movement skills require normal pathing and which ignore pathing?
- Which target movement effects can move allies, enemies, or both?

Direction:

- Movement is an effect entry, not only implicit data on
  `ValidatedTacticalAction`.
- Use `movementMode` enum values discovered from real skills, such as normal
  path move, teleport, pull, forced move, line rush, or other real cases.
- Movement effects use validated destination/impact/target data rather than
  choosing targets again.

### SpawnOrSplitEffectData

Potential fields:

- spawn unit id,
- amount source,
- split amount rule,
- destination source,
- spawned unit initial state,
- inherited skills,
- removed skills.

Open questions:

- Does `Stone_Throw` remain a split skill exactly as legacy behavior?
- How should spawned stack ids be generated for server-side future?

Direction:

- Avoid a one-off `SplitAndSpawn` custom effect if simple entries can express
  the behavior.
- Prefer separate effects such as `ModifyStackAmount` and `SpawnUnit`.
- Stack amount changes should route through the shared StackModification API.

### StackModificationEffectData

Skill-side stack amount changes must use the same domain API/model as reward
stack modifications. Do not create a battle-skill-only stack modification
contract.

This same API should also be usable by damage resolution after damage is
calculated, because battle damage ultimately changes stack HP/amount state.
StackModification should support both preview and apply so rewards, skill
effects, damage results, and future server validation can share before/after
stack outcome logic.

Potential fields:

- target source,
- operation: add units, remove units, set amount, split amount, create stack,
- amount rule,
- minimum amount rule,
- resulting stack id rule for created stacks,
- preview before/after values.

Open questions:

- Which existing reward stack modification service/model should be reused as
  the canonical API?
- How does battle-local stack modification reconcile back to run/reward stack
  identity?

Direction:

- StackModification is a shared domain model for rewards, skills, damage, and
  future server-side validation.
- It should support amount and temp/front HP state, not only amount.
- It should support preview and apply in the long term.
- Preview modes can include exact, estimated, and range, but preview execution
  is a separate future task.

### CostEffectData

Potential fields:

- HP cost,
- percent HP cost,
- minimum survivor rule,
- cooldown,
- consumes turn,
- allows movement after use,
- repeatable in turn.

Open questions:

- Which skills have hidden HP costs or self penalties in `CastManager`?

## Effect Type Checklist

049C should refine this checklist, not fully map every skill yet:

- Damage
- ApplyStatus
- CleanseStatus
- Heal
- PlaceTrap
- MoveUnit
- ModifyStackAmount
- SpawnUnit
- ApplyHpCostOrSelfDamage
- ApplyPresentationCue later if presentation data needs an effect hook

Per-skill effect mapping should happen through later family-specific grills
using the 049B target family map.

## Initial Grill Questions For 049C

Use these when this PRD is grilled:

1. Are effect data families broad enough, or should damage/status/trap/spawn be
   split differently?
2. Should status definitions be separate SOs or embedded in first-pass skill SO?
3. How should legacy `AddNewTimeSpell` map into data?
4. How do we represent damage derived from unit stats versus fixed values?
5. Which effect data is required for validation/preview, and which can wait for
   execution migration?
6. How do spawned stacks get stable ids later?
7. How do we preserve current cooldown and turn-cost behavior without XML flags?
8. Which legacy side effects are intentional skill behavior and must be
   migrated, and which are obsolete implementation artifacts?

## Acceptance Criteria

Done when:

- Every active skill can be assigned to one or more effect data families.
- Each effect family has simple enum/field data requirements.
- Legacy values are marked "copy existing" unless explicitly questioned.
- Open questions are ready for detailed skill-by-skill grilling.
- 049E can use this PRD as input for execution migration planning.
