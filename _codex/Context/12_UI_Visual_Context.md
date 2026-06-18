# 12 UI Visual Context

Status: active
Project: TArenaUnity3D
Last updated: 2026-06-18

## Purpose

This document defines the visual sizing, spacing, and polish rules for TArena
UGUI mockups. Use it when improving a functional mockup into a readable,
project-consistent UI.

This is visual guidance only. Do not break script wiring, button callbacks,
nested prefab instances, backend/data bindings, or serialized field contracts
to make UI prettier.

Visual polish must preserve the UI Architecture Contract from
`_codex/Context/11_UI_Context.md`: screen-level objects keep only top-level
controls and view-class references, complex panels/items are view classes, and
repeated UI uses `parent + prefab` under a configured layout parent.

## Identity Direction

TArena UI should support:

- Mewgenics-like short run decisions,
- Heroes 3-like army and stack readability,
- quick comparison of route, reward, shop, unit, and skill choices,
- the feeling that a run produces a concrete army worth saving.

Visual tone:

- readable tactical parchment/fantasy UI,
- framed information panels,
- clear army/stack icons,
- modest ornament, not dense decoration,
- fast scanning over cinematic presentation.

## Source Assets

Use these first:

- RPG frames, slot backgrounds, icon frames:
  `TArenaUnity3D/Assets/Classic_RPG_GUI`
- Parchment, paper, panel, bar, button, ornament assets:
  `TArenaUnity3D/Assets/Old_Paper_Gui`
- Skill icons:
  `TArenaUnity3D/Assets/Resources/Skill_Icons`
- Unit icons:
  `TArenaUnity3D/Assets/Resources/UI/Unit_Icons`
- Existing PRD19 UI prefabs:
  `TArenaUnity3D/Assets/Resources/UI/PRD_19/020_StartRun`
  and `TArenaUnity3D/Assets/Resources/UI/PRD_19/024_RunShop`

Reference screenshots should live in:

```text
TArenaUnity3D/Assets/Resources/UI/References
```

PRD polish screenshots should be saved under:

```text
TArenaUnity3D/Assets/Resources/UI/PRD_<number>/Screenshots
```

If a task uses the shorthand `UI/References` or `UI/PRD`, map it to the
`Assets/Resources/UI/...` paths above.

## Canvas Baseline

Default review target:

- reference resolution: `1600 x 900`,
- aspect ratio: `16:9`,
- root screen size: `1600 x 900`,
- safe outer margin: `32`,
- major section gap: `24`,
- minor group gap: `12`,
- dense row gap: `8`.

Use multiples of `8` for most spacing. Use `10` only when matching existing
legacy UI spacing.

## Zones

For full-screen run/shop/reward screens:

- header zone: `80-112` high,
- left army zone: `360-440` wide,
- center choice/offer zone: `520-680` wide,
- right preview/details zone: `360-440` wide,
- bottom command zone: `72-104` high,
- result/status message: `28-44` high.

For battle HUD:

- top turn bar: `56-80` high,
- bottom skill/action bar: `96-140` high,
- active unit panel: `280-360` wide,
- inspection/info panel: `320-420` wide,
- skill button row gap: `8-12`.

For PRD019 Run Map UI, use `_codex/Context/RunMap_UI_Context.md` before doing
visual polish. Run Map has stricter requirements for connected paths, current
party marker, node states, symbolic node icons, and map-background context than
generic UI panels.

## Element Sizes

Unit and skill icons:

- small inline icon: `32 x 32`,
- normal unit icon: `48 x 48`,
- large unit portrait/icon: `72 x 72`,
- skill button icon: `56 x 56`,
- route node icon: `48 x 48`,
- reward/shop primary icon: `64 x 64`.

Rows and cards:

- compact stack row: `320-420 x 56-72`,
- normal army row: `360-440 x 72-88`,
- offer/reward card: `160-220 x 220-300`,
- wide route option: `420-560 x 96-140`,
- wallet/resource summary: `240-360 x 56-80`.

Buttons:

- small icon button: `40 x 40`,
- normal command button: `160-220 x 48-64`,
- primary confirm button: `220-280 x 56-72`,
- tab/filter button: `120-180 x 40-52`.

Text:

- screen title: `30-36`,
- section header: `20-24`,
- card title: `18-22`,
- body/row text: `14-18`,
- numeric badge/counter: `14-18`,
- tiny metadata: `12-14`.

Use `TextMeshProUGUI`. Do not use legacy `Text`.

## Readability Rules

- Each screen needs one dominant read path.
- A player should identify the main action within one second.
- Army rows must expose unit icon, unit name, stack amount, and key delta/state.
- Reward/shop cards must show icon, title, cost/value, and selection state.
- Route options must show node sequence, risk/danger, and reward promise.
- Disabled states must be visible without hiding labels.
- Selected state must use frame/overlay emphasis, not only color.
- Cooldown/locked/owned/purchased states should be overlays inside the repeated
  prefab, not separate loose objects in the parent screen.
- Avoid decorative clutter that competes with army values, route choices, or
  skill buttons.

## Composition Rules

- Prefer parchment or framed panel surfaces for information sections.
- Use Classic RPG GUI frames for slots, icons, skill-like controls, and item-like
  blocks.
- Use Old Paper GUI for larger panels, shop/reward surfaces, and readable
  content backgrounds.
- Keep repeated rows/cards visually identical except state overlays and content.
- Repeated rows/cards/buttons are prefab-owned view classes, not loose copied
  children in the screen root.
- Place selected, disabled, locked, cooldown, owned, and purchased states inside
  the repeated prefab/view class that owns that item.
- Keep all section headers aligned to the same inset within a screen.
- Keep button rows aligned and evenly spaced.
- Use nested prefab instances for repeated rows/cards/buttons.
- Do not hand-copy the children of a repeated prefab into the parent screen.

## Screenshot Review Checklist

When reviewing a Unity screenshot, check:

- primary action is obvious,
- section hierarchy is readable,
- no labels overlap,
- no text is clipped,
- icons are large enough to identify,
- repeated rows/cards are aligned and consistent,
- selected/disabled/locked/cooldown states are visible,
- visual style matches parchment/RPG frame direction,
- empty space separates decisions instead of scattering content,
- no default Unity placeholder styling remains where project assets exist.

## Iteration Rule

After each visual pass:

1. Save a Unity screenshot under the PRD screenshot folder.
2. Compare it against screenshots in `UI/References`.
3. List the top 3 readability/style issues.
4. Fix only those issues.
5. Repeat until the screen is readable and visually aligned.

Do not keep polishing if the functional wiring breaks.
