# 09 Current Skills

Status: active
Project: TArenaUnity3D
Last updated: 2026-06-13

## Purpose

This document captures the current TArenaUnity3D skill assignment and execution
model as verified in the local repository. It is code/context truth for agents
working on skill logic, skill UI, VFX/SFX presentation, or future skill data
cleanup.

## Current Skill Data Flow

Skills are currently assigned to units by string names in unit
`ScriptableObject` assets, one asset per unit, collected by the central
`UnitCatalog`.

At runtime, `TosterHexUnit.InitateType(name)` asks `DataMapper` for the matching
unit definition from the catalog, reads the unit stats, reads the skill list,
and stores those skill names in:

- `TosterHexUnit.skillstrings`

The skill strings are the runtime identity of the unit's skills. A skill string
must match the method names in `CastManager`.

The unit catalog stores the full legal skill list for a unit. Run state,
reward state, shop state, and future player progression may mark some of those
legal skills as locked or unlocked for a specific stack. Do not remove a skill
from the unit catalog just because it starts locked in a run.

## Skill UI Flow

`UICanvas` reads `MouseControler.SelectedToster.skillstrings` to populate skill
buttons.

Skill icons are loaded by string path:

- `Resources/Sprites/Skill_Icons/{SkillName}`

Skill descriptions and right-click info are loaded from:

- `TArenaUnity3D/Assets/Resources/Data/skills.xml`

This means the same skill name is expected to connect unit assignment, UI icon,
skill info, and skill execution.

## Skill Input And Targeting Flow

`MouseControler` owns the active skill slot and targeting flow.

Key runtime fields and methods:

- `MouseControler.SelectedSpellid` is the selected skill slot index.
- `MouseControler.CastSkillBooleans(int, TosterHexUnit)` starts skill targeting.
- `MouseControler.CastSkillOnlyBooleans(TosterHexUnit)` refreshes targeting for
  multi-step skills.
- `MouseControler.SpellCasting()` waits for the player to click a target hex.
- `MouseControler.startSpell(...)` commits the selected skill after target click.

When a skill slot is selected, `MouseControler` looks up:

- `SelectedToster.skillstrings[SelectedSpellid]`

Then it asks `CastManager` for the targeting mode.

## CastManager Reflection Model

`CastManager` contains most current skill logic.

Each active skill normally has two method names:

- `{SkillName}M` configures targeting mode.
- `{SkillName}` executes the skill.

Examples:

- `Fire_BallM()` configures range, AoE, cooldown, and turn usage.
- `Fire_Ball()` applies the actual skill behavior.
- `Stone_StanceM()` configures self-cast targeting.
- `Stone_Stance()` applies the self-cast behavior.

`CastManager.getMode(spellID, ST)` uses reflection to invoke:

- `spellID + "M"`

`CastManager.startSpell(spellID, hex)` uses reflection to invoke:

- `spellID`

If the skill string does not exactly match a `CastManager` method pair, the
skill path can break at runtime.

## Current Logic Locations

Main skill-related files:

- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/CastManager.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/MouseControler.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/SpellOverTime.cs`
- `TArenaUnity3D/Assets/UICanvas.cs`
- `TArenaUnity3D/Assets/RightClickInfoSkill.cs`
- `TArenaUnity3D/Assets/Resources/0_Data/UnitCatalog.asset`
- `TArenaUnity3D/Assets/Resources/0_Data/Units/*.asset`
- `TArenaUnity3D/Assets/Resources/Data/skills.xml`

## Current Presentation Coupling

Skill presentation is not currently a clean separate layer.

Current examples:

- Some skills directly play Animator states such as `Skill1`, `Skill2`, or
  `"Skill" + (SelectedSpellid + 1)`.
- Some skills directly instantiate projectile prefabs from
  `CastManager.Projectiles`.
- Some ranged paths store a projectile prefab on `TosterHexUnit.Projectile`.
- Some damage paths rely on `TosterHexUnit.DealMePURE`, `ShootME`, `DealMeDMG`,
  or `DealMeDMGDef` to trigger combat hit/death presentation.
- Some status/self-cast/passive skills have little or no visible presentation.

## Known Risk: Unit Catalog Assignment Vs Presentation Catalog

The planned skill VFX/SFX presentation model uses a central presentation catalog
keyed by the same skill string used in the unit catalog. This avoids putting skill
presentation ownership on unit models, but it still creates a data consistency
risk:

- the unit catalog decides which skills a unit has.
- the central presentation catalog decides which skill VFX/SFX entries exist.

If those two sources drift, a unit can have a playable skill with missing
presentation, or the catalog can contain entries for skills no unit currently
uses.

Agreed design direction for the first implementation:

- use the unit-catalog skill string as the stable skill id,
- store skill presentation entries in one central `ScriptableObject` catalog,
- each catalog entry has the skill name plus cast/projectile/impact VFX and
  SFX references,
- trap and other persistent board-state visuals may use the catalog entry's
  `spawnModel` field, while gameplay trap state remains owned by `HexClass` and
  `Traps`,
- use catalog `projectileVfx` with controlled code movement for projectile
  skills, replacing old `CastManager.Projectiles[index]`, `Axe(...)`, and
  `FireBall(...)` paths as those skills are migrated,
- treat missing presentation entries as silent no-ops,
- defer catalog/presentation validation tooling to a separate PRD/task.

Do not move skill assignment into presentation data. Presentation data should
remain additive and keyed by the same skill id used by the unit catalog.
