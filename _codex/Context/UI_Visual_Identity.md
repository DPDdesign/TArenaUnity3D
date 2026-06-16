# TArena UI Visual Identity

Status: active v0.2
Project: TArenaUnity3D
Last updated: 2026-06-15

## Purpose

This file is the canonical visual identity spec for TArena Unity UGUI work.

Use it when creating or polishing UI mockups so font, color, sizing, contrast,
and component choices stay consistent across battle HUD, run map, shop, reward,
army, metagame, and menu screens.

This is a semantic token spec first. Prefer updating this file when the UI
identity changes, then applying the new tokens to existing UI without changing
layout structure, gameplay wiring, serialized fields, or button callbacks.

## Core Identity

TArena UI should feel like:

* dark tactical fantasy,
* brown metallic HUD,
* readable strategy-game interface,
* premium but not overdecorated,
* fast to scan,
* practical for repeated battles and metagame decisions.

The UI should not feel like:

* parchment RPG menu,
* mobile fantasy ad UI,
* shiny AI-generated metal,
* generic medieval scroll interface,
* overly bright paper/card game UI,
* unreadable dark fantasy mockup.

The player should immediately understand:

1. what screen they are on,
2. what is selected,
3. what decision is available,
4. what the cost/risk/reward is,
5. what button continues the flow.

## Reference Targets

Primary visual reference:

* `TArenaUnity3D/Assets/Resources/UI/Arena_Footer.prefab`

Use `Arena_Footer` for:

* brown metallic HUD direction,
* density,
* TMP font choice,
* warm text on dark surfaces,
* slot and stat readability,
* grounded tactical fantasy tone.

Do not use `Arena_Footer` as a layout-scale reference. Its scene/prefab
transforms are legacy and may be oddly scaled or positioned.

## Canvas And Responsiveness

Canonical polish/authoring target:

```text
resolution: 1920 x 1280
aspect: 3:2
authoring scale: logical 1:1 prefab coordinates
```

Canvas Scaler default:

```text
mode: Scale With Screen Size
reference resolution: 1920 x 1280
screen match mode: 0.5
```

Notes:

* Default polish review and authoring should start from `1920 x 1280`.
* Runtime support targets remain `16:9` and `16:10` unless explicitly changed.

Supported aspect groups:

```text
16:9   1280x720, 1920x1080, 3840x2160
16:10  1280x800, 1920x1200, 2560x1600
```

Responsive rules:

* Full-screen UI must define safe margins and stretch behavior for `16:9` and `16:10`.
* HUD/footer UI anchors to the bottom and preserves fixed logical height.
* Wider screens gain horizontal spacing or wider middle content.
* Do not unpredictably scale child objects.
* Text size tokens stay logical; Canvas Scaler handles resolution scaling.
* Do not copy odd scale or transform values from legacy prefabs into new UI.

## Visual Direction

Default style:

* very dark background,
* dark brown/black panels,
* warm bronze/gold trim,
* clear readable text,
* restrained metallic ornament,
* strong active/selected state,
* primary action button clearly visible.

Avoid:

* large shiny gradients,
* bright parchment as default surface,
* too many medium-brown panels,
* low-contrast brown text on black,
* decorative frames that compete with values,
* blue body text,
* red/green-only meaning without icons or labels.

Parchment/paper assets are allowed only for special-case screens, such as lore,
contracts, old records, or tutorial notes. They are not the default TArena UI
identity.

## Asset Priority

Use existing project UI components and sprites before drawing new shapes.

Priority:

1. `TArenaUnity3D/Assets/Classic_RPG_GUI`
   RPG frames, slots, buttons, icon frames, battle/HUD elements.

2. `TArenaUnity3D/Assets/GUI_Parts`
   Simpler neutral GUI parts, icons, support elements.

3. `TArenaUnity3D/Assets/Old_Paper_Gui`
   Fallback or special-case paper surfaces only.

4. `TArenaUnity3D/Assets/Resources/UI`
   Existing generated UI prefabs.

5. `TArenaUnity3D/Assets/Resources/Skill_Icons`
   Skill content.

6. `TArenaUnity3D/Assets/Resources/UI/Unit_Icons`
   Unit content.

Gameplay icons should come from gameplay icon sources. Do not replace skill,
unit, stat, or reward meaning with decorative package icons.

## Color System

The identity uses very dark surfaces with warm readable text and bronze/gold
interaction accents.

Do not make every element gold. Gold means priority, interaction, active state,
or important value.

## Core Palette

```text
color/black                 #040506
color/nearBlack             #080604

color/brown/deep            #120B07
color/brown/base            #1A100A
color/brown/raised          #24160D
color/brown/inset           #090605

color/brown/trimDark        #2E1A10
color/brown/trim            #4A2A18
color/brown/copper          #7A421F
color/brown/copperBright    #A45F2D

color/gold/dim              #8F6A42
color/gold/muted            #B88A55
color/gold/base             #D6A86A
color/gold/bright           #F0C58E

color/text/primary          #F4D3A5
color/text/secondary        #C79C70
color/text/muted            #8B735E
color/text/faint            #6F5A47

color/red/deep              #5A2222
color/red/base              #B9554D
color/red/bright            #F07A6A

color/green/deep            #234A26
color/green/base            #4E9A47
color/green/bright          #79D96F

color/blue/focus            #4D90E8
color/blue/focusDeep        #1D4F91
color/blue/focusSoft        #6FA8FF
```

## Surface Tokens

```text
surface/screen              #040506
surface/base                #080604
surface/panel               #120B07
surface/panelRaised         #1A100A
surface/card                #1A100A
surface/cardRaised          #24160D
surface/inset               #050403 at 80-95% alpha
surface/overlay             #040506 at 65-80% alpha
surface/lockedOverlay       #040506 at 70-85% alpha

surface/trimDark            #2E1A10
surface/trim                #4A2A18
surface/trimActive          #D6A86A
surface/trimBright          #F0C58E
```

Usage:

* `surface/screen` is the full-screen background.
* `surface/panel` is the default large panel surface.
* `surface/panelRaised` is for important containers.
* `surface/card` is for unit cards, reward cards, route cards, shop cards.
* `surface/inset` is for text fields, stat boxes, icon backgrounds, dark wells.
* `surface/trimActive` is for selected/active frames.
* `surface/trimBright` is used sparingly for strong highlights.

## Text Tokens

```text
text/title                  #F0C58E
text/primary                #F4D3A5
text/secondary              #C79C70
text/muted                  #8B735E
text/faint                  #6F5A47
text/onLight                #040506

text/value                  #F4D3A5
text/valueImportant         #F0C58E
text/valueMuted             #C79C70

text/critical               #F07A6A
text/danger                 #B9554D
text/success                #79D96F
text/disabled               #8B735E at 55-70% alpha
text/cooldown               #FFFFFF
```

Usage:

* `text/title` is for screen titles and important section headers.
* `text/primary` is for important readable labels.
* `text/secondary` is for normal non-critical support text.
* `text/muted` is for metadata.
* `text/faint` is only for decorative or very low-priority text.
* Never use `text/faint` for gameplay-critical information.
* `text/valueImportant` is for numbers the player must notice.

## Interaction And State Tokens

```text
state/activeFrame           #D6A86A
state/activeGlow            #F0C58E at 25-45% alpha
state/activeFill            #2A170C

state/selectedFrame         #F0C58E
state/selectedFill          #24160D
state/selectedGlow          #F0C58E at 30-55% alpha

state/focusFrame            #4D90E8
state/focusGlow             #4D90E8 at 25-45% alpha

state/hoverFrame            #D6A86A
state/hoverFill             #24160D

state/pressedFill           #120B07
state/pressedFrame          #A45F2D

state/disabledFill          #120B07 at 60-75% alpha
state/disabledFrame         #4A2A18 at 45-60% alpha
state/disabledText          #8B735E at 55-70% alpha

state/lockedOverlay         #040506 at 70-85% alpha
state/lockedFrame           #4A2A18
state/lockedIcon            #C79C70

state/dangerFrame           #B9554D
state/dangerGlow            #F07A6A at 25-45% alpha
state/dangerFill            #5A2222

state/successFrame          #79D96F
state/successFill           #234A26
state/successGlow           #79D96F at 20-35% alpha
```

Rules:

* Selected gameplay objects use warm gold as the main selected state.
* Blue is reserved for keyboard/controller focus, targeting focus, or magical UI accents.
* Do not use blue as ordinary body text.
* Disabled and locked states must change more than color: use overlay, icon, frame, or opacity.
* Danger and success states need label/icon support. Do not rely on hue alone.

## Contrast Rules

Readability wins over visual style.

* Important gameplay text must use `text/primary`, `text/title`, or `text/valueImportant`.
* `text/secondary` is readable support text, not critical gameplay text.
* `text/muted` is metadata only.
* Do not place small text directly on `surface/trim`, `color/brown/copper`, or other medium-brown surfaces.
* If text must sit on a medium-brown surface, add `surface/inset` behind the text.
* Buttons must have enough contrast between label, fill, and frame.
* Red and green values need icons, signs, labels, or layout support.
* Selected, disabled, locked, cooldown, and unavailable states must have frame/overlay/shape changes in addition to color.
* Important text minimum size is `18`.

## Typography Tokens

Use TextMeshProUGUI for all new or polished UI text.

Do not introduce legacy Unity `Text`.

Text component rule:

* Every polished UI must use `TextMeshProUGUI` or `TMP_Text`, never `UnityEngine.UI.Text`.
* If an existing prefab still uses legacy `Text`, convert it during polish instead of leaving mixed text systems.
* If a script field, property, helper, or binding still expects `Text`, migrate that code to `TMP_Text` or `TextMeshProUGUI` as part of the same polish pass.
* Do not leave a polished prefab depending on legacy `Text` just because the old script type still compiles.

Font assets:

```text
font/body                  NotoSans_SemiCondensed-SemiBold SDF
font/numeric               NotoSans_SemiCondensed-SemiBold SDF
font/title                 NotoSans_SemiCondensed-SemiBold SDF
font/largeCounter          NotoSans-Bold SDF
```

Type scale:

```text
type/screenTitle           42-52
type/title                 32-40
type/sectionHeader         24-28
type/cardTitle             21-24
type/label                 18-20
type/body                  16-18
type/value                 22-28
type/largeCounter          36-48
type/button                20-24
type/tab                   17-20
type/tinyMeta              12-14
```

Minimums:

```text
important gameplay text    18
normal body text           16
tiny metadata              12-14 only for non-critical labels
button text                20
```

Text style rules:

* Use uppercase for major buttons and short headers.
* Avoid long all-caps paragraphs.
* Use numeric font for values, counters, army value, gold, damage, level, risk, cost.
* Do not copy extreme legacy font sizes such as `130` from `Arena_Footer`.

## Spacing System

Use an `8 px` spacing rhythm.

```text
space/xxs                  4
space/xs                   8
space/s                    12
space/m                    16
space/l                    24
space/xl                   32
space/xxl                  48
```

Rules:

* Small internal padding: `12-16`.
* Normal panel padding: `20-24`.
* Major screen margins: `32-48`.
* Large column gaps: `24-40`.
* Avoid random spacing values unless matching an existing sliced sprite.

## Component Scale Tokens

Buttons:

```text
button/smallIcon           40 x 40
button/icon                48 x 48
button/normal              180-220 x 52-60
button/primary             220-300 x 64-72
button/secondary           168-220 x 52-60
button/tab                 128-180 x 44-52
button/footerPrimary       220-280 x 60-72
button/footerSecondary     168-220 x 52-60
```

Button rules:

* Primary CTA should be visually stronger than secondary buttons.
* On flow screens, primary CTA usually sits bottom-right or inside the selected detail panel.
* Button labels use `type/button`.
* Primary button label uses `text/title` or `text/primary`.
* Disabled buttons keep readable labels but use dark overlay and muted frame.

Slots and icons:

```text
slot/skill                 72 x 72 total
slot/skillIcon             56 x 56
slot/skillSmall            60 x 60 total
slot/unitSmall             56 x 56
slot/unitNormal            72 x 72
slot/rewardIcon            64 x 64
slot/statIcon              32-40
portrait/small             48 x 48
portrait/normal            64 x 64
portrait/large             96 x 96
```

Rows, cards, and panels:

```text
row/compactHeight          64-72
row/normalHeight           80-96
row/largeHeight            104-128

card/rewardMin             220 x 300
card/shopMin               220 x 280
card/unitMin               240 x 96
card/routeNode             112-136 diameter

panel/insetPadding         12-16
panel/sectionPadding       20-24
panel/largePadding         28-32
panel/footerHeight         96-128
```

## Layout Hierarchy

Every screen should have one clear visual priority.

Recommended priority order:

1. screen title / current mode,
2. selected object or current decision,
3. primary action,
4. important resources,
5. secondary details,
6. flavor/meta text.

Do not give every panel the same visual strength.

Use:

* stronger trim for active/selected areas,
* darker trim for passive containers,
* larger type only for current decision and main values,
* muted text for helper explanations.

## Full-Screen Screen Structure

Recommended full-screen UI structure:

```text
[Top / Title Area]
- screen title
- current mode / short description
- optional resource summary

[Main Content Area]
- primary decision space
- map, army, shop, rewards, or battle data

[Side Inspector]
- selected item details
- risk/reward/cost
- action relevance

[Footer]
- back/cancel
- run/account resources
- primary continue action
```

Footer rules:

* Footer should not become a row of unrelated boxes.
* Group status values together.
* Keep primary CTA visually dominant.
* Keep BACK weaker than the primary action.

## Run Map Screen Rules

Run map screens should prioritize route readability and node selection.

Recommended hierarchy:

1. `RUN MAP` title,
2. selected route/node,
3. possible rewards/risk,
4. `TRAVEL` CTA,
5. army and run status.

Node states:

```text
node/available             normal bronze frame
node/selected              gold frame + subtle glow
node/current               warm active fill + current badge
node/completed             dim frame + checkmark
node/locked                dark overlay + lock
node/boss                  larger frame + danger/elite accent
```

Route line states:

```text
route/available            #7A421F at 80-100% alpha
route/selected             #F0C58E at 85-100% alpha
route/completed            #B88A55 at 55-75% alpha
route/locked               #4A2A18 at 35-50% alpha
```

Node content:

* Do not repeat technical labels like `Route Node` everywhere.
* Prefer gameplay labels: `Battle`, `Elite`, `Reward`, `Shop`, `Boss`, `Unknown`.
* Keep node text short.
* Use icon support for reward/risk when possible.

Right inspector should show:

```text
Node Name
Node Type
Risk
Possible Rewards
Short description
Primary action if relevant
```

## Army Panel Rules

Army panel should be readable at a glance.

Army row recommended structure:

```text
[48-56 portrait] [Unit Name]
                 [Unit count]
                 [Value / role / short stat]
```

Army row sizes:

```text
row height                 64-72
portrait                  48 x 48 or 56 x 56
unit name                  18-20
unit count                 14-16
unit value                 13-15
```

Rules:

* Portraits must not disappear into black.
* Add a dark inset behind portraits.
* Use `text/primary` for unit names.
* Use `text/secondary` for count.
* Use `text/valueMuted` or `text/valueImportant` for value depending on importance.
* Selected/hovered army row gets warm frame or raised fill.

## Reward Screen Rules

Reward screens should make the choice feel clear and satisfying.

Reward card hierarchy:

1. reward type,
2. reward name,
3. main value/effect,
4. condition/cost,
5. secondary description.

Reward cards:

```text
card width                 220-280
card height                300-380
icon                       64-96
title                      22-26
effect value               24-32
body                       16-18
```

Rules:

* Highlight affordable/available rewards.
* Locked rewards need overlay + label.
* Primary claim/confirm action must be obvious.
* Do not use long paragraphs inside reward cards.

## Shop Screen Rules

Shop screens should prioritize cost, affordability, and comparison.

Shop item card should show:

```text
Item Name
Icon
Effect
Cost
Availability
Buy Button
```

Rules:

* Cost must be highly readable.
* Affordable state uses success support.
* Unaffordable state uses disabled state, not only red text.
* Buy button should not overpower the screen-level exit/continue button unless purchase is the main flow.

## Battle HUD Rules

Battle HUD must prioritize gameplay values over decoration.

Priority:

1. current selected unit,
2. available skills,
3. target/action feedback,
4. turn order,
5. health/resources,
6. secondary info.

Rules:

* Skill icons must remain recognizable.
* Cooldown numbers must be large and readable.
* Disabled skills remain identifiable.
* Current active unit needs clear frame/glow.
* Avoid putting important battle text over busy 3D background without a dark panel.

## Menu Screen Rules

Main menu and meta menus may be more atmospheric than battle HUD, but must stay readable.

Rules:

* Title/logo gets the strongest presentation.
* Buttons use consistent primary/secondary states.
* Background should be darker and less detailed behind interactive elements.
* Do not use heavy ornament around every button.

## Brown Metallic Treatment

The metallic feel should come from restrained layering:

1. dark base surface,
2. slightly raised brown panel,
3. thin bronze trim,
4. subtle inner shadow,
5. limited gold highlight for active state.

Recommended layering:

```text
outer shadow               black at 40-60% alpha
outer trim                 #2E1A10
main frame                 #4A2A18
inner highlight            #7A421F or #A45F2D
active highlight           #D6A86A or #F0C58E
inset surface              #050403 at 80-95% alpha
```

Avoid:

* large shiny gradients,
* random orange glow everywhere,
* bright bevels on every panel,
* decorative spikes near small text,
* metal effects that reduce readability.

## State Presentation

Selected:

* gold frame,
* subtle glow,
* optional raised fill,
* never color-only.

Focused:

* blue frame or blue glow,
* used for keyboard/controller/navigation focus,
* not for normal body text.

Active:

* warm text or frame,
* clear priority over inactive controls.

Hover:

* slightly brighter frame,
* slightly raised fill,
* no layout shift.

Pressed:

* darker fill,
* reduced highlight,
* no text movement unless intentional.

Disabled:

* dark overlay or lowered alpha,
* readable label,
* muted frame,
* do not hide the label entirely.

Locked:

* dark overlay,
* lock icon or explicit badge,
* label remains readable when useful.

Cooldown:

* dark overlay,
* large numeric counter,
* icon still visible enough for recognition.

Danger / negative:

* red frame or value,
* add icon/sign/label when meaning matters.

Success / positive:

* green value or badge,
* add icon/sign/label when meaning matters.

## Polish Output Structure

Every polished UI output must be created in a dedicated sibling folder instead
of mixing polished files with source files.

Required structure:

```text
<ScreenFolder>/
  <SourcePrefab>.prefab
  Prefabs/
  Polished_1/
    <SourcePrefab>_Polished.prefab
    Prefabs/
      <NestedPrefabA>_Polished.prefab
      <NestedPrefabB>_Polished.prefab
```

Rules:

* Never write polished copies next to the source prefab at the root of the source screen folder.
* Create `Polished_1` for the first polished pass.
* If another isolated polished pass is needed later, create `Polished_2`, `Polished_3`, and so on.
* The polished main prefab lives inside the polished folder root.
* All polished nested prefabs live inside the polished folder's `Prefabs/` subfolder.
* The polished main prefab must reference only polished nested prefabs from the same polished folder set.
* Source prefabs and source nested prefabs remain untouched unless the user explicitly requests in-place edits.

## Polish Rules For Agents

When polishing UI:

1. Preserve script components, serialized fields, button callbacks, and nested prefab instances.
2. Prefer creating or updating a safe polished prefab copy in a dedicated `Polished_N` folder instead of editing the source prefab in place.
3. Replace default Unity placeholder visuals with approved project assets.
4. Prefer existing UI prefabs/components over hand-copying children.
5. Apply this file's semantic tokens before inventing new colors or sizes.
6. Validate text contrast, clipping, and overlap at supported aspect groups.
7. If a token does not fit a screen, document the exception instead of silently drifting the style.
8. Do not redesign gameplay flow unless explicitly requested.
9. Do not add new screens, currencies, stats, or mechanics while polishing visual identity.
10. UI polish should improve readability first, beauty second.
11. Migrate legacy `UnityEngine.UI.Text` to `TextMeshProUGUI` inside the polished prefab set.
12. If scripts in the polished screen still reference `Text`, migrate them to `TMP_Text` or `TextMeshProUGUI` in the same pass.

## Minimal Model

```text
UIVisualIdentity
- colors
- surfaces
- text
- fonts
- typeScale
- spacing
- componentScale
- statePresentation
- assetPriority
- responsiveTargets
```

Example:

```text
surface/screen        = #040506
surface/panel         = #120B07
surface/card          = #1A100A
surface/trim          = #4A2A18
surface/trimActive    = #D6A86A

text/title            = #F0C58E
text/primary          = #F4D3A5
text/secondary        = #C79C70
text/muted            = #8B735E

state/selectedFrame   = #F0C58E
state/focusFrame      = #4D90E8

font/body             = NotoSans_SemiCondensed-SemiBold SDF
slot/skill            = 72 x 72
button/primary        = 220-300 x 64-72
canvas/reference      = 1920 x 1280
```
