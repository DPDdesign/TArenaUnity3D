---
name: polish-ui-mockup
description: Improve a functional TArenaUnity3D Unity UI mockup into a more readable, polished, identity-consistent UGUI prefab. Use after make-ui-mockup or UI implementation when the prefab already works but needs better visual assets, sizes, spacing, composition, screenshot review, consistency with UI reference screenshots, and alignment with the TArena UI Visual Context.
---

# Polish UI Mockup

Use this after `make-ui-mockup` has produced a functional, wired UI prefab.
This skill improves visual quality without breaking functionality.

Default output mode: create or update a safe polished copy of the prefab rather
than editing the source prefab in place.

Do not use this skill to create an empty UI from scratch. If the mockup is not
functional, fix it with `make-ui-mockup` first.

## Project UI Text Rule

TArenaUnity3D uses TextMesh Pro only. Keep or replace text components with TMP
types such as `TMP_Text` and `TextMeshProUGUI`; do not introduce or preserve
legacy `UnityEngine.UI.Text`.

## UI Architecture Contract

Follow `_codex/Context/11_UI_Context.md` during polish.

- Preserve or migrate toward screen controllers/screen views that reference
  top-level view classes and global controls only.
- Complex panels/items should be view classes that own their internal fields.
- Repeated UI should remain `parent + prefab` or nested prefab instances under
  a configured layout parent.
- Do not polish by flattening nested prefabs, hand-copying repeated children,
  or moving child fields onto the screen controller.
- If a prefab uses a presentation catalog for type/state/category visuals,
  preserve that catalog wiring. Polish catalog entries or catalog-assigned
  sprites/colors instead of hardcoding visual choices in the controller.
- Start Run may remain a working legacy exception unless the task explicitly
  asks to refactor it.

## Project UI Frame And Image Rules

- Every text UI component must be a child of an otherwise empty GameObject
  named `Frame` that has an `Image` component.
- When adding or updating UI images for frames, panels, buttons, or similar
  sliced UI art, configure them to use `Image.Type = Sliced`.

## Required Context

Read these before editing:

1. `_codex/Context/CONTEXT-MAP.md`
2. `_codex/Context/11_UI_Context.md`
3. `_codex/Context/UI_Visual_Identity.md`
4. `_codex/Context/12_UI_Visual_Context.md`
5. `_codex/skills/make-ui-mockup/SKILL.md` when checking wiring rules

Use `_codex/Context/19_Identity.md` only when the screen's purpose is unclear or
the visual direction conflicts with TArena identity.

## Workflow

1. Inspect the target prefab and confirm it is functional:
   - script owner GameObjects exist,
   - scripts are attached,
   - required serialized fields are assigned,
   - buttons have callbacks,
   - repeated rows/cards/buttons use nested prefab instances where available.
2. Create or locate a safe polished prefab copy before visual edits:
   - prefer a sibling prefab copy or prefab variant,
   - use a clear suffix such as `_Polished`,
   - do not overwrite the source prefab unless the user explicitly asks for an
     in-place polish,
   - keep the polished copy in the same feature folder unless the task says
     otherwise.
3. Load reference screenshots from:
   `TArenaUnity3D/Assets/Resources/UI/References`.
   If the folder is missing or empty, report that reference screenshots are
   unavailable and continue with `UI_Visual_Identity.md` and
   `12_UI_Visual_Context.md`.
4. Load canonical font, color, contrast, component scale, asset priority, and
   responsiveness rules from `_codex/Context/UI_Visual_Identity.md`.
5. Load UI sizing/style rules from `_codex/Context/12_UI_Visual_Context.md`.
6. Replace plain/default visuals with project UI assets from:
   - `TArenaUnity3D/Assets/GUI_Parts`,
   - `TArenaUnity3D/Assets/Classic_RPG_GUI`,
   - `TArenaUnity3D/Assets/Old_Paper_Gui`,
   - `TArenaUnity3D/Assets/Resources/Skill_Icons`,
   - `TArenaUnity3D/Assets/Resources/UI/Unit_Icons`,
   - existing generated UI prefabs under `Resources/UI`.
7. Preserve functionality while polishing:
   - do not remove script components,
   - do not break serialized references,
   - do not replace nested prefab instances with copied children,
   - do not remove button callbacks,
   - do not rename serialized/public fields,
   - do not move repeated child fields onto the screen controller,
   - do not replace catalog-driven visuals with hardcoded controller logic,
   - do not leave legacy `UnityEngine.UI.Text` in a polished prefab set.
8. Migrate text stack to TextMesh Pro when needed:
   - use `TextMeshProUGUI` for prefab text components,
   - use `TMP_Text` or `TextMeshProUGUI` in scripts that bind those components,
   - if a script currently expects `Text`, migrate it in the same polish pass,
   - keep serialized field names stable where possible so prefab references survive the migration.
9. Adjust sizes, spacing, anchors, typography, icons, and section proportions
   using `UI_Visual_Identity.md` first, then `12_UI_Visual_Context.md` for
   screen-specific guidance.
   - If a text component does not already sit under a `Frame` parent with an
     `Image`, add that wrapper before polishing the text block.
   - When using UI images for frames, panels, buttons, or similar scalable
     surfaces, set the `Image` type to `Sliced`.
10. Run available prefab validation from `make-ui-mockup`.
11. Capture a Unity screenshot with:
   `py _codex/skills/polish-ui-mockup/scripts/capture_prefab_screenshot.py --prefab <Assets/...prefab>`.
   The helper loads the prefab in Unity, places it on a screenshot Canvas, and
   saves a PNG under `TArenaUnity3D/Assets/Resources/UI/PRD_<number>/Screenshots`.
   If Unity is unavailable or project compilation blocks `executeMethod`, report
   the blocker and request a manual screenshot after import.
12. Compare the screenshot against `UI/References`, `UI_Visual_Identity.md`,
    and the visual context.
13. Fix the top 3 readability/style issues only, then repeat screenshot review
    if Unity is available.

If Unity cannot be run by the agent, report that clearly and ask the user for a
screenshot after import. Do not claim visual QA was completed without a
screenshot.

## Visual Polish Rules

- Use the brown metallic tactical direction from `UI_Visual_Identity.md`.
- Use parchment/paper surfaces only as fallback or deliberate special-case
  screen treatment.
- Prefer existing `Classic_RPG_GUI` and `GUI_Parts` components before
  hand-built primitives.
- Make the main action and main decision obvious.
- Increase icon size before adding explanatory text.
- Use `TextMeshProUGUI`, never legacy `Text`.
- If the prefab or its bound scripts still use `Text`, migrate them to TMP as
  part of polish instead of shipping mixed text systems.
- Every text component must sit under a parent GameObject named `Frame` with an
  `Image` component, even when the text itself is the only child.
- Use `Image.Type = Sliced` for UI images that act as frames, panels, buttons,
  or other scalable container art.
- Keep text readable and unclipped at the target resolution.
- Use overlays for selected, disabled, locked, cooldown, owned, and purchased
  states.
- Keep repeated rows/cards/buttons visually identical except content and state.
- Prefer nested prefab variants or prefab overrides over hand-edited duplicate
  structures.
- Keep state overlays and local visual fields inside the repeated item view
  prefab that owns them.
- Prefer polishing a `_Polished` copy or variant over editing the source prefab
  in place.
- Keep spacing on an 8px rhythm unless matching an existing legacy component.
- Do not add decorative ornament where it reduces tactical readability.

## Screenshot Review

For every screenshot, check:

- primary action visible,
- section hierarchy clear,
- army/stack values readable,
- card or row comparison easy,
- icon scale matches the visual context,
- text has enough contrast,
- repeated elements align,
- selected/disabled states are visible,
- screen matches TArena identity: run-built armies, Heroes-like readability,
  quick reward/shop/route decisions.

Save screenshots as:

```text
TArenaUnity3D/Assets/Resources/UI/PRD_<number>/Screenshots/<prefab>_<pass>.png
```

## Final Report

Keep the final report short:

```text
Polish changes:
- <GameObject/prefab>: <asset/size/spacing/state change>

References used:
- <screenshot or asset folder>

Screenshot QA:
- Saved: <path or not run>
- Issue: <short issue> -> <fix>

Kept functional:
- Scripts: <yes/no>
- Button callbacks: <yes/no>
- Nested prefabs: <yes/no>
- Serialized refs: <yes/no>

Output prefab:
- Source: <path>
- Polished copy: <path>
```

If Unity screenshot QA was not run, write:

```text
Screenshot QA: not run, Unity screenshot needed.
```
