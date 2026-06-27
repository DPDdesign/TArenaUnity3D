# [TARENA] PRD: Battle Footer Right-Click Skill Info

- Status: draft
- Type: PRD / diagnosis handoff
- Area: Battle UI, Footer, Skill Info, Tooltip, Skill Presentation
- Label: ready-for-agent
- Created: 2026-06-27
- Related: `_codex/Context/11_UI_Context.md`
- Related: `_codex/Context/09_CurrentSkills.md`
- Related: `_codex/agents/docs/codebase/skills-effects-code-map.md`

## Problem Statement

In the battle scene footer, right-clicking a skill icon should display that
skill's name, type, and description. The current setup does not reliably show
the skill info panel.

From the player's perspective, holding right mouse button on a footer skill does
nothing useful. The skill icon receives the click, and the runtime knows the
correct skill id, but the correct local skill info panel is not found and shown.

From the developer's perspective, the battle footer prefab has several nested
objects named `SkillInfo`, `Panel`, `NameS`, `TYP`, and `INFO`, but the runtime
wiring does not expose a stable component boundary. The same visual structure is
duplicated across skill slots, but not all expected text objects are present
under the same local root. The current code is therefore trying to infer
structure from names, which is fragile.

## Current Observed Setup

The battle footer UI is driven by `UICanvas`.

The current battle footer skill button flow is:

1. `UICanvas.Update()` reads the active unit from `MouseControler.SelectedToster`.
2. `UICanvas.Update()` iterates `SelectedToster.skillstrings`.
3. For each skill slot, `UICanvas` sets the button icon using `DataMapper.LoadSkillIcon(skillId)`.
4. `UICanvas.SetRightClickSkillId(...)` calls `RightClickInfoSkill.SetSkillId(skillId)`.
5. `RightClickInfoSkill` receives `IPointerDownHandler` and `IPointerUpHandler` events.
6. `RightClickInfoSkill` is expected to call `SkillInfoPresentation.ShowSkill(skillId)`.
7. `SkillInfoPresentation.ShowSkill(...)` uses `DataMapper.Instance.FindSkill(skillId)` and writes name/type/info into UI text fields.

Runtime logs confirmed:

1. The right-click raycast is working.
2. The raycast hits the skill icon object, for example `UI (1)/Footer/Skills/SKillsBG/Skills/SKILL/SkillImage`.
3. `RightClickInfoSkill.OnPointerDown(...)` is called.
4. `eventData.button` is `Right`.
5. `skillId` is populated, for example `Blind_by_light`, `Chope`, or `Unstoppable_Light`.
6. The failure point is `presentation=<null>`.

The key debug evidence from the latest run:

```text
[DEBUG-SKILLINFO] CreatePresentation failed root=UI (1)/Footer/Skills/SKillsBG/Skills/SKILL/SkillImage/Panel/SkillInfo hasTmpName=False hasTmpType=False hasTmpInfo=False hasLegacyName=True hasLegacyType=False hasLegacyInfo=False
[DEBUG-SKILLINFO] PointerDown object=UI (1)/Footer/Skills/SKillsBG/Skills/SKILL/SkillImage button=Right skillId=Blind_by_light presentation=<null> raycast=UI (1)/Footer/Skills/SKillsBG/Skills/SKILL/SkillImage
[DEBUG-SKILLINFO] PointerDown ignored object=UI (1)/Footer/Skills/SKillsBG/Skills/SKILL/SkillImage isRight=True hasPresentation=False hasSkillId=True
```

This means the click path is not the blocker. The local info-panel wiring is the
blocker.

## What Has Been Done

`RightClickInfoSkill` was instrumented with `[DEBUG-SKILLINFO]` logs to identify
whether the issue was raycast, missing skill id, or missing presentation
reference.

`UICanvas` was instrumented with a right-click `EventSystem.RaycastAll(...)`
probe. This confirmed that UI raycasts hit the skill icon.

`RightClickInfoSkill` was extended to:

1. Try an explicit serialized `SkillInfoPresentation` reference.
2. Try a local `SkillInfoPresentation` on itself or children.
3. Try the nearest skill slot root.
4. Try to create/configure a `SkillInfoPresentation` at runtime from local
   child objects named `SkillInfo`, `NameS`, `TYP`, and `INFO`.

`SkillInfoPresentation` was extended with runtime configuration methods for
both TMP and legacy `UnityEngine.UI.Text` fields.

These changes produced useful diagnosis but did not complete the feature,
because the current prefab hierarchy does not provide the expected local
`NameS`, `TYP`, and `INFO` set under one stable local root.

## Known Current Problems

The current prefab hierarchy appears inconsistent across skill slots.

At least one runtime slot has:

- a `SkillInfo` object under the clicked slot,
- a `NameS` legacy `Text` reachable under that selected `SkillInfo`,
- no local `TYP` text reachable under that same selected `SkillInfo`,
- no local `INFO` text reachable under that same selected `SkillInfo`.

The footer prefab also contains multiple objects named `SkillInfo`, `Panel`,
`Panel (1)`, `NameS`, `TYP`, and `INFO`. Name-based lookup can accidentally find
the wrong panel or stop at a partial panel.

There are likely old prefab variants or duplicated slot fragments in the footer
asset. Some parts appear to use TextMesh Pro, while other parts use legacy
`UnityEngine.UI.Text`. The project rule is to use TextMesh Pro for project UI,
so the final solution should not depend on legacy `Text`.

Temporary debug logs remain in the touched scripts and should be removed once
the issue is fixed.

## Solution

Replace the current implicit name-scanning setup with one explicit battle footer
skill info presenter.

The desired final design:

1. The footer owns one tooltip/info panel for skill details.
2. The tooltip panel is wired once in the footer controller or a dedicated view.
3. Every skill button forwards its slot index or skill id to that one presenter.
4. The presenter reads skill metadata from `DataMapper` / `SkillDefinitionAsset`.
5. The presenter writes into TMP text fields only.
6. The presenter shows on right-button down and hides on right-button up.
7. Skill buttons do not search arbitrary parent/child hierarchies at runtime.

This avoids per-slot duplicated info panels and avoids guessing based on object
names like `SkillInfo`, `NameS`, `TYP`, and `INFO`.

## User Stories

1. As a player, I want to right-click a battle footer skill icon, so that I can read what the skill does before using it.
2. As a player, I want the info panel to show the correct skill name, so that I know which skill I am inspecting.
3. As a player, I want the info panel to show the skill type, so that I can distinguish active, passive, stance, or other skill categories.
4. As a player, I want the info panel to show the skill description, so that I can make tactical decisions.
5. As a player, I want the panel to appear while I hold right mouse button, so that the interaction feels direct.
6. As a player, I want the panel to disappear when I release right mouse button, so that it does not block the battle HUD.
7. As a player, I want each skill slot to show its own correct info, so that duplicated icons or cooldown overlays do not confuse the tooltip.
8. As a player, I want disabled or cooldown skills to still be inspectable, so that I can understand why they matter later.
9. As a developer, I want the right-click event path to be explicit, so that raycast bugs and data bugs are easy to separate.
10. As a developer, I want skill info text references wired once, so that duplicated prefab fragments cannot drift apart.
11. As a developer, I want the footer to use TMP text fields, so that the UI follows project rules.
12. As a developer, I want obsolete legacy text fields removed from the final tooltip path, so that future work does not revive old UI wiring.
13. As a designer, I want one tooltip panel layout, so that skill descriptions are visually consistent across all footer slots.
14. As QA, I want debug logs removed after validation, so that console output remains meaningful.
15. As QA, I want a repeatable Play Mode checklist, so that the feature can be verified across units with different skill counts.

## Implementation Decisions

- Build or introduce a dedicated battle footer skill info presenter rather than
  continuing to patch `RightClickInfoSkill` with deeper hierarchy scanning.
- Keep `UICanvas` as the current battle UI bridge unless a broader battle HUD
  refactor is explicitly approved.
- Keep `MouseControler.SelectedToster.skillstrings` as the current source of
  footer skill ids.
- Keep `DataMapper.FindSkill(skillId)` or SO-backed skill metadata as the
  source for display name, type, and description.
- Use TMP fields for the final info panel.
- Treat current legacy `UnityEngine.UI.Text` fields in the footer as migration
  debt, not as the target architecture.
- Prefer one shared tooltip panel over one duplicated tooltip subtree per skill
  slot.
- Skill button interaction should remain small: right down calls show with the
  current skill id, right up calls hide.
- Disabled cooldown skill buttons must still be raycastable or must have a
  separate right-click handler on an always-raycastable child image.
- Remove temporary `[DEBUG-SKILLINFO]` logs once the feature is validated.
- Do not edit battle scene, prefab, or asset files without explicit permission
  in the implementation task. If prefab edits are approved, remove obsolete
  duplicated tooltip fragments after the new single-panel wiring works.

## Testing Decisions

Good tests should validate externally visible behavior: a right-click on a
footer skill causes the correct skill metadata to be sent to the presentation
panel. Tests should not depend on deep transform names such as
`Panel/SkillInfo/NameS`.

Recommended automated coverage:

- A small EditMode test for a pure presenter/helper that binds skill id to
  display name/type/info using a fake or fixture metadata source.
- A small EditMode test for right-click handler behavior if the handler can be
  isolated from Unity's live EventSystem.

Recommended manual Unity validation:

1. Enter Play Mode in the battle scene.
2. Select or wait for an active unit with skills.
3. Right-click each visible footer skill slot.
4. Confirm the tooltip shows the correct name, type, and info.
5. Confirm cooldown or disabled skills can still be inspected.
6. Confirm releasing right mouse hides the tooltip.
7. Confirm the tooltip does not show the unit stats/info panel.
8. Test a unit with one skill, two skills, three skills, and four skills.
9. Test at least `Blind_by_light`, `Chope`, and `Unstoppable_Light`, because
   these appeared in the diagnosis logs.
10. Confirm the console has no leftover `[DEBUG-SKILLINFO]` logs after cleanup.

## Acceptance Criteria

1. Right-clicking a battle footer skill icon opens the skill info panel.
2. The opened panel is the skill info panel, not the unit stats/info panel.
3. The panel displays the correct skill id metadata from current skill data.
4. Releasing right mouse button hides the panel.
5. The solution does not rely on matching partial child names across duplicated
   tooltip subtrees.
6. The final implementation uses TextMesh Pro for visible text.
7. Temporary diagnosis logs are removed before the implementation task is
   closed.
8. Obsolete runtime fallback code that auto-adds presentation components is
   removed unless explicitly kept as a deliberate compatibility bridge.

## Out of Scope

- Full battle HUD redesign.
- Replacing `UICanvas` as the battle HUD controller.
- Changing skill gameplay logic, cooldown logic, or targeting logic.
- Changing skill ids or skill catalog data.
- Editing battle scenes or prefabs unless explicitly approved in the
  implementation task.
- Migrating all legacy battle UI text to TMP outside the footer skill info
  path.
- Reworking skill execution or `CastManager`.

## Further Notes

The current debugging showed that something is definitely missing or miswired in
the local footer skill info setup. The missing part is not the right-click event
and not the skill id. The missing part is a stable, complete presentation panel
for the clicked slot, or a single shared footer skill info panel that every slot
uses.

The strongest recommendation is to stop duplicating tooltip panels per skill
slot and create one shared footer-owned tooltip. That will make the setup
visible in the Inspector, reduce prefab drift, and avoid future bugs where
`NameS` exists under one branch but `TYP` and `INFO` live somewhere else.

