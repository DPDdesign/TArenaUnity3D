# 049AC PRD Skill Target And Effect Data Model

- Status: grilled draft
- Type: PRD
- Area: skills, target families, ScriptableObjects, effect parameters, migration
- Owner: TBD

## Goal

Create one migration map for active skill targeting and effect data so every
current active skill can be represented by `SkillDefinitionAsset` data instead
of hidden `CastManager` logic, XML flags, or hardcoded method values.

049AC supersedes archived drafts:

- `_codex/tasks/archive/049B_PRD_SkillTargetFamilyMigrationMap.md`
- `_codex/tasks/archive/049C_PRD_SkillActionDefinitionEffectDataModel.md`

049AC does not rebalance skills. Existing values should be copied unless a
later design decision explicitly changes them.

## Scope Decisions

- Use the current skill data and runtime behavior as the migration baseline.
- `SkillDefinitionAsset.skillName` remains the canonical skill id.
- The model only covers effects needed by current active skills.
- Do not design open-ended future effect types.
- Keep rules enum-driven and simple.
- Targeting and effect data must be understandable by validator, AI, preview,
  and future API/server validation.
- `CastManager` is migration reference only, not the new authority.
- Presentation remains in `SkillPresentationCatalog`, joined by skill id.

## Relationship To 049A, 049D, And 049E

049A creates the first validator/UI vertical slice.

049AC provides the target and effect data map needed by later migration work:

- target family and target rules,
- activation/cost rule shape,
- ordered effect data,
- active skill mapping,
- passive and stance mapping,
- deferred and legacy-removal decisions.

049D uses this data so AI can generate and score legal actions without guessing
targets.

049E uses this data so execution can consume `ValidatedTacticalAction` plus
skill definition data instead of calling legacy `CastManager` methods.

## Main Target Families

Keep target families broad:

- `Self`
- `UnitTarget`
- `HexTarget`
- `Movement`

Do not create separate target families for passive, stance, trap, AoE, spawn,
status, or damage. Those are tags, activation kinds, or effects.

### Self

No external target selection. The actor is the source and target.

Examples:

- self buff,
- no-target area around caster,
- self transformation.

Example tags:

- `Buff`
- `Status`
- `AoE`
- `Damage`

### UnitTarget

The skill resolves one or more unit targets, or auto-selects a unit set by team
filter.

Examples:

- single enemy,
- single ally/self,
- all enemies,
- all allies,
- ordered multi-unit selection.

Example tags:

- `Enemy`
- `Ally`
- `SelfAllowed`
- `AllEnemies`
- `AllAllies`
- `MultiTarget`
- `DuplicateTargetsAllowed`
- `Status`
- `Damage`
- `Buff`
- `Spawn`

### HexTarget

The skill resolves one or more board hexes. A hex may be empty or occupied
depending on the skill rule.

Examples:

- area center,
- trap placement,
- empty placement,
- occupied impact hex when explicitly allowed.

Example tags:

- `AreaCenter`
- `AoE`
- `Trap`
- `EmptyHex`
- `WalkableRequired`
- `Damage`

### Movement

The skill changes position or uses path/line movement as part of legality or
preview.

Examples:

- actor moves,
- target moves,
- pull,
- line/rush,
- move then hit,
- move then area.

Example tags:

- `ActorMove`
- `TargetMove`
- `Pull`
- `Line`
- `MoveThenHit`
- `MoveThenArea`
- `PathRequired`
- `EmptyDestination`
- `HpCost`

## Active Skill Mapping

| Skill id | Current / target behavior summary | Main family | Tags | Target count | Target roles | Key validation notes | Effect outline / open questions |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Chope | Spin damage around caster. | Self | AoE, Damage | 0 | ActorSelf | Affected units are enemies around actor. | `Damage` to affected enemies. Confirm exact radius from legacy code. |
| Rush | Straight-line rush. Hits first enemy in line if present; otherwise moves to last legal line hex. | Movement | Line, ActorMove, Damage | 1 | RushLineHex | Legal target is the first occupied enemy hex in the straight line, or the last legal hex if no enemy blocks the line. Other intermediate hexes are not legal targets. Stops before ally/blocker. | `MoveUnit` actor; optional `Damage` if primary unit exists. Damage effect may use `skipIfNoTarget`. Confirm final line range. |
| Double_Throw | Select two enemy targets; same enemy may be selected twice. Each throw deals 40% normal ranged damage. | UnitTarget | Enemy, MultiTarget, DuplicateTargetsAllowed, Damage | 2 | EnemyUnitHex, EnemyUnitHex | Both targets must contain enemies. Duplicate target is legal. Target order has no gameplay meaning for now. | One `Damage` effect over `SelectedUnits`, `damageScale = 0.4`. If one unit is selected twice it receives two 40% hits. |
| Axe_Rain | Area attack around selected hex. | HexTarget | AreaCenter, AoE, Damage | 1 | AreaCenterHex | Center hex may be empty or occupied. | `Damage` to affected units. Center/splash split is copied from legacy: center normal, surrounding reduced. Confirm range. |
| Slash | Move to destination, then choose direction/impact hex for area attack. | Movement | ActorMove, MoveThenArea, Damage, PathRequired, EmptyDestination | 2 | MovementDestinationHex, DirectionalImpactHex | First target is legal movement destination. Second target defines direction/impact. Movement-first UX is accepted for now but marked as UX debt. | `MoveUnit` actor, then `Damage` to derived affected units. Confirm area pattern. |
| Hate | Single enemy status/taunt-like effect. | UnitTarget | Enemy, Status | 1 | EnemyUnitHex | Target hex must contain enemy. | `ApplyStatus` to actor and target if legacy mutual-link behavior is retained. Confirm status payload. |
| Insult | Global enemy debuff. | UnitTarget | AllEnemies, Status | 0 | AutoAllEnemies | Affects all enemies globally. | `ApplyStatus` to `AffectedUnits`. Confirm final duration/effect values. |
| Rage | Self buff. | Self | Buff | 0 | ActorSelf | No external target. | `ApplyStatus` to actor. Confirm exact buff payload. |
| Spike_Trap | Place trap on empty board hex. | HexTarget | Trap, EmptyHex, WalkableRequired | 1 | EmptyPlacementHex | Target must be empty, walkable, and must not already contain a trap. | `PlaceTrap(Spike_Trap)`. Trap trigger uses trap definition effects. |
| Rope_Trap | Place trap on empty board hex. | HexTarget | Trap, EmptyHex, WalkableRequired | 1 | EmptyPlacementHex | Target must be empty, walkable, and must not already contain a trap. | `PlaceTrap(Rope_Trap)`. Trap trigger uses trap definition effects. |
| Tough_Skin | Defensive buff/status on ally or self. | UnitTarget | Ally, SelfAllowed, Status, Buff | 1 | AllyOrSelfUnitHex | Target must contain friendly unit or actor self. | `ApplyStatus`. Preserve current unit-type dependent value as data. |
| Defence_Ritual | Global defensive buff/status for allies. | UnitTarget | AllAllies, Status, Buff | 0 | AutoAllAllies | Affects all allies globally. | `ApplyStatus` to `AffectedUnits`. Preserve unit-type dependent value as data. |
| Force_Pull | Select allied unit, then choose empty destination. | Movement | Pull, TargetMove, Ally, EmptyDestination, MultiTarget | 2 | AllyUnitHex, EmptyDestinationHex | First target must be ally. Second target is chosen by player and must be empty/legal. | `MoveUnit` target with teleport/pull mode. Confirm destination radius. |
| Stone_Stance | Self defensive stance/buff. | Self | Buff, Status | 0 | ActorSelf | No external target. | `ApplyStatus` to actor. Confirm duration and counterattack rule. |
| Toxic_Fume | Move to destination, then emit enemy-affecting aura around new caster position. | Movement | ActorMove, MoveThenArea, Status, PathRequired, EmptyDestination | 1 | MovementDestinationHex | First and only target is legal movement destination. Affected enemies are derived around caster after movement. No second area target. | `MoveUnit` actor, then `ApplyStatus` to affected enemies. Confirm radius and payload. |
| Shapeshift | Self transformation / stat swap. | Self | Buff, Status | 0 | ActorSelf | No external target. | `ApplyStatus` or custom status if simple stat modifiers are insufficient. Confirm transformed state and duration. |
| Long_Lick | Select enemy, then choose empty hex adjacent to caster as pull destination. | Movement | Pull, TargetMove, Enemy, EmptyDestination, MultiTarget | 2 | EnemyUnitHex, AdjacentEmptyDestinationHex | Works like enemy `Force_Pull`. Destination must be empty and adjacent to caster. Old random-destination wording is obsolete. | `MoveUnit` target with teleport/pull mode. Confirm range to enemy. |
| Blind_by_light | Single enemy blind/status. | UnitTarget | Enemy, Status | 1 | EnemyUnitHex | Target hex must contain enemy. | `ApplyStatus(Blind)` to target. Confirm duration. |
| Stone_Throw | Select enemy unit; impact target and spawn split Stone Golem near target. | UnitTarget | Enemy, Damage, Spawn | 1 | EnemyUnitHex | Target must be enemy unit. Empty hex target is not legal. Spawn hex must be empty, adjacent to target, and closest to caster. If no spawn hex exists, skill is illegal. | `Damage`, `ModifyStackAmount`, `SpawnUnit`. Confirm tie-breaker for equal spawn hexes and exact split values. |
| Fire_Ball | Area attack around selected hex. | HexTarget | AreaCenter, AoE, Damage | 1 | AreaCenterHex | Center hex may be empty or occupied. | `Damage` to affected units. Confirm radius and scale values. |
| Heavy_Fists | Move to destination, then choose direction/impact hex for cone/area attack. Costs HP. | Movement | ActorMove, MoveThenArea, Damage, HpCost, PathRequired, EmptyDestination | 2 | MovementDestinationHex, DirectionalImpactHex | First target is legal movement destination. Second target defines direction/impact. Movement-first UX is accepted for now but marked as UX debt. | `ApplyHpCostOrSelfDamage`, `MoveUnit`, then `Damage`. Confirm area pattern and HP cost. |

## Stance Mapping

Stance commands are not normal `Self` skills. They are mode toggles.

Current target behavior:

- one visible stance command per unit,
- toggles `Range/Melee`,
- changes the button/icon,
- default mode is `Range`,
- no turn cost,
- no cooldown,
- usable before or after movement.

| Stance command | Current / target behavior summary | Tags | Target count | Effect outline |
| --- | --- | --- | --- | --- |
| Range_Stance_Barb | Barbarian stance command toggles Range/Melee and swaps button/icon. | StanceToggle, DefaultRange, ButtonIconSwap | 0 | `SetStanceMode` or `ToggleStance`; preserve current stat modifiers. |
| Range_Stance_Lizard | Lizard stance command toggles Range/Melee and swaps button/icon. | StanceToggle, DefaultRange, ButtonIconSwap | 0 | `SetStanceMode` or `ToggleStance`; preserve current stat modifiers. |
| Melee_Stance_Barb | Stance partner/state/icon id, not a separate visible command in current unit data. | StanceState | 0 | Keep compatibility if needed; do not model as separate player target action. |
| Melee_Stance_Lizard | Stance partner/state/icon id, not a separate visible command in current unit data. | StanceState | 0 | Keep compatibility if needed; do not model as separate player target action. |

## Passive Mapping

Passive skills are listed separately from active target families. Passive
effects should still be represented as data, status definitions, or tight
trigger hooks so validator/API work can reason about them later.

| Skill id | Current behavior summary | Trigger tags | Effect outline / open questions |
| --- | --- | --- | --- |
| Cold_Blood | Passive damage reduction plus delayed retaliation-like behavior. | AlwaysOn, TurnEnd | Basic status plus custom `OnExpire` or `OnTurn` hook that emits damage effects. Confirm affected units and damage basis. |
| Massochism | Passive retaliation based on damage taken. | OnDamaged | Custom hook records damage and emits future damage/pure damage effect. Confirm trigger timing. |
| Stone_Skin | Passive defensive reduction. | AlwaysOn | Basic/custom status modifying flat damage reduction. Confirm exact reduction rule. |
| Fire_Movement | Leaves fire trail while moving. | OnMove | Custom hook emits `PlaceTrap(Fire_Trap)` on old hex. Confirm persistence and stacking. |
| Fire_Skin | Nearby-unit fire/stat aura. | Aura | Custom `OnExpire` or turn hook emits `ApplyStatus(Fire_Skin_Debuff)` to nearby units. Confirm filter and radius. |
| Terrifying_Presence | Presence aura affecting nearby units at turn start. | TurnStart | Custom hook emits debuff/status effects. Confirm target filter and counterattack rule. |
| Rotting | Self decay over time. | TurnTick | Custom or self-damage effect with minimum survivor rule. Confirm minimum HP behavior. |
| Unstoppable_Light | Passive armor pierce on attacks. | OnAttack | Status/effect modifying defense penetration for outgoing attacks. |

## Deferred / Not In Current Scope

| Skill id | Reason | Future direction |
| --- | --- | --- |
| Cleanse | Exists in legacy `CastManager`, but is not currently in skill assets or unit assignments. User confirmed it is valid as a future skill, but should not consume this PRD now. | Revisit when adding asset/unit assignment or when a cleanse-focused PRD is opened. Likely `UnitTarget + Ally + SelfAllowed + CleanseStatus`. |

## Legacy Removal

| Skill id | Reason | Direction |
| --- | --- | --- |
| Brak_Weny | Legacy placeholder/no-op in `CastManager`; not current active gameplay. | Do not migrate. Remove during legacy skill cleanup after reference audit. |

## Effect Model

Each `SkillDefinitionAsset` action entry should contain an ordered `effects[]`
list.

Each effect entry uses:

- `effectType` enum,
- one target source,
- simple serialized fields for that effect type.

Effects execute in list order. This order is authoring data and should not be
silently resorted by the executor.

One effect has one target source. If a skill affects actor and enemies, use two
effect entries.

Missing target behavior is fail/reject by default. An effect can explicitly set
`skipIfNoTarget`, for cases such as no-hit `Rush` where movement still happens
but damage has no primary target.

## Minimal Effect Types

Only add effect types required by current active skills:

- `Damage`
- `ApplyStatus`
- `PlaceTrap`
- `MoveUnit`
- `ModifyStackAmount`
- `SpawnUnit`
- `ApplyHpCostOrSelfDamage`
- `SetStanceMode` or `ToggleStance`

Do not add future-only effect types.

`Heal` and `CleanseStatus` are not required for current active skill migration.
`CleanseStatus` can be added later when `Cleanse` becomes active again.

## Target Sources

Effect target selection should reference validated action output. Candidate
target-source enum values:

- `Actor`
- `PrimaryUnit`
- `SelectedUnits`
- `AffectedUnits`
- `SelectedHexes`
- `AffectedHexes`
- `DestinationHex`
- `TriggeringUnit`

Do not let effects choose live scene targets again after validation.

## Activation And Cost Rules

Cooldown and turn-cost rules belong in activation/cost rules, not `effects[]`.

Examples:

- `cooldownTurns`
- `consumesTurn`
- `canUseAfterMove`
- `canMoveAfterSkill`
- `repeatableInTurn`
- `activationKind`: active, passive, stance command

HP cost, self damage, movement, damage, status, trap placement, spawn, and stack
changes are effects.

## Damage Effect

Damage modes are domain enum values discovered from current skills, not legacy
method names and not future guesses.

Minimum current damage modes:

- `BasicAttackDamage` - normal attack/defense/random/amount formula.
- `RangedBasicAttackDamage` - current ranged shot behavior, no counterattack by
  default.
- `FixedDamageThroughDefense` - fixed base value recalculated through attack
  and defense, used by skills such as `Stone_Throw`.
- `PureDamage` - applies already-calculated damage directly.
- `DamageOverTime` - status/trap tick damage.
- `PercentOfDamageTaken` - used by passive effects such as `Cold_Blood` and
  `Massochism`.

Common fields:

- `damageMode`
- `targetSource`
- `damageScale`
- `fixedDamageValue`
- `repeatCount`
- `allowsCounterattack`
- `skipIfNoTarget`

`allowsCounterattack` defaults to `false`.

Use `damageScale` for per-skill tuning. Example:

- `Double_Throw` uses one `Damage` effect over `SelectedUnits` with
  `damageScale = 0.4`.
- If the same unit is selected twice, the effect applies two 40% hits.

Do not model `Double_Throw` as a hidden temporary self-status.

Damage should eventually calculate outcome, then apply HP/amount changes
through the shared StackModification domain API where possible.

## Status Definitions

Statuses become separate `StatusDefinition` ScriptableObjects.

Skill effects use `ApplyStatus` pointing at a `StatusDefinition`.

Default status definitions should use simple fields:

- duration,
- stack behavior,
- stat modifiers,
- damage-over-time,
- outgoing damage modifier,
- incoming resistance modifier,
- flat damage reduction,
- defense penetration,
- counterattack modifier,
- `isDebuff`,
- `canBeCleansed`,
- source skill id.

Custom statuses are allowed only where current skills require them.

Custom status behavior should be tight:

- `OnApply`
- `OnExpire`
- `OnTurn`
- possibly `OnMove`, `OnAttack`, or `OnDamaged` when required by current
  passive skills.

Custom hooks must emit predictable effect data or gameplay result events. They
should not directly mutate hidden state in a way validator/API/preview cannot
reconstruct.

## Trap Definitions

Trap placement uses `PlaceTrap` pointing at a separate `TrapDefinition`.

Trap definitions migrate together with the skill that places them.

Trap trigger effects reuse the same `EffectEntry` model as skills, usually
targeting `TriggeringUnit`.

Trap definitions should include:

- trap id,
- duration,
- trigger kind,
- target filter,
- triggered effects,
- owner/source identity,
- whether existing trap blocks placement.

Accepted target rule:

- traps require empty walkable hex,
- traps cannot stack with another trap on the same hex.

## Movement Effect

Movement is an effect entry, not only implicit data on
`ValidatedTacticalAction`.

Movement effects use validated destination/impact/target data rather than
choosing targets again.

Candidate movement modes needed by current skills:

- normal path move,
- line rush,
- pull/teleport target,
- move-then-area.

Fields:

- `movementMode`
- `targetSource`
- `destinationSource`
- `requiresPath`
- `requiresEmptyDestination`
- `skipIfNoTarget`

## Spawn And Stack Modification

Use separate effects where possible:

- `ModifyStackAmount`
- `SpawnUnit`

Avoid a one-off `SplitAndSpawn` custom effect if simple entries can express the
behavior.

Stack amount changes must route through the shared StackModification domain
API/model used by rewards, skills, damage, and future server-side validation.

StackModification should support:

- amount,
- temp/front HP state,
- preview and apply,
- created stack id rule,
- battle-local to run/reward identity reconciliation.

Open question:

- `Stone_Throw` needs final split amount, spawn amount, and tie-breaker rules
  for multiple equally valid adjacent spawn hexes.

## Family Decisions Captured

- `Passive` is a separate section, not mixed into main target families.
- `Stance` is a separate section, not mixed into `Self`.
- Stance default is `Range`.
- Stance keeps current one-command toggle UX.
- `HexTarget + AreaCenter` can use an empty or occupied center hex.
- Do not add extra `EmptyHexAllowed` / `OccupiedHexAllowed` tags for normal
  area-center skills.
- Traps require an empty walkable hex and cannot stack with another trap on the
  same hex.
- `Double_Throw` allows duplicate target selection; target order has no
  gameplay meaning for now.
- `Rush` may legally miss and move if no enemy is in the line.
- `Rush` target choice is constrained to the first occupied enemy hex in line
  or the last legal line hex if no enemy blocks the path.
- Movement skills keep movement destination as the first target for now, but
  this is UX debt.
- `Toxic_Fume` has only one selected target: movement destination.
- `Long_Lick` is changed to behave like enemy `Force_Pull`: enemy target plus
  player-chosen adjacent destination.
- `Stone_Throw` is changed to require an enemy unit target, not an empty hex.
- `Insult` is global `AllEnemies`.
- `Defence_Ritual` is global `AllAllies`.
- `Tough_Skin` allows self.
- `Chope` affects enemies around caster.
- Cooldown and turn-cost values live in activation/cost rules.

## Non-Goals

- Do not implement code in this PRD.
- Do not rebalance skill values.
- Do not decide final AI scoring.
- Do not remove `CastManager`.
- Do not create narrow one-off target families for individual skills.
- Do not add new behavior to `CastManager`.
- Do not solve movement targeting UX here beyond recording the debt.
- Do not design visual polish or VFX timing unless required by effect
  execution data.

## Acceptance Criteria

Done when:

- Active targetable skills are listed in the active mapping table.
- Passive skills are listed with trigger tags and effect direction.
- Stance commands are listed and preserve current UX.
- Deferred skills are explicitly separated from migrated scope.
- Legacy-removal skills are explicitly marked.
- Each active targetable skill has one main family.
- Tags are broad enum candidates, not one-off custom rules.
- Each current active skill has a minimal effect outline.
- Effect types are limited to current migration needs.
- Open questions are recorded instead of guessed.
- 049D and 049E can use this PRD as input for AI and execution planning.
