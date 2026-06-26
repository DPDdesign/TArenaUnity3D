# TArenaUnity3D UI Context Map

Status: active
Last updated: 2026-06-26

## Use When

Use this map when the task touches new UI, HUD, battle UI, active unit panels,
skill buttons, cooldown display, right-click skill info, UI visuals, Run Map UI,
responsive behavior, or UI mockups.

## UI Context

Read only the files relevant to the current UI surface:

- `_codex/Context/11_UI_Context.md`
- `_codex/Context/12_UI_Visual_Context.md` when improving UI visuals,
  screenshot readability, icon/button/zone sizes, layout proportions, or visual
  consistency with TArena identity
- `_codex/Context/RunMap_UI_Context.md` when designing, reviewing,
  prototyping, polishing, or implementing the Run Map screen
- `_codex/Context/BattleUIResponsiveGuidelines.md` when the task is about
  battle HUD scaling, aspect-ratio behavior, queue containment, chat
  containment, or footer protection
- `_codex/Context/09_CurrentSkills.md` when skill buttons or skill ids are
  involved
- `_codex/Context/10_Skill_Design_Rules.md` when changing skill ids, skill
  presentation, or skill data boundaries

Use `_codex/Context/maps/run-metagame-map.md` only when the UI task explicitly
touches run routes, Offline Mode, rewards, shops, saved armies, summary, battle
result, persistence, or screen data movement.

## UI Mockup Skills

- `_codex/skills/make-ui-mockup/SKILL.md` when a task needs a Unity UI mockup,
  task mockup prefab, repeated UI prefab templates, or post-implementation UI
  prototype
- `_codex/skills/polish-ui-mockup/SKILL.md` when a functional UI mockup exists
  and should be made more readable, prettier, and consistent with reference
  screenshots

UI mockups should use project assets from:

- `TArenaUnity3D/Assets/Classic_RPG_GUI`
- `TArenaUnity3D/Assets/Old_Paper_Gui`
- `TArenaUnity3D/Assets/Resources/Skill_Icons`
- `TArenaUnity3D/Assets/Resources/UI/Unit_Icons`

UI visual polish should compare against reference screenshots when a current
task provides or permits such a folder. Save PRD screenshots under
`TArenaUnity3D/Assets/Resources/UI/PRD_<number>/Screenshots` only when the
current task permits those asset writes.

## Battle UI Bridge

Current battle UI bridge:

- read active unit state from `MouseControler.SelectedToster`,
- read action availability from `MouseControler.activeButtons`,
- read skill ids from `SelectedToster.skillstrings`,
- read cooldowns from `SelectedToster.cooldowns`,
- invoke skills through `MouseControler.CastSkill(slotIndex)`.

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
run state from the explicit run-metagame/offline-database services named by the
current task, then bind ready DTOs/representations. They should not carry sample
run ids, mock route node ids, or fallback in-memory stores in production UI.

When UI code replaces a legacy wiring style, remove obsolete serialized/public
Inspector fields from the component. Commenting old logic is acceptable only as
a temporary code note; obsolete fields should not remain visible in Unity.

## Run Map UI

For Run Map UI work, read `_codex/Context/RunMap_UI_Context.md`. The Run Map
should read as a connected route map with background context, visible path
connections, large symbolic nodes, a clear current party marker, and a linked
army bar, not as a debug grid of buttons.
