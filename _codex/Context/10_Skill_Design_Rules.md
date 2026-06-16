# 10 Skill Design Rules

Status: active
Project: TArenaUnity3D
Last updated: 2026-06-13

## Purpose

This document records project-specific rules for skill design and skill
presentation work in TArenaUnity3D.

## Current Skill Identity Rule

The current stable skill id is the string stored in
`TosterHexUnit.skillstrings`, loaded from the unit `ScriptableObject` catalog.

For now, any skill-side system must treat that exact string as the identifier.
Do not introduce a second unrelated skill id without an explicit migration plan.

## Reflection Contract

Current executable skill logic lives mostly in `CastManager` and follows this
string convention:

- `{SkillName}M` configures targeting and availability.
- `{SkillName}` executes the committed skill.

Changing a skill name affects XML assignment, UI icon lookup, skill info lookup,
and `CastManager` reflection. Do not rename skill strings casually.

## Skill Presentation Rule

Skill VFX/SFX presentation should be additive over current gameplay logic.

The first pass should not change:

- damage, healing, status values, or cooldowns,
- targeting and range rules,
- turn consumption,
- existing unit skill assignment in XML,
- public or serialized field names.

Presentation may be missing for a skill and should fail as a silent no-op.

## Unit Catalog Assignment Vs Presentation Catalog Rule

Current skill assignment is data-driven by one `ScriptableObject` unit catalog
with one `UnitDefinitionAsset` per unit. VFX/SFX authoring should use one central
Inspector-authored `ScriptableObject` presentation catalog. The skill string is
the join key between skill ownership, skill execution, UI, and presentation.

Agreed approach after the unit-data migration:

- the unit catalog is the source of which skills a unit has.
- legacy `Units.xml` may be used only as an editor migration source.
- the central `ScriptableObject` presentation catalog supplies optional VFX/SFX
  for skill ids.
- each catalog entry uses the same string as `skillstrings`.
- run, reward, shop, and future player progression state may lock or unlock
  skills from the catalog's legal skill list.
- missing presentation entries are allowed during iteration.
- unused presentation entries are allowed temporarily but should be findable by
  a separate future validator task.

Do not make presentation catalog data silently grant, remove, or reorder skills.

Keep playback scene-owned: the catalog stores data, while a scene-level
presentation manager reads the catalog and spawns/plays effects.

Catalog skill ids are manual strings for the first implementation. Do not build
catalog/XML validation tooling as part of the first skill VFX/SFX PRD; that is a
separate future PRD/task.

## Presentation Phases

Use these phase names when adding skill feedback:

- `cast`: feedback at the caster when the skill is committed.
- `projectile`: optional feedback between caster and target/hex for skills that
  launch or throw a projectile.
- `impact`: feedback at the target unit, target hex, caster, or area center.

Do not play `cast` feedback when the player only opens targeting mode. Play it
when the skill is actually committed.

Projectile VFX should be moved by presentation code from caster to target/hex,
without Rigidbody physics. New projectile skill presentation should use the
central catalog `projectileVfx`; old `CastManager.Projectiles[index]`,
`Axe(...)`, and `FireBall(...)` paths are legacy cleanup targets, not the desired
long-term model.

Projectile presentation order is:

- cast feedback,
- projectile VFX flies from caster to target/hex,
- impact VFX/SFX plays after projectile arrival.

Non-projectile presentation order is:

- cast feedback,
- impact VFX/SFX after the entry's `impactDelaySeconds`.

Gameplay resolution should keep the current timing in the first implementation;
do not delay damage, healing, status, cooldown, or turn usage to wait for the
projectile.

Impact location should be configured per catalog entry with an `ImpactAnchor`
setting. Supported first-pass anchors should include `TargetHex`, `TargetUnit`,
`Caster`, and `AreaCenter`. Default to `TargetHex` when unset.

VFX and SFX are single references per phase in the first implementation:

- one cast VFX and one cast SFX,
- one optional projectile VFX and one optional projectile SFX,
- one impact VFX and one impact SFX.

Do not add random VFX/SFX variant arrays until a later task explicitly expands
the content model.

Persistent board-state visuals use the same skill id but are not impact VFX.
Trap skills and similar lasting world objects may define `spawnModel` on their
catalog entry. Gameplay ownership stays in the relevant gameplay model
(`HexClass` / `Traps` for current traps), while the presentation manager only
spawns and returns the persistent model instance for that state object to own
and destroy.

## Skill-Specific Presentation Timing

`Force_Pull` and `Long_Lick` are two-location pull presentations. The committed
skill should show `castVfx` on the destination target hex first, then after the
catalog `impactDelaySeconds` play `impactVfx` and `impactSfx` on the pulled
target unit. The actual `TeleportToHex(...)` call belongs to that impact phase,
after the target-unit impact presentation has been triggered.

## Out Of Scope For First Skill Presentation Pass

- full skill-system rewrite,
- moving skill assignment into the presentation catalog,
- changing balance or gameplay float values,
- forcing all skills to have all three presentation phases,
- making gameplay wait for projectile flight timing,
- editing prefabs, scenes, Animator Controllers, materials, generated Unity
  files, `.inputactions`, `.asmdef`, or `.asmref`.
