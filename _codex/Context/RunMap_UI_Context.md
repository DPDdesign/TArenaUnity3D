# RunMap UI Context

Status: active
Project: TArenaUnity3D
Last updated: 2026-06-18

## Purpose

Use this context whenever designing, reviewing, prototyping, or implementing the
PRD019 Run Map UI.

The Run Map should feel like a connected expedition route, not a debug grid of
buttons. The player must immediately understand:

- where the army is now,
- which nodes can be chosen next,
- how nodes are connected,
- what kind of node each choice represents,
- that the bottom army bar belongs to the army travelling on this map.

This document is visual and UX guidance. It does not override the PRD019/PRD030
data-flow rules: Run Map UI still reads persisted run state through
`OfflineRunContextDbReader`, `OfflineRunMapAdapter`, services, and DTOs. UI
controllers must not query SQLite directly or own route progression rules.

## Core Rule

Build a connected route map with clear paths, current position, and symbolic
node choices. Do not present the screen as a flat list or grid of node buttons.

The primary reference quality is Mewgenics-like map readability:

- the map has a background with place context,
- paths guide the eye,
- nodes are large and symbolic,
- the current choice is visually dominant,
- the UI has small organic movement and strong readable outlines.

## Map Background

Do not leave the Run Map on a pure black or near-empty background.

Preferred background direction:

- dark parchment expedition map,
- ruined arena district,
- city/road/outer wall route,
- desaturated metal/stone/brown-grey palette,
- low detail behind the route,
- medium contrast, never black under dark-brown nodes.

The goal is that the screen reads as a journey through a location, not a debug
menu. Background art must not compete with node icons, path lines, current
position, or army information.

## Route Connections

Connections between nodes are mandatory for production Run Map UI.

Every node should show lines, dotted paths, or route strokes to its valid next
nodes. The player should understand the flow of the run without reading labels.

Connection states:

- available path: bright enough to scan quickly,
- locked path: dim and low contrast,
- completed path: marked differently, for example gold, stamped, or softly lit,
- current suggested direction: subtle pulse or glow.

Without visible connections, the Run Map does not communicate route decisions.

## Node Content

Nodes should communicate primarily through icon and state, not text.

On the node itself:

- large node-type icon,
- strong state frame or overlay,
- optional tiny status badge only if needed,
- no debug node id,
- no duplicated labels such as `LOCKED`, type, status, and internal id all at
  once.

Detailed node name, type, reward preview, danger, and description should live in
a hover/focus/click detail panel or tooltip.

Recommended node type symbols:

- Battle: crossed swords or skull,
- Elite: larger skull, helmet, or champion mark,
- Shop: coin bag or coins,
- Random Event: question mark,
- Final Battle: boss skull, crown, gate, or arena gate,
- Rest/Shrine: candle, shrine, or small altar.

Text must not be the main information carrier.

## Node Sizes

Readability is more important than fitting many small nodes on screen.

Recommended visual and hitbox sizes:

- normal node: `110-130 px`,
- currently available node: `130-150 px`,
- boss/final node: around `160 px`,
- minimum clickable area: `120 x 120 px`.

The clickable area may be larger than the visible art. This is preferred for
mouse, touch-style UI, and controller focus.

## Node States

The Run Map needs four visibly different states.

Current / Here:

- highest contrast,
- army marker, banner, ring, or glow,
- light idle animation,
- unambiguous "you are here" read.

Available:

- bright frame,
- readable icon,
- subtle pulse,
- hover scale,
- clear focus ring for keyboard/controller navigation.

Locked:

- visually quiet,
- opacity around `35-45%`,
- lock overlay or equivalent shape cue,
- no animation,
- much less attention than available nodes.

Completed:

- checkmark, stamp, or completed overlay,
- icon and frame slightly dimmed,
- completed path behind it softly highlighted.

Do not rely on color alone. Use frame, icon overlay, animation, and/or shape
language in addition to color.

## Party Marker

The army must exist on the map, not only in the bottom army bar.

Add a party marker on or near the current node:

- small army pawn,
- banner,
- compact unit portraits,
- flag with army name,
- or a stack-themed marker that matches TArena's army identity.

This marker gives the feeling that "my army is travelling through the run." It
should be tied visually to the current node and may idle subtly.

## Header And Location

The top of the Run Map should name the current route/location.

Examples:

- `BORDER CAMPAIGN`,
- `Route: Outer Guard`,
- `THE OUTER WALL`,
- `Run 1 / Day 2`.

The exact fiction can change, but the player should know where this route is in
the run. Keep the header readable and compact; it should support the map rather
than become a large landing-page title.

## Army Bar Relationship

The bottom army bar is a good element and should stay, but it must feel linked
to the route map.

Rules:

- label it clearly, for example `YOUR ARMY`,
- keep portraits and stack counts high contrast,
- show only important numbers,
- provide hover/focus detail for a unit instead of crowding the bar,
- consider a thin banner, trail, or visual accent from the army bar toward the
  current node.

The army bar should read as "the party travelling on this map", not as a
separate detached HUD.

## Accessibility And Readability

Minimum rules:

- do not use color as the only state difference,
- normal text should be at least `18-22 px` where it must be read during play,
- node names should be `20-24 px` only in detail panels or focused states,
- node type labels should be `16-18 px` if used,
- remove debug ids from production node display,
- available nodes must be brighter than the background and locked nodes,
- hitboxes should be at least `120 x 120 px`,
- hover scale should be about `1.08-1.15`,
- keyboard/controller focus must have a strong visible frame.

No important text should overlap node art, route lines, or the army bar.

## Feel And Motion

Use small animations only where they improve decision clarity.

Available node:

- subtle frame pulse,
- soft glow,
- hover scale,
- short paper/metal click sound.

Locked node:

- no idle animation,
- dull click or light shake only when the player tries to select it.

Completed node:

- checkmark or stamp feedback,
- dimmed icon,
- softly highlighted completed path.

Current node:

- army marker idles or breathes,
- ring/glow stays visible,
- small banner movement is acceptable.

Avoid decorative motion that competes with the current choice.

## Production Order

When improving Run Map UI, prioritize in this order:

1. Readability pass:
   background, route connections, larger nodes, no debug text, four node states.
2. Feeling pass:
   hover scale, available-node pulse, animated party marker, click/locked SFX,
   detail tooltip or detail panel.
3. Polish pass:
   location title, stronger icon set, subtle parallax/background life, light
   particles/fog, transition animation after choosing a node.

The first shippable improvement is not more decoration. It is making the nodes
read as a connected path map.

## Data And Architecture Boundaries

Run Map UI polish must respect these boundaries:

- use TextMesh Pro types for all text,
- keep screen flow in the screen controller,
- keep node visuals in route-node view classes or prefabs,
- use a presentation catalog for node icons, frames, colors, and state overlays,
- keep route progress, current node, available nodes, and army state in
  adapter/service DTOs,
- do not hardcode DB ids, sample run ids, or mock node ids in production UI,
- do not change gameplay float values, route rules, reward rules, or persisted
  state while doing visual polish,
- do not edit Unity assets, prefabs, scenes, or materials unless a task
  explicitly permits it.

## Related Context

- `_codex/Context/11_UI_Context.md`
- `_codex/Context/12_UI_Visual_Context.md`
- `_codex/agents/docs/PRD019_PRD030_RunMetagame_Code_Map.md`
- `_codex/Documentation/PRD030_OfflineDatabase_Map.md`
