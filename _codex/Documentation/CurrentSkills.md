# TArenaUnity3D Current Skills

Status: active
Project: TArenaUnity3D
Last updated: 2026-06-25

## Current Runtime Model

TArenaUnity3D currently identifies skills through stable string ids, with
SO-backed skill definitions added by PRD49ABC.

The source of unit skill ownership is:

- `TArenaUnity3D/Assets/Resources/0_Data/UnitCatalog.asset`
- `TArenaUnity3D/Assets/Resources/0_Data/Units/*.asset`

`TosterHexUnit.InitateType(name)` loads the unit entry through `DataMapper`,
reads the skill list, and stores it in:

- `TosterHexUnit.skillstrings`

The same strings drive UI, targeting, presentation lookup, and compatibility
execution.

The source of current skill definition data is:

- `TArenaUnity3D/Assets/Resources/0_Data/SkillCatalog.asset`
- `TArenaUnity3D/Assets/Resources/0_Data/Skills/*.asset`
- `SkillDefinitionAsset.skillName`

`SkillDefinitionAsset` now owns activation, targeting, resolution, and ordered
effect data for the active skill set.

## Skill API After PRD49ABC

The current shared skill API is:

- `SkillUse` - untrusted submitted skill request.
- `SkillContext` - snapshot/action context.
- `SkillRules.CanUse(...)` - start legality.
- `SkillRules.GetTargets(...)` - legal next-target generation.
- `SkillRules.Validate(...)` - normalize a complete use into `SkillCast`.
- `SkillRules.Preview(...)` / `SkillResult` - preview/result event shape.
- `SkillQuery` - thin query API for future AI/server callers.

This is the API future Tactical AI, anti-cheat, and server-side validation
should analyze first.

## UI And Info

`UICanvas` reads the selected unit's `skillstrings` and loads skill button
icons through `DataMapper.LoadSkillIcon(...)`.

Right-click skill info flows through `RightClickInfoSkill`,
`SkillInfoPresentation`, and `DataMapper.FindSkill(...)`. `DataMapper` now
builds that compatibility skill info from `SkillDefinitionAsset` data.

Cooldown values still come from runtime `SelectedToster.cooldowns`. Cooldown
fill maximum reads `SkillDefinitionAsset.ActivationRule.cooldownTurns` first.

## Current Legacy Boundary

PRD49ABC did not fully replace live/default skill execution.

Current compatibility debt:

- live skill commits can still call `MouseControler` / `CastManager`
  reflection bodies for mutation,
- passive trigger mutation can still live in `TosterHexUnit`,
  `SpellOverTime`, `HexClass`, and related hooks.

Do not treat those legacy paths as the current architecture for new work. They
are migration targets for PRD49ED and PRD49F.

Legacy diagnostic convention:

- `{SkillName}M` configured targeting mode.
- `{SkillName}` executed the committed skill.

The old `skills.xml` file is not a current source of truth for active skill
text, flags, cooldowns, targeting, or effect data. Treat it as legacy/migration
history unless current code inspection proves a remaining runtime dependency.

## Example

```text
UnitCatalog skill string: Fire_Ball
SkillDefinitionAsset.skillName: Fire_Ball
Icon path: Resources/Sprites/Skill_Icons/Fire_Ball
Presentation catalog key: Fire_Ball
Current validation path: SkillRules.GetTargets / Validate
Legacy compatibility body, if still reached: CastManager.Fire_Ball()
```

## Presentation Catalog

Skill VFX/SFX presentation should be authored in a central `ScriptableObject`
catalog. Each entry uses the exact skill id from the unit catalog and skill
definition asset.

Example:

```text
SkillPresentationCatalog.asset
- Fire_Ball
  - cast VFX + cast SFX
  - projectile VFX + projectile SFX
  - impact VFX + impact SFX
```

Missing presentation should be allowed and silent. The presentation catalog must
not decide which skills a unit owns.
