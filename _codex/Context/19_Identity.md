# 19 Identity

Status: Initial Design
Project: TArenaUnity3D
Last updated: 2026-06-13

This file is the identity guardrail for TArenaUnity3D. It is marked
`Initial Design`: it captures the first synthesis from local references and
should be grilled before being treated as final identity truth.

## Source Treatment

Use these sources for initial identity work:

- `_codex/Context/Zamysł i pomysły.txt` for the clearest TArena-specific
  identity intent.
- `_codex/Context/Retsot GDD.md` for legacy origin and influences only.
- `_codex/Context/Opisy jednostek invitational.pptx` for unit fantasy and
  role language.
- `TArenaUnity3D/Assets/Resources/0_Data/UnitCatalog.asset` and its unit
  definition assets for current runtime unit and skill assignment data.

Do not copy Retsot's backend, monetization, mobile, or community goals into
TArena identity unless the user explicitly restores them.

## Core Identity Sentence

Confirmed identity direction after the 2026-06-13 design grill:

```text
TArenaUnity3D is a run-based tactical army game: the player builds a stack army
through short Mewgenics-like routes, fights Heroes of Might and Magic 3-like
hex battles, proves the army in a final PvE encounter, and saves that exact run
army for asynchronous offence and defence.
```

Short internal label:

```text
COBA - Chess Online Battle Arena.
```

Open: whether `COBA` remains public-facing, internal shorthand, or a temporary
working label. The stronger current identity label is:

```text
Mewgenics-like run, Heroes 3-like battles.
```

## Player Role

The player is an army builder and battle commander. They choose a starting army,
grow it through a short run, command its stack units in hex battles, then decide
whether the completed army is worth saving for offence or defence.

What the player does:

- chooses or builds a smaller starting army,
- travels through a short node-route run,
- grows the army through battles, rewards, shops, skills, and stack changes,
- controls units separately during battle,
- chooses movement, targeting, attacks, skills, waits, or other turn actions,
- uses positioning, stack value, and unit skills to win Heroes-like battles,
- preserves enough army strength to pass the final PvE encounter,
- saves completed-run armies into a small army roster,
- uses saved armies for asynchronous offence or selects one as current defence.

The player is not currently defined as:

- a single hero,
- an action-game avatar,
- a backend profile,
- a live-service account,
- a shop/economy user first.

Those may exist later, but they should not define identity during recovery.

## Main Toy

The main toy is not raw damage numbers. It is producing a concrete army through
a run, then proving that army in readable hex battles.

Core toys:

- a small starting army that grows into a stronger final army,
- stack preservation and stack growth,
- quick 1-of-3 reward card decisions,
- shop decisions around healing, resurrection, skills, and upgrades,
- saved armies as run artifacts,
- asynchronous offence/defence using saved armies,
- Heroes-like positioning, turn order, and stack value exchange,
- unit skills that create local tactical decisions,
- traps, pulls, taunts, stance changes, AoE, projectiles, buffs, debuffs, and
  persistent effects.

## Main Decision

Confirmed main decision:

```text
How do I grow and preserve this run's army so that it can beat the final PvE
test and become a saved offence/defence army?
```

Repeated run questions:

- Which route, battle, reward, or shop choice best shapes my final army?
- Do I take more quantity, a higher class with fewer units, a new stack, a new
  skill, or a recovery option?
- Do I spend currency to repair the current army or push its build direction?
- Is this army becoming something worth saving?

Repeated battle questions:

- Do I spend this unit's action on movement, attack, skill, or setup?
- Do I expose a valuable unit to create pressure?
- Do I accept stack losses to win faster?
- Do I use a skill now or preserve a better position/timing?
- Do I win by damage, control, traps, positioning, or preserving army value?

## Identity Pillars

### 1. Run-Built Armies

The player should remember a successful run as "this is my new army." A run is
not only a source of abstract currency; it produces a concrete army that can be
saved and used.

Production rule: do not let rewards, shops, or account progress become detached
from the army the player is building in the run.

### 2. Heroes-Like Hex Battles

Battles should feel closest to Heroes of Might and Magic 3: stack armies on a
hex battlefield, movement, attacks, skills, positioning, initiative/turn order,
and exchange of losses.

Production rule: do not replace the battle core with real-time action, auto
battling, or pure skill-combo puzzles. Skill decisions should enrich the
Heroes-like structure, not erase it.

### 3. Mewgenics-Like Run Decisions

Mewgenics influence means short runs, route choices, strange unit/skill
decisions, and thinking about how to use specific unit abilities well. It does
not mean uncontrolled randomness or chaotic battle rules.

Production rule: run rewards should create fast build decisions without forcing
spreadsheet-style army optimization.

### 4. Skill-Forward Units

Units are not only HP/attack/defense blocks. Their identity comes from active
and passive skills: throws, traps, pulls, stances, taunts, rituals, fire, stone,
light, decay, and self-costing power.

Production rule: skill readability and skill-id stability are identity-critical.
Do not rename skill ids or rewrite skill ownership casually.

### 5. Account Progress Expands Options

Metaprogress unlocks more units, more skills for those units, and more saved
army slots. It should expand the range of possible runs and final armies.

Production rule: account progress should primarily add options, not flat stat
superiority that invalidates tactical combat.

### 6. Asynchronous Army Challenge

Saved armies matter because they can be used for offence or selected as the one
current defence army. Offence/defence is not active PvP; the defender is played
by AI.

Production rule: offence/defence rewards should be ranking plus account
experience, not stealing units or destroying another player's saved army.

### 7. Recovery-First Tactical Core

TArena's future identity must survive the removal or isolation of PlayFab, PUN,
Photon, multiplayer, and backend surfaces.

Production rule: the run, local battle, saved army, and asynchronous challenge
identity should survive cleanup. Legacy backend implementations are not
identity.

## Non-Identity

TArenaUnity3D should not currently become:

- backend-first,
- multiplayer-first during recovery,
- a shop/economy prototype,
- a mobile monetization project,
- a generic RPG battler,
- a generic auto-battler,
- a real-time action game,
- a spreadsheet army optimizer,
- a pure deckbuilder with army visuals,
- a feature-growth project before the local battle loop is stable.

Legacy Retsot references may mention online, PlayFab, community, Discord,
rewards, Toster Coins, ads, premium, mobile, and release schedules. These are
not current identity pillars.

## Unit Identity Notes

Initial unit identity from the deck and XML:

- Rusher: fast barbarian pressure; reckless forward action.
- Thrower: ranged axe pressure; stance and repeated throwing.
- Axeman: melee bruiser; cone/cleave and duel pressure.
- HeavyHitter: heavy barbarian threat; insult, rage, and damage retaliation.
- Trapper: lizard/forest engineer; visible and hidden trap control.
- Healer: lizard support; skin, ritual, protection.
- Specialist: utility/paranormal lizard; pull, reposition, stone stance.
- Tank: swamp lizard defender; provoke, shapeshift, tongue pull.
- Wisp: light unit; blind and armor-piercing identity.
- StoneGolem: slow durable construct; split/throw and stone skin.
- FireElemental: fire zone pressure; fire movement, fireball, aura.
- FleshGolem: cursed high-tier abomination; high power with self-decay/cost.

Open: which of these are final TArena factions and which are legacy Retsot
content to preserve only as reference.

## Legacy Preservation Rules

Preserve until explicitly replaced:

- unit skill ownership loaded from the unit catalog,
- skill strings as stable ids across the unit catalog, UI, skill info, and
  `CastManager`,
- hex-grid battle identity,
- Heroes-like stack battle identity,
- run-produced saved army identity,
- account progress as option expansion, not flat stat dominance,
- army point-cost concept,
- turn/initiative-based unit control if verified in Unity,
- clear distinction between design intent and runtime data.

Do not preserve by default:

- PlayFab as identity,
- PUN/Photon as identity,
- online account statistics as identity,
- old release schedule,
- old monetization model,
- old community plan.

## Identity Tests

A proposed feature supports TArena identity if it strengthens at least one of:

- meaningful army composition,
- meaningful run growth,
- saved-army value,
- hex positioning,
- skill timing,
- tier/phase pressure,
- readable tactical state,
- local battle recovery.

A proposed feature weakens identity if it:

- makes the hex map irrelevant,
- makes all units feel like interchangeable stat blocks,
- makes rewards require heavy arithmetic after every battle,
- makes account progress mostly flat power,
- makes saved armies irrelevant,
- turns the project into backend recovery for its own sake,
- adds progression/economy before battle clarity,
- hides core decisions behind unclear VFX, UI, or data drift.

## Open Questions For Grill

1. Is `COBA` the final public identity label, an internal label, or obsolete?
2. Which first route maps should exist, and what unit/skill/reward pools should
   they bias?
3. Which factions and units are identity-critical?
4. Should optional event nodes become core to run identity later?
5. How should account unlock pacing expose new units and skills without flat
   power creep?
6. Which current Unity behavior must never break during recovery?
