# 009 PRD Skill Impact Routine Flow Coding Agent Completion

## Task

`_codex/tasks/009_PRD_SkillImpactRoutineFlow.md`

## Files Changed

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/FrontendResultReveal.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillPresentationManager.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/CastManager.cs`

## Systems Touched

- Shared combat frontend hit/death reveal.
- Attack and counterattack hit timing.
- Basic ranged attack projectile impact reveal.
- Sequenced skill presentation for `Fire_Ball`.
- Sequenced instant AoE reveal for `Heavy_Fists`.
- Skill projectile SFX lifetime handling.

## Behavior Or Setup Summary

Added `FrontendResultReveal` as a small transport-agnostic result snapshot for
already-resolved combat results. It captures source action type, source unit,
target unit, target view, damage, and survival result so frontend hit/death
feedback can play later without asking board occupancy for a killed unit.

Extended `TosterHexUnit` with shared frontend reveal helpers. Migrated attack
and counterattack timing now apply damage at the existing attack hit moment,
then call the shared reveal routine for hit/death animation and SFX. Immediate
legacy attack paths also route hit/death feedback through the same helper.

Changed basic ranged attacks so `ShootME(..., true)` applies damage immediately,
then lets `SkillPresentationManager.PlayBasicRangedAttack(...)` reveal the
already-resolved result after catalog projectile impact. Non-migrated
`ShootME(..., false)` callers keep their old immediate behavior.

Extended `SkillPresentationManager` with sequenced projectile and instant hit
flows. These flows optionally wait for the caster skill animation, play cast
presentation, run projectile travel or instant impact timing, play impact
VFX/SFX, and then dispatch per-target frontend reveal. Missing manager/catalog
entries still fall back to hit/death reveal so VFX/SFX setup gaps do not hide
combat results.

Updated projectile SFX playback from one-shot to a temporary looping
`AudioSource` that starts with projectile travel and stops when the projectile
arrives or no projectile visual exists.

Migrated `Fire_Ball` to apply damage immediately through the new result
snapshot path, then play cast/projectile/impact/hit reveal through
`PlaySequencedProjectileHits`. Its old immediate `PresentImpactOnHitTargets`
path and manual skill animation call were removed from that skill body.

Migrated `Heavy_Fists` to apply its representative instant AoE damage
immediately through `DealMeDMGForFrontendReveal`, then play impact and
per-target hit/death reveal through `PlaySequencedInstantHits`. The previous
local damage reveal through legacy `JustDmg` was removed for this skill slice
to avoid double frontend feedback.

## Unity Checks

Not run. Project rules say the user compiles and tests inside Unity unless a
specific Unity test command is allowed.

Recommended manual checks in Unity:

- Ensure the battle scene has one `SkillPresentationManager` with an
  `AudioSource` and an assigned `SkillPresentationCatalog`.
- Configure catalog entries for `Fire_Ball`, `Heavy_Fists`, and
  `defaultBasicRangedAttackEntry`.
- In Play Mode, perform one melee attack and one counterattack and confirm
  hit/death animation and SFX occur at the attack hit moment.
- In Play Mode, perform one basic ranged attack and confirm projectile travel,
  impact VFX/SFX, then target hit/death reveal occur in that order.
- In Play Mode, cast `Fire_Ball` on one and multiple targets and confirm damage
  resolves immediately while impact and hit/death reveal wait until projectile
  arrival.
- In Play Mode, cast `Fire_Ball` on an empty area and confirm no per-unit
  hit/death reveal plays.
- In Play Mode, cast `Heavy_Fists` on multiple affected units and confirm one
  impact SFX cluster plus per-target hit/death reveal without double hit/death
  feedback.
- Test missing `Fire_Ball`, `Heavy_Fists`, or basic ranged catalog entries and
  confirm hit/death reveal still occurs without VFX/SFX crashes.

## Intentionally Not Included

- No Unity scene, prefab, material, controller, `.inputactions`, `.asmdef`, or
  `.asmref` edits.
- No generated `.meta` file edits.
- No final VFX/SFX assets or catalog asset creation.
- No XML skill id, cooldown, targeting, damage, movement, or balance value
  changes.
- No broad skill-system rewrite and no migration of `Double_Throw` or
  `Axe_Rain`.
- No Git, `dotnet`, Unity build, package restore, or SDK installation commands.
