# TArenaUnity3D Context Map

Status: active
Last updated: 2026-06-17

## Purpose

This file routes agents to the right TArenaUnity3D project context.

This workspace is TArenaUnity3D, not Retsot Horde. Do not use context, tasks,
agents, or markdown files from another project unless the user explicitly asks
for comparison or migration. Chat/task titles must start with `[TARENA]`.

Use this file first when a task needs design, production, gameplay, level,
skill, feel, AI, cleanup, or implementation context.

Do not read every source document by default. Pick only the contexts relevant
to the current task.

## Mandatory Run Metagame Maps

For gameplay design, UI design, and programming tasks, always read both maps
below before proposing or changing work that can touch PRD019/PRD030 run
metagame, Offline Mode screens, persistence, database state, or data movement
between screens:

- `_codex/agents/docs/PRD019_PRD030_RunMetagame_Code_Map.md`
- `_codex/Documentation/PRD030_OfflineDatabase_Map.md`

These maps are required routing context for Start Run, Run Map, Run Battle,
Reward Map, Run Shop, Summary Value, Saved Armies, Battle Result, shared army
snapshots, Offline Mode database adapters, and SQLite table ownership.

Current PRD030 run-context rule:

- `OfflineRunContextDbReader` is the shared run-context read side used by
  frontend screen controllers/adapters for persisted run state.
- `OfflineRunContextDbWriter` is the shared write side for creating and updating
  `offline_runs`.
- Start Run, Run Map, Run Battle, Reward Map, Run Shop, and Summary Value should
  not own direct `INSERT/UPDATE offline_runs` SQL outside the writer.
- When a run-metagame UI needs current army, currency, node, summary, latest
  battle result, or Start Run-created record state, route through reader/service
  APIs instead of serialized placeholder ids or ad hoc DB queries.

## Project Documentation Layout

Canonical project markdown lives under `_codex/`:

- `_codex/Context/` - design, production, gameplay, level, AI, feel, and
  technical context templates.
- `_codex/Documentation/` - setup guides and practical project documentation.
- `_codex/tasks/` - active, archived, analysis, and QA task files.
- `_codex/agents/` - project agent briefs and support docs.
- `_codex/skills/` - project-local workflow skills.

## Current Project Goal

TArenaUnity3D is currently a legacy recovery project. Work should serve these
goals:

- excavate and document legacy code,
- cut non-working PlayFab, PUN, Photon, and multiplayer functionality,
- replace assets where needed,
- improve code architecture through small, safe, testable steps.

Do not treat this as a feature-growth project until the local gameplay loop and
legacy dependency map are stable.

## Guided Design Fill Workflow

Use this when the user asks to fill project design context, especially:

- "Uzupelnijmy GDD"
- "Uzupełnijmy GDD"
- "uzupelnijmy identity"
- "uzupelnijmy feel"
- "uzupelnijmy gameplay"

Read:

- `_codex/Context/01_Game_Design_Document.md`
- `_codex/Context/19_Identity.md`
- `_codex/Context/GameplayFeelDoctrine.md`
- `_codex/Context/Reward_Design.md` when rewards, shops, reward card UI,
  army growth, or reward scaling are involved
- `_codex/Context/08_Current_Mechanics.md`
- `_codex/Context/02_Current_State.md`
- this `CONTEXT-MAP.md`

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
- Keep GDD, Identity, and Feel separate:
  - GDD defines intended play structure and scope.
  - Identity defines what makes the game recognizably TArena.
  - Feel defines how player feedback should communicate decisions and results.

## Default Implementation Context

Use for normal Unity/C# work:

- `AGENTS.md`
- `_codex/agents/coding-agent.md`
- `_codex/agents/runbooks/unity-coding.md`
- `_codex/agents/runbooks/testing.md`
- `_codex/agents/docs/codebase-map.md`
- `_codex/agents/docs/PRD019_PRD030_RunMetagame_Code_Map.md` and
  `_codex/Documentation/PRD030_OfflineDatabase_Map.md` when the implementation
  touches gameplay design, UI design, run metagame, persistence, database
  state, or screen data flow
- the specific task file from `_codex/tasks/`, when provided
- relevant C# files under `TArenaUnity3D/Assets`

## UI Context

Use when the user asks about new UI, HUD, battle UI, active unit panels, skill
buttons, cooldown display, right-click skill info, or where UI should connect
to gameplay logic:

- `_codex/agents/docs/PRD019_PRD030_RunMetagame_Code_Map.md` and
  `_codex/Documentation/PRD030_OfflineDatabase_Map.md` when the UI touches
  Offline Mode, PRD019/PRD030 screens, run metagame, saved armies, rewards,
  shops, summary, battle result, persistence, or data movement between screens
- `_codex/Context/11_UI_Context.md`
- `_codex/Context/12_UI_Visual_Context.md` when improving UI visuals,
  screenshot readability, icon/button/zone sizes, layout proportions, or visual
  consistency with TArena identity
- `_codex/Context/RunMap_UI_Context.md` when designing, reviewing,
  prototyping, polishing, or implementing the PRD019 Run Map screen
- `_codex/Context/BattleUIResponsiveGuidelines.md` when the task is about
  battle HUD scaling, aspect-ratio behavior, queue containment, chat
  containment, or footer protection
- `_codex/skills/make-ui-mockup/SKILL.md` when a task needs a Unity UI mockup,
  task mockup prefab, repeated UI prefab templates, or post-implementation UI
  prototype
- `_codex/skills/polish-ui-mockup/SKILL.md` when a functional UI mockup exists
  and should be made more readable, prettier, and consistent with reference
  screenshots
- `_codex/Context/09_CurrentSkills.md` when skill buttons or skill ids are
  involved
- `_codex/Context/10_Skill_Design_Rules.md` when changing skill ids, skill
  presentation, or skill data boundaries

UI mockups should use project assets from:

- `TArenaUnity3D/Assets/Classic_RPG_GUI`
- `TArenaUnity3D/Assets/Old_Paper_Gui`
- `TArenaUnity3D/Assets/Resources/Skill_Icons`
- `TArenaUnity3D/Assets/Resources/UI/Unit_Icons`

UI visual polish should compare against screenshots in
`TArenaUnity3D/Assets/Resources/UI/References` and save PRD screenshots under
`TArenaUnity3D/Assets/Resources/UI/PRD_<number>/Screenshots`.

Current battle UI bridge: read active unit state from
`MouseControler.SelectedToster`, action availability from
`MouseControler.activeButtons`, skill ids from `SelectedToster.skillstrings`,
cooldowns from `SelectedToster.cooldowns`, and invoke skills through
`MouseControler.CastSkill(slotIndex)`.

Default UI architecture rule: UI programming, prototyping, mockups, and polish
must follow `_codex/Context/11_UI_Context.md`'s UI Architecture Contract.
Screen controllers own flow and top-level controls, while panels/cards/rows use
view classes. Repeated UI defaults to `Transform parent + prefab` under a
Unity-configured layout parent. Avoid screen-level arrays of raw `Image`,
`TMP_Text`, `Button`, or repeated child components unless a fixed authored
positional layout explicitly needs them.

Default unit UI programming tip: parent UI views should receive and bind ready
`UnitRepresentation`, `StackRepresentation`, or other child view components
instead of rebuilding unit UI from raw `Image`/`TMP_Text` arrays. The parent owns
screen selection/card state, while the child representation owns unit
presentation and is filled from `UnitInfoData`, `StackInfoData`, or a matching
screen DTO.

Default run-metagame UI data tip: screen controllers should represent persisted
run state from `OfflineRunContextDbReader` and slice adapters, then bind ready
DTOs/representations. They should not carry sample run ids, mock route node ids,
or fallback in-memory stores in production UI.

When UI code replaces a legacy wiring style, remove obsolete serialized/public
Inspector fields from the component. Commenting old logic is acceptable only as
a temporary code note; obsolete fields should not remain visible in Unity.

## Production Context

Use when reviewing progress, deciding next work, or controlling scope:

- `_codex/Context/02_Current_State.md`
- `_codex/Context/03_Production_Rules.md`
- `_codex/Context/04_Milestones.md`

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

## Run, Metaprogression, And Async Defence Context

Use when a task touches run routes, reward cards, shops, army saving, account
progress, offence/defence, ranking, or AI goals:

- `_codex/agents/docs/PRD019_PRD030_RunMetagame_Code_Map.md`
- `_codex/Documentation/PRD030_OfflineDatabase_Map.md`
- `_codex/Context/01_Game_Design_Document.md`
- `_codex/Context/19_Identity.md`
- `_codex/Context/GameplayFeelDoctrine.md`
- `_codex/Context/Reward_Design.md`
- `_codex/Context/08_Current_Mechanics.md` when checking what exists now
- `_codex/Context/18_Game_Difficulty.md` when tuning risk/reward or encounter
  difficulty

Confirmed design boundaries:

- Run map is node-route style, not a spatial adventure map.
- Standard battle reward is a fast 1-of-3 card choice.
- Shop uses one main run currency and is a breathing-room node.
- Account progress unlocks units, skills, and saved army slots.
- Saved army slots start at 2 and may grow to about 3-5 through metaprogress.
- Any saved army can be used for offence; one saved army is current defence.
- Offence/defence rewards are ranking plus account experience.
- Ranking should behave like an ELO-style system to discourage farming weaker
  opponents.
- Defence is AI-controlled; active PvP is not the current model.
- Run AI may have different goals, including "try to win" and "deal maximum
  losses"; offence/defence starts with one win-focused chess-like AI.
- Event nodes, route-specific battle map rules, and selectable defence AI types
  are future/open scope.
- Online random generation is server-authoritative. Random numbers for online
  armies, routes, rewards, rerolls, and other run-critical outcomes must be
  calculated by the online authority/backend or delivered as server-owned
  results. Offline/client runtime RNG is acceptable only for local playtest
  mocks and deterministic replay tooling, not trusted online state.

Do not implement metagame systems ahead of local battle recovery unless the user
explicitly requests that work.

## Mods Context

Use when a task touches mods, modding, player-authored content, external unit
files, XML or JSON unit import, catalog validation, or future user-created unit
data:

- `_codex/Context/Mods.md`
- `_codex/tasks/031_PRD_ModUnitDataImportValidation.md` when planning the future
  mod unit import and validation module
- `_codex/Context/09_CurrentSkills.md` when skill ids or skill ownership are
  involved
- `_codex/Context/10_Skill_Design_Rules.md` when skill strings, presentation
  catalog boundaries, or skill execution identity are involved
- `_codex/agents/docs/codebase-map.md` when checking current `DataMapper`,
  `UnitCatalog`, or unit runtime loading paths

Mods are future scope. Do not implement mod loading, mod folders, runtime import,
packaging, signing, dependency resolution, load order, or external content
execution unless a future task explicitly approves that work.

## Current Mechanics Context

Use when the task asks what currently exists, what gameplay path is verified,
what systems are legacy, or what a Coding agent must avoid breaking:

- `_codex/Context/08_Current_Mechanics.md`
- `_codex/Context/BattleActionRules.md` when battle turn and skill-availability
  rules must be treated as current accepted gameplay contract
- `_codex/Context/02_Current_State.md`
- `_codex/agents/docs/codebase-map.md`

Current mechanics should be filled from TArena code inspection, user
confirmation, and Unity verification. If evidence is weak, mark a mechanic as
`Unverified`.

## Code Architecture Context

Use for code navigation, cleanup planning, dependency cutting, and agent
handoffs:

- `_codex/agents/docs/codebase-map.md`
- `_codex/tasks/Analysis/001_CODEMAP-001_CodebaseContextMap_Analysis.md`

Code-map headline: apparent core game scripts live in root `Assets/*.cs`,
`Scripts/Lesisz/HexMap`, `Scripts/Lesisz/Menu`, `Scripts/Lesisz/Skills`,
`Scripts/Lesisz/PathFinding`, `Scripts/Cielu`, and `Scripts/Multiplayer`.
Photon/PUN, PlayFab, PhotonChatApi, and plugin/demo folders are heavy
legacy/vendor surfaces and should not be used as default implementation context
unless the task is specifically about dependency removal.

## Backend And Multiplayer Cleanup Context

Use when planning removal or replacement of PlayFab, PUN, Photon, chat,
networking, shop/profile backend, cloud stats, or matchmaking:

- `_codex/Context/02_Current_State.md`
- `_codex/Context/03_Production_Rules.md`
- `_codex/agents/docs/codebase-map.md`
- active cleanup task under `_codex/tasks/`

Do not delete SDK/package folders until game-code references and Unity scene
dependencies have been mapped for that task.

For future multiplayer/sync work, audit visual-only presentation workarounds
such as `Stone_Throw` renderer hiding before impact. Those local fixes should
be replaced by explicit synced reveal timing when networking returns.

## Gameplay And Level Context

Use when designing or reviewing gameplay, skills, maps, levels, AI, or
difficulty:

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
asynchronous offence/defence, also use the "Run, Metaprogression, And Async
Defence Context" section above.

For Run Map UI work, also use `_codex/Context/RunMap_UI_Context.md`. The Run
Map should read as a connected route map with background context, visible path
connections, large symbolic nodes, a clear current party marker, and a linked
army bar, not as a debug grid of buttons.

Current skill model: unit skill ownership is loaded from the unit
`ScriptableObject` catalog into `TosterHexUnit.skillstrings`.
`MouseControler` selects a skill slot and
`CastManager` invokes `{SkillName}M` and `{SkillName}` methods by reflection.
Skill strings also drive UI icons and skill info, so string drift is a runtime
risk.

The unit catalog stores full legal skill ownership and unit tier. Run,
reward, shop, and future player progression state can mark those legal skills
as locked or unlocked for a specific stack.

## Agent Guidance Context

Use when a task needs stable agent-facing guidance that should live as context,
not as an active task:

- `_codex/Context/BattleActionRules.md`
- `_codex/Context/BattleUIResponsiveGuidelines.md`

These are ongoing context/guideline documents, not PRDs and not active task
files.

When planning skill VFX/SFX, also read:

- `_codex/tasks/003_PRD_SkillVfxSfxFlow.md`
- `_codex/Documentation/CurrentSkills.md`

Important presentation decision: skill assignment is unit-catalog-driven, while
VFX/SFX presentation should live in one central Inspector-authored
`ScriptableObject` catalog. Keep the unit-catalog skill string as the join key
and do not let presentation catalog data decide which skills a unit owns.

Persistent board-state visuals such as trap surface models also route through
the skill presentation catalog. Use `SkillPresentationEntry.spawnModel` for
lasting models created by skills, while gameplay state remains in gameplay
classes such as `HexClass` and `Traps`.

## Combat Presentation Context

Use when working on combat animations, hit/death reactions, combat SFX, or unit
model presentation:

- `_codex/tasks/001_PRD_CombatHitAnimationFlow.md`
- `_codex/tasks/archive/002_PRD_CombatSfxFlow.md`
- `_codex/Documentation/User_Setup_Guide.md`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterView.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/CombatSfxManager.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterSfxSet.cs`

Current combat SFX model: each unit model can define `attack`, `hit`, and
`death` clips on the same GameObject as its Animator through `TosterSfxSet`.
The scene owns one manually placed `CombatSfxManager` with one `AudioSource`.
SFX playback is global/2D and uses `AudioSource.PlayOneShot`, so multiple
combat SFX can overlap.

Current background music model: the scene can own a separate
`BackgroundMusicManager` with its own `AudioSource`. It autoplays a configured
looping music clip and should not share the combat SFX `AudioSource`.

## Task Tracker Context

For local task workflows, use:

- `_codex/agents/runbooks/task-tracker.md`
- `_codex/skills/analyze-task/SKILL.md`
- `_codex/skills/implement-task/SKILL.md`
- `_codex/skills/fix-task/SKILL.md`
- `_codex/skills/qa-review/SKILL.md`
- `_codex/skills/close-task/SKILL.md`

## Source Priority

1. Current user instruction.
2. Specific task file.
3. Requested role file.
4. `AGENTS.md`.
5. Relevant runbook.
6. This context map.
7. Specific context document.
8. Existing C# code for implementation details.
