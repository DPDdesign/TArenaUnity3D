# 09 Current Skills

Status: active
Project: TArenaUnity3D
Last updated: 2026-06-25

## Purpose

This document captures the current TArenaUnity3D skill assignment, skill API,
and remaining legacy boundaries after PRD49ABC. It is code/context truth for
agents working on skill logic, skill UI, VFX/SFX presentation, Tactical AI,
future anti-cheat, or server-side validation.

For PRD49 work, also read:

- `_codex/tasks/049_PRD_TacticalActionSkillMigrationProgram.md`
- `_codex/tasks/archive/049ABC_PRD_SkillAPIAndFullMigration.md`
- `_codex/tasks/049ED_PRD_TacticalAIActionSelectionAndExecutionMigration.md`
- `_codex/tasks/049F_PRD_LegacySkillSystemCleanup.md`
- `_codex/Context/10_Skill_Design_Rules.md`
- `_codex/Documentation/ADR_015_SkillActionDefinitionOwnsSkillTextAndRules.md`

## Current State After PRD49ABC

PRD49ABC is closed as the skill API, ScriptableObject data, and validation
boundary foundation.

Current accepted state:

- `SkillDefinitionAsset` is the current skill definition asset type.
- `SkillDefinitionAsset.skillName` is the canonical skill id.
- All current active skill assets under `Assets/Resources/0_Data/Skills/` have
  migrated activation, targeting, resolution, and ordered effect data.
- `SkillRules`, `SkillUse`, `SkillCast`, `SkillTarget`, `SkillResult`,
  `SkillContext`, and `SkillQuery` exist as the shared skill API/data model.
- `MouseControler` uses `SkillRules` for skill start legality, target
  highlighting, and clicked-target validation.
- `DataMapper` loads skill metadata from `SkillCatalog` /
  `SkillDefinitionAsset`; the old XML fallback is disabled.
- `UICanvas` reads cooldown-fill max values from
  `SkillDefinitionAsset.ActivationRule` before old compatibility parsing.
- `Long_Lick` now uses the approved two-step contract: enemy target, then
  player-selected adjacent empty destination.
- `Stone_Throw` now requires an enemy unit target instead of an empty hex.

Current remaining legacy boundary:

- Live/default skill commit can still call legacy `MouseControler` /
  `CastManager.startSpell(...)` reflection bodies for actual mutation.
- Passive trigger mutation can still live in `TosterHexUnit`,
  `SpellOverTime`, `HexClass`, and related legacy hooks.
- PRD49ED must treat every current active asset-backed and unit-assigned skill
  as still in scope for runtime migration.
- PRD49F must remove old paths only after replacement, reference audit, tests,
  and manual Unity validation.

Do not report `CastManager` reflection execution or passive legacy hooks as the
desired/current architecture. They are current compatibility debt.

## Current Skill Identity And Ownership

Skills are assigned to units by string names in unit `ScriptableObject` assets,
one asset per unit, collected by the central `UnitCatalog`.

At runtime, `TosterHexUnit.InitateType(name)` asks `DataMapper` for the matching
unit definition from the catalog, reads the unit stats, reads the skill list,
and stores those skill names in:

- `TosterHexUnit.skillstrings`

The skill strings are the runtime identity of the unit's skills and the join key
for skill definitions, UI icons, presentation, and compatibility execution.

The unit catalog stores the full legal skill list for a unit. Run state,
reward state, shop state, and future player progression may mark some of those
legal skills as locked or unlocked for a specific stack. Do not remove a skill
from the unit catalog just because it starts locked in a run.

## Current Skill Definition Data Flow

Current skill definition truth is SO-backed:

- `Assets/Resources/0_Data/SkillCatalog.asset`
- `Assets/Resources/0_Data/Skills/*.asset`
- `TArenaUnity3D/Assets/Scripts/SkillDefinitionAsset.cs`
- `TArenaUnity3D/Assets/Scripts/SkillCatalog.cs`
- `TArenaUnity3D/Assets/Scripts/DataMapper.cs`

`SkillDefinitionAsset` currently owns:

- stable `skillName`,
- display/type/info compatibility data,
- `ActivationRuleData`,
- `TargetingRuleData`,
- `ResolutionRuleData`,
- ordered `SkillEffect[]`.

The old `skills.xml` file is not a current source of truth for active skill
text, flags, cooldowns, targeting, or effect data. If a historical task refers
to XML, treat it as legacy/migration history unless the current code proves a
runtime dependency.

## Current Skill API

Use the PRD49ABC API responsibilities as the current shared model:

- `SkillUse` is the untrusted request shape from player UI, future AI, or
  future server command. It contains actor id, skill id, and ordered selected
  hexes.
- `SkillContext` carries snapshot/action context needed by rules without
  relying on live scene selection state.
- `SkillRules.CanUse(...)` checks activation, cooldown, passive/stance/action
  state, and other start legality.
- `SkillRules.GetTargets(...)` returns legal next targets for the current
  partial selection.
- `SkillRules.Validate(...)` normalizes a complete `SkillUse` into a trusted
  `SkillCast`.
- `SkillRules.Preview(...)` and `SkillResult` provide result/event shape for
  preview, AI scoring, and future server comparison.
- `SkillRules.Apply(...)` / runtime adapter is the intended execution boundary,
  but full live execution migration is still PRD49ED work.
- `SkillQuery` is the thin query surface for future AI/server callers.

Future anti-cheat and server-side validation must treat `SkillUse` as
untrusted and must recompute legality/effects from snapshot plus
`SkillDefinitionAsset` data. Do not trust UI-selected affected units, damage,
spawn positions, or target resolution supplied by a client.

## Current Skill UI Flow

`UICanvas` reads `MouseControler.SelectedToster.skillstrings` to populate skill
buttons.

Skill icons are loaded through `DataMapper.LoadSkillIcon(...)`, currently using
the project skill icon Resources path.

Right-click skill info flows through `RightClickInfoSkill` and
`SkillInfoPresentation`, which read skill metadata through `DataMapper`.
`DataMapper.FindSkill(...)` now builds its compatibility `SkillDefinition`
from `SkillDefinitionAsset` data.

Cooldown display still reads runtime cooldown values from
`SelectedToster.cooldowns` in slot order. Cooldown-fill maximum uses
`SkillDefinitionAsset.ActivationRule.cooldownTurns` when available.

Skill button clicks still enter through:

- `MouseControler.CastSkill(slotIndex)`

New UI should call the existing `MouseControler` button entry unless the task is
explicitly part of PRD49ED/PRD49F skill execution migration.

## Current Input And Targeting Flow

`MouseControler` owns the active skill slot and targeting flow.

Key runtime fields and methods:

- `MouseControler.SelectedSpellid` is the selected skill slot index.
- `MouseControler.CastSkillBooleans(int, TosterHexUnit)` starts skill targeting.
- `MouseControler.CastSkillOnlyBooleans(TosterHexUnit)` refreshes targeting for
  multi-step skills.
- `MouseControler.SpellCasting()` waits for the player to click a target hex.
- `MouseControler.startSpell(...)` commits the selected skill after target click.

After PRD49ABC, target legality should route through `SkillRules` instead of
being recomputed by UI.

Current rule:

- UI may present and stage selections.
- `SkillRules.GetTargets(...)` owns legal next-target answers.
- `SkillRules.Validate(...)` owns final clicked-target acceptance/rejection.
- Live mutation is still a PRD49ED migration boundary until execution no longer
  depends on `CastManager`.

## Current Logic Locations

Main skill-related files:

- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/CastManager.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/MouseControler.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/SpellOverTime.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillRules.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillQuery.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillUse.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillCast.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillResult.cs`
- `TArenaUnity3D/Assets/UICanvas.cs`
- `TArenaUnity3D/Assets/RightClickInfoSkill.cs`
- `TArenaUnity3D/Assets/SkillInfoPresentation.cs`
- `TArenaUnity3D/Assets/Resources/0_Data/SkillCatalog.asset`
- `TArenaUnity3D/Assets/Resources/0_Data/Skills/*.asset`
- `TArenaUnity3D/Assets/Resources/0_Data/UnitCatalog.asset`
- `TArenaUnity3D/Assets/Resources/0_Data/Units/*.asset`

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

PRD053 adds planned hover skill indicators to the same presentation catalog.
These indicators are authored per skill entry and remain presentation-only. The
existing valid target highlight and `SkillRules` validation flow remain the
source of truth for whether a hovered hex is legal. Indicator catalog data must
not grant, remove, reorder, or validate skills.

## Active Skill Assignment Snapshot

Current unit skill assignments from the PRD49ABC audit:

| Unit | Skills |
| --- | --- |
| `Axeman` | `Slash`, `Hate`, `Cold_Blood` |
| `FireElemental` | `Fire_Movement`, `Fire_Ball`, `Fire_Skin` |
| `FleshGolem` | `Heavy_Fists`, `Terrifying_Presence`, `Rotting` |
| `Healer` | `Tough_Skin`, `Defence_Ritual` |
| `HeavyHitter` | `Insult`, `Rage`, `Massochism` |
| `Rusher` | `Chope`, `Rush` |
| `Specialist` | `Force_Pull`, `Stone_Stance` |
| `StoneGolem` | `Stone_Throw`, `Stone_Skin` |
| `Tank` | `Toxic_Fume`, `Shapeshift`, `Long_Lick` |
| `Thrower` | `Range_Stance_Barb`, `Double_Throw`, `Axe_Rain` |
| `Trapper` | `Range_Stance_Lizard`, `Spike_Trap`, `Rope_Trap` |
| `Wisp` | `Blind_by_light`, `Unstoppable_Light` |

## Legacy Reference Section

Use this section for diagnosis, not as the desired or current authoritative
architecture.

Before PRD49ABC, skills were described as split across:

- `skills.xml` text/type/flags,
- `CastManager` reflection methods,
- `MouseControler` targeting state,
- UI passive/cooldown parsing,
- ad hoc AI skill intent bridges.

Legacy `CastManager` convention:

- `{SkillName}M` configures targeting mode.
- `{SkillName}` executes the committed skill.

This convention can still matter while PRD49ED is unfinished because live
mutation may still call the reflection body. It must not be used for new
legality, targeting, AI planning, anti-cheat, or server-side validation work.

Legacy XML:

- may exist as migration/reference history,
- must not receive new gameplay flags,
- must not be restored as runtime fallback,
- should be removed by PRD49F after runtime references are gone.

Legacy-only skill methods with no current skill asset and no unit assignment
are reference-audit only unless a task proves a live demo reference.
