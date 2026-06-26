# TArenaUnity3D Design And Gameplay Context Map

Status: active
Last updated: 2026-06-26

## Use When

Use this map when defining or reviewing game design, identity, feel, gameplay,
levels, difficulty, rewards, shops, current mechanics, or design-grill work.

## Guided Design Fill Workflow

Use this when the user asks to fill project design context, especially
"Uzupelnijmy GDD", "Uzupelnijmy identity", "Uzupelnijmy feel", or
"Uzupelnijmy gameplay".

Read:

- `_codex/Context/01_Game_Design_Document.md`
- `_codex/Context/19_Identity.md`
- `_codex/Context/GameplayFeelDoctrine.md`
- `_codex/Context/Reward_Design.md` when rewards, shops, reward card UI,
  army growth, or reward scaling are involved
- `_codex/Context/08_Current_Mechanics.md`
- `_codex/Context/02_Current_State.md`
- `_codex/Context/CONTEXT-MAP.md`

Rules:

- Ask questions in small batches, usually 3-5 questions.
- Do not fill unknown TArena answers from Retsot or another project.
- Treat user answers as design truth only for TArena.
- Treat local code and Unity verification as current-mechanics truth.
- If an answer is uncertain, write it under open questions.
- Keep feature growth below recovery work until the local loop and legacy
  dependency map are stable.
- Treat `Status: Initial Design` documents as working drafts for grilling, not
  final project truth.

## Design Grill Workflow

Use this when the user wants to grill, harden, or finalize the initial design
documents.

Primary grill PRDs:

- `_codex/tasks/006_PRD_Grill_GDD_InitialDesign.md`
- `_codex/tasks/007_PRD_Grill_Identity_InitialDesign.md`
- `_codex/tasks/008_PRD_Grill_Feel_InitialDesign.md`

Rules:

- Use the matching PRD as the agenda for the grilling session.
- Keep the target document open as the thing being refined.
- Convert confirmed answers into the target context document.
- Keep uncertain answers as open questions.
- Do not promote `Initial Design` claims to final truth until the user confirms
  them.
- For current-state claims, require code inspection or Unity-side verification.
- For skill architecture grills, use `_codex/Context/10_Skill_Design_Rules.md`;
  plan skills as `ScriptableObject` definitions with enum/rule data, with code
  owning validation and execution.
- Keep GDD, Identity, and Feel separate: GDD defines intended play structure and
  scope; Identity defines what makes the game recognizably TArena; Feel defines
  how player feedback should communicate decisions and results.

## GDD Context

Use when defining or updating the overall game design:

- `_codex/Context/01_Game_Design_Document.md`
- `_codex/Context/19_Identity.md`
- `_codex/Context/GameplayFeelDoctrine.md`
- `_codex/Context/08_Current_Mechanics.md`
- `_codex/Context/02_Current_State.md`

The GDD is the high-level design charter. It should explain the intended game,
the main playable structure, production boundaries, and open design questions.
It should not become a current-code inventory or a copied design document from
another project.

Current high-level design direction from the 2026-06-13 grill:

- Mewgenics-like short run structure,
- Slay the Spire-like node route, reward, and shop readability,
- Heroes of Might and Magic 3-like stack battles on hexes,
- completed runs produce saved armies,
- saved armies are used for asynchronous offence/defence,
- account progress unlocks more units, skills, and saved army slots rather than
  flat stat power.

## Identity Context

Use when a task touches core identity, player fantasy, main player decision,
genre boundaries, what to preserve from legacy behavior, or whether an idea
makes the game more distinct or more generic:

- `_codex/Context/19_Identity.md`
- `_codex/Context/01_Game_Design_Document.md`
- `_codex/Context/02_Current_State.md`

Identity claims must be concrete: what the player sees or does, and what
production rule follows.

Current identity headline:

```text
Mewgenics-like run, Heroes 3-like battles.
```

The player should remember a successful run as "this is my new army."

## Feel Context

Use when a task touches game feel, combat feel, hit/death/skill feedback,
movement and selection feedback, camera, VFX, SFX, UI cues, pacing, payoff, or
readability:

- `_codex/Context/GameplayFeelDoctrine.md`
- `_codex/Context/Reward_Design.md` when run reward choices, shop feel, reward
  card previews, or army-growth feedback are involved
- `_codex/Context/19_Identity.md`
- `_codex/Context/08_Current_Mechanics.md`
- `_codex/Context/10_Skill_Design_Rules.md` when skill presentation is involved

Feel work should improve clarity and player feedback without silently changing
damage, cooldowns, targeting, unit ownership, turn rules, or serialized/public
field contracts.

Current feel split:

- run battles should be fast, readable, and smaller in active combat space,
- final, boss, offence, and defence battles can be more monumental through
  larger armies and larger maps, with the current large map treated as an upper
  scale limit,
- reward cards should be quick 1-of-3 choices with plain previews,
- shop nodes should feel like breathing room, inspired by Mewgenics and Slay
  the Spire, without becoming full army optimizers.

## Gameplay And Level Context

Use when designing or reviewing gameplay, maps, levels, AI, or difficulty:

- `_codex/Context/01_Game_Design_Document.md`
- `_codex/Context/19_Identity.md`
- `_codex/Context/GameplayFeelDoctrine.md`
- `_codex/Context/BattleActionRules.md` when the task touches WAIT, DEFENCE,
  stance toggles, post-move skill legality, `NI`, `AM`, or skill-repeat rules
- `_codex/Context/Reward_Design.md` when reward or shop choices are involved
- `_codex/Context/08_Current_Mechanics.md`
- `_codex/Context/05_Level_Design_Rules.md`
- `_codex/Context/07_Current_Level_Designs.md`
- `_codex/Context/09_CurrentSkills.md`
- `_codex/Context/10_Skill_Design_Rules.md`
- `_codex/Context/18_Game_Difficulty.md`

For run routes, reward cards, shops, saved armies, account progress, or
asynchronous offence/defence, use `_codex/Context/maps/run-metagame-map.md`.

For Tactical Battle AI work, use `_codex/Context/maps/combat-skill-ai-map.md`.

For Run Map UI work, use `_codex/Context/maps/ui-map.md`.

Current skill model: unit skill ownership is loaded from the unit
`ScriptableObject` catalog into `TosterHexUnit.skillstrings`.
`SkillDefinitionAsset.skillName` is the canonical skill id for skill data.
After PRD49ABC, `SkillRules` / `SkillQuery` are the shared API for skill start
legality, target generation, validation, preview/result shape, and future
AI/server-facing queries. `MouseControler` still selects a skill slot from
`SelectedToster.skillstrings`, but target legality should route through
`SkillRules`.

Remaining legacy boundary: live/default skill commit can still call
`CastManager.startSpell(...)` reflection bodies for mutation, and passive
trigger mutation can still live in `TosterHexUnit`, `SpellOverTime`, and
`HexClass`. Treat those paths as PRD49ED/PRD49F migration debt, not the desired
architecture.

The unit catalog stores full legal skill ownership and unit tier. Run, reward,
shop, and future player progression state can mark those legal skills as locked
or unlocked for a specific stack.
