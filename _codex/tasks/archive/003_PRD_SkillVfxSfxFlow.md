# [TARENA] PRD: Skill VFX And SFX Flow

- Status: closed-implemented-pending-unity-validation
- Type: PRD
- Area: Skills, Combat Presentation
- Label: ready-for-agent

## Problem Statement

Skills currently resolve gameplay effects through a large skill execution path,
but they do not have a consistent project-owned presentation layer for VFX and
SFX. Some skills apply effects immediately on a target or hex, some are
self-cast buffs, some affect an area, some move or pull units, and some spawn
projectile GameObjects. Because presentation is mixed directly into individual
skill bodies, adding or changing audiovisual feedback skill-by-skill is risky
and easy to make inconsistent.

The player should be able to read the three common skill moments:

1. The caster starts using a skill.
2. A projectile visual exists when the skill launches or throws something.
3. The target, hex, or area receives the impact.

The content author should be able to assign VFX prefabs and SFX clips in Unity
without editing generated Unity assets, Resources XML, or gameplay floats.

## Solution

Add a small skill presentation layer that can play optional `cast`, `projectile`,
and `impact` feedback for each skill. The layer should be data-driven from
Inspector-assigned references and callable from the existing skill execution
flow at the points where a skill is actually committed.

Each skill can define:

- cast VFX and cast SFX at the caster,
- projectile VFX and optional projectile SFX from caster to target when the
  skill launches or throws a projectile,
- impact VFX and impact SFX at a target unit, target hex, caster hex, or area.

The first implementation should not rewrite skill mechanics or require every
skill to be migrated at once. Missing presentation data should be a silent
no-op, so skills can be covered incrementally.

## User Stories

1. As a player, I want skill casts to have visible feedback, so that I can tell which unit used a skill.
2. As a player, I want skill casts to have sound, so that skill activation feels intentional.
3. As a player, I want projectile-like skills to have projectile VFX, so that I can read where the skill is going.
4. As a player, I want projectile-like skills to have projectile SFX when appropriate, so that flying or thrown effects feel present.
5. As a player, I want skill impacts to have visible feedback, so that I can understand which unit or hex was affected.
6. As a player, I want skill impacts to have sound, so that damage, healing, buffs, pulls, and debuffs feel distinct.
7. As a player, I want self-cast buffs to show feedback on the caster, so that I can notice non-targeted skill use.
8. As a player, I want area skills to show feedback on the affected area, so that AoE coverage is readable.
9. As a player, I want instant targeted skills to show feedback at the target, so that non-projectile effects do not look empty.
10. As a player, I want ranged thrown skills to show projectile and impact feedback, so that they do not feel like immediate invisible damage.
11. As a player, I want each configured skill phase to play its assigned SFX, so that skill feedback is understandable even before variants exist.
12. As a player, I want missing skill feedback on unfinished content to fail silently, so that incomplete content does not interrupt play.
13. As a content author, I want to assign skill VFX prefabs through Inspector fields, so that I do not need to change code for every visual.
14. As a content author, I want to assign skill SFX clips through Inspector fields, so that audio setup stays in Unity.
15. As a content author, I want each skill to support separate cast, projectile, and impact assets, so that feedback can match the skill behavior.
16. As a content author, I want projectile feedback to be optional, so that self-cast and instant skills are not forced into projectile logic.
17. As a content author, I want impact feedback to support target unit and target hex positions, so that skills can hit units or locations.
18. As a content author, I want area skills to be able to spawn impact feedback on multiple affected hexes or one central hex, so that heavy AoE can stay readable.
19. As a content author, I want the system to work with existing projectile prefabs where useful, so that old content is not discarded unnecessarily.
20. As a player, I want stance toggles to have cast feedback, so that changing between melee and range reads as an intentional action.
21. As a player, I want basic ranged attacks to use the same projectile/impact presentation rules, so that non-skill ranged shots do not remain on the old hard-coded projectile path.
22. As a content author, I want a default basic ranged attack presentation entry, so that ordinary ranged shots can be configured without making them fake XML skills.
23. As a developer, I want one narrow skill presentation API, so that individual skill bodies do not each reinvent VFX and SFX playback.
24. As a developer, I want skill presentation calls near skill commit points, so that feedback does not play while the player is only choosing a target.
25. As a developer, I want missing per-skill data to be safe, so that skills can be migrated one by one.
26. As a developer, I want missing scene-level presentation setup to warn once, so that scene configuration mistakes are visible without log spam.
27. As a developer, I want skill SFX playback to overlap with combat SFX, so that rapid feedback does not cut itself off.
28. As a developer, I want skill VFX lifetime to be controlled by simple serialized values or prefab self-destruction, so that spawned effects do not leak forever.
29. As a developer, I want the first implementation to avoid changing damage math, cooldowns, targeting rules, or skill float values, so that presentation work does not change balance.
30. As a developer, I want the first implementation to avoid editing prefabs and scenes directly, so that the user can wire content in Unity.
31. As a QA tester, I want a clear manual checklist for self-cast, instant target, AoE, and projectile skills, so that the system can be validated in Play Mode.

## Implementation Decisions

- Add a skill presentation module with simple calls for `cast`, `projectile`, and
  `impact`.
- Skill presentation should be keyed by the existing skill identifier string
  used by the current skill execution flow.
- Add per-skill presentation data that can hold one cast VFX prefab, one cast
  SFX clip, one projectile VFX prefab, one projectile SFX clip, one impact VFX
  prefab, one impact SFX clip, optional lifetime, optional impact delay, and
  optional projectile timing.
- Keep skill presentation content assigned through Unity Inspector references.
  Do not use `Resources.Load` for skill VFX or SFX assets in this PRD.
- Store skill presentation data in a central Inspector-authored catalog keyed by
  the exact skill name used in XML and `TosterHexUnit.skillstrings`.
- Basic ranged attack is the one first-pass exception to XML skill-id keying,
  because it is executed from `MouseControler.Shot(...)` and
  `TosterHexUnit.ShootME(...)`, not from `TosterHexUnit.skillstrings`. Represent
  it with a single catalog default/basic ranged attack entry, not a fake XML
  skill assignment.
- The central catalog owns the default VFX/SFX for each skill. Unit models do not
  decide which skill presentation exists in the first implementation.
- Implement the central catalog as a `ScriptableObject` asset, not as scene
  state. A scene-level skill presentation manager references this asset and
  handles playback.
- Reuse the existing scene-level SFX playback approach for the first pass so
  skill SFX can overlap with combat SFX through `PlayOneShot`.
- Do not rename the existing combat SFX classes as part of this PRD. A broader
  audio naming cleanup can happen later if needed.
- Add a scene-level skill presentation manager or service that references the
  catalog asset, instantiates VFX prefabs, and requests SFX playback.
- Keep VFX and SFX references single-value fields for the first implementation.
  Do not add random VFX/SFX variant arrays until a later task explicitly expands
  the content model.
- Missing per-skill presentation entries, missing VFX prefabs, and missing SFX
  clips should be silent no-ops.
- Missing scene-level presentation setup should warn once, because it indicates
  a Unity scene setup problem.
- Catalog skill ids are manual strings that must match XML skill names.
  Catalog/XML validation tooling is intentionally deferred to a separate PRD.
- Cast feedback should play only when the skill is committed, not when the user
  merely opens targeting mode or hovers a hex.
- Self-cast skills should use the caster position for cast and impact feedback.
- Instant targeted skills should use the caster position for cast feedback and
  the target unit or target hex position for impact feedback.
- AoE skills that affect units should spawn impact VFX on each actually
  affected target unit, not on the selected hex as a fallback.
- If a target-affecting AoE skill hits no unit, it should play cast feedback
  only and should not spawn impact feedback on the selected hex just because the
  player targeted that hex.
- AoE skills that affect only a location, trap, summon point, or terrain can
  still use the selected/central hex as their impact anchor.
- The impact location should be configurable per catalog entry with an
  `ImpactAnchor` value such as `TargetHex`, `TargetUnit`, `Caster`, or
  `AreaCenter`.
- Projectile or thrown skills should use projectile feedback from caster to
  target or selected hex, then play impact feedback at the configured
  destination.
- Projectile VFX should be a controlled prefab moved by code from caster to
  target/hex, without Rigidbody physics.
- The catalog `projectileVfx` is the projectile prefab source. Remove old
  hard-coded `CastManager.Projectiles[index]` usage from migrated projectile
  skills instead of keeping it as fallback.
- Remove the old `Axe(...)` and `FireBall(...)` hard-coded projectile helpers
  when their callers are migrated to the presentation manager.
- Do not spawn both the catalog projectile and the old hard-coded projectile for
  the same committed skill at any point during migration.
- Stance and basic ranged attack projectile setup are also migration targets.
  Do not leave `Range_Stance_*M` / `Melee_Stance_*M` assigning
  `SelectedT().Projectile = Projectiles[0]` as the long-term ranged projectile
  setup.
- After migration, stance skills should change range/melee gameplay state and
  play stance cast/impact feedback, while the actual basic ranged attack
  projectile VFX/SFX should come from the catalog default/basic ranged attack
  entry when `MouseControler.Shot(...)` and `TosterHexUnit.ShootME(...)` commit a
  ranged shot.
- Replace `HexMap.ThrowSomething(..., t.Projectile)` with catalog-driven
  controlled projectile presentation for migrated basic ranged attacks. Do not
  keep the old Rigidbody `ThrowSomething` projectile and the new catalog
  projectile active for the same shot.
- The first pass should be fire-and-forget presentation. It should not delay or
  reorder gameplay effect application unless a later task explicitly introduces
  sequenced skill resolution.
- For projectile skills, presentation should run as `cast -> projectile flies ->
  impact VFX/SFX after projectile arrival`.
- For non-projectile skills, presentation should run as `cast -> impact after
  impactDelaySeconds`.
- Gameplay effects such as damage, healing, status, cooldown, and turn usage
  should still resolve through the current logic timing. Do not delay gameplay
  resolution to wait for the projectile in the first implementation.
- Do not add more presentation phases for the first pass. Movement, teleport,
  pull, trap placement, summon/split, stance toggle, teamwide buff, and global
  debuff skills should still be represented with only `cast`, optional
  `projectile`, and `impact`.
- For movement, teleport, pull, and dash skills, use cast feedback at the acting
  unit and impact feedback at the final destination, moved unit, selected hex,
  or affected area center. Do not add path-trail or movement-sequencing work in
  this PRD.
- For trap placement skills, use cast feedback at the caster and impact feedback
  on the trap hex. Persistent trap visuals are out of scope unless the existing
  trap system already owns them.
- For summon or split skills, use cast feedback at the original caster and
  impact feedback on the spawn hex or affected target. The PRD should not add a
  separate summon presentation phase.
- For stance toggle and other instant self-cast skills that commit inside
  `{SkillName}M`, presentation may be called from the mode method because there
  is no later target click. The call must use the original skill id before any
  method mutates `skillstrings[SelectedSpellid]`.
- For normal targeted skills, `{SkillName}M` remains targeting/setup only and
  should not play VFX/SFX.
- For multi-click skills, call presentation only when a real gameplay effect is
  committed. Do not play presentation on intermediate selection clicks that only
  collect targets or on a final `SetFalse()` click that only exits targeting.
- For skills that defer gameplay through `[PunRPC]` methods, play presentation
  from the same committed execution side as the gameplay effect, and avoid
  playing once in the caller and once again in the RPC handler.
- Passive/info skills that only print a message and call `SetFalse()` should not
  play active cast feedback in this PRD. Passive trigger VFX/SFX needs separate
  hooks at the actual passive trigger point, such as start turn, on hit, aura, or
  movement tile trigger, and should be handled later.
- Skill animations that already call Animator states such as `Skill1` or
  `Skill2` should remain compatible, but the new VFX/SFX layer should not depend
  on every Animator Controller having matching states.
- Keep damage, healing, status, movement, teleport, targeting, cooldown, and turn
  consumption behavior unchanged.
- Do not edit Animator Controllers, prefabs, scenes, materials, generated Unity
  files, serialized Unity assets, `.asmdef`, `.asmref`, or `.inputactions` as
  part of the code task.

## Testing Decisions

- The user compiles and tests inside Unity. No command-line Unity build, dotnet
  test run, package restore, or SDK command is required for this PRD.
- A good test validates external behavior: which VFX/SFX play, where they play,
  when they play, and whether gameplay behavior stays unchanged.
- Do not test private helper implementation details directly unless a small pure
  C# data lookup module is extracted and can be tested without Unity scene
  dependencies.
- Manual Play Mode testing should cover one self-cast buff skill.
- Manual Play Mode testing should cover one instant targeted enemy skill.
- Manual Play Mode testing should cover one area skill.
- Manual Play Mode testing should cover one projectile or thrown skill.
- Manual Play Mode testing should cover one stance toggle or instant self-cast
  skill whose gameplay commits inside `{SkillName}M`.
- Manual Play Mode testing should cover one basic ranged attack after entering
  ranged stance; the expected result is catalog projectile and impact feedback,
  not the old `TosterHexUnit.Projectile` / `HexMap.ThrowSomething(...)` visual.
- Manual Play Mode testing should cover one movement, teleport, pull, or dash
  skill.
- Manual Play Mode testing should cover one trap placement or summon/split
  skill.
- Manual Play Mode testing should cover one multi-click or multi-target skill
  and confirm feedback plays on committed effects, not on target collection
  clicks.
- Manual Play Mode testing should cover one RPC-deferred skill and confirm VFX
  and SFX do not double-play.
- Manual Play Mode testing should cover one passive/info skill selection; the
  expected result is no active cast VFX/SFX and no crash.
- Manual Play Mode testing should cover a skill with only cast feedback.
- Manual Play Mode testing should cover a skill with cast, projectile, and impact
  feedback.
- Manual Play Mode testing should cover a skill with missing presentation data;
  the expected result is no VFX/SFX and no content warning spam.
- Manual Play Mode testing should cover a scene without the required manager;
  the expected result is one setup warning and no crash.
- Manual Play Mode testing should confirm combat attack, hit, and death SFX from
  the earlier combat SFX PRD still work after skill SFX is added.
- Manual Play Mode testing should confirm repeated SFX can overlap rather than
  cutting each other off.
- Manual Play Mode testing should confirm spawned VFX are cleaned up by prefab
  behavior or configured lifetime.
- Suggested first sample skills for validation are one self-cast/status skill,
  one direct damage skill, one AoE damage skill, and one thrown/projectile skill.
- Do not build editor validation tooling for catalog skill ids in this PRD.

## Out of Scope

- Rewriting the full skill system.
- Moving skill definitions out of the current string/reflection based flow.
- Changing skill balance, cooldowns, damage, healing, status values, movement
  values, or targeting rules.
- Renaming public or serialized fields.
- Editing Unity prefabs, scenes, Animator Controllers, animation clips,
  materials, generated Unity files, `.inputactions`, `.asmdef`, or `.asmref`.
- Building a full audio mixer, volume settings UI, positional 3D audio model, or
  persistent audio settings.
- Making gameplay resolution wait for projectile flight or impact timing.
- Adding camera shake, haptics, post-processing, or screen feedback unless a
  later task explicitly expands skill presentation beyond VFX and SFX.
- Creating final VFX or SFX assets. This PRD covers the code path and authoring
  hooks for assets the user wires in Unity.
- Building a catalog/XML validator. That should be a separate PRD/task.

## Further Notes

Closure note, 2026-06-11:

- PRD 3 is closed as the first-pass data-driven skill VFX/SFX presentation
  layer: catalog, scene manager, Inspector setup, basic ranged attack migration,
  and representative `CastManager` call sites.
- Sequenced skill impact timing, attack-like `onImpact` routines, and skill
  `onHit` integration are intentionally moved to
  `_codex/tasks/009_PRD_SkillImpactRoutineFlow.md`.

Implementation summary, 2026-06-11:

- Added `SkillPresentationCatalog` and `SkillPresentationManager`.
- Added catalog entries keyed by XML skill ids plus one default basic ranged
  attack entry for non-XML ranged shots.
- Wired active skill presentation call sites in `CastManager` for direct target,
  self-cast, AoE, projectile, multi-target, movement/teleport/pull, trap,
  summon/split, stance toggle, and selected RPC-deferred paths.
- Migrated basic ranged attack away from `TosterHexUnit.Projectile` /
  `HexMap.ThrowSomething(...)` playback to `SkillPresentationManager`.
- Updated target-affecting AoE presentation so impact VFX plays on each
  actually affected unit and no selected-hex impact fallback plays when the AoE
  hits no unit.
- Added presentation support for VFX-only secondary impacts and projectile
  travel without automatic destination impact.
- Removed active `Axe(...)` / `FireBall(...)` projectile helper usage from
  migrated skill casts.
- Added detailed Inspector setup instructions to
  `_codex/Documentation/User_Setup_Guide.md`.
- Unity compile and Play Mode validation remain user-side per project policy.

PRD 1/2 alignment note:

- PRD 1 anchors hit/death animation to the real damage path, and PRD 2 anchors
  hit/death SFX to real combat events. PRD 3 follows the same rule for skill
  impact presentation: impact VFX/SFX should be emitted where the skill actually
  affected a unit or location, not merely where targeting mode pointed before
  resolution.

Diagnosis summary:

- The existing skill execution flow is centralized enough to add a presentation
  service, but individual skill bodies currently apply effects in many different
  ways.
- Existing combat SFX already established the right production pattern:
  Inspector-assigned clips, centralized playback, silent missing content, and a
  warning only for missing scene setup.
- Existing projectile-like skill visuals and basic ranged attack visuals are
  hard-coded through projectile list indexes, `TosterHexUnit.Projectile`,
  `HexMap.ThrowSomething(...)`, and Rigidbody force. These should be replaced by
  catalog-driven projectile VFX moved by code, without physics.
- The highest-risk mistake would be trying to refactor all skill mechanics while
  adding VFX/SFX. The safer task is a presentation layer plus a small first set
  of migrated representative skills.

Reviewed skill code examples:

- `Skill1` is an instant multi-click enemy damage skill. It has target/impact
  moments but no real projectile path in current code.
- `Skill2` is an AoE damage skill around the selected hex. It should use cast
  feedback at the caster and impact feedback on each actually affected target,
  without projectile feedback. If the selected area hits no unit, it should not
  fall back to an impact on the selected hex.
- `Skill3` is a friendly heal. It should use cast feedback at the caster and
  impact feedback on the healed target.
- `Rzutnik_Skill2`, `Rzutnik_Skill3`, and `Stone_Stance` are self-cast/status
  style skills. They should use cast feedback and optional impact feedback on
  the caster, with no projectile.
- `Double_Throw`, `Axe_Rain`, and `Fire_Ball` currently instantiate or imply
  projectile visuals through `CastManager.Projectiles`, `Axe(...)`,
  `FireBall(...)`, `TosterHexUnit.Projectile`, or `HexMap.ThrowSomething(...)`.
  These are the clearest first candidates for the optional `projectile` phase.
- For migrated versions of those projectile skills, use the catalog
  `projectileVfx` and remove the old `Projectiles[index]`, `Axe(...)`, and
  `FireBall(...)` path from those casts. Avoid double-spawning legacy and
  catalog projectiles.
- `Range_Stance_Barb`, `Melee_Stance_Barb`, `Range_Stance_Lizard`, and
  `Melee_Stance_Lizard` currently set `SelectedT().Projectile =
  Projectiles[0]`. Those assignments are legacy setup for basic ranged attack
  visuals and should be migrated out together with the basic ranged attack
  projectile path.
- Basic ranged attack currently flows through `MouseControler.Shot(...)`,
  `TosterHexUnit.ShootME(..., true)`, and `HexMap.ThrowSomething(...,
  t.Projectile)`. It should use the same catalog-driven controlled projectile
  presentation as skill projectiles, using the catalog default/basic ranged
  attack entry rather than a XML skill id.
- `Stone_Throw` creates/splits a unit and applies damage; despite the name, the
  current implementation is not a simple projectile path. Treat it as cast plus
  impact/summon presentation unless later implementation changes the behavior.
- `Toxic_Fume` combines status/AoE with movement. It should not be forced into
  projectile presentation unless a real projectile is added later.

CastManager coverage audit:

- Direct targeted damage, heal, and status skills are covered by cast at caster
  plus impact at target unit or target hex. Examples: `Skill3`, `Hate`,
  `Tough_Skin`, `Blind_by_light`, `Topornik_Skill2`, and `Rzutnik_Skill2`.
- Unit-affecting AoE skills are covered by cast plus impact VFX on every
  actually affected unit. Examples: `Skill2`, `Chope`, `Topornik_Skill1`,
  `Tank_Skill1`, `Defence_Ritual`, `Insult`, `Toxic_Fume`, `Heavy_Fists`, and
  the hit targets of `Fire_Ball`.
- Location-only, trap, summon, movement, or terrain-style AoE may still use one
  readable impact at `AreaCenter` or `TargetHex` when no unit-specific hit list
  exists.
- Multi-target and multi-click skills need explicit call sites, not an automatic
  hook around `SetFalse()`. `Skill1` applies damage on each successful click and
  exits on a later click, while `Double_Throw` and `Rzutnik_Skill1` collect
  targets before resolving multiple hits.
- Projectile/thrown skills remain the only skills that should use the
  `projectile` phase. Confirmed examples are `Double_Throw`, `Axe_Rain`, and
  `Fire_Ball`; `Axe_Rain` may spawn more than one projectile presentation from a
  single cast because it hits multiple units.
- Basic ranged attack is also a projectile presentation case, but it is not a
  skill. Treat it as a catalog default/basic ranged attack entry and migrate it
  with `Range_Stance_*`, `Melee_Stance_*`, `MouseControler.Shot(...)`,
  `TosterHexUnit.ShootME(..., true)`, and `HexMap.ThrowSomething(...)`.
- Movement, dash, teleport, and pull skills should stay in the same three-phase
  model. Examples: `Rush`, `Slash`, `Tank_Skill2`, `TeleportOT`, `Force_Pull`,
  `Long_Lick`, `Toxic_Fume`, and `Heavy_Fists`. Use impact at destination,
  pulled target, selected hex, or area center.
- Trap skills should use impact on the placed trap hex. Examples:
  `Spike_Trap` and `Rope_Trap`.
- Summon/split behavior should use impact on the spawn hex or affected target.
  Example: `Stone_Throw`.
- Stance toggle and instant self-cast skills can commit inside their `M` method,
  so presentation must be placed there if they are covered. Examples:
  `Range_Stance_Barb`, `Melee_Stance_Barb`, `Range_Stance_Lizard`,
  `Melee_Stance_Lizard`, `Rage`, `Shapeshift`, and `Stone_Stance`.
- Passive/info skills in `CastManager` are not covered by active skill cast
  presentation in the first pass. Examples: `Cold_Blood`, `Massochism`,
  `Unstoppable_Light`, `Stone_Skin`, `Fire_Movement`, `Fire_Skin`,
  `Terrifying_Presence`, and `Rotting`.
- RPC-deferred skills need one presentation decision point, preferably where the
  gameplay effect is actually applied on all clients. Examples: `Rush`/`rrush`,
  `Slash`/`slash`, `Hate`/`hate`, `Tough_Skin`/`tough_Skin`,
  `Long_Lick`/`long_Lick`, and `Heavy_Fists`/`heavy_fists`.
- Empty placeholders and unassigned template methods do not need presentation
  data until they become real assigned skills.

Grill decisions and recommended answers:

1. Should every skill require all three phases?
   Recommended answer: no. Every skill may define cast and impact; projectile is
   optional and should exist only when the skill actually launches or throws a
   projectile.

2. Should impact timing change gameplay timing?
   Recommended answer: no for the first pass. Presentation should be
   fire-and-forget so the task does not accidentally rebalance combat.

3. Should presentation data live on unit models or in one central catalog?
   Decision: one central Inspector-authored catalog. Each entry uses the exact
   skill name from XML plus its cast/projectile/impact VFX and SFX references,
   except the catalog default/basic ranged attack entry, which covers ordinary
   ranged shots because they are not XML skills.

4. Should the central catalog be a scene component or a `ScriptableObject`?
   Decision: `ScriptableObject`. Skill presentation entries are content data,
   while scene playback remains the responsibility of a scene manager.

5. Should skill SFX use the existing scene SFX playback path?
   Recommended answer: yes for the first pass. It keeps overlapping SFX behavior
   consistent and avoids a premature audio-system rewrite.

6. What is the minimum useful implementation slice?
   Recommended answer: build the presentation layer, then wire representative
   categories: self-cast, instant target, AoE, projectile skill, stance toggle,
   and basic ranged attack.

7. Should `projectileVfx` replace old `CastManager.Projectiles[index]` entries?
   Decision: yes. The catalog `projectileVfx` replaces the old hard-coded
   projectile list path. `CastManager.Projectiles[index]`, `Axe(...)`, and
   `FireBall(...)` are legacy implementation details to remove while migrating
   projectile skills. `TosterHexUnit.Projectile` and
   `HexMap.ThrowSomething(...)` are also legacy projectile setup/playback for
   basic ranged attacks and should be migrated to the catalog default/basic
   ranged attack entry. Do not play both legacy and catalog projectiles for the
   same skill cast or basic ranged shot.

8. Should projectile movement use Rigidbody physics or controlled code movement?
   Decision: controlled code movement from caster to target/hex, without
   Rigidbody physics. This makes impact timing and final position predictable.

9. When should projectile impact VFX/SFX play?
   Decision: presentation sequence is `cast -> projectile flies -> impact after
   projectile arrival`. Gameplay still resolves through current logic timing and
   should not wait for projectile arrival in the first implementation.

10. Where should impact VFX/SFX spawn?
    Decision: configurable per skill entry with `ImpactAnchor`, with values such
    as `TargetHex`, `TargetUnit`, `Caster`, and `AreaCenter`. Default should be
    `TargetHex`.

11. Should VFX and SFX be single references or variant arrays?
    Decision: single VFX and single SFX reference per phase for the first
    implementation. No random variants yet.

12. When should impact play for non-projectile skills?
    Decision: after a per-skill `impactDelaySeconds`, defaulting to `0`. For
    projectile skills, impact plays after projectile arrival plus optional
    `projectileImpactDelaySeconds`.

13. Should this PRD include catalog/XML validation tooling?
    Decision: no. Catalog `skillId` is a manual string matching XML skill names.
    Validation tooling should be handled by a separate PRD/task.

Minimal model:

- SkillPresentationManager
- catalog
- PlayCast(skillId, caster)
- PlayProjectile(skillId, caster, target)
- PlayImpact(skillId, caster, targetOrHex)
- PlayBasicRangedAttack(caster, target)

- SkillPresentationCatalog.asset
- entries[]
- defaultBasicRangedAttackEntry

- SkillPresentationEntry
- skillId
- castVfx
- castSfx
- projectileVfx
- projectileSfx
- projectileSpeed
- impactAnchor
- impactVfx
- impactSfx
- impactDelaySeconds
- effectLifetimeSeconds
- projectileImpactDelaySeconds

- Existing skill execution flow
- validates target
- commits skill
- asks presentation layer to play the configured phase feedback

Example:

- `Fire_Ball`
- cast VFX/SFX at caster
- projectile VFX/SFX from caster to selected hex
- impact VFX on each actually hit target unit, with no selected-hex fallback
  when no unit is hit
