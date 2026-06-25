# 049B PRD Skill Target Family Migration Map

- Status: grilled draft
- Type: PRD
- Area: skills, target families, validation migration, ScriptableObjects
- Owner: TBD

## Goal

Create the migration map for active skills by broad target shape before the
full PRD49 skill data and execution migration.

This PRD does not implement the validator and does not migrate exact effect
numbers. It defines how each skill should be classified for the future
`SkillDefinitionAsset` action model created by PRD 049A.

049B is intentionally a map. Exact damage values, cooldown values, status
payloads, and execution effect entries belong to 049C and later implementation
PRDs.

## Relationship To 049A And 049C

PRD 049A:

- creates the first SO + validator + UI vertical slice,
- uses representative acceptance skills,
- does not integrate AI,
- does not rewrite execution.

PRD 049B:

- maps all active skills to broad target families,
- separates active target skills, passives, and stance commands,
- records simple tags and target-shape decisions,
- records deferred or legacy-removal skills,
- does not balance or rewrite effect values.

PRD 049C:

- should define exact full `SkillDefinitionAsset` effect data for migrated
  skills,
- should copy existing damage/status/cooldown/HP-cost values unless explicitly
  changed by design,
- should handle detailed effect params and cleanup-ready data completeness.

## Scope Decisions From Grill

- Use the current skill data and runtime behavior as the migration baseline.
- `SkillDefinitionAsset.skillName` remains the canonical skill id.
- Cooldown and turn-cost values are not tagged in 049B. They are copied later
  from existing code into activation/cost rules.
- Broad effect tags such as `Damage`, `Status`, `Buff`, `Trap`, `Spawn`, and
  `HpCost` are allowed in 049B.
- `Heal` is not listed as an active tag because no current active skill needs
  it.
- `Cleanse` is deferred, not grilled now.
- `Brak_Weny` is legacy-only and should be removed, not migrated.
- Stance commands keep current behavior: one visible command toggles
  `Range/Melee` and swaps the button/icon.
- Stance default is `Range`.
- Stance toggle does not consume turn, has no cooldown, and can be used before
  or after movement.
- Passive skills are listed in their own section with short trigger tags.
- Movement-first targeting is accepted for now, but is UX debt. See
  `_codex/Documentation/ADR_016_PRD049B_MovementSkillTwoStepUXDebt.md`.

## Core Decisions

- Keep target families broad. Do not create one family per unusual skill.
- Use enum tags/simple fields for details.
- Tags should also be valid future enum candidates where useful.
- Active targetable skills get one main target family plus optional tags.
- Passive skills get a passive trigger tag, not a main target family.
- Stance commands get a stance section, not normal `Self` skill treatment.
- Unknown or risky behavior is recorded as an open question.

## Main Target Families

### Self

No external target selection. The actor is the source and target.

Includes:

- self cast,
- self buff,
- no-target area around caster.

Example tags:

- Buff
- Status
- AoE
- Damage

### UnitTarget

The skill resolves one or more unit targets, or auto-selects a unit set by team
filter.

Includes:

- single enemy,
- single ally/self,
- all enemies,
- all allies,
- ordered multi-unit selection.

Example tags:

- Enemy
- Ally
- SelfAllowed
- AllEnemies
- AllAllies
- MultiTarget
- DuplicateTargetsAllowed
- Status
- Damage
- Buff
- Spawn

### HexTarget

The skill resolves one or more board hexes. A hex may be empty or occupied
depending on the skill rule.

Includes:

- area center,
- trap placement,
- empty placement,
- occupied impact hex when explicitly allowed.

Example tags:

- AreaCenter
- AoE
- Trap
- EmptyHex
- WalkableRequired
- Damage

### Movement

The skill changes position or uses path/line movement as part of its legality or
preview.

Includes:

- actor moves,
- target moves,
- pull,
- line/rush,
- move then hit,
- move then area.

Example tags:

- ActorMove
- TargetMove
- Pull
- Line
- MoveThenHit
- MoveThenArea
- PathRequired
- EmptyDestination
- HpCost

## Active Skill Mapping

| Skill id | Current / target behavior summary | Main family | Tags | Target count | Target roles | Key validation notes | Open questions |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Chope | Spin damage around caster. | Self | AoE, Damage | 0 | ActorSelf | Affected units are enemies around actor. | Confirm exact radius from legacy code during 049C. |
| Rush | Straight-line rush. Hits first enemy in line if present; otherwise moves to last legal line hex. | Movement | Line, ActorMove, Damage | 1 | RushLineHex | Legal target is the first occupied enemy hex in the straight line, or the last legal hex if no enemy blocks the line. Other intermediate hexes are not legal targets. Stops before ally/blocker. | Confirm final line range from legacy highlight logic. |
| Double_Throw | Select two enemy targets; same enemy may be selected twice. | UnitTarget | Enemy, MultiTarget, DuplicateTargetsAllowed, Damage | 2 | EnemyUnitHex, EnemyUnitHex | Both targets must contain enemies. Duplicate target is legal. Target order has no gameplay meaning for now. | None for 049B. |
| Axe_Rain | Area attack around selected hex. | HexTarget | AreaCenter, AoE, Damage | 1 | AreaCenterHex | Center hex may be empty or occupied. Exact center/splash damage split belongs to 049C. | Confirm range from legacy code during 049C. |
| Slash | Move to destination, then choose direction/impact hex for area attack. | Movement | ActorMove, MoveThenArea, Damage, PathRequired, EmptyDestination | 2 | MovementDestinationHex, DirectionalImpactHex | First target is legal movement destination. Second target defines direction/impact. This movement-first UX is accepted for now but marked as UX debt. | Future UX may choose impact first and derive movement options. |
| Hate | Single enemy status/taunt-like effect. | UnitTarget | Enemy, Status | 1 | EnemyUnitHex | Target hex must contain enemy. | Confirm detailed status payload in 049C. |
| Insult | Global enemy debuff. | UnitTarget | AllEnemies, Status | 0 | AutoAllEnemies | Current decision: affects all enemies globally. | Confirm final duration/effect values in 049C. |
| Rage | Self buff. | Self | Buff | 0 | ActorSelf | No external target. | Confirm exact buff payload in 049C. |
| Spike_Trap | Place trap on empty board hex. | HexTarget | Trap, EmptyHex, WalkableRequired | 1 | EmptyPlacementHex | Target must be empty, walkable, and must not already contain a trap. | Confirm trap visibility/effect values in 049C. |
| Rope_Trap | Place trap on empty board hex. | HexTarget | Trap, EmptyHex, WalkableRequired | 1 | EmptyPlacementHex | Target must be empty, walkable, and must not already contain a trap. | Confirm trap visibility/effect values in 049C. |
| Tough_Skin | Defensive buff/status on ally or self. | UnitTarget | Ally, SelfAllowed, Status, Buff | 1 | AllyOrSelfUnitHex | Target must contain friendly unit or actor self. | Confirm exact race-dependent payload in 049C. |
| Defence_Ritual | Global defensive buff/status for allies. | UnitTarget | AllAllies, Status, Buff | 0 | AutoAllAllies | Current decision: affects all allies globally. | Confirm duration/effect values in 049C. |
| Force_Pull | Select allied unit, then choose empty destination. | Movement | Pull, TargetMove, Ally, EmptyDestination, MultiTarget | 2 | AllyUnitHex, EmptyDestinationHex | First target must be ally. Second target is chosen by player and must be empty/legal. | Confirm final destination radius from legacy code during 049C. |
| Stone_Stance | Self defensive stance/buff. | Self | Buff, Status | 0 | ActorSelf | No external target. | Confirm exact duration and counterattack rule in 049C. |
| Toxic_Fume | Move to destination, then emit enemy-affecting aura around new caster position. | Movement | ActorMove, MoveThenArea, Status, PathRequired, EmptyDestination | 1 | MovementDestinationHex | First and only target is legal movement destination. Affected enemies are derived around caster after movement. No second area target. | Confirm radius and immobilize/provoke payload in 049C. |
| Shapeshift | Self transformation / stat swap. | Self | Buff, Status | 0 | ActorSelf | No external target. | Confirm exact transformed state and duration in 049C. |
| Long_Lick | Select enemy, then choose empty hex adjacent to caster as pull destination. | Movement | Pull, TargetMove, Enemy, EmptyDestination, MultiTarget | 2 | EnemyUnitHex, AdjacentEmptyDestinationHex | Should work like Force Pull, but enemy-targeted. Destination must be empty and adjacent to caster. Description must be updated; old random-destination wording is obsolete. | Confirm range to enemy in 049C. |
| Blind_by_light | Single enemy blind/status. | UnitTarget | Enemy, Status | 1 | EnemyUnitHex | Target hex must contain enemy. | Confirm exact duration/effect values in 049C. |
| Stone_Throw | Select enemy unit; impact target and spawn split Stone Golem near target. | UnitTarget | Enemy, Damage, Spawn | 1 | EnemyUnitHex | Target must be enemy unit. Empty hex target is not legal. Spawn hex must be empty, adjacent to target, and closest to caster. If no spawn hex exists, skill is illegal. Description must be updated. | Confirm tie-breaker if multiple adjacent spawn hexes are equally close. |
| Fire_Ball | Area attack around selected hex. | HexTarget | AreaCenter, AoE, Damage | 1 | AreaCenterHex | Center hex may be empty or occupied. Exact damage values belong to 049C. | Confirm final radius from legacy code during 049C. |
| Heavy_Fists | Move to destination, then choose direction/impact hex for cone/area attack. Costs HP. | Movement | ActorMove, MoveThenArea, Damage, HpCost, PathRequired, EmptyDestination | 2 | MovementDestinationHex, DirectionalImpactHex | First target is legal movement destination. Second target defines direction/impact. This movement-first UX is accepted for now but marked as UX debt. | Confirm exact area pattern and HP cost in 049C. |

## Stance Mapping

Stance commands are not normal `Self` skills for 049B. They are mode toggles.

Current target behavior:

- one visible stance command per unit,
- toggles `Range/Melee`,
- changes the button/icon,
- default mode is `Range`,
- no turn cost,
- no cooldown,
- usable before or after movement.

| Stance command | Current / target behavior summary | Tags | Target count | Notes |
| --- | --- | --- | --- | --- |
| Range_Stance_Barb | Barbarian stance command toggles Range/Melee and swaps button/icon. | StanceToggle, DefaultRange, ButtonIconSwap | 0 | Keep current one-command UX. Do not require two visible buttons for 049B. |
| Range_Stance_Lizard | Lizard stance command toggles Range/Melee and swaps button/icon. | StanceToggle, DefaultRange, ButtonIconSwap | 0 | Keep current one-command UX. Do not require two visible buttons for 049B. |
| Melee_Stance_Barb | Stance partner/state/icon id, not a separate visible command in current unit data. | StanceState | 0 | Keep asset/code compatibility if needed, but do not model as a separate player target action for now. |
| Melee_Stance_Lizard | Stance partner/state/icon id, not a separate visible command in current unit data. | StanceState | 0 | Keep asset/code compatibility if needed, but do not model as a separate player target action for now. |

## Passive Mapping

Passive skills are listed separately from active target families. Tags are kept
short on purpose; exact trigger payloads belong to 049C.

| Skill id | Current behavior summary | Trigger tags | Notes / open questions |
| --- | --- | --- | --- |
| Cold_Blood | Passive damage reduction plus delayed retaliation-like behavior. | AlwaysOn, TurnEnd | Confirm exact turn-end affected units and damage basis in 049C. |
| Massochism | Passive retaliation based on damage taken. | OnDamaged | Confirm exact trigger: on damaged, on attacked, or previous-turn damage in 049C. |
| Stone_Skin | Passive defensive reduction. | AlwaysOn | Confirm exact reduction rule in 049C. |
| Fire_Movement | Leaves fire trail while moving. | OnMove | Confirm affected hex persistence and stacking in 049C. |
| Fire_Skin | Nearby-unit fire/stat aura. | Aura | Confirm ally/enemy filter, timing, and radius in 049C. |
| Terrifying_Presence | Presence aura affecting nearby units at turn start. | TurnStart | Confirm target filter and exact counterattack rule in 049C. |
| Rotting | Self decay over time. | TurnTick | Confirm minimum HP rule in 049C. |
| Unstoppable_Light | Passive armor pierce on attacks. | OnAttack | Moved from draft UnitTarget row to Passive because asset/XML mark it passive. |

## Deferred / Not In Current Scope

| Skill id | Reason | Future direction |
| --- | --- | --- |
| Cleanse | Exists in legacy `CastManager`, but is not currently in skill assets or unit assignments. User confirmed it is valid as a future skill, but should not consume 049B grill time now. | Revisit when adding asset/unit assignment or when a cleanse-focused PRD is opened. Likely `UnitTarget + Ally + SelfAllowed + Cleanse`. |

## Legacy Removal

| Skill id | Reason | Direction |
| --- | --- | --- |
| Brak_Weny | Legacy placeholder/no-op in `CastManager`; not current active gameplay. | Do not migrate. Remove during legacy skill cleanup after reference audit. |

## Family Grill Decisions Captured

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
- Cooldown and turn-cost values are not part of 049B tags.
- Broad effect tags remain in 049B; exact values move to 049C.

## Non-Goals

- Do not implement code in this PRD.
- Do not rebalance skill values.
- Do not migrate exact effect values here; that belongs in 049C.
- Do not create narrow one-off target families for individual skills.
- Do not add new behavior to `CastManager`.
- Do not solve movement targeting UX here beyond recording the debt.

## Acceptance Criteria

Done when:

- Active targetable skills are listed in the active mapping table.
- Passive skills are listed in the passive section with short trigger tags.
- Stance commands are listed in the stance section and preserve current UX.
- Deferred skills are explicitly separated from migrated scope.
- Legacy-removal skills are explicitly marked.
- Each listed skill has one main family.
- Tags are broad enum candidates, not one-off custom rules.
- Open questions are recorded instead of guessed.
- The table is ready to feed 049C effect-data grilling.
