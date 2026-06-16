# [TARENA] PRD: Run Metagame And Reward Framework

- Status: ready-for-agent
- Type: PRD
- Area: Metagame, Rewards, Shops, Saved Armies, Account Progress
- Label: ready-for-agent

## Problem Statement

TArenaUnity3D has a strong battle core direction: Heroes of Might and Magic
3-like hex battles with stack armies, movement, attacks, unit skills, and loss
exchange. The larger game loop is now defined as a short Mewgenics-like run
where the player grows a smaller starting army into a final army, proves it in
a PvE final encounter, and saves that exact army for asynchronous offence and
defence.

The biggest unresolved product problem is the reward and metagame framework.
Rewards must feel good, scale across a run, be readable in UI, influence the
army meaningfully, and stay quick enough that the player does not need an Excel
sheet after every battle. The project needs a PRD that turns the current design
conversation into a taskable product plan for run structure, reward cards,
shops, account unlocks, saved armies, and asynchronous offence/defence.

## Solution

Build the metagame around a short run loop:

1. The player starts with a smaller army.
2. The player moves through a node-route map inspired by Mewgenics and Slay the
   Spire.
3. Run battles are short Heroes-like stack battles.
4. After battles, the player chooses 1 of 3 reward cards.
5. Shop nodes provide a slower breathing-room decision for recovery, skills,
   stack growth, and upgrades.
6. The run ends in a controlled PvE final encounter.
7. If the final is won, the pre-final army snapshot is validated and can be
   saved.
8. Saved armies can be used for offence, and one saved army can be selected as
   the current defence.
9. Offence/defence gives ranking plus account experience.
10. Account progress unlocks future options: new maps, new units, skills for
    starting units and future run pools, and additional saved army slots.

## Mode Architecture Guardrail - 2026-06-14

Current implementation target:

- Build PRD 19 as `Offline Mode` first.
- Offline Mode is the current playable product path. It stores run, reward,
  shop, account-progress, and saved-army state locally.
- Offline Mode may use SQLite-ready local persistence and temporary adapters
  over current local build/save systems, but `PlayerPrefs`, local build files,
  `LocalLegacyRuntime`, and compatibility stubs must not become the domain
  authority.

Future online target:

- `Online Mode` is a separate future game mode, not an extension of the current
  offline save files.
- Online Mode stores authoritative account, run, reward, shop, battle-result,
  ranking, and saved-army state online.
- Online Mode must be backend-verified. The client can present previews and
  submit commands, but it must not be trusted as the final authority for rewards,
  purchases, saved-army creation, ranking, or account progress.
- Future Online Mode should use project-owned adapters and explicit payloads,
  not legacy PlayFab, PUN, Photon, or old compatibility classes as identity.

Shared architecture rule:

- The stable seam is the run/metagame domain interface, not the storage
  technology.
- Deep modules should own run state, reward generation/resolution, shop
  resolution, saved-army snapshots, account unlocks, and result payloads.
- Offline and future Online should be separate adapters at that seam:
  `Offline` is local-authoritative, while `Online` is backend-authoritative.
- UI presenters should consume mode-neutral view data and command results so the
  same flow can be understood in both modes without duplicating gameplay rules.
- Do not add a backend SDK, live multiplayer, cloud sync, or online migration in
  these tasks unless a later task explicitly authorizes it.

Saved-army slot rule:

- The design target still starts with 2 unlocked saved-army slots.
- The current/local UI may expose 8 physical compatibility slots, but slot
  availability must be data-driven through an unlocked-slot count.
- Additional slots are account-progress unlocks. They expand future options and
  must not mutate old saved armies.

The reward framework should use six player-facing reward families:

- Mass: more units in an existing stack.
- Quality: fewer units, higher class or stronger unit type.
- Width: new stack or tactical role.
- Skill: teach or unlock a skill for a legal stack.
- Recovery: heal, revive, or recover losses.
- Economy: run currency or future shop value.

Each reward card should have one main verb, a clear target, a plain preview, and
one concrete operation. The default post-battle choice is 1 of 3 cards shaped as
Stabilize, Strengthen, and Pivot.

## User Stories

1. As a player, I want a short run to start with a smaller army, so that I can
   feel the army grow into something worth saving.
2. As a player, I want run routes to be node-based, so that I can make quick
   decisions without navigating a large overworld.
3. As a player, I want each route to bias rewards, units, skills, or encounter
   types, so that different maps can produce different final armies.
4. As a player, I want run battles to be short, so that a complete run can fit
   into roughly 20-30 minutes.
5. As a player, I want run battles to use the existing Heroes-like battle feel,
   so that movement, attacks, stack losses, and unit skills remain central.
6. As a player, I want normal battle rewards to be 1 of 3 card choices, so that
   I can make a fast but meaningful build decision.
7. As a player, I want reward cards to show a clear before-and-after preview,
   so that I do not need to calculate value manually.
8. As a player, I want reward cards to use simple categories like more units,
   stronger units, new stack, skill, recovery, or gold, so that I understand
   what kind of decision I am making.
9. As a player, I want one reward card to stabilize my army, so that I can
   recover from losses without stopping the run.
10. As a player, I want one reward card to strengthen my current direction, so
    that good builds can snowball in a readable way.
11. As a player, I want one reward card to offer a pivot, so that I can change
    direction when a run opens an interesting possibility.
12. As a player, I want reward cards to propose specific transactions, so that
    I am not asked to freely optimize the whole army after every fight.
13. As a player, I want skill reward cards to ask only for a legal target stack,
    so that assigning a skill is quick and clear.
14. As a player, I want class exchange rewards to show exactly what changes
    into what, so that promotion or demotion choices are readable.
15. As a player, I want recovery rewards to revive or repair meaningful losses,
    so that combat damage matters without ending the run too early.
16. As a player, I want economy rewards to be simple, so that saving for a shop
    feels like a clear alternative to immediate power.
17. As a player, I want shop nodes to feel like a breathing room, so that I can
    repair and shape my army between pressure points.
18. As a player, I want shops to use one main run currency, so that the economy
    remains easy to understand.
19. As a player, I want shops to offer healing, resurrection, skills, stack
    purchases, and upgrade/exchange opportunities, so that I can fix or improve
    the army before the final.
20. As a player, I want shops to remain limited, so that they do not become a
    full army optimizer.
21. As a player, I want my starting army to be weaker than my final army, so
    that run growth feels real.
22. As a player, I want winning with small losses to still grow my army, so that
    successful play feels rewarded.
23. As a player, I want large losses to matter, so that run battles carry
    pressure.
24. As a player, I want the final PvE encounter to prove my army, so that saving
    the army feels earned.
25. As a player, I want the saved army to be based on the pre-final snapshot, so
    that winning the final does not make the reward worse.
26. As a player, I want a successful run to create a saved army, so that I feel
    "this is my new army."
27. As a player, I want saved armies to stay immutable, so that each one remains
    a real artifact of a completed run.
28. As a player, I want to choose which saved army is my current defence, so
    that I can express strategic preference.
29. As a player, I want any saved army to be usable for offence, so that my run
    outputs remain useful.
30. As a player, I want only one current defence army, so that defence state is
    easy to understand.
31. As a player, I want to start with 2 saved army slots, so that I can compare
    a small number of run outputs.
32. As a player, I want metaprogress to unlock more saved army slots, so that I
    can keep more successful run armies over time.
33. As a player, I want account progress to unlock new maps, so that future
    runs can pursue different army outcomes.
34. As a player, I want account progress to unlock new units, so that future
    starting armies and run reward pools become more varied.
35. As a player, I want account progress to unlock skills for starting units,
    so that future runs can start with more interesting options.
36. As a player, I want account unlocks to affect future runs rather than
    editing old saved armies, so that saved armies remain honest run results.
37. As a player, I want skills bought during a run to remain on that army if the
    run is won and saved, so that run choices matter.
38. As a player, I want run-only skills to disappear when the run fails, so that
    failure has a clear consequence.
39. As a player, I do not want to buy skills for a defence army after the run,
    so that defence armies do not become post-run edited profiles.
40. As a player, I want offence victories to give ranking and account
    experience, so that saved armies matter beyond the run.
41. As a player, I want defence victories to give appropriate ranking or
    account progress, so that a good defence army feels valuable.
42. As a player, I want ranking to behave like ELO, so that attacking stronger
    or equal opponents is better than farming weaker ones.
43. As a player, I do not want offence to steal or destroy another player's
    army, so that async competition is not frustrating.
44. As a player, I want run enemies to have different goals, so that some
    encounters try to win and others try to cause losses.
45. As a designer, I want reward families to be explicit, so that new reward
    templates do not become arbitrary stat bundles.
46. As a designer, I want reward templates to use one main verb, so that card
    effects stay readable.
47. As a designer, I want rarity to change opportunity type, so that rare cards
    can mean new possibility rather than only bigger numbers.
48. As a designer, I want stage-based scaling to happen inside templates, so
    that the player sees concrete outcomes instead of raw budget math.
49. As a designer, I want a small authored template set first, so that reward
    feel can be proven before building a broad generator.
50. As a UI designer, I want reward cards to have a consistent anatomy, so that
    every card can be scanned quickly.
51. As a UI designer, I want reward previews to show the affected stack and the
    outcome, so that reward choice is easy to verify.
52. As a UI designer, I want shops to separate recovery, skill, stack, upgrade,
    and economy offers, so that the shop feels deliberate but not overwhelming.
53. As a future coding agent, I want a reward catalog boundary, so that reward
    definitions can be authored and tested without rewriting run flow.
54. As a future coding agent, I want a reward resolver boundary, so that
    applying rewards to an army can be tested independently.
55. As a future coding agent, I want a run state boundary, so that route,
    currency, army, and encounter state can be tested without UI.
56. As a future coding agent, I want a saved army snapshot boundary, so that
    post-run immutability is enforced.
57. As a future coding agent, I want account unlocks separated from run rewards,
    so that metaprogress does not accidentally mutate saved armies.
58. As a QA reviewer, I want tests around reward application, so that card
    previews and actual army changes cannot drift.
59. As a QA reviewer, I want tests around saved army immutability, so that old
    armies are not changed by later account unlocks.
60. As the project director, I want the metagame framework documented as a PRD,
    so that it can be split into small implementation tasks later.

## Implementation Decisions

- Treat this PRD as product and domain design for future tasks. It does not
  authorize immediate feature growth ahead of battle recovery.
- Keep the existing unit stats, balance, skills, cooldowns, SFX, and VFX intact
  unless a future task explicitly allows changes.
- The run route is node-based, not a spatial adventure map.
- Core first-scope node types are battle, shop, recruit/reward, and final/boss
  battle.
- Event nodes are optional future scope.
- A complete run should target roughly 20-30 minutes.
- Run battles are fast and use smaller arenas or smaller active spaces than
  monumental battles where possible.
- Final, boss, offence, and defence battles can be larger and more
  monumental, but should not exceed the current large map as the expected upper
  scale.
- The run starts with a smaller army and should grow toward a final army.
- The final PvE encounter validates the pre-final army snapshot.
- A saved army is the immutable result of a completed run.
- A saved army includes skills and changes acquired during that successful run.
- A failed run does not produce a saved army.
- Post-run systems must not edit saved armies by adding skills, units, or
  upgrades.
- Account progress unlocks future options: maps, units, skills for starting
  units or future run pools, and saved army slots.
- Account progress should expand options, not grant flat stat superiority.
- The player starts with 2 saved army slots.
- Metaprogress may unlock more saved army slots, with a target range of about
  3-5.
- Existing local save UI may expose 8 physical compatibility slots. Treat those
  as a roster capacity/display concern; gameplay availability still comes from
  the unlocked saved-army slot count unless `/grill-me` explicitly changes that
  rule.
- Any saved army can be used for offence.
- One saved army is selected as current defence.
- Defence is AI-controlled; active PvP is not the current model.
- Offence/defence rewards are ranking plus account experience.
- Ranking should behave like ELO to discourage farming weaker opponents.
- Offence should not steal units or destroy another player's saved army.
- Run opponents are hand-made plus procedural.
- Run AI can use encounter goals such as "try to win" or "deal maximum losses."
- Offence/defence can start with one win-focused AI; selectable defence AI
  types are future scope.
- Rewards use six families: Mass, Quality, Width, Skill, Recovery, Economy.
- Default post-battle reward is a 1-of-3 card choice.
- A normal reward choice should usually contain Stabilize, Strengthen, and
  Pivot intentions.
- Each reward card should have one main verb and one concrete operation.
- Reward cards must not open the full army editor.
- If local choice is needed, keep it small, such as choosing a legal stack for a
  skill.
- Cards should show plain previews such as stack count before-and-after, unit
  replacement, or skill gained.
- Internal reward budget, rarity, and stage scaling can exist but should not be
  exposed as raw math to the player.
- Start with a small authored reward template catalog, around 12-18 templates,
  before building a broad generator.
- Shop nodes use one main run currency.
- Shop nodes are slower than reward cards but still constrained.
- Shop offers can include healing, resurrection, skills, stack purchases,
  upgrades, and exchanges.
- The likely deep modules for future implementation are:
  - Run state model: route node, current army, currency, stage, and run status.
  - Army snapshot model: immutable saved army output from a won run.
  - Reward catalog: authored reward templates and rarity/stage metadata.
  - Reward generator: selects valid 1-of-3 reward choices from templates.
  - Reward resolver: applies one concrete reward operation to an army.
  - Shop catalog and resolver: generates offers and applies purchases.
  - Account progression model: maps, unit unlocks, starting-unit skill unlocks,
    and saved army slots.
  - Saved army roster: stores, selects, deletes, and marks current defence.
  - Async offence/defence model: picks attacker and defender saved armies,
    records ranking/experience outcomes, and delegates defence play to AI.
  - Reward and shop UI presenters: display card anatomy, previews, legal
    targets, and purchase results.
- These modules should be designed as deep modules where possible: simple
  interfaces, clear domain inputs/outputs, and behavior testable without Unity
  scene dependencies.

## Testing Decisions

- Good tests should verify external behavior and domain outcomes, not private
  helper implementation.
- Reward catalog tests should verify that authored templates declare one family,
  one verb, legal target rules, rarity, and preview data.
- Reward generator tests should verify that a 1-of-3 choice can contain
  Stabilize, Strengthen, and Pivot intentions when legal.
- Reward resolver tests should verify that the previewed effect matches the
  actual army mutation.
- Reward resolver tests should cover Mass, Quality, Width, Skill, Recovery, and
  Economy families.
- Skill reward tests should verify that only legal stacks can receive a skill.
- Exchange reward tests should verify that the displayed before-and-after stack
  values match the applied result.
- Recovery reward tests should verify that losses can be recovered only within
  the intended limits.
- Shop tests should verify that purchases spend the one run currency and apply
  the selected operation.
- Run state tests should verify route progression, battle completion, reward
  selection, shop purchase, final encounter gating, and run win/loss state.
- Saved army tests should verify that a won run can produce a saved army and a
  failed run cannot.
- Saved army immutability tests should verify that account unlocks do not
  mutate existing saved armies.
- Account progress tests should verify that unlocks affect future runs and
  starting options, not completed saved armies.
- Saved army roster tests should verify the starting slot limit, unlocked slot
  growth, offence selection, and single current defence selection.
- Ranking tests should verify ELO-like behavior at the domain level:
  stronger/equal opponents should be more rewarding than farming weak targets.
- AI goal tests should verify that run encounter goals can be represented
  separately from offence/defence win-focused AI.
- UI-facing tests should verify that reward and shop view data includes enough
  information for clear previews without exposing raw internal reward budgets.
- Manual Unity validation will still be required later for battle feel,
  animation pacing, VFX/SFX readability, and map scale.

## Task Mockup Workflow - 2026-06-15

Supersedes earlier non-Unity prototype notes.

- After each PRD019 implementation task that has an actual UI/screen surface,
  create or update a Unity UGUI mockup prefab through
  `_codex/skills/make-ui-mockup/SKILL.md`.
- Backend-only tasks do not require UI mockup prefabs. Do not create a prefab
  just to satisfy the PRD019 mockup habit when the task is only a domain,
  adapter, persistence, or backend boundary.
- A task mockup is complete only when it has Unity assets that can be opened in
  the project: the final screen prefab and reusable repeated or section prefabs
  live under
  `TArenaUnity3D/Assets/Resources/UI/PRD_19/<TaskNumber_FeatureName>/`.
- The Unity mockup must be functional enough for Inspector review: visible UGUI
  components, `TextMeshProUGUI` labels, buttons/toggles/sliders where relevant,
  `Script_*` owners, serialized field wiring, sample data, disabled/error
  states, and documented backend gaps.
- Repeated rows, cards, buttons, route nodes, route edges, slots, and sections
  should be prefab assets or nested prefab instances inside the same task
  folder, not copied child hierarchies expanded into the parent screen.
- Validate edited prefab YAML/reference integrity with the local
  `make-ui-mockup` validator before reporting completion. Unity import/manual
  Play Mode validation remains required when Unity was not run.
- Browser/prototype-page updates do not satisfy a PRD019 task mockup
  requirement. New PRD019 mockup work must be Unity prefab work.
- If a task genuinely needs only a product sketch and not a Unity mockup, write
  that explicitly in the task before implementation; otherwise "mockup" means
  Unity UGUI prefab in this project.
- Task mockups must not replace the production implementation, tests, or Unity
  validation. They exist to make the content, hierarchy, and wiring inspectable
  directly inside Unity.

## Out of Scope

- Implementing the PRD immediately.
- Splitting the PRD into task files in this step.
- Changing current unit stats, balance, damage, cooldowns, or skill effects.
- Renaming skill ids or changing skill ownership data without a specific task.
- Editing Unity assets, prefabs, scenes, animation clips, controllers,
  materials, generated Unity files, input assets, or assembly definitions,
  except explicitly requested Unity UI mockup prefabs following
  `_codex/skills/make-ui-mockup/SKILL.md`.
- Replacing the current battle core.
- Building PlayFab, PUN, Photon, live multiplayer, or legacy backend systems.
- Creating a full economy, monetization model, or live-service product loop.
- Finalizing exact reward numbers, rarity weights, or balance tables.
- Finalizing exact route maps, encounter tables, or enemy rosters.
- Finalizing selectable defence AI types.
- Building a full army optimizer after every battle.
- Adding post-run edits to saved armies.

## Further Notes

Key product rule:

```text
Account progress expands future options.
The run builds one concrete army.
The saved army is a frozen result of the won run.
```

The design should preserve the strongest current identity statement:

```text
Mewgenics-like run, Heroes 3-like battles.
```

The reward framework should be gameplay-first. It should be judged by whether a
player can understand the reward quickly, feel the army change, and make a real
choice without doing heavy arithmetic.

Recommended first breakdown after this PRD:

1. Define the run domain model and saved army snapshot contract.
2. Define reward template schema and 12-18 authored templates.
3. Prototype reward preview and reward application in isolation.
4. Define shop offer schema and one-currency purchase flow.
5. Define account unlock rules for starting units, starting-unit skills, maps,
   and saved army slots.
6. Define saved army roster and current defence selection.
7. Define async offence/defence outcome model and ranking/experience rewards.
8. Define run AI encounter goal vocabulary.
