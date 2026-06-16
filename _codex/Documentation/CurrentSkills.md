# TArenaUnity3D Current Skills

Status: active
Project: TArenaUnity3D
Last updated: 2026-06-11

## Current Runtime Model

TArenaUnity3D currently assigns and executes skills through string names.

The source of unit skill assignment is:

- `TArenaUnity3D/Assets/Resources/Data/Units.xml`

`TosterHexUnit.InitateType(name)` loads the unit entry from
`Resources.Load("data/Units")`, reads the skill list, and stores it in:

- `TosterHexUnit.skillstrings`

The same strings drive UI, targeting, and execution.

## UI And Info

`UICanvas` reads the selected unit's `skillstrings` and loads skill button icons
from:

- `Resources/Sprites/Skill_Icons/{SkillName}`

Skill text/right-click info is loaded from:

- `TArenaUnity3D/Assets/Resources/Data/skills.xml`

## Execution Flow

`MouseControler` owns the active skill slot:

- `SelectedSpellid`
- `CastSkillBooleans(...)`
- `SpellCasting()`
- `startSpell(...)`

`CastManager` owns most skill logic. It uses reflection:

- `{SkillName}M` sets targeting mode and availability.
- `{SkillName}` executes the skill after target selection.

Example:

```text
Units.xml skill string: Fire_Ball
CastManager targeting method: Fire_BallM()
CastManager execution method: Fire_Ball()
Icon path: Resources/Sprites/Skill_Icons/Fire_Ball
Info entry: skills.xml / Fire_Ball
```

## Presentation Catalog

Skill VFX/SFX presentation should be authored in a central `ScriptableObject`
catalog. Each entry uses the exact skill name from XML and stores the VFX/SFX
references for that skill.

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
