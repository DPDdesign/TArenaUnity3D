# 049A PRD Tactical Action Validation Execution UI Vertical Slice

- Status: merged into closed 049ABC; retained as reference
- Type: PRD
- Area: battle actions, skill validation, skill data, deterministic execution,
  UI targeting, preview
- Owner: TBD

## Goal

Reference note - 2026-06-25:

This standalone 049A PRD was merged into
`_codex/tasks/archive/049ABC_PRD_SkillAPIAndFullMigration.md`. Do not start it
as a new active task. Use it only as historical/reference detail for the
validator/UI slice that informed PRD49ABC and future PRD49ED work.

Create the first complete vertical slice of the future tactical action model:
extended `SkillDefinitionAsset` data, a pure snapshot-based validator,
UI targeting/highlight/preview driven by that validator, and deterministic
execution for a small set of representative skills.

This PRD is the foundation for later AI and server-side validation work. The
validator and execution contracts must be shared in shape by player input,
future AI candidate generation, and future authoritative server validation. The
validator checks legality only. It must not judge tactical quality.

049A is not only a validator/UI slice. For the acceptance skills it includes
commit execution through the new generic action/effect layer, not
`CastManager.startSpell()`.

## Problem Statement

Current battle skill legality is distributed across `MouseControler`,
`CastManager`, UI highlight state, reflection methods, and ad hoc AI intent
generation. Tactical AI can currently reason about a skill mostly as "a usable
slot" and then guess targets later. That is not enough for skills whose legal
target is not simply an enemy unit.

Known design-test bugs:

- `Spike_Trap` / `Rope_Trap` can be selected on an occupied enemy hex, even
  though traps should be placed on empty walkable hexes and trigger on entry.
- `Rush` needs a clear line-resolution contract instead of an ambiguous
  "use Rush" intent.
- `Double_Throw` requires two ordered target selections and may target the same
  enemy twice.
- `Heavy_Fists` requires an ordered movement-destination plus impact selection,
  then derives affected units from direction.
- `Force_Pull` requires an ordered target unit selection plus empty destination
  hex selection.

The new model must separate:

- generating legal choices,
- validating a submitted choice,
- previewing normalized consequences,
- executing gameplay effects.

For the 049A acceptance skills, execution is part of the slice. `CastManager`
is legacy/frozen and must not be the source of legality, targeting, or commit
behavior for those skills.

## Locked Implementation Direction

- Extend the existing `SkillDefinitionAsset`; do not add a separate
  `SkillActionDefinition` SO in PRD49.
- `SkillDefinitionAsset.skillName` is the canonical skill id for PRD49.
- `SkillDefinitionAsset` becomes the target source of truth for skill text,
  activation, costs, targeting, resolution, and effect data.
- `skills.xml` is migration/reference input only, not a runtime fallback.
- Presentation remains separate in
  `TArenaUnity3D/Assets/Resources/0_Data/SkillPresentationCatalog.asset`,
  joined by `skillName` / `skillId`.
- PRD49 does not create a second `CastManager`.
- The new action layer uses generic validators, resolution handlers, and effect
  handlers driven by `SkillDefinitionAsset` data.
- Do not add per-skill validator/executor methods such as
  `ValidateSpikeTrap()` or `ExecuteHeavyFists()`.
- For 049A skills, `CastManager` is not in the target selection or commit path.
- Existing low-level runtime primitives may be used by the live apply adapter
  when they are not skill-specific authorities.
- All migrated skill values are copied 1:1 from legacy behavior unless a later
  explicit design task changes balance.
- One game seed drives map generation, rewards, enemies, and damage.
- Damage uses deterministic per-action derived seeds, not direct
  `UnityEngine.Random`.

## Goals

- Extend `SkillDefinitionAsset` as the authoring source for legality,
  targeting, activation cost, and effect parameters.
- Use enum-driven rule data and simple serialized fields only.
- Validate actions from `BattleSnapshot` plus skill/action definitions only.
- Replace legacy skill target legality for the acceptance skills with
  validator-driven legal target state while allowing UI to keep similar visual
  highlight/selection styles.
- Provide preview output from the same validator/resolution path used for
  legality.
- Execute acceptance skills through generic effect handlers and structured
  result events.
- Use deterministic damage from game seed plus battle action context.
- Keep player-facing validation and future server validation aligned.
- Keep the first slice small, but representative enough to test hard target
  families.

## Non-Goals

- Do not integrate Tactical AI with the new API in this PRD.
- Do not migrate every active skill in this PRD.
- Do not remove legacy XML, `CastManager`, or reflection paths in this PRD.
- Do not create custom per-skill rule plugins, polymorphic nested rule objects,
  or custom ScriptableObject sub-rules.
- Do not create a new skill-name manager that mirrors `CastManager`.
- Do not judge tactical value in the validator.
- Do not change gameplay balance values unless a value is migrated unchanged
  from existing skill behavior.
- Do not move VFX/SFX definitions into `SkillDefinitionAsset`.
- Do not edit prefabs, scenes, `.asmdef`, generated Unity files, or assets
  outside the explicitly approved new/changed skill definition assets for this
  PRD.

## Core Decisions

- The shared model is tactical action first, not "move versus skill special
  case".
- `SkillDefinitionAsset` is the authoring source for skill rules.
- C# validators and executors own behavior. SO data describes the skill.
- Rules are expressed through enums and simple fields only.
- All target selections are modeled as an ordered sequence of target hexes.
- Simple skills use one selected hex. Self skills use zero. Multi-step skills
  use multiple ordered hexes.
- Duplicate target hexes are disallowed by default and allowed per skill when
  needed, such as `Double_Throw`.
- `SubmittedActionIntent` contains the player's minimal selection.
- `ValidatedTacticalAction` contains normalized results derived by the
  validator.
- Validator input is only `BattleSnapshot` plus skill/action definitions.
- Skill ownership, cooldowns, and used-skill state are keyed by stable skill id,
  not by UI slot.
- Actor identity should be stable stack id. Current runtime ids may be adapted
  during migration, but the PRD target is stable stack identity.
- For 049A skills, `CastManager` does not consume validated action results and
  is not called for commit. The validator never calls `CastManager`,
  `MouseControler` UI state, or live Unity scene objects.

## Domain Model

### SkillDefinitionAsset

One existing skill asset per skill id. It is the long-term complete description
of the skill.

Minimum fields for 049A:

- stable skill id
- display name
- activation rules
- targeting rules
- resolution rules
- effect family and effect parameters needed by preview/validation
- optional UI style metadata only for visuals, not legality geometry

Long-term fields:

- cooldown
- turn cost
- movement permissions
- damage/heal/status/trap/movement/HP-cost parameters
- presentation hooks

Implementation direction:

- keep the current text/migration fields,
- add grouped serializable data blocks instead of a flat field list,
- use `skillName` as canonical skill id for PRD49,
- keep VFX/SFX in `SkillPresentationCatalog`.

Minimum grouped data:

- `ActivationRuleData`
- `TargetingRuleData`
- `ResolutionRuleData`
- `EffectEntry[]`

### ActivationRuleData

Defines whether a skill can start from the current actor state.

Fields should cover:

- active, passive, toggle
- cooldown turns
- allowed before move / after move
- consumes turn
- allows movement after use
- repeatable in current turn

Current flag migration direction:

- missing `AM` means before-move only.
- `AM` means usable after move.
- missing `NI` means consumes turn.
- `NI` means does not consume turn and may allow movement after use.
- stance toggles are repeatable, no-cost toggles.

### TargetingRuleData

Defines what the player can select.

Fields should cover:

- target count min/max
- ordered target roles by index
- same rule for all targets
- allow duplicate target hexes, default false
- walkable requirement
- occupancy requirement
- team requirement for occupying unit
- range requirement
- simple relation to previous selected hex when needed

Representative target roles:

- None
- Self
- ExactHex
- EmptyPlacementHex
- EnemyUnitHex
- AllyUnitHex
- AreaCenter
- LineDirectionHint
- MovementDestination
- DirectionalImpactHex
- TeleportDestination

### ResolutionRuleData

Defines what the selected target hex sequence means and what normalized outputs
are derived from it.

Fields should cover:

- target hex interpretation: exact hex, direction hint, area center, line
  endpoint, relative impact
- resolution family
- line behavior
- no-hit behavior
- affected hex/unit derivation
- destination/impact derivation

Representative resolution families:

- DirectUnit
- EmptyHexPlacement
- MultiDirectUnit
- LineScan
- MoveThenDirectionalAreaAttack
- TeleportTargetToDestination
- AreaAroundTarget

### SubmittedActionIntent

Minimal untrusted selection from player, UI, AI later, or server command later.

Fields:

- actor stack id
- action id / skill id
- ordered target hexes
- optional client request id for networking/debug

The submitted intent does not contain trusted destination, impact, affected
unit ids, damage, or effect results.

### ValidationResult

Fields:

- status
- reject reason code
- optional debug message
- optional validated tactical action

Status values should include:

- Valid
- Invalid
- UnsupportedLegacySkill
- MissingActionSpec
- BlockedByTurnState

Reject reason must be enum/code-first. Debug text is secondary.

### ValidatedTacticalAction

Normalized legal output.

Fields should cover:

- actor stack id
- action id / skill id
- ordered target hexes
- normalized target hexes
- destination hex
- impact hex
- primary target unit id
- affected unit ids
- affected hexes
- action cost / turn cost
- cooldown to apply
- effect family
- preview data needed by UI

This is domain output, not a `CastManager` instruction payload.

### TacticalActionResult

Structured output from execution. UI and presentation consume this result; they
do not recalculate gameplay effects.

Fields should cover:

- action index,
- actor stack id,
- action id / skill id,
- ordered result events.

Result event examples:

- UnitMoved,
- DamageApplied,
- StatusApplied,
- TrapPlaced,
- TrapTriggered,
- CooldownApplied,
- TurnCostApplied.

### DamageResult

Damage must expose both prediction and applied result.

Fields should cover:

- target unit id,
- predicted min damage,
- predicted max damage,
- expected damage for AI scoring,
- applied damage,
- before/after amount,
- before/after HP.

Preview uses min/max. AI scoring uses expected damage. Commit uses deterministic
applied damage.

### Deterministic Seed Data

`BattleSnapshot` must carry deterministic action data:

- game seed,
- battle id,
- next action index.

Every battle must have an explicit game seed:

- Offline run battle uses the run/game seed.
- Standalone or dev battle uses a serialized/test seed.
- Missing seed is a configuration error, not a reason to generate a random seed.

Damage seed is derived per action/hit:

```text
damageSeed = Hash(gameSeed, battleId, actionIndex, actorId, skillId, targetId, hitIndex)
```

This keeps one game seed while avoiding fragile global RNG consumption order.

## API Shape

The PRD should result in one shared validator surface with operations, not
separate live/planning validators.

Required operations:

- validate a full submitted action intent,
- get legal next target hexes for a partial target sequence,
- preview normalized consequences for a candidate/hovered target sequence,
- execute a validated action through generic effect handlers,
- generate legal actions later for AI using the same rule machinery.

049A implements player/UI operations and the execution path for the acceptance
skills. AI generation is still deferred to a later PRD, but 049A must produce
the data and contracts that AI will consume.

## Generic Effect Handlers For 049A

049A may implement only the generic effect handlers needed by the acceptance
skills:

- `PlaceTrap`
- `TriggerTrap`
- `MoveUnit`
- `TeleportUnit`
- `ApplyDamage`
- `ApplyHpCost`
- `ApplyStatus`
- `ApplyCooldown`
- `ApplyTurnCost`

Handlers must be generic and data-driven. Skill-specific differences belong in
`SkillDefinitionAsset`, `TrapDefinition`, or `StatusDefinition` data.

## UI Requirements

- Legacy `CastManager` target legality is replaced for acceptance skills.
- UI asks the validator for legal next target hexes.
- UI asks the validator/resolution path for preview consequences.
- UI must not duplicate legality geometry.
- UI may keep the current visual style where it works: subtle possible-target
  highlight, stronger hover/selected highlight, and staged multi-target
  selection.
- For broad/global target skills such as traps, UI does not need to
  aggressively highlight the whole map. It may show legal state subtly and
  emphasize hover or selected hexes.
- `CastManager.*M()` does not define target mode for 049A skills.
- If a hex is not legal, it must not be highlighted as selectable.
- If a submitted action is illegal anyway, validator rejects it with a stable
  reason code.

Preview may show:

- selected target hexes,
- destination hex,
- impact hex,
- affected units,
- affected hexes.
- damage min/max,
- deterministic applied result after commit,
- HP cost,
- status applications,
- trap placement and trap trigger consequence where previewable.

Preview must not judge whether the action is tactically good.

## Acceptance Skills

### Spike_Trap / Rope_Trap

Target family: empty hex placement.

Rules:

- one target hex,
- target hex must exist,
- target hex must be walkable,
- target hex must be empty,
- occupied enemy hex is illegal and not highlighted,
- validator rejects occupied target submit,
- execution places a trap through generic `PlaceTrap(trapId, duration=999)`,
- trap trigger behavior is migrated in 049A.

Trigger rules:

- `Spike_Trap` triggers on enter,
- `Spike_Trap` copies legacy values 1:1: damage from trap owner, duration `2`,
  movement modifier `-2`, then removes trap,
- `Rope_Trap` triggers on enter,
- `Rope_Trap` copies legacy values 1:1: duration `1`, legacy
  `SpecialResistance +30` mapped to a domain status field, then removes trap.

### Rush

Target family: line direction hint.

Rules:

- one target hex,
- target hex is interpreted as direction hint, not exact endpoint,
- validator resolves a line from actor through that direction,
- if an enemy is found on the line, normalized output includes target unit,
  impact hex, and destination before impact according to final rule details,
- if no enemy is found, action remains legal,
- no-hit Rush destination is the last legal hex in the line until obstacle,
- execution moves actor to resolved destination,
- if target exists, execution applies damage/status copied from legacy behavior,
- legacy `Rush` temporary status/stat values are copied 1:1.

### Double_Throw

Target family: ordered multi direct unit.

Rules:

- two ordered target hexes,
- each target hex must contain an enemy,
- duplicate target hexes are allowed,
- normalized output includes both target unit entries in selection order,
- execution applies two ordered damage effects,
- legacy damage modifier `+60` is copied.

### Heavy_Fists

Target family: move then directional area attack.

Rules:

- two ordered target hexes,
- target 0 is movement destination,
- target 1 is directional impact hex,
- movement destination must be legal for the actor,
- impact selection must form a legal direction/adjacency relation from the
  movement destination,
- normalized output includes destination, impact, direction, affected units,
  and affected hexes,
- execution moves actor, applies HP cost, and applies area damage,
- legacy HP cost `20` is copied,
- legacy damage modifier `-30` is copied.

### Force_Pull

Target family: teleport target to destination.

Rules:

- two ordered target hexes,
- target 0 selects the unit to pull,
- target 1 selects an empty destination hex,
- destination must be legal according to the skill range/placement rules,
- normalized output includes target unit and teleport destination,
- execution uses generic `TeleportUnit`.

Open behavior to verify from legacy during implementation:

- whether first target can be ally, enemy, or both.

## Snapshot Requirements

Extend `BattleSnapshot` only when validator needs data that is not currently
available.

The snapshot must contain all state required for legality:

- stable actor stack id,
- active stack id where command validation requires it,
- unit positions,
- team ownership,
- alive/actionable state,
- walkability,
- occupancy,
- trap state,
- cooldowns by skill id,
- used skill ids in current turn window,
- movement/action state,
- skill ownership by skill id.
- game seed,
- battle id,
- next action index for deterministic damage.

Validator must not read live Unity objects as a fallback.

## 049A Mini-Tasks

Implement 049A as mini-tasks inside this PRD:

1. Core model and `SkillDefinitionAsset` schema.
2. Validator and resolution framework.
3. Deterministic seed/action-index damage foundation.
4. Generic executor and result events.
5. Runtime apply adapter.
6. Trap definitions and `Spike_Trap` / `Rope_Trap`.
7. `Double_Throw`.
8. `Rush`.
9. `Force_Pull`.
10. `Heavy_Fists`.
11. UI integration for validator-driven legal target states.
12. Preview integration.
13. Automated tests.
14. Manual Unity QA pass.

## Testing Requirements

### Automated Validator Tests

Add focused tests for pure validation logic where feasible.

Required examples:

- trap on empty walkable hex returns valid,
- trap on occupied enemy hex returns reject code for occupied target,
- Rope Trap on empty walkable hex returns valid,
- Rush with no enemy on line returns valid and destination is last legal line
  hex before obstacle,
- Double Throw allows duplicate enemy target hexes,
- Double Throw rejects non-enemy target,
- Heavy Fists rejects invalid ordered pair and validates legal pair,
- Force Pull validates enemy/unit selection plus empty destination,
- Force Pull rejects occupied destination.

### Automated Execution Tests

Required examples:

- trap placement creates `TrapPlaced`,
- entering `Spike_Trap` creates `TrapTriggered`, `DamageApplied`,
  `StatusApplied`, and removes trap,
- entering `Rope_Trap` creates `TrapTriggered`, `StatusApplied`, and removes
  trap,
- Double Throw produces two ordered damage results,
- deterministic damage repeats with the same game seed/action context,
- deterministic damage differs when action index or target/hit index differs,
- Heavy Fists produces move, HP cost, and area damage events.

### Manual Unity Scenarios

Manual Play Mode validation remains required for 049A.

Trap:

- empty walkable hex is highlighted and can be selected,
- occupied enemy hex is not highlighted,
- forced submit against occupied hex is rejected,
- placed trap appears through existing presentation,
- unit entering trap triggers migrated behavior.

Rush:

- selecting a direction with enemy previews impact/target/destination,
- selecting a direction without enemy previews no-hit destination,
- obstacle stops no-hit destination,
- commit uses deterministic damage/result events.

Double Throw:

- first and second enemy selections are highlighted by validator rules,
- same enemy can be selected twice,
- non-enemy hex is not selectable.

Heavy Fists:

- first step highlights legal movement destinations,
- second step highlights only legal directional impact hexes,
- preview shows affected units/hexes and min/max damage,
- commit moves actor, applies HP cost, and damages affected units.

Force Pull:

- first step selects a legal pull target,
- second step highlights legal empty destinations,
- occupied destination is rejected,
- target teleports through result/apply path.

## PRD Split After This Work

This PRD is PRD 049A.

Planned follow-up PRDs:

1. Skill-group grill PRDs by target family, followed by implementation for each
   group.
2. Full active skill migration to complete `SkillDefinitionAsset` data.
3. AI integration so Tactical AI uses only legal actions generated by the new
   API.
4. Execution migration from legacy skill code to SO-driven action execution.
5. Cleanup of legacy XML/flags/highlight/CastManager sources of truth.

## Acceptance Criteria

Done when:

- The PRD A SO schema exists for the acceptance skills.
- The validator reads only snapshot plus skill/action definitions.
- Validator results are stable code-first results.
- UI target highlights for acceptance skills are driven by the validator.
- UI preview uses normalized validator/resolution output.
- Player commit for acceptance skills does not call `CastManager.startSpell()`.
- Execution uses generic effect handlers, not per-skill executor methods.
- Execution returns structured result events.
- Damage uses deterministic game seed plus action context.
- Trap placement and trap trigger behavior are migrated for `Spike_Trap` and
  `Rope_Trap`.
- Presentation remains through `SkillPresentationCatalog`.
- The acceptance skills are covered by automated validator tests where feasible.
- Manual Unity scenarios for each acceptance skill are documented and pass.
- No new legality rule is added to `CastManager`.
- `CastManager` remains only for not-yet-migrated skills.

## Minimal Model

```text
SkillDefinitionAsset
- skillName = Spike_Trap
- activationRule = Active, cooldown 3, consumes turn
- targetingRule = one EmptyPlacementHex, walkable, empty
- effects = PlaceTrap(Spike_Trap, 999)

TrapDefinition
- trapId = Spike_Trap
- trigger = OnEnter
- effects = ApplyDamage(owner -> triggering unit), ApplyStatus(SpikeTrapSlow)

SubmittedActionIntent
- actionType = Skill
- actorUnitId = team-0-slot-1
- skillId = Spike_Trap
- targets = [(3,4)]

ValidatedTacticalAction
- actorUnitId = team-0-slot-1
- skillId = Spike_Trap
- selectedTargetHexes = [(3,4)]
- affectedHexes = [(3,4)]

TacticalActionResult
- actionIndex = 12
- events = TrapPlaced(Spike_Trap, 3,4)
```
