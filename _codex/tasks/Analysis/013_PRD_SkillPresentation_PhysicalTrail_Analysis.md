# [TARENA] Analysis: Skill Presentation For Physical Weapon Trails

- Source PRDs: `_codex/tasks/archive/003_PRD_SkillVfxSfxFlow.md`, `_codex/tasks/010_PRD_MigrateRemainingSkillImpactRoutines.md`, `_codex/tasks/013_PRD_MigrateRemainingPassiveDeferredSkillPresentation.md`
- Area: Skills, VFX/SFX presentation, weapon trails
- Status: analysis-only

## Key Concern

Some skills are physical weapon actions. They should not be forced into a
spawned world VFX model. For these skills, the readable presentation may be a
short sword/weapon trail, weapon glow, slash smear, or attached hit accent
instead of a standalone VFX prefab spawned at caster, target, or hex.

The important architectural point is that this should still go through the same
skill presentation layer as normal VFX/SFX. The layer should remain
`SkillPresentationManager` + `SkillPresentationCatalog`; only the authored
presentation operation should vary per phase.

## Current Fit

The existing PRD direction is correct:

- skill presentation is keyed by existing skill ids,
- missing presentation is a silent no-op,
- active and runtime-triggered skills should use the same presentation route,
- SFX is already part of the same presentation layer.

The gap is that the current model names the visual fields as `castVfx`,
`projectileVfx`, and `impactVfx`, and the current manager mainly instantiates
prefabs at resolved world positions. That fits magic, projectiles, AoE impacts,
auras, traps, and hex effects. It does not fully describe weapon-attached
presentation, because a sword trail needs an attachment target and a duration
over the attack animation, not only a position.

## Recommended PRD Adjustment

Keep the same three high-level timing phases:

- `Cast`
- `Travel` / optional `Projectile`
- `Impact`

But generalize the visual operation inside a phase:

- `None`: no visual, SFX may still play.
- `SpawnAtAnchor`: current cast/impact VFX behavior.
- `ProjectileToAnchor`: current projectile behavior.
- `AttachToCaster`: temporary VFX attached to caster root or named child.
- `AttachToWeapon`: temporary trail/glow attached to a weapon/bone/socket.
- `ToggleTrail`: enable an existing `TrailRenderer` or similar component during
  a timed window.

This keeps "Sword Trail" as a presentation choice, not a separate skill system.

## Product Rule

Physical skills should be allowed to be SFX-only or trail-only. They should not
need fake impact VFX just to satisfy the catalog. Examples:

- `Slash`: likely `AttachToWeapon` or `ToggleTrail` during the slash animation,
  optional impact SFX/reveal on target.
- `Rush`: possible cast/approach SFX plus weapon/body trail during movement,
  impact feedback only if a unit is hit.
- melee stance skills: caster/weapon glow or SFX-only, no projectile.
- passive movement effects such as `Fire_Movement`: hex trail/VFX at the real
  movement trigger, optionally no caster animation.

## Acceptance Criteria To Add

- A skill catalog entry may define no world VFX and still be valid if it plays
  SFX, an attached weapon trail, or no presentation.
- Physical skills are not forced into `projectileVfx` unless an object truly
  travels from caster to target.
- Weapon trails are triggered from the same skill presentation API and catalog
  lookup as other VFX/SFX.
- Weapon trails have deterministic cleanup: duration, animation window, or
  explicit disable.
- Missing weapon socket/trail setup is safe: no crash, optional one-time setup
  warning only if the entry explicitly requires the attachment.
- Result reveal remains separate from cosmetic trail playback. Damage/heal/status
  reveal still happens exactly once.

## Implementation Implication

Do not add a second `WeaponSkillPresentationManager`. Extend the existing
catalog/manager model with a broader "visual operation" concept when this
becomes an implementation task. The first implementation can be small: support
one attached weapon trail operation and keep all existing VFX/SFX fields
backward-compatible.
