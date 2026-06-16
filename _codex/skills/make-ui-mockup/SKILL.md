---
name: make-ui-mockup
description: Create or update TArenaUnity3D Unity UI mockups and production-shaped UGUI prefab prototypes after UI/task implementation. Use for task mockups, menu screens, HUD panels, route/army/reward previews, repeated UI prefab templates, coherent visual composition, reusable existing UI elements, functional buttons/controls, Unity hierarchy structure, layout groups, nested prefabs or variants, required output folders for scripts and prefabs, serialized script wiring, backend/data-source gaps, final GameObject-script setup reports, and prefab YAML/reference validation.
---

# Make UI Mockup

Use this after implementing a UI-facing task or when the user asks for a Unity
mockup/prototype prefab for a task. The output should be a Unity-shaped mockup:
coherent enough for review, structured enough to become production UI, and
wired enough that the owning scripts can be inspected in the prefab.

This skill produces Unity UGUI prefabs only. Do not create or update HTML,
JavaScript, browser pages, or `_codex/Gen_Im/RETSOT ONLINE/` as a substitute for
the mockup. If Unity prefab authoring is blocked, report the blocker instead of
falling back to web output.

Do not create empty RectTransform-only skeleton prefabs. A valid UI mockup must
be visible, component-based, and wired.

## Project UI Text Rule

TArenaUnity3D uses TextMesh Pro only. Use TMP components such as `TMP_Text` and
`TextMeshProUGUI` for all text content. Do not introduce or keep legacy
`UnityEngine.UI.Text`.

Read `_codex/Context/CONTEXT-MAP.md`, then `_codex/Context/11_UI_Context.md`.
For exact project asset folders, workflow rules, and known pitfalls, read
`references/mock-ui-rules.md`.

If editing or checking prefab YAML outside Unity, also use
`scripts/validate_ui_prefab.py`.

## Workflow

1. Inspect the task/PRD and existing UI context before editing assets.
2. Identify the main PRD name, task name, PRD number, screen owner, repeated
   elements, dynamic regions, and static decorative regions before building
   the hierarchy.
3. Inspect UI-facing scripts created or changed by the implementation task and
   plan prefab wiring around those scripts.
4. Search the current scene for an existing `Canvas` and place new scene UI
   under that canvas unless the task explicitly requires a new canvas.
5. Use project UI art from the approved folders in the UI context.
6. Reuse existing project UI prefabs, buttons, frames, panels, icons, and item
   templates before creating new elements.
7. Create C# scripts under
   `TArenaUnity3D/Assets/Scripts/RunMetagame/<TaskNumber_FeatureName>/`, for
   example `TArenaUnity3D/Assets/Scripts/RunMetagame/024_RunShop/`.
8. Store final task/PRD UI prefabs under
   `TArenaUnity3D/Assets/Resources/UI/PRD_19/<TaskNumber_FeatureName>/`.
9. Store reusable generated section/repeated-item prefabs under
   `TArenaUnity3D/Assets/Resources/UI/PRD_19/<TaskNumber_FeatureName>/Prefabs/`.
10. Build obvious repeated UI as prefabs, nested prefabs, prefab variants, or
   clearly named prefab templates.
11. In the final screen prefab, instantiate existing row/card/button/section
   prefabs as nested prefab instances. Do not manually recreate or expand their
   child hierarchy inside the parent prefab.
12. Use `VerticalLayoutGroup`, `HorizontalLayoutGroup`, or `GridLayoutGroup`
   only where the number of child objects is likely to change.
13. Keep static decoration, dynamic lists, and interactive controls in separate
   named sections.
14. Prefix GameObjects that own scripts with `Script_`.
15. Wire controller/view references through serialized fields in the prefab.
16. Wire buttons, toggles, and other controls to real scripts or documented
   placeholder hooks.
17. Wire existing backend/service/data-source dependencies when the task
   requires them. Do not invent backend work when none exists.
18. Validate YAML and reference graph after prefab edits.
19. End with the required short setup and test report.

If the PRD number cannot be identified, do not invent a final PRD folder. Ask
the user or report the missing PRD number before creating final prefabs.

## Coherent UI Rules

- Start from the player's task on that screen: choose, inspect, confirm, route,
  cast, buy, or compare. Remove controls that do not serve that task.
- Keep one primary interaction path. Secondary controls should be visually
  quieter and grouped away from the primary action.
- Reuse the same frame, paper surface, icon treatment, typography scale, and
  spacing rhythm within one screen.
- Prefer existing TArena UI asset folders over invented visual systems.
- Make repeated elements match exactly unless their state differs intentionally.
- Use real project icons where available. Use clearly named placeholders only
  when the final dynamic source does not exist yet.
- Name visual states in the hierarchy when they matter, for example
  `State_Selected`, `State_Disabled`, `CooldownOverlay`, or `LockedOverlay`.

## Hierarchy Contract

- Root: one clearly named screen or panel root, for example
  `Mock_RunRewardScreen` or `Mock_BattleHud`.
- Script owner: one obvious `Script_*` GameObject for each controller/view
  script. Avoid hiding scripts on decorative objects.
- Sections: use `Section_*` for large functional regions.
- Lists: use `List_*` or `*_List` on parents that own repeated children and
  layout groups.
- Repeated items: use prefab assets or prefab-template roots for cards, rows,
  route options, route nodes, route edges, skill buttons, reward cards, unit
  frames, and similar repeated blocks.
- Visual leaves: keep icons, labels, frames, overlays, and decorative images
  as children of the smallest repeated item that owns them.
- Do not create UI hierarchies where script ownership, visual ownership, and
  data ownership are impossible to read from the Inspector.

## Script Wiring Contract

- Do not rely on runtime name lookup for UI references.
- Do not add `Find`, `transform.Find`, or automatic child discovery just to make
  a mockup work.
- Prefer explicit serialized fields and Inspector/prefab wiring.
- Add or use serialized arrays/lists for repeated UI slots when the data source
  is not implemented yet, then fill them with mock references.
- Required script references should not remain `{fileID: 0}` in prefab YAML.
- Keep public and serialized field names stable unless the user explicitly
  approves a rename.

## Functional UI Contract

- UI must be functional, not only visual, unless the user explicitly asks for a
  static mockup.
- A prefab with only GameObjects and RectTransforms is a failed mockup.
- Add visible UGUI components where appropriate: `Image`, `Button`, `Slider`,
  `Toggle`, and `TextMeshProUGUI`.
- Add sample labels, icons, selected/disabled/cooldown states, and visible
  placeholder data so the prefab is not empty in Scene view.
- Every visible button, toggle, tab, slider, input, or selectable card should
  have an intended script owner and an expected result.
- Use `TextMeshProUGUI` for UI text. Do not use legacy `Text`.
- Use `Slider` for progress bars.
- Use real existing project bridge methods when they exist. For battle skill
  buttons, call `MouseControler.CastSkill(slotIndex)`.
- UI-facing scripts created by the implementation task must be attached to
  `Script_*` GameObjects and have required child GameObjects assigned.
- When backend, save data, account data, database data, matchmaking, shop data,
  or remote profile data is missing, keep the UI wired to an explicit mock or
  placeholder field and write exactly: `tutaj powinno byc z bazy danych`.
- If no backend exists and the UI can use local mock/sample data, do not invent
  SQL, API, or persistence.
- Do not hide missing backend work behind hardcoded final-looking data. Mark it
  in hierarchy names, serialized fields, or final report.
- If the current task does not permit C# edits, wire only existing scripts and
  report any missing hook as a required next script change.
- If generated script `.meta` GUIDs are missing, do not fake prefab script
  references. Let Unity import scripts, then use the generated GUIDs, or report
  the missing import as a blocker.

## Unity UI Structure Rules

- Use layout groups only where the number of child objects is likely to change.
- Avoid deep chains of nested layout groups on UI that changes often.
- Use anchors and `LayoutElement` sizes for stable composition.
- Anchor UI elements relative to intended screen position: top-left to
  top-left, center to center, bottom UI to bottom, and scalable panels or
  backgrounds with stretch anchors.
- Do not position UI with absolute world coordinates. Use anchored positions.
- Set pivots deliberately. Use `(0.5, 0.5)` for center scaling.
- Use `sizeDelta` for pixel-perfect dimensions when anchors are together.
- Keep UI Z position at `0` unless intentionally layering.
- Use consistent spacing, preferably multiples of `8` or `10`.
- Split static, dynamic, and interactive regions into separate canvases only
  when the mockup already represents a large or frequently updating UI surface.
- Remove `GraphicRaycaster` from non-interactive canvases when creating a
  production-shaped mockup.
- Disable `Raycast Target` on static text/images when practical.
- Do not put `Animator` components on UI elements unless the element is meant
  to change every frame or the user explicitly asks for animated UI.

## Prefab Rules

- Keep UI hierarchy aligned with code ownership: sections, lists, rows, cards,
  route options, nodes, icons, and script owners should be easy to inspect.
- Always create a prefab for repeated object groups.
- Always create a prefab for UI objects that are separate sections of the UI.
- Always reuse an existing generated prefab for repeated objects after creating
  it once.
- If a row, card, button, route node, or section prefab already exists and works,
  the parent screen prefab must contain nested prefab instances of it. Copying
  or hand-rebuilding its children into the parent is a defect.
- Store final task/PRD prefabs under
  `Assets/Resources/UI/PRD_19/<TaskNumber_FeatureName>/`.
- Store generated reusable/section prefabs under the same task folder, usually
  in `Assets/Resources/UI/PRD_19/<TaskNumber_FeatureName>/Prefabs/`.
- Use nested prefabs when the same child prefab should remain linked across
  screens.
- Use prefab variants when a repeated element has the same base structure with
  different default visuals or state.
- Use copied/test prefabs for risky nested prefab conversion before replacing a
  working prefab.

## Validation

For prefab YAML edited outside Unity, run checks equivalent to:

- no `}  m_` or child/parent lines joined on one line,
- unique `fileID` values,
- every `m_Component` points to an existing object,
- every non-stripped `RectTransform` child and parent points to an existing
  `RectTransform`,
- no serialized UI controller field remains `{fileID: 0}` unless intentionally
  optional,
- visible UI has real UI components such as `Image`, `Button`, `Slider`,
  `Toggle`, `TMP_Text`/`TextMeshProUGUI`, or task-specific scripts,
- repeated rows/cards/buttons use nested prefab instances when corresponding
  prefab assets exist,
- root reaches all intended local GameObjects.

When the script is present, run:

```bash
py -3 _codex/skills/make-ui-mockup/scripts/validate_ui_prefab.py <prefab-path>
```

If generated scripts are involved, also pass the script folder:

```bash
py -3 _codex/skills/make-ui-mockup/scripts/validate_ui_prefab.py <prefab-path> --script-dir <generated-script-folder>
```

Unity import is still the final validation. Tell the user when Unity was not
run.

## Final Report Format

End every UI mockup task with short, complete lists. Do not write long
descriptions.

Use this shape:

```text
UI setup:
- GameObject: <name> | Script: <script or none> | Ustawic: <fields/assets/click binding> | Robi: <short behavior> | Po akcji: <expected result>

Nested prefabs:
- Parent: <screen/list> | Instance: <child instance> | Prefab: <prefab asset path>

Backend gaps:
- <GameObject/field>: tutaj powinno byc z bazy danych

Test:
- Otworz: <scene/prefab>
- Sprawdz: <thing to inspect>
- Kliknij: <control> -> <expected result>
- Brak Unity testu: <yes/no and why>
```

Include every script-owning GameObject and every interactive control. If there
are no backend gaps, write `Backend gaps: brak`. If there are no repeated or
section prefabs, write `Nested prefabs: brak`.
