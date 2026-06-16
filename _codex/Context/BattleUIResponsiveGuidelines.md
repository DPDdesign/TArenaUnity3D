# Battle UI Responsive Guidelines

Status: active
Type: context
Area: battle UI, HUD responsiveness, resolution scaling
Audience: agents working on battle HUD layout, readability, or scaling

## Problem Statement

The current battle UI looks acceptable around a 16:10 aspect ratio, but it
breaks down as the viewport becomes more square and becomes too small at 4K
UHD. The turn queue spreads or overflows, the chat drifts into the character
footer/info area, and the whole HUD lacks a consistent resolution-relative
scale policy.

From the player's perspective, the UI should remain readable and stable across
the expected battle-screen aspect ratios. A wider or taller monitor should not
make important combat controls unreadable, should not cause the turn queue to
cover unrelated panels, and should not let chat overlap the selected-character
footer.

## Solution

Create a responsive battle HUD pass that treats layout and scale as one
coherent system:

- define a battle UI scale policy based on resolution and aspect ratio,
- keep 16:10 as a known good baseline while supporting the target wide layout,
- add explicit behavior for more square aspect ratios,
- make the turn queue fit its available parent width instead of relying on
fixed preview width assumptions,
- constrain chat so it never collides with the character footer/info area,
- preserve the existing gameplay bridge for active unit stats, skill buttons,
  cooldowns, portrait, turn order, and chat messages.

The work should first stabilize the existing legacy UI. It should not become a
full UI redesign unless a later task explicitly asks for new art direction,
new prefabs, or a replacement HUD.

## User Stories

1. As a player on a 16:10 display, I want the current good-looking battle UI to
   remain visually stable, so that the responsiveness pass does not regress the
   best current layout.
2. As a player on a 16:9 display, I want the battle HUD to use the available
   width cleanly, so that the target aspect ratio feels intentional rather than
   stretched.
3. As a player on a more square display, I want the turn queue to compress,
   wrap, clip, or reduce visible entries in a controlled way, so that it does
   not spill into unrelated UI.
4. As a player on a 4K UHD display, I want the HUD to scale up relative to the
   resolution, so that text, buttons, portraits, and queue items remain
   readable.
5. As a player using the battle turn queue, I want every visible queue entry to
   stay inside the queue container, so that upcoming turns are readable.
6. As a player reading battle chat, I want chat messages to stay inside their
   panel, so that chat does not cover the character footer.
7. As a player inspecting the active unit, I want the footer stats to remain
   visible and unblocked, so that HP, attack, defence, damage, movement,
   initiative, and name remain usable during combat.
8. As a player using skill buttons, I want skill icons, cooldown fills, and
   disabled states to retain their current meaning, so that responsiveness does
   not change combat rules.
9. As a player using action buttons, I want Wait, Defence, and other action
   buttons to remain readable and clickable after scaling, so that larger
   resolutions do not make them tiny and square layouts do not hide them.
10. As a player looking at the active unit portrait, I want the portrait to keep
    a stable size relationship to the rest of the footer, so that the HUD reads
    as one panel.
11. As a player receiving many chat messages, I want older messages to scroll,
    clip, or be constrained within chat behavior, so that chat growth does not
    push the footer away.
12. As a player changing window size, I want the HUD to adapt without
    incoherent overlap, so that windowed play remains usable.
13. As a developer, I want one place to reason about battle UI scale decisions,
    so that future UI work does not scatter resolution checks across gameplay
    code.
14. As a developer, I want turn queue layout to be driven by available layout
    space, so that future queue art or slot counts can change without hard-coded
    assumptions breaking aspect-ratio support.
15. As a developer, I want chat, queue, footer, portrait, stats, and skills to
    keep their existing runtime data sources, so that the task does not change
    gameplay state or skill execution.
16. As a QA reviewer, I want a fixed resolution/aspect matrix, so that visual
    regressions can be checked repeatably.
17. As a future UI designer, I want clear responsive rules for priority and
    fallback behavior, so that there is a known answer when there is not enough
    screen space.

## Implementation Decisions

- Keep this task focused on battle HUD responsiveness and readability. Do not
  change combat rules, skill ids, cooldown logic, unit stats, initiative rules,
  queue ordering, chat message content, or turn sequencing.
- Preserve the current legacy UI bridge: active unit state still comes from the
  current mouse/controller selection flow, skills still route through the
  existing skill button flow, and chat still receives existing battle messages.
- Treat 16:10 as the current visual baseline. Treat 16:9 as the likely target
  wide aspect. The user phrase "16.19" should be clarified before final visual
  sign-off if it was meant literally rather than as 16:9.
- Introduce a battle UI scale policy with explicit minimum and maximum scale
  bounds. The policy should account for resolution height/width and should make
  4K UHD visibly larger than the current tiny HUD.
- Prefer a small, testable scale/layout policy module over ad hoc scale math in
  each UI script.
- The turn queue should use the actual available parent/container width when
  deciding how many entries, separators, and future-round markers can be shown.
  Fixed preview sizes may remain as design inputs, but they should not be the
  only constraint.
- When there is not enough horizontal space, the queue should degrade
  intentionally: fewer future entries, reduced future-round preview, compact
  spacing, or a clear overflow affordance. It should not overlap chat, footer,
  or character info.
- The chat panel should have a stable reserved region or bounded layout area.
  It should not move under or over the footer character info when the aspect
  ratio becomes more square.
- Footer character info should be treated as a protected HUD region. Other UI
  surfaces must route around it or reduce their own footprint first.
- Existing public and serialized fields should not be renamed. New serialized
  fields are allowed only if implementation later explicitly needs them and the
  Inspector migration is documented.
- Scene, prefab, Canvas, material, `.inputactions`, `.asmdef`, and `.asmref`
  edits require explicit permission during implementation. If possible, begin
  with code-only layout policy and document any required Unity-side wiring.
- A later implementation should document the chosen reference resolution,
  target aspect assumptions, Canvas Scaler mode, match-width-or-height policy,
  and fallback behavior for more square aspect ratios.

## Testing Decisions

- Good tests should validate external UI behavior: no overlap between named HUD
  regions, readable scale ranges, queue containment, chat containment, and
  preserved interactability semantics. They should not test private layout math
  unless the math is extracted into a dedicated policy module.
- If a pure scale policy module is introduced, write EditMode tests for scale
  outputs at representative resolutions and aspect ratios.
- If queue fitting logic is extracted, write focused tests for visible queue
  capacity and overflow behavior using container widths instead of relying on a
  live Unity scene.
- Manual Unity visual QA is required because the current issue is visual layout.
  Use this minimum resolution matrix:
  - 2560x1600, 16:10 baseline,
  - 1920x1080, 16:9 target,
  - 3840x2160, 4K UHD scale/readability,
  - 2560x1440, common QHD 16:9,
  - 1920x1200, common 16:10,
  - 1600x1200 or 2048x1536, 4:3 square-ish stress case,
  - 1280x1024, 5:4 square-ish stress case,
  - 1366x768, low-height fallback.
- In each manual check, verify that the turn queue remains contained, chat does
  not overlap the footer, footer stats remain visible, skill/action buttons are
  clickable when rules allow them, and 4K does not make the HUD feel tiny.
- The user compiles and tests in Unity unless a later task explicitly allows a
  specific command-line Unity test run.

## Out of Scope

- Full battle HUD redesign.
- New UI art direction or new generated UI assets.
- Replacing the skill system, CastManager routing, turn order rules, or chat
  message sources.
- Changing gameplay float values, unit stats, damage, cooldowns, initiative, or
  movement values.
- Editing Unity scenes, prefabs, materials, controllers, generated Unity files,
  `.inputactions`, `.asmdef`, or `.asmref` without explicit implementation-time
  permission.
- Mobile/touch-specific UI redesign unless the target platform is clarified in
  a later task.
- Accessibility overhaul beyond readable scaling and non-overlap.

## Further Notes

- Current evidence points to the legacy battle UI being driven mainly by the UI
  canvas controller, the turn manager queue preview, and the chat component.
- The turn queue currently has fixed preview width concepts for unit entries and
  turn separators. This is a likely source of aspect-ratio brittleness and
  should be reviewed first during implementation.
- The footer stat list and portrait are already updated from the selected active
  unit. This task should protect that region rather than changing which unit is
  displayed.
- Chat currently instantiates text entries under its panel and caps message
  count. Responsiveness work should constrain the panel and its layout, not
  change combat message generation.
