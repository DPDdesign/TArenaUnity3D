# Reward Design

Status: Initial Design
Project: TArenaUnity3D
Last updated: 2026-06-13

This document defines the reward design framework for TArenaUnity3D runs.
It is gameplay-first: rewards must feel good, scale safely, be quick to choose,
fit cleanly in UI, and meaningfully shape the final army.

## Core Goal

Run rewards are not open-ended stat editing. They are fast decisions about the
direction of the army.

Each reward card should answer one player-facing question:

```text
Do I want more mass, better quality, a new role, a new skill, recovery, or
economy right now?
```

The player should not need spreadsheet-style comparison to make a good choice.

## Design Requirements

Reward choices must:

- feel satisfying as immediate army growth,
- scale across early, mid, and late run stages,
- be fast to understand,
- be easy to present in UI,
- have real impact on the final army,
- avoid opening a full army editor after every battle,
- avoid asking the player to compare hidden formulas or point budgets.

## Reward Families

Use six main reward families.

### 1. Mass

Adds more units to an existing stack.

Examples:

- `+30 Rusher`
- `+12 Wisp`

Player feel:

- the army visibly grows,
- stack value increases,
- the decision is easy to understand.

### 2. Quality

Trades quantity for a stronger class, tier, or unit type.

Examples:

- `60 Rusher -> 22 Axeman`
- `40 lower-class units -> 15 higher-class units`

Player feel:

- the army becomes more elite,
- the player accepts lower count for higher quality,
- the tradeoff is clear without manual calculation.

### 3. Width

Adds a new stack or a new tactical role.

Examples:

- `Add stack: 20 Trapper`
- `Add stack: 10 Healer`

Player feel:

- the army gains a new option,
- the final composition becomes more distinct,
- the run can pivot into a different plan.

### 4. Skill

Adds or unlocks a skill for an existing legal stack.

Examples:

- `Teach Blind to Wisp`
- `Choose a legal Lizard stack: gain Pull`

Player feel:

- a unit becomes more interesting,
- the build opens a new tactical line,
- the player thinks about how to use the unit better.

### 5. Recovery

Restores army value after losses.

Examples:

- `Revive 25% of losses from the last battle`
- `Restore a damaged stack`
- `Recover fallen units from one selected stack`

Player feel:

- relief,
- stabilizing the run,
- protecting an army that is worth saving.

### 6. Economy

Gives run currency or improves a future purchase.

Examples:

- `+80 gold`
- `Next shop: skill offers cost 20% less`

Player feel:

- delaying power for a future payoff,
- preparing for a shop,
- choosing flexibility over immediate army strength.

## One Verb Rule

Each reward card should have one main verb.

Good verbs:

- Add
- Grow
- Promote
- Demote
- Replace
- Teach
- Revive
- Heal
- Earn
- Discount

Avoid cards that combine many unrelated effects, such as:

```text
+20 units, +5 attack, unlock a skill, but lose 10 gold.
```

The reward can have secondary presentation text, but the mechanical operation
should be one clear action.

## 1-Of-3 Choice Shape

The default post-battle reward is a 1-of-3 card choice.

The three cards should usually represent different player intentions:

- Stabilize: recovery, mass, or gold.
- Strengthen: quality, stack growth, or skill.
- Pivot: new stack, replacement, rare skill, or build-changing option.

This prevents the player from comparing three almost-identical numbers. The
choice should feel like:

```text
Do I repair, push my current plan, or change direction?
```

## UI Preview Rules

Cards should show the result, not the formula.

Examples:

```text
Grow
Rusher stack
42 -> 72
More bodies for the final army
```

```text
Promote
Rusher -> Axeman
60 -> 22
Less mass, stronger melee stack
```

```text
Teach Skill
Choose: Wisp / Healer / Specialist
Gain: Blind
```

Rules:

- show `before -> after` when a stack changes,
- show the affected unit or legal targets,
- show the main tradeoff in plain language,
- hide internal point-budget math from the normal card view,
- keep wording short enough for quick scanning.

## Scaling Model

Scaling should happen in the reward generator, not in the player's head.

Internally, cards may use stage, rarity, and budget:

```text
Stage 1 common = low reward value
Stage 2 uncommon = medium reward value
Stage 3 rare = high reward value
```

The player should only see the final concrete effect:

```text
+30 Rusher
40 -> 70
Add stack: 12 Trapper
```

Do not ask the player to spend a raw reward budget after every battle.

## Rarity Rules

Rarity changes the kind of opportunity, not only the size of a number.

Common rewards:

- small stack growth,
- small gold,
- light recovery,
- simple low-risk exchanges.

Uncommon rewards:

- new stack,
- useful skill,
- larger stack growth,
- meaningful class exchange.

Rare rewards:

- strong skill,
- major build pivot,
- unusual stack,
- strong recovery,
- high-impact upgrade or exchange.

Rare rewards should often mean "new possibility", not just "more stats".

## Anti-Micro-Optimization Rules

Rewards should avoid turning the run into an army spreadsheet.

Rules:

- post-battle rewards do not open the full army editor,
- each card proposes a concrete transaction,
- if a choice is needed, keep it local and small,
- examples of local choice: choose which legal stack gets a skill, or choose one
  of 2-3 legal stacks for a replacement,
- do not let the player freely convert arbitrary units after every fight,
- keep full repair/planning decisions mostly in shop nodes.

## Shop Relationship

Post-battle rewards are fast. Shop nodes are the breathing room.

Reward cards:

- fast,
- 1 of 3,
- one concrete operation,
- minimal local targeting.

Shop nodes:

- slower and more deliberate,
- still limited,
- one main run currency,
- offers healing, resurrection, skills, stack purchases, upgrades, and
  exchanges,
- inspired by Mewgenics and Slay the Spire,
- should not become a full army optimizer.

## Minimal Model

```text
RewardCard
- family: Mass / Quality / Width / Skill / Recovery / Economy
- verb: Grow / Promote / Replace / Teach / Revive / Earn
- rarity: Common / Uncommon / Rare
- targetRule: which stacks this can affect
- preview: before -> after
- applyEffect: one concrete operation

RewardChoice
- card 1: Stabilize
- card 2: Strengthen
- card 3: Pivot
```

## First Implementation Guidance

Start with a small authored set of reward templates before building a large
generator.

Recommended first content target:

- 12-18 reward templates,
- all six reward families represented,
- common/uncommon/rare examples,
- at least one clean UI preview for each family,
- stage-based scaling handled by template parameters.

The generator should mix proven templates. It should not invent arbitrary
reward math until the core reward feel is proven.

## Open Questions

1. Which exact unit classes and tiers can promote, demote, or replace each
   other?
2. Which skills can appear as run rewards before account unlocks?
3. How should rarity distribution change across early, mid, and late run?
4. How much recovery is healthy before losses stop mattering?
5. Which card families should be unavailable in specific route maps?
6. What is the minimum UI preview that lets a player choose in under a few
   seconds?
