# [TARENA] PRD053A: Skill Indicator Runtime

- Status: closed-core-implemented-followups-allowed
- Type: PRD / coding task
- Area: Combat Presentation, Skill Targeting Preview, Skill Presentation Catalog
- Label: ready-for-agent
- Created: 2026-06-29
- Related: `_codex/Context/09_CurrentSkills.md`
- Related: `_codex/Context/10_Skill_Design_Rules.md`
- Related: `_codex/Context/maps/combat-presentation-map.md`
- Related: `_codex/agents/docs/codebase/skills-effects-code-map.md`
- Follow-up: `_codex/tasks/053B_PRD_SkillIndicatorCatalogSetup.md`
- Explicit Asset Permission: may edit `TArenaUnity3D/Assets/Scenes/MainMenu_Scene.unity` only to add/configure `SkillIndicatorService` on a GameObject named `SkillIndicatorService`.

## Problem Statement

Players can currently see which hexes are valid targets through the existing
highlight system, but the battle UI does not show skill-shaped cast indicators
when hovering a valid target. Rush-like skills should feel like an arrow from
the caster to the hovered target. AoE skills should show a ground marker under
the hovered area. Scatter skills should show multiple arrows. Hex and arc skills
should show their intended shape without changing gameplay rules.

The desired indicator layer is presentation-only. It must not decide legality,
range, affected units, damage, cooldowns, movement, or turn state. Existing
target validation and valid target highlighting remain the source of truth.

## Solution

Add a runtime skill indicator layer driven by the existing
`SkillPresentationCatalog` and `MouseControler` hover state. Do not create a
second indicator catalog.

The runtime system adds four Inspector-visible fields to each
`SkillPresentationEntry`:

- `indicatorType`
- `indicatorPlacement`
- `indicatorSprite`
- `indicatorMaterial`

The runtime renders indicators in world space with pooled `GameObject` +
`SpriteRenderer` instances. It shows an indicator only when the mouse is over an
already-valid highlighted target. It hides indicators immediately when hover
leaves a valid target, skill targeting is cancelled, the selected skill changes,
or the skill is committed.

`SkillIndicatorService` should be added to the existing `MainMenu_Scene` on a
GameObject named `SkillIndicatorService`. The service should expose one global
shine/reflection material field in the Inspector. That material is configured on
the service, not per skill.

## User Stories

1. As a player, I want Rush-like skills to show an arrow from the caster to the hovered valid target, so that I can understand the movement line before clicking.
2. As a player, I want Scatter-like skills to show multiple arrows from the caster to selected/hovered targets, so that multi-target throws are readable.
3. As a player, I want AoE skills to show a marker under the hovered valid area, so that I understand where the area skill will land.
4. As a player, I want Hex indicator skills to show markers under relevant hexes, so that single-hex and multi-hex targeting is visually clear.
5. As a player, I want Arc skills to show a directional ground marker, so that cleave direction is understandable before commit.
6. As a player, I want indicators to disappear immediately when the mouse leaves a valid target, so that stale targeting information never remains on the board.
7. As a player, I want existing valid target highlights to keep working, so that I can still trust the current targeting feedback.
8. As a designer, I want indicator visuals configured per skill presentation entry, so that each skill can choose Line, Scatter, AoE, Arc, Hex, or None.
9. As a designer, I want only simple indicator fields in the Inspector, so that the skill presentation catalog remains maintainable.
10. As a developer, I want indicator rendering to be presentation-only, so that validator and gameplay rules remain the source of truth.
11. As a developer, I want missing indicator data to fail as a silent no-op, so that incomplete catalog entries do not break targeting.
12. As a developer, I want reusable pooled renderers, so that hover updates do not create avoidable allocations.
13. As QA, I want one runtime system to verify all indicator types, so that Line, Scatter, AoE, Arc, and Hex behavior can be checked consistently.

## Implementation Decisions

- Add `SkillIndicatorType` values: `None`, `Line`, `Scatter`, `AoE`, `Arc`, and `Hex`.
- Add `SkillIndicatorPlacement` values sufficient for v1, including `None`, `CasterToHover`, `UnderHover`, `UnderAffectedHexes`, `UnderTargets`, `UnderAllAllies`, and `UnderAllEnemies`.
- `UnderAllAllies` and `UnderAllEnemies` are required v1 placements. They support skills such as healer/all-allies and Axeman/all-enemies style previews. They must still use current validated/known battle state and must not decide gameplay legality.
- Extend the existing skill presentation entry model with only the four Inspector-visible indicator fields: type, placement, sprite, and material.
- Add a small public read API on the existing presentation path if needed, for example `SkillPresentationManager.TryGetEntry(skillId, out entry)` or an equivalent method, so `MouseControler`/indicator code can reuse the existing catalog lookup without duplicating catalog ownership.
- Keep non-authoring defaults as code constants inside the indicator service: ground offset, arrow width, draw duration, pulse amount, rotate speed, and shine behavior.
- Do not add per-skill height offset, rotation offset, width, animation settings, or shine material in v1.
- Treat sprite convention as `+X` facing right. Arrow assets are expected to be authored facing right, and code rotates from `+X` toward the target direction.
- Render with runtime `SpriteRenderer` objects. Use pooling and make `HideAll()` immediately disable/return all active indicator renderers.
- Line and Scatter use the same arrow renderer logic. Line renders one arrow; Scatter renders multiple arrows.
- Line and Scatter should prefer `SpriteRenderer.drawMode = Sliced` and set `SpriteRenderer.size` so arrow shafts can stretch without stretching the arrow head. If the sprite cannot support sliced rendering, fall back to normal transform scale.
- Line and Scatter animation should show a short grow-in and then a living shine/reflection. The shine material is one serialized global field on `SkillIndicatorService`, not a per-skill catalog field. If no suitable material is assigned, fallback to a subtle pulse.
- AoE indicators use code-side rotation and optional pulse.
- Hex indicators use code-side fade/scale-in and optional pulse.
- Arc indicators use code-side scale-in and direction alignment.
- `MouseControler` remains the lifecycle owner. It calls show only when the current hover is a valid target according to existing highlighting/validation flow, and calls `HideAll()` when hover, skill, selection, cancellation, or commit invalidates the preview.
- Existing valid target highlighting remains separate. Do not replace or mutate `hex.Highlight` behavior for this task.
- Runtime data for placement should come from already-known state: selected caster, hovered valid hex, selected target list, validated `SkillCast` when available, current units/teams when a placement explicitly means all allies or all enemies. Indicator code must not calculate whether those targets are legal.
- Do not add gameplay validation, target legality, damage, range, pathfinding, cooldown, or turn-state logic to indicator scripts.
- Missing manager, missing catalog entry, missing sprite, missing material, or `indicatorType = None` should be a safe no-op.
- More Mountains Feel can remain a future enhancement. Runtime must work without Feel.

## Testing Decisions

Good tests should verify externally visible behavior and lifecycle, not internal
pool implementation details.

Recommended automated coverage:

- EditMode tests for pure request-building or small helper methods if they can
  be isolated from `MouseControler`.
- Tests should confirm that invalid or missing indicator data returns no
  request/no render instead of throwing.
- Tests should confirm Line/Scatter geometry uses caster-to-target direction and
  distance from supplied positions.

Recommended manual Unity validation:

1. Enter Play Mode in the battle scene.
2. Confirm `MainMenu_Scene` contains a `SkillIndicatorService` GameObject with the service script.
3. Select a unit with a skill configured for a Line indicator after PRD053B.
4. Hover a valid highlighted target and confirm one arrow appears from caster to target.
5. Move the mouse off the valid target and confirm the arrow disappears immediately.
6. Change selected skill and confirm stale indicators disappear immediately.
7. Cancel targeting and confirm all indicators disappear immediately.
8. Commit a skill and confirm all hover indicators disappear immediately.
9. Repeat with Scatter, AoE, Arc, Hex, UnderAllAllies, and UnderAllEnemies after PRD053B configures catalog data.
10. Confirm existing valid target highlights still appear independently.
11. Confirm no gameplay legality changes: skills can and cannot target exactly the same hexes as before.

## Acceptance Criteria

1. `SkillPresentationEntry` exposes only `indicatorType`, `indicatorPlacement`, `indicatorSprite`, and `indicatorMaterial` for indicator authoring.
2. `MainMenu_Scene` contains a `SkillIndicatorService` GameObject with the `SkillIndicatorService` script and a serialized optional shine material field.
3. `MouseControler` shows indicators only while hovering an existing valid highlighted target.
4. `MouseControler` immediately hides indicators when hover leaves valid target, skill changes, targeting is cancelled, or skill is committed.
5. Line indicators render from caster to hovered valid target.
6. Scatter indicators render multiple caster-to-target arrows using the same arrow logic as Line.
7. AoE, Arc, Hex, UnderAllAllies, and UnderAllEnemies indicator requests render as ground markers without gameplay effects.
8. Existing hex valid-target highlighting remains separate and functional.
9. Indicator code adds no gameplay validation, pathfinding, affected-target calculation, damage, cooldown, or turn logic.
10. Missing indicator configuration is a silent no-op.

## Out of Scope

- Configuring every skill in `SkillPresentationCatalog.asset`; this belongs to PRD053B.
- Editing Unity scenes other than adding/configuring the `SkillIndicatorService` GameObject in `MainMenu_Scene`.
- Editing prefabs, Animator Controllers, `.asmdef`, `.asmref`, or input assets.
- Replacing the existing valid target highlight system.
- Changing skill balance, legality, cooldowns, movement, damage, or target rules.
- Full Feel integration.
- Creating a custom shader or splitting arrows into separate shaft/head objects.
- Server-side or AI usage of indicators.

## Further Notes

This task should be implemented before PRD053B. PRD053B depends on the runtime
fields and service existing before the catalog can be configured for every
skill.

The highest-risk integration point is `MouseControler`, because it already owns
selection, hover, targeting, and highlight refresh. Keep edits small and use the
existing valid-highlight state as the visible source of truth for whether an
indicator may show.

## Implementation - 2026-06-30

### What Changed

- Core runtime support for skill indicators is considered implemented and good
  enough to unblock follow-up work.
- `MouseControler` remains the lifecycle owner for hover-driven indicator
  visibility. That integration should stay in place for now.
- Remaining work is treated as follow-up iteration, polish, or edge-case
  extension rather than a blocker for using the current marker core.

### QA Verdict

- Core verdict: pass for the current runtime marker foundation.
- Non-blocking follow-up work may continue in later tasks without reopening the
  core task status unless the marker foundation itself regresses.

## Closure - 2026-06-30

Closed as core-complete.

Interpretation for downstream tasks:

- marker runtime/service exists as the accepted foundation,
- follow-up twists, polish, and further edits may still happen,
- later preview/UI tasks should treat this task as closed enough to depend on.
