# TArena Mock UI Rules

Use this reference when creating task mockups, Unity UI prefabs, copied mockup
prefabs, or reusable UI template prefabs.

These rules are grounded in TArena project context and Unity UGUI practice:

- split large UI by update frequency when canvases become large,
- avoid unnecessary `GraphicRaycaster` and `Raycast Target` work,
- use layout groups where they describe real child layout,
- avoid deep nested layout groups on frequently changing UI,
- use nested prefabs and prefab variants for repeated production UI.

Useful external references:

- https://unity.com/how-to/unity-ui-optimization-tips
- https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/UIAutoLayout.html
- https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/UIBasicLayout.html
- https://docs.unity3d.com/2022.3/Documentation/Manual/script-GraphicRaycaster.html
- https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/TextMeshPro/index.html
- https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Slider.html
- https://docs.unity3d.com/Manual/NestedPrefabs.html
- https://docs.unity3d.com/Manual/PrefabVariants.html

## Asset Sources

Use these project-local assets first:

- UI frames, panels, borders, buttons: `TArenaUnity3D/Assets/Classic_RPG_GUI`
- Paper/parchment surfaces: `TArenaUnity3D/Assets/Old_Paper_Gui`
- Skill icons: `TArenaUnity3D/Assets/Resources/Skill_Icons`
- Unit icons: `TArenaUnity3D/Assets/Resources/UI/Unit_Icons`

Do not invent new visual systems when these folders contain a usable asset.
If a final dynamic source is not implemented yet, use placeholder icons from
these folders and name the placeholder clearly in the prefab hierarchy.

## Reuse Existing UI Elements First

Before creating a new button, card, frame, panel, icon slot, list row, or
selection state, search the approved UI asset folders and existing UI prefabs.

- Prefer existing project sprites, panels, frames, and button visuals.
- Prefer copying or varianting an existing repeated element over inventing a
  visually unrelated one.
- Keep new UI compatible with existing UGUI components unless the task
  explicitly asks for UI Toolkit or another system.
- Do not leave plain default Unity UI controls when a project-styled element
  exists.
- New placeholder visuals must be named as placeholders and listed in the final
  report.
- If a suitable generated row/card/button/section prefab already exists and
  works, use that prefab as a nested prefab instance in the parent screen.
- Do not copy the children of a working row/card/button prefab into the parent
  screen as normal GameObjects.

## Coherent Visual Composition

Before creating objects, decide what the screen is mainly for:

- choose one reward,
- inspect an army,
- pick a route,
- cast or inspect a skill,
- buy/sell/upgrade,
- confirm a run or battle action.

Then build around that main action.

- Give the primary action the clearest position and strongest button treatment.
- Keep secondary actions smaller, dimmer, or grouped away from the main action.
- Use one panel/frame family per screen unless a contrast has meaning.
- Keep icon sizes consistent inside one repeated element family.
- Keep list spacing and card padding consistent across the screen.
- Do not mix fantasy frame styles randomly. Prefer one base surface and one
  accent treatment.
- Show important game state directly: unit count, stack size, cooldown, locked
  state, selected state, disabled state, cost, reward rarity, or route danger.
- Put state overlays inside the repeated item prefab, not scattered in the main
  screen root.

## How To Make Mock UI

Build the UI like a future production prefab, not like a flat drawing.

- Do not create RectTransform-only skeleton prefabs.
- A valid prefab must contain visible UGUI components, wired scripts, and sample
  labels/icons/states that can be inspected in Unity.
- Start from a clear root and named sections.
- Prefix script-owner objects with `Script_`, for example
  `Script_StartRunScreenController`.
- Keep visual sections as normal named objects, for example
  `Section_StartingArmies`, `ArmyPreview_StackRowsList`,
  `RoutePreview_RoutesList`.
- Name repeated list parents as `List_*` or `*_List`.
- Use `VerticalLayoutGroup` for vertical lists: army cards, stack rows, route
  option lists, reward cards, menu option lists.
- Use `HorizontalLayoutGroup` for horizontal lists: unit icon rows, route node
  rows, skill button rows, stat columns when they behave as a row.
- Use `GridLayoutGroup` for inventory-like icon grids only when rows and columns
  are both meaningful.
- Do not use layout groups for fixed one-off decoration or fixed child counts.
- Add padding and spacing on the layout parent, not by scattering arbitrary
  offsets across every child.
- Do not overuse layout groups on isolated decorative elements.
- Avoid deep chains of nested layout groups on UI that changes every frame or
  refreshes frequently.
- Use anchors and `LayoutElement` preferred sizes for stable mock composition.
- Do not manually tune driven RectTransform fields under a layout controller as
  if those values are stable authored layout.
- Add prefab assets for every repeated element: `ArmyCard`, `ArmyCard_UnitFrame`,
  `ArmyPreview_StackRow`, `RouteOption`, `RouteNode`, `RouteEdge`, reward cards,
  skill buttons, or similar.
- Use nested prefabs when the parent screen should keep links to repeated child
  prefab assets. This is required for repeated rows, cards, buttons, route
  nodes, route edges, and separate reusable sections.
- The parent screen hierarchy should show prefab instances such as
  `CurrentArmy_Row_01`, `OfferCard_01`, or `Button_BuyFocusedOffer`, but their
  internal implementation should come from the prefab asset.
- If Unity/YAML authoring cannot safely create a nested prefab instance, stop
  and report a blocker instead of hand-expanding the prefab into the parent.
- Use prefab variants when several elements share the same base structure but
  need different default art, state, or labels.
- For risky nested prefab work, first create a copy such as
  `PRD_XX_YY_copy.prefab` and verify import in Unity before replacing the
  original.
- Keep the main prefab hierarchy readable even when nested prefabs are used:
  list parent -> repeated prefab instance -> internal visual parts.

## Component Rules

- Always use `TextMeshProUGUI` for UI labels. Do not use legacy `Text`.
- Use `Image` for visible panels, frames, icons, backgrounds, overlays, and
  selection states.
- Use `Button` for clickable commands and selections.
- Use `Slider` for progress bars.
- Use layout groups only when the number of children can change.
- Search the current scene for an existing `Canvas` before adding scene UI.
- Keep UI Z position at `0` unless intentionally layering.
- Use clear callback names with the `OnButtonNameClicked()` pattern.
- Use `[Header]` attributes in UI scripts to organize Inspector fields.
- Cache frequently accessed components in `Awake()` or `Start()`.
- Keep UI scripts single-responsibility. Separate display code from game/backend
  logic.

## Recommended Tree Shapes

Use these shapes as patterns, not exact required names.

Battle HUD:

```text
Mock_BattleHud
- Script_BattleHudView
- Section_TopTurnBar
- Section_LeftActiveUnit
  - ActiveUnit_PortraitFrame
  - ActiveUnit_StatsList
- Section_BottomSkills
  - List_SkillButtons
    - SkillButton_01
      - Icon
      - CooldownOverlay
      - State_Disabled
- Section_RightInspection
```

Reward choice:

```text
Mock_RunRewardChoice
- Script_RunRewardChoiceView
- Section_Header
- Section_RewardCards
  - List_RewardCards
    - RewardCard_OptionA
      - Icon
      - Title
      - Body
      - State_Selected
- Section_Confirm
```

Route preview:

```text
Mock_RoutePreview
- Script_RoutePreviewView
- Section_CurrentArmy
- Section_Routes
  - List_RouteOptions
    - RouteOption_A
      - List_RouteNodes
      - RouteDangerLabel
- Section_Confirm
```

Repeated element prefab:

```text
RewardCard
- Script_RewardCardView
- Frame
- IconSlot
- TextBlock
- State_Selected
- State_Disabled
```

Parent screen using repeated prefabs:

```text
Mock_RunShopScreen
- Script_RunShopScreenController
- Section_CurrentArmy
  - List_CurrentArmyRows
    - CurrentArmy_Row_01 -> prefab instance: PRDXX_ArmyPreview_StackRow
    - CurrentArmy_Row_02 -> prefab instance: PRDXX_ArmyPreview_StackRow
- Section_GroupedOffers
  - List_OfferCards
    - OfferCard_01 -> prefab instance: PRDXX_OfferCard
    - OfferCard_02 -> prefab instance: PRDXX_OfferCard
- Section_Commands
  - Button_BuyFocusedOffer -> prefab instance: PRDXX_CommandButton
```

## Script Wiring

- Before creating the prefab, inspect the implementation task output and the
  C# scripts it created or changed. The mock UI must be wired to those scripts
  when they are UI-facing.
- Script-owner GameObjects must be obvious in hierarchy by name.
- Wire all script references in prefab/Inspector fields.
- Do not use name-based lookup as a substitute for proper wiring.
- Do not add `Find`, `transform.Find`, `GameObject.Find`, runtime
  `AddComponent`, or child discovery caches for mock UI.
- If a mock needs dynamic data later, expose serialized arrays/lists for now and
  fill them with mock references.
- Keep public/serialized field names stable unless the user explicitly approves
  a rename.
- For a screen-level view/controller, serialized fields should point to section
  roots, repeated item templates, important buttons, and any placeholder slots
  the user must inspect in Unity.
- For repeated item views, serialized fields should point only to the item's own
  children unless there is an explicit cross-item relationship.
- A required serialized UI field left as `{fileID: 0}` is a defect. If a field is
  optional, call it out in the final report or name it clearly in code/docs.
- Attach script components to `Script_*` GameObjects. Do not leave script owner
  objects as empty RectTransforms.
- Wire serialized fields to real child `Image`, `Button`, `Slider`,
  `TextMeshProUGUI`, section roots, or repeated item prefabs.
- Button click bindings should call the current project bridge method when one
  exists. For battle skill buttons, use `MouseControler.CastSkill(slotIndex)`
  rather than calling `CastManager` directly.
- If generated script `.meta` files do not exist yet, stop. Let Unity import
  scripts first, then wire prefab GUIDs. Do not invent or fake GUIDs.
- If a script requires GameObject references, data sources, child views,
  templates, or buttons, assign them in serialized fields before reporting the
  mockup as done.

## Functional Wiring

UI mockups must be usable in Unity as far as the available scripts and backend
allow.

- Buttons must have an intended `OnClick` target, even if the current task only
  wires a placeholder script field.
- Buttons must not be left with empty `m_OnClick` calls unless explicitly
  documented as disabled visual-only state.
- Toggles, tabs, card selections, sliders, and inputs must have an owner script
  and an expected state change.
- If the screen displays list data, the list owner should expose serialized
  mock entries or connect to the existing data source.
- If the implementation task created a service, adapter, repository, SQL/API
  bridge, or backend-facing interface, wire the UI controller to that existing
  backend path when the task requires live data.
- If no backend exists and the UI can work from local mock/sample data, do not
  invent SQL, API, or persistence. Use the local mock/sample data and omit the
  backend gap unless the task explicitly expects backend data.
- If live data does not exist yet, mark the serialized source or report line as:
  `tutaj powinno byc z bazy danych`.
- If account/profile/shop/matchmaking/save backend is missing, do not fake it as
  finished. Use a local mock list or placeholder and report the backend gap.
- If a missing C# method prevents a button from working, report the exact
  GameObject, script, missing method, and expected result.

Required final report lines:

```text
UI setup:
- GameObject: <name> | Script: <script or none> | Ustawic: <fields/assets/click binding> | Robi: <short behavior> | Po akcji: <expected result>

Backend gaps:
- <GameObject/field>: tutaj powinno byc z bazy danych

Test:
- Otworz: <scene/prefab>
- Sprawdz: <object/field/state>
- Kliknij: <button/control> -> <expected result>
```

Rules for the report:

- Keep every line short.
- Include every script-owner GameObject.
- Include every interactive control.
- Use `Script: none` only for purely visual objects.
- Use `Backend gaps: brak` when there are no backend/data-source gaps.
- Mention Unity not run only once, in the `Test` section.

## Canvas And Interaction Boundaries

- Keep static decorative UI, frequently changing values, and interactive
  controls as separate named regions.
- Split canvases only when the mockup is large enough that rebuild isolation
  matters or the task explicitly involves a full screen/HUD.
- A non-interactive canvas should not keep a `GraphicRaycaster`.
- Static images and labels should have `Raycast Target` disabled when they do
  not need pointer events.
- Avoid UI `Animator` components for one-off feedback states. Prefer explicit
  state objects or code/tween-driven feedback in later implementation.

## Output Folders

Use these folders for generated UI work:

- C# scripts:
  `TArenaUnity3D/Assets/Scripts/RunMetagame/<TaskNumber_FeatureName>/`
- Final PRD/task UI prefabs:
  `TArenaUnity3D/Assets/Resources/UI/PRD_19/<TaskNumber_FeatureName>/`
- Reusable generated section and repeated-item prefabs:
  `TArenaUnity3D/Assets/Resources/UI/PRD_19/<TaskNumber_FeatureName>/Prefabs/`

Example:

```text
PRD RunMetagame / task Shop
- scripts: Assets/Scripts/RunMetagame/024_RunShop/
- final prefabs: Assets/Resources/UI/PRD_19/024_RunShop/
- section/item prefabs: Assets/Resources/UI/PRD_19/024_RunShop/Prefabs/
```

## Problems Already Hit And Fixes

- Unity can report `Failed to load prefab` when YAML lines get joined. In
  particular, `- {fileID: ...}  m_Father: ...` is invalid. Split it into two
  lines.
- Prefabs need the standard Unity YAML header:
  `%YAML 1.1` and `%TAG !u! tag:unity3d.com,2011:`.
- After hierarchy rewrites, verify local positions. Re-parenting can double
  apply offsets if old local coordinates are treated as absolute coordinates.
- Nested prefab instances are fragile when generated by hand. Prefer creating a
  copy first, and validate stripped `GameObject`, stripped `RectTransform`, and
  stripped script references.
- If replacing multiple siblings under one parent, update the parent's full
  `m_Children` list once. Replacing one child at a time can reintroduce old
  `fileID`s.
- Template prefabs copied from a main prefab may contain stale references after
  later wiring changes. Regenerate or refresh templates after script reference
  wiring.
- Always validate:
  - no joined YAML fields,
  - unique `fileID`s,
  - component references exist,
  - child/parent references exist,
  - repeated-looking objects such as `*_Row_01`, `OfferCard_01`, or
    `Button_*` are nested prefab instances when matching prefab assets exist,
  - prefab is not a RectTransform-only skeleton,
  - visible `Image`, `Button`, `Slider`, and `TextMeshProUGUI` components exist
    where appropriate,
  - script-owner GameObjects have real script components,
  - buttons have wired methods or are explicitly documented as disabled,
  - generated script `.meta` files exist before prefab GUID wiring,
  - no required serialized UI fields are `{fileID: 0}`,
  - all intended local GameObjects are reachable from root.

If `scripts/validate_ui_prefab.py` exists, run it on edited prefab files before
final reporting. Treat its errors as blocking. Treat its warnings as manual
review items unless the task says all optional references must be wired.
