# 02 Current State

Status: active
Project: TArenaUnity3D
Last updated: 2026-06-24

## Current Project Goal

TArenaUnity3D is currently a legacy recovery project. The main goal is to make
the Unity project workable again for humans and agents by:

- excavating and documenting the legacy code,
- cutting non-working features, especially PlayFab, PUN, and multiplayer paths,
- replacing assets where needed,
- improving code architecture in small, safe, testable steps.

This is not a feature-growth phase. New systems should not be built before the
legacy surface is mapped, dead dependencies are isolated, and the playable local
loop is understood.

## Current Code State

The first repository scan found 443 `.cs` files under `TArenaUnity3D/Assets`.
Most source volume is third-party or vendor code:

- Photon/PUN package and demos: 190 files.
- PlayFab SDK and editor extensions: 124 files total.
- Other copied plugins: PhotonChatApi, AsyncAwaitUtil, WebSocket, OutlineEffect,
  AShopExport, UnityOutlineFX.
- Game/legacy scripts outside vendor folders: about 96 files.

The apparent game core is a hex-grid tactics prototype around toster units:

- map generation, hex state, movement, traps, turns, and highlighting,
- army/build selection saved through local files and `PlayerPrefs`,
- units, stats, cooldowns, damage, status effects, and skills,
- menu/shop/profile UI,
- AI prototype and simulation helpers.

Recent turn-system recovery:

- Team turn selection now treats null, dead, and zero-amount units as
  non-actionable, including queue preview and new-turn DOT/trap death checks.
  This was implemented for PRD 015 after a FireElemental fire trap could kill a
  HeavyHitter but still let the dead unit receive a turn. Unity Play Mode
  validation is still pending.

The code is heavily coupled to Unity scene objects, Inspector fields,
`PlayerPrefs`, Resources XML, local serialized build files, Photon/PUN, and
PlayFab singleton access. Even the local/single-player path still passes
through some PUN classes and PlayFab calls.

Current Offline Mode run-metagame recovery:

- PRD035/040 moved the current Start Run and encounter direction to
  generator-backed flows: generated starting army offers come from
  `ArmyGeneratorRuleSet`/`DeterministicRunGenerationCatalog`, and generated
  enemy armies come through `EnemyEncounterRuleCatalog` and enemy rule sets.
  Older authored `Default*Catalog` examples are not current balance truth.
- PRD37 closed the materialized Reward Map flow: normal battle wins persist
  reward rows, Reward Map loads persisted choices instead of rolling fallback
  screen-time rewards, clicking a legal card applies immediately, and successful
  apply returns to Run Map.
- PRD37 follow-up routing closed the battle handoff mismatch: normal wins route
  to Reward, final wins route to Summary Value, and losses route to run loss via
  persisted `RunBattleNextScreen`.
- PRD41 closed the reward value-parity pass: materialized reward amounts scale
  from average live stack value so late-run Add Stack / More Units rewards no
  longer collapse to tiny early-run values.
- Unity Test Runner and Play Mode smoke validation remain manual.

Current Tactical Battle AI recovery:

- PRD046 implemented Tactical Battle AI V1 as a pure-planning plus live
  revalidation architecture. The AI now has battle snapshots, action intents,
  candidate generation, fixed-budget profile/cache support, 3-ply search and
  scoring, CastManager skill bridging, and live enemy-turn integration through
  the current `MostStupidAIEver` entry point.
- PRD047 implemented the first async decision pipeline. The battle AI captures
  snapshot/profile/skill metadata on the main thread, runs search on a worker
  task, then consumes matching results on the main thread through the existing
  execution bridge. Legacy AI remains as fallback.
- PRD046/047 are accepted for the current project state. Unity compilation,
  EditMode tests, and Play Mode validation remain manual/user-side.
- Future AI improvement should be a focused follow-up, not more PRD046 scope:
  move async planning earlier after logical battle-state commit if the goal is
  to hide more thinking time behind presentation.

## Current Combat Presentation Notes

- Combat animation flow is code-driven through `TosterHexUnit` and
  `TosterView`, using Animator state names such as `attack`, `hit`, and
  `death`.
- Combat SFX now has a small project-owned script path:
  `TosterSfxSet` stores per-model `attack`, `hit`, and `death` clip arrays on
  the same GameObject as the Animator, and `CombatSfxManager` plays them
  through one scene-level `AudioSource`.
- Background music has a separate scene-level `BackgroundMusicManager` path with
  its own `AudioSource`, so looping music can play independently from combat
  SFX.
- Combat SFX setup is documented in `_codex/Documentation/User_Setup_Guide.md`.
- `Stone_Throw` currently uses a local visual workaround: the split unit is
  instantiated immediately in backend state, but its renderers stay hidden
  until projectile impact so the visible spawn happens after the explosion.
  This is acceptable for local presentation work, but it should be treated as a
  future multiplayer risk because hide/show renderer state is not a strong
  replicated presentation contract.

## Current Context Documentation State

Core gameplay-context files now exist as TArena-specific documentation. Some
are initial design drafts and some remain guided templates:

- `_codex/Context/01_Game_Design_Document.md` is marked `Initial Design` and
  captures the first GDD synthesis from local notes, legacy Retsot reference,
  invitational unit descriptions, and runtime XML data.
- `_codex/Context/19_Identity.md` is marked `Initial Design` and captures the
  first identity guardrails: COBA, point-budget army building, hex tactics,
  skill-forward units, tier/phase pressure, and recovery-first boundaries.
- `_codex/Context/GameplayFeelDoctrine.md` is marked `Initial Design` and
  captures the first feel doctrine around tactical readability, targeting
  feedback, skill feedback, phase feedback, and persistent-effect clarity.
- `_codex/Context/08_Current_Mechanics.md` remains a template for verified
  current mechanics and still needs local code inspection or Unity-side
  verification before it becomes current-state truth.
- `_codex/Context/AI_Context.md` is active and captures the current Tactical
  Battle AI V1/async-planning contract from PRD046/047.
- `_codex/Context/CONTEXT-MAP.md` routes GDD, identity, feel, gameplay, current
  mechanics, production, cleanup, task-tracker work, and design-grill PRDs to
  local TArena files.

The GDD, Identity, and Feel documents are not final design truth yet. They are
working drafts to grill through:

- `_codex/tasks/006_PRD_Grill_GDD_InitialDesign.md`
- `_codex/tasks/007_PRD_Grill_Identity_InitialDesign.md`
- `_codex/tasks/008_PRD_Grill_Feel_InitialDesign.md`

## Production Interpretation

The project should be treated as a salvage/refactor workspace:

- Preserve working local gameplay behavior until it is explicitly replaced.
- Prefer documentation and dependency isolation before deletion.
- Remove multiplayer/backend systems only through narrow tasks with compile and
  scene checks in Unity.
- Do not rename serialized/public fields or change gameplay floats without
  explicit permission.

## Must Not Be Copied Here

- Do not import design decisions, current state, tasks, milestones, enemy lists,
  skills, map designs, or gameplay truth from another project unless the user
  explicitly asks for a comparison or migration note.
- Do not reference another project's local files as default context.
