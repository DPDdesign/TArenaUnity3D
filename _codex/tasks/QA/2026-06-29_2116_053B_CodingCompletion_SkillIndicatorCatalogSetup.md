# [TARENA] Coding Completion - PRD053B Skill Indicator Catalog Setup

Task: `_codex/tasks/053B_PRD_SkillIndicatorCatalogSetup.md`

## Summary

Implemented the PRD053B skill indicator catalog setup:

- Configured indicator authoring for every current skill in `SkillPresentationCatalog.asset`.
- Corrected the existing `Double_Throw` preview from a hex marker/default material to a scatter arrow with barbarian material.
- Preserved `Rush` as the line-preview baseline and switched enabled indicators to Unity's built-in default sprite material by default.
- Added optional masked energy fill support: `indicatorFillTexture` as a per-skill override, a transparent sprite shader, and runtime `MaterialPropertyBlock` assignment for `_FillTex`.
- Added missing presentation entries for `Melee_Stance_Lizard`, `Range_Stance_Lizard`, and `Unstoppable_Light` so the catalog covers all current skill assets.
- Deferred direct use of the current 3D/PBR `MagicEnergy_*` materials because they do not render cleanly on 2D `SpriteRenderer` indicators. Energy styling now uses albedo textures through a dedicated 2D sprite shader/material path.
- Chose `None` intentionally for passive/stance/self-buff skills that should not show a hover indicator in this pass.
- Added a focused EditMode metadata test that checks full catalog coverage and the expected indicator mapping per skill.

## Changed Files

- `TArenaUnity3D/Assets/Resources/0_Data/SkillPresentationCatalog.asset`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/SkillPresentationCatalogIndicatorTests.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillPresentationCatalog.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillIndicatorService.cs`
- `TArenaUnity3D/Assets/Scripts/Editor/SkillPresentationEntryDrawer.cs`
- `TArenaUnity3D/Assets/Content Library/UI/UI_HUD/SkillIndicators/SkillIndicatorEnergyFill.shader`

## Implementation Notes

- `AoE` is used for `Axe_Rain` and `Fire_Ball` with `UnderAffectedHexes`.
- `Line` is used for `Rush` with `CasterToHover`.
- `Scatter` is used for `Double_Throw` with `UnderTargets`.
- `Arc` is used for `Slash` and `Heavy_Fists` with `UnderAffectedHexes`.
- `Hex` is used for exact-cell previews such as single-target spells, traps, pull skills, all-allies/all-enemies groups, and multi-hex post-move effects.
- `Chope`, `Defence_Ritual`, and `Insult` now have intentional non-`None` catalog mappings even though targetless/auto-group skills still depend on the existing PRD053A runtime path to render outside the normal hover flow.
- `defaultBasicRangedAttackEntry` remains intentionally `None`.
- No vendor/source asset files were modified. The catalog references existing HUD sprites and Unity's built-in default sprite material directly. Optional energy fill normally uses the albedo texture assigned on the material; `indicatorFillTexture` is only an override.
- The arrow sprite remains a non-sliced source asset in this task. Current runtime behavior therefore uses the existing fallback path rather than a derived sliced arrow asset.

## Verification

- Text-level validation confirmed `SkillPresentationCatalog.asset` now contains 33 skill entries, matching the 33 current skill assets under `Resources/0_Data/Skills/`.
- Text-level validation confirmed all non-`None` indicator entries now have a non-null sprite and material reference.
- Text-level validation confirmed `indicatorFillTexture` serialized for all 33 entries plus `defaultBasicRangedAttackEntry`.
- Text-level validation confirmed the missing skill ids `Melee_Stance_Lizard`, `Range_Stance_Lizard`, and `Unstoppable_Light` were added to the presentation catalog.
- Added `SkillPresentationCatalogIndicatorTests` to verify:
  - catalog coverage for every current skill asset,
  - the expected indicator type/placement per skill,
  - expected sprite/material names for enabled indicators,
  - intentional `None` on `defaultBasicRangedAttackEntry`.
- Unity EditMode tests were not run automatically because project rules require the user to run them manually in Unity.
- Unity was not run, so Play Mode verification is still required for actual hover rendering.

## Manual Unity Test Target

Run the focused new EditMode test plus adjacent indicator/runtime regressions in Unity Test Runner:

- `EditMode > SkillPresentationCatalogIndicatorTests`
- Adjacent runtime/manual follow-up:
  - PRD053A hover indicator behavior in battle Play Mode
