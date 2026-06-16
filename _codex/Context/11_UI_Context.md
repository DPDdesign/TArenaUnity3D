# 11 UI Context

Status: active
Project: TArenaUnity3D
Last updated: 2026-06-14

## Purpose

This document records the current TArenaUnity3D battle UI integration points,
especially how the active unit's skills are shown and selected.

Use this context when the user asks about the new UI, HUD, battle UI, skill
buttons, active unit display, cooldowns, right-click skill info, or where UI
should connect to gameplay logic.

For task mockups or post-implementation UI prototype prefabs, also use:

- `_codex/skills/make-ui-mockup/SKILL.md`
- `_codex/skills/make-ui-mockup/references/mock-ui-rules.md`

For visual polish of an already functional mockup, also use:

- `_codex/Context/12_UI_Visual_Context.md`
- `_codex/skills/polish-ui-mockup/SKILL.md`

## UI Mockup Asset Sources

Use these project-local folders first for new mock UI:

- Frames, panels, borders, RPG buttons: `TArenaUnity3D/Assets/Classic_RPG_GUI`
- Paper/parchment surfaces: `TArenaUnity3D/Assets/Old_Paper_Gui`
- Skill icons: `TArenaUnity3D/Assets/Resources/Skill_Icons`
- Unit icons: `TArenaUnity3D/Assets/Resources/UI/Unit_Icons`

Reference screenshots for visual polish should be stored in:

- `TArenaUnity3D/Assets/Resources/UI/References`

PRD polish screenshots should be stored in:

- `TArenaUnity3D/Assets/Resources/UI/PRD_<number>/Screenshots`

## How To Make Mock UI

Task mockups should be production-shaped Unity UI, not flat drawings.

- Use `VerticalLayoutGroup` and `HorizontalLayoutGroup` on obvious repeated
  lists: army cards, stack rows, route options, route nodes, skill rows, reward
  cards, and unit icon rows.
- Do not overuse layout groups on single decorative elements.
- Every repeated UI element should have a prefab or prefab template: cards,
  unit frames, rows, route options, route nodes, route edges, skill buttons,
  reward cards, and similar repeated blocks.
- GameObjects that own scripts should start with `Script_`.
- Script references should be wired in serialized fields on the prefab. Do not
  rely on name-based lookup to connect UI.
- For risky nested prefab conversion, create a copy prefab first and verify it
  imports before replacing a working prefab.

Known prefab/YAML pitfalls from the PRD19 Start Run prefab:

- Do not allow `m_Children` entries and `m_Father` to join on one line.
- Keep the standard Unity YAML header.
- Recheck local positions after re-parenting.
- Refresh template prefabs after script wiring changes.
- Validate unique `fileID`s, existing component references, existing
  parent/child references, and required serialized fields.

## Current Battle UI Ownership

The legacy battle UI is driven mostly by:

- `TArenaUnity3D/Assets/UICanvas.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/MouseControler.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/CastManager.cs`
- `TArenaUnity3D/Assets/RightClickInfoSkill.cs`

There is no clean presenter/view-model layer yet. The UI reads gameplay state
directly from `MouseControler` and `TosterHexUnit`.

## Active Unit Source

The current active unit is stored on:

- `MouseControler.SelectedToster`

It is assigned from:

- `TurnManager.AskWhosTurn()`

The current UI action availability flag is:

- `MouseControler.activeButtons`

`MouseControler.SelectTosterMovement()` sets `activeButtons = true` when the
active unit can receive player actions. Many movement/attack/turn-transition
paths set it back to `false`.

For new battle UI work, treat these as the current legacy bridge:

```csharp
MouseControler mouseControler = FindObjectOfType<MouseControler>();
TosterHexUnit activeUnit = mouseControler.SelectedToster;
bool canUseActionButtons = mouseControler.activeButtons;
```

## Active Unit Skill Display

Current unit skills are stored as string ids on:

- `TosterHexUnit.skillstrings`

Current skill cooldowns are stored in matching slot order on:

- `TosterHexUnit.cooldowns`

`UICanvas.Update()` currently rebuilds skill button state every frame when
`MouseControler.activeButtons == true`:

- iterates `MC.SelectedToster.skillstrings`,
- enables one skill button per skill slot,
- reads `MC.SelectedToster.cooldowns[i]`,
- loads icon sprites from `Resources/Sprites/Skill_Icons/{SkillName}`,
- disables passive skills,
- shows cooldown text for skills on cooldown.

The skill string is not just display text. It is the legacy skill id.

## Footer Selected Unit Stats

`UICanvas` exposes `FooterTextList` for footer stat display. It mirrors the
same stat order used by `InfoTextsList`:

- 0: max/current effective HP text,
- 1: current temporary HP,
- 2: attack,
- 3: defense,
- 4: damage range,
- 5: movement speed,
- 6: initiative,
- 7: unit name.

Current behavior: the footer selected unit is the active turn unit,
`MouseControler.SelectedToster`. This is intentionally a bridge behavior until
the project has separate click-selection for inspecting other units.

Future UI work may introduce a separate selected/inspected unit, but should keep
the footer update path separate from skill ownership and skill execution.

## Current Unit Portrait

`UICanvas` exposes `CurrentUnitPortrait` for a normal UI `Image` portrait. The
current implementation loads the active unit sprite from:

- `MouseControler.SelectedToster.TosterSpriteName`

That value comes from the `spritePath` field in the unit catalog and is
loaded with:

- `Resources.Load<Sprite>(SelectedToster.TosterSpriteName)`

Current behavior: the portrait displays the active turn unit. Later UI work may
switch this to a separately inspected/selected unit.

RenderTexture/camera portrait was tested as an experiment and recorded in:

- `_codex/Documentation/ADR_001_UI_Portrait_RenderTexture_Experiment.md`

## Skill Button Click Flow

Current skill buttons call:

- `MouseControler.CastSkill(int slotIndex)`

That method RPCs to:

- `MouseControler.CastSkillBooleanss(...)`

Which then calls:

- `MouseControler.CastSkillBooleans(slotIndex, selectedUnit)`

`CastSkillBooleans(...)` sets:

- `MouseControler.SelectedSpellid`
- `MouseControler.SkillState`

Then it asks:

- `CastManager.getMode(SelectedToster.skillstrings[SelectedSpellid],
  SelectedToster)`

`CastManager.getMode(...)` invokes `{SkillName}M` by reflection. After the
player clicks the target hex, `MouseControler.startSpell(...)` calls:

- `CastManager.startSpell(skillId, targetHex)`

And `CastManager.startSpell(...)` invokes `{SkillName}` by reflection.

For a new UI skill button, the safest current call is:

```csharp
mouseControler.CastSkill(slotIndex);
```

Do not call `CastManager` directly from new UI unless the skill/targeting flow
is being intentionally refactored.

## Skill Metadata And Right Click Info

Skill descriptions and skill type are loaded from:

- `TArenaUnity3D/Assets/Resources/Data/skills.xml`

Current right-click skill info uses:

- `RightClickInfoSkill.SetPanelSkill(skillName)`

That class reads `skills.xml` and fills name, type, and info text. Existing
`UICanvas.GetTypeOfSkill(skillName)` also reads `skills.xml` and returns the
skill type, currently used to disable passive skill buttons.

## Skill Id Coupling Rule

The same skill string is currently expected to match all of these:

- unit skill assignment in the unit catalog,
- runtime `TosterHexUnit.skillstrings`,
- UI icon path `Resources/Sprites/Skill_Icons/{SkillName}`,
- info entry in `Resources/Data/skills.xml`,
- `CastManager` methods `{SkillName}M` and `{SkillName}`.

When building new UI, keep this string as the internal `skillId`. If the UI
needs nicer labels, localized text, or separate visual names, add those as
display data and keep the legacy id unchanged.

## Recommended New UI Adapter Shape

For near-term UI work, prefer a thin adapter over changing gameplay logic:

- Read active unit from `MouseControler.SelectedToster`.
- Read action availability from `MouseControler.activeButtons`.
- Read skill slots from `SelectedToster.skillstrings`.
- Read cooldowns from `SelectedToster.cooldowns`.
- Load icons by current Resources path until a deliberate asset migration exists.
- On click, call `MouseControler.CastSkill(slotIndex)`.
- For descriptions, use the same skill id against `skills.xml`.

## Massochism UI Note

`Massochism` does not need a dedicated trigger-time visual effect just because
its deferred bonus damage value is refreshed. For future UI work, prefer showing
the currently stored bonus damage value as a small stack/count-style indicator
on the relevant skill slot, active-unit panel, or other battle HUD element.

Treat that value as read-only gameplay state exposed by the active unit. Keep
the UI as a thin bridge over existing runtime logic rather than moving the
`Massochism` calculation into UI code.

Keep this adapter small. Do not rename public/serialized fields or move skill
ownership out of XML unless the user explicitly asks for a broader refactor.

## Known UI Risks

- UI state is refreshed every frame in `UICanvas.Update()`.
- UI is tightly coupled to `MouseControler` and `TosterHexUnit`.
- Skill ids are fragile strings and can break UI, icon lookup, info lookup, and
  skill execution if renamed.
- Passive detection depends on `skills.xml` type text being exactly `Passive`.
- Some scenes use `MouseControler.CastSkill(int)` in button `OnClick`; one older
  scene uses wrapper methods `CastSkill1B` through `CastSkill4B`.
- `UICanvas` is legacy UI. A new UI should bridge to the existing flow first,
  then refactor only in small explicit steps.

## Related Context

- `_codex/Context/09_CurrentSkills.md`
- `_codex/Context/10_Skill_Design_Rules.md`
- `_codex/agents/docs/codebase-map.md`
