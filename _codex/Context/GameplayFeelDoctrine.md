# Gameplay Feel Doctrine

Status: Initial Design
Project: TArenaUnity3D
Last updated: 2026-06-25

This document defines the initial feel direction for TArenaUnity3D. It is
marked `Initial Design`: it is a working draft for future grilling, not final
feel doctrine.

## Source Treatment

Initial feel sources:

- `_codex/Context/Zamysł i pomysły.txt` for targeting, phase, cast indicator,
  and skill-flow concerns.
- `_codex/Context/Opisy jednostek invitational.pptx` for unit fantasy and skill
  flavor.
- `_codex/Context/Retsot GDD.md` for legacy influence only.
- the unit catalog and `skills.xml` for current skill ids and UI/info text.
- Current skill context documents for the unit-catalog/reflection skill contract.

Do not turn feel work into balance work. If a feel task changes damage,
cooldowns, targeting, turn rules, unit ownership, public fields, serialized
fields, or XML skill ids, it is no longer only a feel task.

## Target Feel

Initial target feel:

- Tactical: each action should look like a deliberate board decision.
- Readable: the player should know who acts, what is selected, what is legal,
  and what happened.
- Heroes-like: battles should feel closest to Heroes of Might and Magic 3:
  stack units, hexes, movement, attacks, skills, positioning, and loss exchange.
- Skill-expressive: skills should make units feel distinct and create local
  "how do I use this well?" decisions, without replacing the Heroes-like battle
  core.
- Run-driven: outside battle, the player should feel the army growing quickly
  through route, reward, and shop decisions.
- Pressured: phases and possible sudden death should prevent passive waiting.
- Physical enough: hits, throws, traps, pulls, fire, stone, and heavy units
  should have enough animation/VFX/SFX response to be understood.
- Tight: the battlefield should not feel oversized for the number of units and
  decisions currently visible.
- Snappy: animation and spell-entry timing should support quick tactical
  comprehension instead of delaying the result.

## Confirmed Feel Identity

The feel target is:

```text
Mewgenics-like run rhythm, Heroes 3-like battle readability.
```

Mewgenics is a reference for short-run pressure, route/shop/reward pacing, army
growth, and the feeling that each unit or skill choice matters. It is not a
reference for adding uncontrolled randomness or changing the battle core.

Heroes 3 is the closest combat feel reference. The default battle decision is
movement, attack, skill use, positioning, and stack value exchange. Current
units, stats, balance, skills, cooldowns, SFX, and VFX are treated as existing
content, not feel-change targets.

## Current Feel Corrections To Explore

Project director observation recorded on 2026-06-13:

- the map feels somewhat too large,
- the game looks better when there are fewer units in the center,
- current animations feel too slow,
- spells should enter from animation faster,
- a default spell-entry transition around `0.7` feels too long.

Treat these as confirmed feel direction for future correction work, not as
permission to change gameplay or Unity asset values without a specific task.

Correction intent:

- tighten the perceived combat area or engagement distance,
- avoid central visual clutter from too many units,
- audit animation speeds where they slow down readability,
- shorten spell-entry transition timing so cast intent is communicated earlier.

Related ADR: `_codex/Documentation/ADR_003_CombatScaleAndSnappierActionFeel.md`.

## Feel Loop

Initial feel loop:

```text
clear turn cue -> unit/skill selection -> valid target preview -> committed
action -> visible result -> updated tactical state
```

The player should always be able to answer:

- Whose turn or action opportunity is this?
- Which unit is selected?
- Which skill or action is selected?
- Is the target legal?
- What will this probably affect?
- Did the action commit?
- Who was damaged, healed, moved, trapped, buffed, debuffed, killed, or blocked?
- What can I do next?

## Run Feel Loop

The larger run should feel like:

```text
quick pressure -> quick reward -> army grows -> shop gives breathing room ->
final encounter proves the army -> saved army becomes offence/defence value
```

Run battles should be short and readable. The player should feel that the
starting army is weaker than the final army, and that each successful encounter
pushes the army toward a more concrete final composition.

After a good run, the remembered feeling should be:

```text
This is my new army.
```

That army should feel like a concrete artifact of the run, not just abstract
account progress.

## Battle Scale And Pace

Run battles:

- should be fast and low-ceremony,
- should use smaller arenas or smaller active combat spaces than the current
  large map,
- should create contact and meaningful decisions quickly,
- should emphasize clear decision -> action -> result flow,
- should avoid long animation or VFX delays that do not improve readability.

Final, boss, offence, and defence battles:

- can feel more monumental,
- can use larger armies and larger maps,
- should not exceed the current large map as the expected upper scale,
- may support more dramatic framing and payoff,
- should still remain readable and avoid empty travel time.

## Feedback Hierarchy

### 1. Cannot Play Without It

- active unit indication,
- selected unit indication,
- selected skill/action indication,
- valid target feedback,
- invalid target feedback,
- movement/hex availability,
- action commit confirmation,
- damage/heal/status result,
- death/removal result,
- turn or phase change,
- in run UI: current army state, current route/node, reward choice outcome, and
  whether a saved army can be produced.

### 2. Should Be Clear Soon

- skill cast feedback,
- projectile or thrown-object feedback,
- impact feedback,
- cooldown or use failure feedback,
- trap placement and trigger feedback,
- DoT/CC/persistent effect feedback,
- stance changes,
- pull/teleport/movement result,
- phase-based skill unlock or stat buff.
- reward card preview,
- shop offer clarity,
- stack growth/loss preview,
- skill assignment target clarity.

Stack growth/loss previews should eventually use the same domain
StackModification preview model across rewards, skill effects, and battle
damage. A player should learn the likely before/after stack outcome from one
consistent feedback language instead of separate reward-only and combat-only
presentation rules.

### 3. Nice After Basics Work

- extra particles,
- camera accents,
- animation variants,
- richer audio identity,
- UI polish,
- optional voice/unit barks,
- cosmetic presentation.
- route-specific battle map variation.

## Reward And Shop Feel

Detailed reward families, scaling rules, card anatomy, and anti-micro-
optimization rules live in `_codex/Context/Reward_Design.md`.

Post-battle rewards should be card-style, fast, and low-analysis:

- standard reward choice is 1 of 3,
- reward types can be mixed: unit quantity, new stack, skill, upgrade, exchange,
  or currency,
- each card should describe one concrete operation rather than opening a full
  army editor,
- examples: `+30` of a unit, give a skill to a legal stack, exchange one unit
  for another of the same class, upgrade to a higher class with fewer units,
  downgrade to a lower class with more units, or buy a new stack,
- each card should show a simple preview such as `40 -> 70`, `Tier II 30 ->
  Tier III 12`, or `Wisp gains Blind`,
- the choice should feel meaningful but quick, not like arithmetic homework.

Reward selection should eventually receive strong presentation polish while the
player is choosing:

- hovering a card should make the affected army slot feel clearly previewed,
- selecting/applying a card should have a satisfying visual response on the
  changed stack slot, not only text state changes,
- preview, selection, and apply effects should reinforce the feeling that the
  army is physically changing,
- these effects are feel/presentation scope only and must not alter reward
  legality, stack values, counts, targeting, or persistence.

Shop nodes should feel like a breathing room:

- heavier than a reward card choice,
- lighter than a full army optimizer,
- inspired by Mewgenics and Slay the Spire shop pacing,
- one main run currency,
- clear offers for healing, resurrection, stack purchase, skill purchase,
  upgrades, and exchanges.

Shop and reward UI should prevent micro-optimization by constraining choices and
showing the result plainly.

## Movement, Selection, And Targeting Feel

Initial design needs:

- `HighlightSelf`: the active/selected unit should be visually obvious.
- `HighlightUnderTarget`: the hex or target under consideration should be
  readable.
- Valid cast indicator: when the target is castable, show a positive cue.
- Invalid cast indicator: when the target is not castable, show a blocked cue.

The notes suggest a checkmark for valid cast and crossed circle for invalid
cast. Treat these as intent-level symbols, not final UI art.

Targeting must not rely on hidden logic. If a skill cannot be cast, the player
should learn before committing, not after a silent failure.

## Combat And Skill Feel

Skills should communicate three moments when relevant:

1. Cast: the acting unit commits the skill.
2. Travel/projectile: something moves from caster to target or selected hex.
3. Impact: the result lands on a unit, hex, or area.

Not every skill needs all three moments:

- self-buffs may only need cast and caster impact,
- direct target skills may need cast and target impact,
- projectile skills need cast, projectile travel, and impact,
- passive skills should only show feedback when their passive trigger actually
  happens.

Combat feedback should tell the player:

- who caused the result,
- who was affected,
- whether it was beneficial or harmful,
- whether the state changed,
- whether a unit died, moved, became trapped, was pulled, taunted, buffed, or
  debuffed.

Skills are already part of current content. Feel work should make them easier
to understand and time, not rebalance them. The target is for the player to
think "how do I use this unit's skill well here?" while still playing a
Heroes-like battle.

## Phase Feel

The phase system must be felt, not only calculated.

When a phase changes, the player should understand:

- that the phase changed,
- which units or tiers changed,
- which skills unlocked,
- whether any stat buffs happened,
- whether sudden-death pressure is approaching.

Open: exact UI/VFX/audio treatment for phase changes.

## Persistent Effects Feel

The notes explicitly raise damage/CC over time and wall/persistent spell
questions. These effects need durable feedback.

Rules:

- A persistent effect should not become invisible while still affecting play.
- A trap should communicate at least placement ownership/state or trigger state.
- DoT/CC should show duration or continuing threat clearly enough for the player
  to plan around it.
- Walls or blocking effects must show occupied/blocked hexes clearly.

Open: exact duration UI, icons, hex overlays, and cleanup timing.

## Army State Feel

TArena uses stack-like army identity. Run pressure should come from preserving
and growing stacks, not from tracking every tiny HP scratch.

Rules:

- losing many units from a stack should feel meaningful,
- winning with small losses should usually still feel like progress if rewards
  grow the army,
- resurrection and recovery should be visible as valuable shop/reward decisions,
- the final PvE encounter should validate the pre-final army snapshot; winning
  the final should not make the saved army worse than the army that entered the
  final.

## Unit Feel Direction

Initial unit feel should match role:

- Rusher: reckless, fast, forward.
- Thrower: quick ranged pressure, axe/projectile clarity.
- Axeman: heavy melee swings and cleave direction.
- HeavyHitter: slow, brutal, intimidating.
- Trapper: setup, hidden danger, triggered consequence.
- Healer: support, protective, ritual/mystic clarity.
- Specialist: strange repositioning and stone-state clarity.
- Tank: weight, provoke, pull, survival.
- Wisp: light, blind, fragile but disruptive.
- StoneGolem: slow, heavy, split/stone durability.
- FireElemental: area fire, burning trail, aura pressure.
- FleshGolem: strong, cursed, decaying, self-costing power.

These are feel notes, not final animation or asset requirements.

## Readability Rules

Every visual/audio/UI effect must answer at least one question:

- What happened?
- Who caused it?
- Who was affected?
- Was it good or bad for me?
- Did it change the battle state?
- What can I do next?

Effects that are loud but do not answer these questions are not useful yet.

## Recovery Boundaries

Feel work may improve:

- selection clarity,
- targeting clarity,
- commit confirmation,
- hit/death/readable result feedback,
- skill presentation,
- phase feedback,
- current UI cues,
- run reward readability,
- shop readability,
- army growth/loss previews.

Feel work must not silently change:

- damage values,
- cooldowns,
- skill ids,
- skill ownership,
- targeting rules,
- movement values,
- initiative,
- turn consumption,
- public or serialized fields,
- Unity assets/scenes/prefabs without explicit permission,
- unit stats,
- unit balance,
- existing skill effects,
- existing SFX/VFX assets.

## Open Questions For Grill

1. What should feel satisfying in the first 10 seconds of a run battle?
2. What presentation makes final/offence/defence battles feel monumental
   without making them slow or empty?
3. What is the minimum feedback for a legal/illegal target?
4. How should phase changes be communicated?
5. How should skill unlocks be communicated?
6. How should sudden death be warned and then confirmed?
7. Which persistent effects are identity-critical: traps, fire, CC, walls,
   taunt, DoT, auras, all, or a smaller set?
8. Which feedback can be placeholder during recovery, and which must be
    production-readable now?
9. Which reward card categories are easiest for players to understand?
10. How should a saved army preview communicate "this is my new army"?

## What Must Not Be Copied Here

- Do not import feel targets from another project as TArena truth.
- Do not use Retsot's old online/community/product goals as feel requirements.
- Do not use feel work as a reason to rewrite combat or skill ownership.
