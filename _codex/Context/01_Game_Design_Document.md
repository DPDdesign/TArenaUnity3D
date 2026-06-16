# 01 Game Design Document

Status: Initial Design
Project: TArenaUnity3D
Last updated: 2026-06-13

This document is the initial top-level game design charter for TArenaUnity3D.
It is intentionally marked `Initial Design`: it captures the first synthesis of
the available reference material and must be grilled before being treated as
final design truth.

## Source Treatment

Current reference sources:

- `_codex/Context/Zamysł i pomysły.txt` is the strongest TArena-specific design
  intent source.
- `_codex/Context/Retsot GDD.md` is a legacy reference. It may explain origin,
  vocabulary, influences, or old assumptions, but it must not be copied as
  current TArena production truth.
- `_codex/Context/Opisy jednostek invitational.pptx` is a useful reference for
  unit fantasy, tier identity, roles, and skill descriptions.
- `TArenaUnity3D/Assets/Resources/0_Data/UnitCatalog.asset` and its unit
  definition assets are the current runtime unit configuration source for unit
  names, stats, costs, sprites, and skill ids.
- `TArenaUnity3D/Assets/Resources/Data/skills.xml` is the current skill
  description source for skill UI/info text.

When these sources conflict:

1. Current user instruction wins.
2. Verified local Unity behavior wins for current-state claims.
3. XML files describe current runtime data.
4. The invitational deck describes intended unit fantasy and early balance
   intent.
5. Retsot GDD describes legacy origin and should be treated cautiously.

Known source conflict: the invitational deck and `Units.xml` disagree on some
unit stats, movement, initiative, cost, and skill values. Do not silently merge
them. Record deck content as design intent and XML content as current data.

## 0. Current Production Snapshot

- TArenaUnity3D is currently a legacy recovery project.
- The project appears to contain a hex-grid, turn-based battle prototype with
  army selection, unit stats, skill ids, skills, movement, traps, turns,
  highlighting, and local Resources XML data.
- PlayFab, PUN, Photon, backend, and multiplayer surfaces are legacy risks and
  should not define the near-term game direction.
- Current mechanics still need Unity-side verification before they become final
  current-state documentation.
- Feature growth should stay below recovery work until the local battle loop,
  data flow, and dependency map are stable.

## 1. Game Overview

TArenaUnity3D is an initial-design COBA: a `Chess Online Battle Arena`.

Working definition after the 2026-06-13 design grill:

```text
TArenaUnity3D is a run-based tactical army game: the player takes a small
starting army through a short Mewgenics-like route, grows it through battles,
shops, and rewards, proves it in a final PvE encounter, then saves the resulting
army for asynchronous offence and defence battles.
```

Genre and structure:

- run-based tactical progression,
- turn-based hex-grid battles,
- Heroes of Might and Magic 3-like battle structure,
- Mewgenics / Slay the Spire-like route, reward, and shop pacing,
- stack-based army building and preservation,
- skill-driven unit identity,
- asynchronous offence/defence using saved armies.

Legacy influence sources:

- Heroes of Might and Magic 3: hex-based, turn-based battle model and stack
  army feel.
- Mewgenics: short run structure, army-as-run-output, route choice, strange
  unit/skill decisions, and progression/shop feel.
- Slay the Spire: node-route map, 1-of-3 reward pacing, and shop readability.
- Warhammer 40k: limited-point army recruitment.
- League of Legends / MMO ability models: units have active/passive skills that
  differentiate them from plain stat blocks.

## 2. Project Philosophy

TArena should stay small enough to recover.

Production rules:

- Recover and document the local battle loop before building new systems.
- Preserve working local gameplay behavior until it is explicitly replaced.
- Treat old backend, live-service economy, Discord/community integration, and
  monetization as legacy or future scope, not current design requirements.
- Preserve the intended metagame direction: short runs produce saved armies for
  asynchronous offence/defence, but do not build those systems ahead of battle
  recovery.
- Do not let feature growth outrun readable turns, skill clarity, and stable
  data flow.
- Do not rebalance floats or rename serialized/public data casually.
- Keep the game identity centered on tactical army combat, not on backend
  services.

## 3. Structure Of Play

Confirmed intended play structure:

1. Player starts a run with a smaller starting army.
2. The run uses a node-route map in the style of Mewgenics / Slay the Spire,
   not a spatial Heroes-style overworld.
3. The player fights short run battles, chooses quick rewards, visits shops,
   and grows the army toward a final composition.
4. Run battles use the existing Heroes 3-like hex battle core: move, attack,
   skill, positioning, initiative/turn order, and stack losses.
5. The run ends in a controlled PvE final or boss encounter.
6. If the final encounter is won, the pre-final army snapshot is validated and
   can be saved as an offence/defence army. The final encounter proves the army;
   it should not make the saved reward weaker by post-final losses.
7. Saved armies can be used to attack other players' defence armies or be set
   as the player's current defence.

Battle rules, stats, skills, cooldowns, SFX, and VFX are treated as existing
content for now. This document does not authorize rebalancing or redesigning
those systems.

Saved army model:

- A completed run produces a concrete army.
- The player starts with 2 saved army slots.
- Metaprogress can unlock more saved army slots, with a current target range of
  about 3-5 slots.
- Any saved army can be used for offence.
- Only one saved army is selected as the current defence army.
- Offence/defence rewards are ranking plus account experience. They do not
  steal units or destroy the defender's army.
- Ranking should behave like an ELO-style system so attacking stronger or equal
  opponents is encouraged over farming weaker ones.

Reference match modes from initial notes, such as quick/medium/long combat
point modes, are legacy design references. They should not override the run
structure above unless explicitly restored.

## 4. Core Gameplay Principles

### Army Value Matters

The player should not bring unlimited units. A good army is constrained by a
point budget, so composition is a strategic choice before the battle starts.

### Unit Tier Should Matter Without Killing Low Tiers

Initial notes propose a phase system where lower-tier units can unlock value
earlier. This exists to solve the common arena problem where high-tier units
dominate unless low-tier units have timing, cost, or phase advantages.

### Battles Are Heroes-Like, Skills Add Unit Decisions

The current combat identity is closer to Heroes of Might and Magic 3 than to a
combo tactics game. The core decision is still movement, attack, skill use,
positioning, and exchange of stack value. Units are made more distinct because
each unit has its own skill set.

Longer-term design may gently push skill use toward more unit-specific puzzle
decisions, but this is future direction. Do not rewrite current units, stats,
balance, skills, cooldowns, SFX, or VFX under the banner of feel work.

Mewgenics influence here means the player should think about how to use a unit's
skill well in the current situation. It does not mean high randomness, chaos, or
replacing the Heroes-like battle core.

### Positioning Should Be Readable

The player should understand:

- which hex is selected,
- which unit is active,
- which targets are legal,
- where a skill will land,
- who will be affected.

## 5. Combat And Interaction Philosophy

Combat should be tactical and skill-led.

Important combat decisions:

- move now or hold position,
- attack or commit a skill,
- use a low-cooldown utility skill or save a stronger effect,
- expose a unit for damage now or preserve value for later phases,
- use traps, pulls, taunts, AoE, buffs, and debuffs to shape the map.

Initial skill roles visible in the reference material:

- direct damage,
- area damage,
- projectile/throw damage,
- stance toggle,
- mobility/dash,
- pull/teleport,
- taunt/provoke,
- traps,
- healing and defense buffs,
- passive armor penetration,
- passive aura/status effects,
- self-costing power effects,
- summon/split behavior.

Damage, cooldowns, CC, DoT, walls, and persistent spell effects are open design
areas. They should be finalized only after the current implementation is mapped.

## 6. Run, Reward, Shop, And Encounter Philosophy

Run target:

- a complete run should usually last about 20-30 minutes,
- the run starts with a weaker army than the intended final army,
- the army should grow through each successful encounter,
- the emotional rhythm is:

```text
quick pressure -> quick reward -> army grows -> shop gives breathing room ->
final encounter proves the army -> saved army becomes offence/defence value
```

Run map:

- node-route map inspired by Mewgenics and Slay the Spire,
- no requirement for a spatial adventure map,
- different route maps may bias the player toward different army outcomes,
  unit pools, skill pools, rewards, and encounter types,
- route-specific battle map rules are possible future scope, not first-scope
  requirement.

Core node types:

- battle,
- shop,
- recruit/reward,
- final/boss battle.

Event nodes are optional and not yet identity-critical.

Reward model:

- normal battle rewards should be fast 1-of-3 card-style choices,
- rewards can be common, uncommon, or rare,
- mixed reward types are allowed: unit quantity, new stack, skill, upgrade,
  exchange, or currency,
- reward cards should define a concrete transaction instead of opening a full
  army editor,
- the detailed gameplay-first reward framework lives in
  `_codex/Context/Reward_Design.md`,
- examples include `+30` of a unit, give a skill to one legal stack, exchange a
  unit for another unit of the same class, upgrade to a higher class with lower
  quantity, downgrade to a lower class with higher quantity, or buy a new stack,
- cards should preview the result plainly, such as `40 -> 70`, `Tier II 30 ->
  Tier III 12`, or `Wisp gains Blind`,
- rewards should support quick decisions without forcing spreadsheet-style
  optimization.

Shop model:

- the shop is a breathing-room node inspired by Mewgenics and Slay the Spire,
- it uses one main run currency,
- it can offer healing, resurrection, army strengthening, stack purchases,
  skill purchases, and upgrade/exchange opportunities,
- it should offer more planning than a post-battle reward but still avoid
  becoming a full army optimizer,
- the player gets some starting gold at run start; exact carryover rules are
  not identity-critical yet.

Army growth and losses:

- TArena uses stack logic closer to Heroes 3: for example, a stack may represent
  many units with individual HP,
- winning with small losses should usually create net growth through rewards,
- large losses should hurt because they lower final army quality,
- some losses can be recovered through shop/reward healing or resurrection,
- the pressure is to keep the army alive and growing until the final encounter,
  not to micro-manage every point of chip damage.

Encounter model:

- run opponents should be hand-made plus procedural,
- hand-made archetypes define intent, such as "try to win" or "deal as many
  losses as possible",
- procedural generation should create controlled variation inside safe limits,
  not random run-killers,
- constraints should include run stage, point budget, enemy roles, AI goal,
  reward/risk, and encounter type.

## 7. Level, Map, Or Battle Space Philosophy

Initial map identity:

- hex battlefield,
- arena-like confrontation,
- positioning and range matter,
- traps and area effects need readable hex ownership,
- sudden death or score pressure may prevent stalled matches.

A good battle space should support:

- flanking or blocking,
- skill ranges,
- safe and risky hexes,
- trap placement,
- AoE positioning,
- unit body blocking or adjacency pressure if verified,
- readable battle state at a glance.

Battle scale:

- run battles should usually use smaller arenas or smaller active combat areas
  than the current large map,
- run battles should create contact and decisions quickly,
- final, boss, offence, and defence battles can feel more monumental through
  larger armies and larger maps,
- the current large map is treated as an upper limit, not the default size for
  every battle.

Open: exact map sizes, terrain rules, objectives, wall behavior, trap
persistence, and future route-specific battle map variations.

## 8. Progression And Content Philosophy

Metaprogress is important to TArena, but it should expand options rather than
grant flat stat superiority.

Account progress unlocks:

- new units,
- new skills for those units,
- additional saved army slots.

The skill pool can overlap with skills bought during a run. Account progress
expands what can appear in future runs and starting setups; the run decides what
the final army actually becomes.

Initial design content that can be preserved as future-facing vocabulary:

- factions or unit groups,
- unit tiers,
- point-costed army building,
- unlockable units,
- skill sets per unit,
- active and passive skills,
- short run maps with different reward/unit/skill emphasis.

Near-term production rule:

- Do not build account economy, asynchronous offence/defence, or route
  generation before the local battle loop is stable and documented.
- Do not let metaprogress become flat stat scaling that invalidates tactical
  combat.

## 9. Units And Skills Initial Design

The invitational deck and XML show a likely core content set:

### Barbarian Units

- Rusher: basic forward-pressure unit with `Chope` and `Rush`.
- Thrower: ranged axe unit with stance and multi-throw/AoE throw skills.
- Axeman: melee bruiser with cone/cleave, duel/hate, and damage-return flavor.
- HeavyHitter: heavy damage unit with insult, rage, and masochism-style passive.

### Lizard / Forest Units

- Trapper: trap and range-stance unit.
- Healer: support unit with skin/defense rituals.
- Specialist: paranormal utility unit with pull/teleport and stone stance.
- Tank: swamp lizard tank with provoke, shapeshift, and pull/taunt tongue skill.

### Mystic / Golem / Elemental Units

- Wisp: light/blind/armor-penetration unit.
- StoneGolem: slow durable unit with split/throw and stone skin.
- FireElemental: fire movement, fireball, and aura-style debuff.
- FleshGolem: high-power cursed unit with self-costing attacks and decay.

### Runtime XML Also Contains

- `TosterHEAL`,
- `axe1`,
- `TosterDPS`,
- `TosterTANK`,
- `Lizard`.

These may be test, placeholder, legacy, or real content. Do not treat them as
final design content until verified.

## 10. AI Direction

AI is not currently defined as finished design.

Run AI direction:

- different run enemies can use different AI goals,
- one goal type is to make the player suffer as many losses as possible,
- another goal type is to try to win the battle outright.

Offence/defence AI direction:

- first version should use one AI that tries to win,
- the desired direction is chess-like forward analysis,
- future versions may allow different AI types to be assigned to saved defence
  armies, but that is later scope.

## 11. Phase System Initial Design

The phase system is one of the strongest TArena-specific ideas in the current
notes.

Initial concept:

- Battle starts in phase 0.
- Later phases unlock more skills or buff certain unit tiers.
- Lower-tier units may gain earlier access to skills or power, making them
  useful despite weaker stats.
- Higher-tier units may become fully online later.
- The number and length of phases may depend on match mode.

Example from notes:

- Phase 1: Tier I units receive a buff or skill unlock.
- Phase 2: Tier I and Tier II units receive a buff or skill unlock.
- Phase 3: Tier I, Tier II, and Tier III.
- Phase 4: all tiers.

Open:

- whether phases unlock skills, buff stats, or both,
- whether every unit can eventually unlock all skills,
- how many turns each phase lasts,
- how phase pacing differs by quick/medium/long mode,
- how sudden death interacts with phases.

## 12. Scope And Production Rules

In scope for design grilling:

- final identity sentence,
- local battle loop,
- run structure,
- reward and shop model,
- saved army model,
- asynchronous offence/defence identity,
- army building,
- phase system,
- unit tier meaning,
- skill-first combat,
- turn/initiative structure,
- map readability,
- feel and feedback requirements.

Out of scope until explicitly restored:

- PlayFab-driven profiles,
- online statistics,
- multiplayer-first design,
- monetization,
- mobile launch plan,
- Discord/community operations,
- large live-service progression.

Asynchronous offence/defence is a long-term identity direction, but old
PlayFab/PUN/Photon implementations are not automatically preserved.

## 13. Open Questions For Grill

1. Is `COBA` a final public-facing genre label or an internal shorthand?
2. What exact starting army options does a new player have?
3. What exactly can a unit do in one turn: move, attack, skill, wait, all of
   these, or a restricted combination?
4. Are phases global by turn count, by round count, by score, or by another
   clock?
5. Do phases unlock skill slots, improve stats, change cooldowns, or enable
   passives?
6. What ends a match before sudden death?
7. Is sudden death true damage, value comparison, shrinking arena pressure, or a
   mode-specific rule?
8. Which XML units are real design content and which are test leftovers?
9. Which factions are still part of TArena: barbarians, lizards, golems,
    shadows, humans/city, all, or a smaller subset?
10. Which run maps/routes exist first, and which unit/skill/reward pools do
    they bias?
11. Should event nodes exist in first scope, or remain later variety?
12. What manual Unity check proves the first GDD slice is real?

## What Must Not Be Copied Here

- Do not import another project's current state or tasks as TArena truth.
- Do not treat Retsot backend, monetization, mobile, or community goals as
  current TArena requirements unless the user explicitly restores them.
- Do not treat deck balance numbers as runtime truth when XML disagrees.
- Do not treat XML placeholder units as final identity content without user or
  Unity verification.
