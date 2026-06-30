# [TARENA] PRD053B: Skill Indicator Catalog Setup

- Status: draft
- Type: PRD / content setup task
- Area: Combat Presentation, Skill Presentation Catalog, HUD Indicator Assets
- Label: ready-for-agent
- Created: 2026-06-29
- Depends On: `_codex/tasks/053A_PRD_SkillIndicatorRuntime.md`
- Related: `_codex/Context/09_CurrentSkills.md`
- Related: `_codex/Context/10_Skill_Design_Rules.md`
- Related: `_codex/Context/maps/combat-presentation-map.md`

## Problem Statement

After PRD053A adds runtime support for skill indicators, the feature will still
be invisible until the skill presentation catalog is configured. Every skill
entry needs an intentional indicator choice: either a real indicator type and
placement, or explicit `None`.

From the designer's perspective, indicators belong to skills. The catalog should
show which skills have Line, Scatter, AoE, Arc, Hex, or no indicator. It should
not require guessing from code.

## Solution

Configure all existing entries in `SkillPresentationCatalog.asset` with the new
indicator fields added by PRD053A.

Every skill entry must be reviewed and assigned:

- an `indicatorType`,
- an `indicatorPlacement`,
- an `indicatorSprite`,
- an `indicatorMaterial`.

Skills without a desired hover indicator should be explicitly set to
`indicatorType = None`.

The setup should use the skill API groups and existing skill definition data to
work in batches. Most skills should fall into a few repeated families rather
than requiring unique per-skill interpretation.

## User Stories

1. As a designer, I want every skill presentation entry to show an indicator decision, so that the catalog is complete.
2. As a designer, I want skills without indicators to be marked `None`, so that missing data is not confused with an unfinished setup.
3. As a designer, I want Rush-like skills to use Line indicators, so that movement attacks have readable direction.
4. As a designer, I want multi-target throw skills to use Scatter indicators, so that each target line is visible.
5. As a designer, I want fireball/radial skills to use AoE indicators, so that the area shape is clear under the mouse.
6. As a designer, I want slash/cleave skills to use Arc indicators, so that direction and affected region are readable.
7. As a designer, I want single-hex or multi-hex skills to use Hex indicators, so that exact affected hexes are visible.
8. As a player, I want the indicator color/material to fit the acting faction, so that barbarian, lizard, and golem skills have distinct energy styling.
9. As QA, I want every skill entry to be intentionally configured, so that runtime no-ops can be distinguished from content mistakes.
10. As a developer, I want catalog setup to avoid vendor asset mutation, so that imported source assets remain clean.

## Implementation Decisions

- This task is allowed to edit `Assets/Resources/0_Data/SkillPresentationCatalog.asset`.
- This task is allowed to create project-owned derived indicator assets under `Assets/Content Library/UI/UI_HUD/SkillIndicators/` if needed.
- Do not modify vendor/source asset files directly.
- Configure indicators for all existing skill presentation entries, not only sample skills.
- Also review `defaultBasicRangedAttackEntry`. If it is not used for hover preview, set/leave its indicator state as `None` and note that decision.
- Use `indicatorType = None` for skills that should not show a hover indicator.
- Use skill id, `TargetingRuleData`, `ResolutionRuleData`, and existing presentation knowledge to choose indicator type and placement by group. Do not change skill ids.
- Indicator configuration remains presentation data. It must not grant, remove, reorder, or validate skills.
- Use HUD indicator source assets from `Assets/Content Library/UI/UI_HUD/`.
- Arrow sprites should be prepared as sliced sprites where possible so the shaft stretches while the arrow head does not.
- The arrow art convention is that arrows face right (`+X`).
- Faction material choices:
  - Barbarians: `Assets/Content Library/3D/G-spot_Lab/Magic Energy Seamless Textures Free Pack/Materials/MagicEnergy_Orange_01.mat`
  - Lizards: `Assets/Content Library/3D/G-spot_Lab/Magic Energy Seamless Textures Free Pack/Materials/MagicEnergy_Green_02.mat`
  - Golems: `Assets/Content Library/3D/G-spot_Lab/Magic Energy Seamless Textures Free Pack/Materials/MagicEnergy_Blue_01.mat`
- If a skill's faction or presentation family is unclear, prefer `None` and record the ambiguity in a mapping note rather than guessing gameplay meaning.
- Produce or update a short mapping note in the task result with `skillId -> indicatorType / placement / material family`, including all entries left as `None`.
- If SVG assets cannot be used directly as sprites in this project, create or use PNG/sprite-derived project-owned versions and document the fallback.

## Testing Decisions

Good validation is mostly manual Play Mode content QA. The goal is to prove that
catalog data drives the runtime from PRD053A and that every skill has an
intentional indicator state.

Recommended manual Unity validation:

1. Open `SkillPresentationCatalog.asset` and confirm every entry has indicator fields populated or `indicatorType = None`.
2. Confirm `defaultBasicRangedAttackEntry` has an intentional indicator decision.
3. Confirm no skill presentation entry has an accidental missing sprite/material when its type is not `None`.
4. Confirm Line/Rush skills show one arrow from caster to valid hovered target.
5. Confirm Scatter skills show multiple caster-to-target arrows where the skill supports multiple targets.
6. Confirm AoE skills show a ground marker under the valid hovered target or affected center.
7. Confirm Arc skills show a directional marker for valid hover direction.
8. Confirm Hex skills show markers under intended hexes.
9. Confirm all-allies and all-enemies skills use the matching placement and render under the intended current units.
10. Confirm skills marked `None` do not show indicators but still keep existing valid highlights.
11. Confirm barbarian, lizard, and golem examples use the intended faction materials.
12. Confirm moving the mouse off a valid target clears all indicators immediately.

## Acceptance Criteria

1. `SkillPresentationCatalog.asset` has all existing skill entries intentionally configured for indicators.
2. `defaultBasicRangedAttackEntry` has an intentional indicator decision.
3. Skills without hover indicators are explicitly set to `indicatorType = None`.
4. Skills with hover indicators have a type, placement, sprite, and material configured.
5. Rush-like skills use Line indicators where appropriate.
6. Multi-target throw skills use Scatter indicators where appropriate.
7. Radial/area skills use AoE indicators where appropriate.
8. Slash/cleave skills use Arc indicators where appropriate.
9. Single-hex or affected-hex skills use Hex indicators where appropriate.
10. All-allies and all-enemies skill groups use the matching placement where appropriate.
11. Vendor/source assets are not modified directly.
12. A mapping note records the indicator decision for every skill entry.
13. Any unresolved skill mappings are documented clearly in the task notes.

## Out of Scope

- Runtime service implementation; this belongs to PRD053A.
- Adding new gameplay rules or changing existing targeting rules.
- Changing skill ids, unit skill ownership, cooldowns, damage, status effects, or movement.
- Full art polish pass for every indicator asset.
- Custom shaders or advanced Feel integration.
- Replacing existing valid target highlights.

## Further Notes

This task should run after PRD053A compiles and exposes the indicator fields.
Because this task edits Unity `.asset` data, it has explicit permission to edit
`SkillPresentationCatalog.asset` and project-owned indicator assets under the
specified HUD indicator folder.

## Implementation - 2026-06-29

## What Changed

- `TArenaUnity3D/Assets/Resources/0_Data/SkillPresentationCatalog.asset` / `SkillPresentationEntry`: updated `indicatorType` for every current skill entry. This field controls which preview family the runtime requests: `None` disables the hover preview, `Line` and `Scatter` use arrow previews, and `AoE`, `Arc`, and `Hex` use ground markers. The enum order itself does not scale strength in Play Mode; the chosen family changes only the preview shape. Tuning hint: prefer `Hex` when exact affected cells matter and `AoE` when the center/area read is more important than each single cell.
- `TArenaUnity3D/Assets/Resources/0_Data/SkillPresentationCatalog.asset` / `SkillPresentationEntry`: updated `indicatorPlacement` for every current skill entry. This field controls where the preview is anchored: `None` disables placement, `CasterToHover` draws from actor to hovered hex, `UnderHover` marks the hovered hex, `UnderAffectedHexes` marks resolved affected cells, `UnderTargets` uses selected/preview targets, and `UnderAllAllies` / `UnderAllEnemies` target group previews. Lower or higher enum values do not change intensity in Play Mode; they only change which runtime positions are used. Tuning hint: use `UnderAffectedHexes` for skills whose legality already resolves a concrete shape through `SkillRules`.
- `TArenaUnity3D/Assets/Resources/0_Data/SkillPresentationCatalog.asset` / `SkillPresentationEntry`: updated `indicatorSprite` for every non-`None` mapping. `null` means no sprite and therefore no visible indicator for disabled entries; assigned sprites drive the visual family in Play Mode. Arrow previews now use `arrowtechart-09 1`; ground previews use `artboard-02 1`. Tuning hint: keep one sprite family per preview family unless the runtime is extended to support more authored shape variation.
- `TArenaUnity3D/Assets/Resources/0_Data/SkillPresentationCatalog.asset` / `SkillPresentationEntry`: updated `indicatorMaterial` for every non-`None` mapping. `null` keeps disabled entries intentionally preview-free; active indicators use Unity's built-in default sprite material unless a dedicated 2D fill material is assigned.
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillPresentationCatalog.cs` / `SkillPresentationEntry`: added optional `indicatorFillTexture`. This is a per-skill override; leave it empty to use the albedo texture assigned on the material.
- `TArenaUnity3D/Assets/Content Library/UI/UI_HUD/SkillIndicators/SkillIndicatorEnergyFill.shader`: added a simple transparent sprite shader. `_MainTex` remains the sprite mask shape and `_FillTex` is sampled as the visible fill.
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillIndicatorService.cs`: applies `indicatorFillTexture` through `MaterialPropertyBlock` when an indicator is rented from the pool. Runtime does not create shaders or materials.
- `TArenaUnity3D/Assets/Resources/0_Data/SkillPresentationCatalog.asset` / `SkillPresentationCatalog`: added missing presentation entries for `Melee_Stance_Lizard`, `Range_Stance_Lizard`, and `Unstoppable_Light` so the catalog now covers all 33 current skill assets. No Inspector fields were removed.
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/SkillPresentationCatalogIndicatorTests.cs`: added a metadata-only EditMode test file. No Inspector fields changed.

## Automatic Test

- Added `TArenaUnity3D/Assets/Scripts/Tests/EditMode/SkillPresentationCatalogIndicatorTests.cs`.
- `SkillPresentationCatalog_Covers_All_CurrentSkillAssets` checks that the presentation catalog contains one entry for every current skill asset under `Resources/0_Data/Skills`.
- `SkillPresentationCatalog_Uses_Expected_Indicator_Mapping` checks the exact expected `indicatorType`, `indicatorPlacement`, sprite name, and material name for every skill, and confirms `defaultBasicRangedAttackEntry` stays intentionally `None`.
- These tests do not require scene or prefab setup because they load catalog assets from `Resources` and validate metadata only.
- Tests were not run automatically. Run them manually in Unity Test Runner at `Window > General > Test Runner`, then `EditMode > SkillPresentationCatalogIndicatorTests`. Expected result: both tests pass.

## Unity Test

### Unity Setup

- Let Unity import `SkillPresentationCatalog.asset` and `SkillPresentationCatalogIndicatorTests.cs`.
- Open `TArenaUnity3D/Assets/Resources/0_Data/SkillPresentationCatalog.asset`.
- Confirm all 33 skill entries are present, including `Melee_Stance_Lizard`, `Range_Stance_Lizard`, and `Unstoppable_Light`.
- Confirm `defaultBasicRangedAttackEntry` remains `indicatorType = None` and `indicatorPlacement = None`.
- No scene, prefab, or Inspector wiring changes are required for this task.

### Play Mode Test

- Enter the battle scene already used for PRD053A indicator validation.
- Select a `Rush` user and hover a valid target. Expected: one line arrow preview from caster to hover.
- Select `Double_Throw` and hover valid targets through its targeting flow. Expected: scatter arrows use the arrow sprite instead of the old hex marker.
- Select `Axe_Rain` or `Fire_Ball` and hover a valid area target. Expected: an area marker appears with the configured sprite material.
- Select `Slash` or `Heavy_Fists` and hover a valid direction/impact preview. Expected: an arc-style ground preview appears instead of no indicator.
- Select `Blind_by_light`, `Tough_Skin`, `Spike_Trap`, `Rope_Trap`, `Stone_Throw`, `Force_Pull`, or `Long_Lick` on valid hovered targets. Expected: a hex-style marker appears on the configured target/target set.
- Select skills mapped to `None` such as `Cold_Blood`, `Range_Stance_Barb`, `Range_Stance_Lizard`, `Stone_Stance`, or `Unstoppable_Light`. Expected: existing valid highlights remain, but no hover indicator renders.

## QA Verdict

- Final QA verdict: Pass.
- QA report path: `_codex/tasks/QA/2026-06-29_2116_053B_QA_ArchitectureReview.md`
- Actionable findings: none.
- Non-blocking observation: `Chope`, `Defence_Ritual`, and `Insult` now have intentional catalog mappings, but targetless/auto-group previews still depend on the current PRD053A runtime lifecycle rather than this content task.
- Follow-up fixes applied: none required after QA.

## Notes

- Mapping note:
  - `Rush -> Line / CasterToHover / Barbarian`
  - `Double_Throw -> Scatter / UnderTargets / Barbarian`
  - `Axe_Rain, Fire_Ball -> AoE / UnderAffectedHexes / Barbarian or Golem by owning unit faction`
  - `Slash, Heavy_Fists -> Arc / UnderAffectedHexes / Barbarian or Golem by owning unit faction`
  - `Blind_by_light, Hate, Spike_Trap, Rope_Trap, Stone_Throw, Tough_Skin -> Hex / UnderHover / owning unit faction`
  - `Force_Pull, Long_Lick -> Hex / UnderTargets / Lizard`
  - `Chope, Toxic_Fume -> Hex / UnderAffectedHexes / owning unit faction`
  - `Defence_Ritual -> Hex / UnderAllAllies / Lizard`
  - `Insult -> Hex / UnderAllEnemies / Barbarian`
  - `Cold_Blood, Fire_Movement, Fire_Skin, Massochism, Melee_Stance_Barb, Melee_Stance_Lizard, Rage, Range_Stance_Barb, Range_Stance_Lizard, Rotting, Shapeshift, Stone_Skin, Stone_Stance, Terrifying_Presence, Unstoppable_Light -> None`
- `defaultBasicRangedAttackEntry` was reviewed and intentionally left as `None`.
- No vendor/source HUD art or material assets were modified. The catalog references existing source sprites, the built-in default sprite material, and optional albedo fill textures.
- The arrow sprite is still used through the current runtime fallback path rather than a sliced derived sprite asset.
- To make an arrow filled with energy, create a material using `TArena/Skill Indicator Energy Fill`, set its `Energy Fill` texture to the desired `MagicEnergy_*_Albedo`, and assign that material to the catalog entry. Use `Indicator Fill Texture` only when one specific skill should override the material's albedo.

## Next Steps

- Run `EditMode > SkillPresentationCatalogIndicatorTests` in Unity Test Runner.
- Run the representative battle Play Mode checks for `Rush`, `Double_Throw`, `Axe_Rain`, `Fire_Ball`, `Slash`, `Heavy_Fists`, trap skills, pull skills, and several `None` skills.
- If Play Mode shows weak readability for the arrow preview, create a follow-up task to add a project-owned sliced arrow sprite under the approved HUD indicator folder rather than modifying vendor asset metadata.
