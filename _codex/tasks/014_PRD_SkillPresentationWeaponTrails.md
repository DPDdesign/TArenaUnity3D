# [TARENA] PRD: Skill Presentation Weapon Trails

- Status: draft
- Type: PRD
- Area: Skills, Combat Presentation, Weapon Trails, VFX/SFX
- Label: ready-for-agent
- Created: 2026-06-12
- Requires: `_codex/tasks/archive/003_PRD_SkillVfxSfxFlow.md`
- Requires: `_codex/tasks/010_PRD_MigrateRemainingSkillImpactRoutines.md`
- Informed by: `_codex/tasks/Analysis/013_PRD_SkillPresentation_PhysicalTrail_Analysis.md`

## Problem Statement

The current skill presentation layer supports catalog-driven cast, projectile,
and impact VFX/SFX. That works for magic, projectile, AoE, trap, summon, and
hex-based effects, but it does not fully support physical weapon skills.

Some skills should not spawn a standalone world VFX. Their correct presentation
is a short weapon trail, sword trail, weapon glow, or slash smear that follows
the unit's weapon during an animation. Forcing those skills into `projectileVfx`
or `impactVfx` creates misleading presentation: a sword attack is not a flying
projectile, and a physical swing may need no world impact VFX at all.

The desired outcome is that physical skills can use the same skill presentation
layer as VFX/SFX skills, but with a weapon-trail operation as one possible
visual action. A skill should be allowed to be SFX-only, trail-only, VFX-only, or
any small combination that matches the skill.

## Solution

Extend the existing `SkillPresentationManager` and `SkillPresentationCatalog`
model so catalog entries can trigger a temporary weapon trail during the
existing sequenced skill presentation flow.

The first implementation should support a minimal authoring model:

- a catalog entry can request a weapon trail for a skill,
- the trail can be enabled for a fixed duration during cast/attack presentation,
- the trail is an existing `TrailRenderer` already attached under the caster's
  `TosterView` hierarchy, usually on the weapon or a weapon child object,
- missing trail setup is safe and does not break skill execution,
- SFX continues to play through the same skill presentation entry,
- result reveal remains separate and still plays exactly once.

This PRD does not introduce a separate weapon skill presentation system. Weapon
trails are one visual operation inside the same presentation layer that already
plays skill VFX/SFX.

## User Stories

1. As a player, I want physical weapon skills to show a trail on the weapon, so that the motion reads as a weapon action instead of a magic projectile.
2. As a player, I want sword trail skills to still play SFX, so that the swing feels intentional even without a spawned VFX.
3. As a player, I want a physical skill with no world VFX to still feel complete, so that not every skill looks magical.
4. As a player, I want weapon trail timing to line up with the attack animation, so that the effect follows the swing instead of appearing too early or too late.
5. As a player, I want hit, death, heal, and status result feedback to remain readable, so that a cosmetic trail does not replace combat feedback.
6. As a player, I want projectile skills and weapon trail skills to look different, so that I can tell a thrown attack from a melee swing.
7. As a player, I want stance or self-buff skills to allow weapon glow or SFX-only presentation, so that they do not need fake impact effects.
8. As a player, I want missing or unfinished trail setup to fail quietly, so that the game does not break while content is being authored.
9. As a content author, I want to configure a weapon trail from the existing skill presentation catalog, so that skill presentation remains in one place.
10. As a content author, I want to identify the trail under the unit view without changing XML skill assignment, so that ownership and presentation stay separate.
11. As a content author, I want the same skill id string to drive VFX, SFX, and weapon trail setup, so that there is no second skill identity system.
12. As a content author, I want a physical skill to have only a trail and SFX if that is enough, so that I do not need to assign placeholder VFX.
13. As a content author, I want trail duration to be configurable per skill entry, so that quick slashes and heavier swings can read differently.
14. As a content author, I want missing trail references to be safe, so that incomplete prefabs do not crash Play Mode.
15. As a developer, I want weapon trails to be handled by `SkillPresentationManager`, so that there is not a second parallel presentation manager.
16. As a developer, I want this to preserve existing cast/projectile/impact sequencing, so that PRD 009 and PRD 010 result reveal timing remains valid.
17. As a developer, I want the first implementation to avoid prefab or scene edits, so that the user can wire content manually in Unity.
18. As a developer, I want existing catalog entries to remain backward-compatible, so that current VFX/SFX setup does not need to be rebuilt.
19. As a developer, I want no gameplay values, targeting rules, cooldowns, or damage timing to change, so that this remains presentation-only.
20. As QA, I want a manual test that proves one skill can play a weapon trail without projectile VFX, so that the feature is validated on its core case.
21. As QA, I want a manual test that proves missing trail setup does not block result reveal, so that incomplete content is safe.
22. As QA, I want a manual test that proves existing projectile skills still use projectile presentation, so that this change does not regress magic or thrown skills.

## Implementation Decisions

- Keep `SkillPresentationManager` as the only scene-owned skill presentation playback owner.
- Keep `SkillPresentationCatalog` as the central Inspector-authored presentation data source.
- Keep skill presentation keyed by the existing XML / `TosterHexUnit.skillstrings` skill id.
- Do not add a separate `WeaponSkillPresentationManager`.
- Do not add a second skill id, weapon skill id, animation id, or presentation id.
- Add weapon trail as an optional visual operation inside a skill presentation entry.
- Preserve existing `castVfx`, `projectileVfx`, `impactVfx`, and SFX fields so existing catalog content remains valid.
- The minimal first implementation should support toggling an existing `TrailRenderer` found under the caster's `TosterView` hierarchy.
- The first implementation should not instantiate or attach trail prefabs. The
  `TrailRenderer` is authored on the unit/weapon prefab by the user in Unity.
- The trail should be selected by a serialized name/id field on the skill presentation entry, not by hard-coded skill name checks.
- The trail operation should be optional. If no trail is configured, existing VFX/SFX behavior should remain unchanged.
- The trail should have a serialized duration. If no duration is set, use a small safe default chosen in code, not a gameplay balance value.
- The trail should be disabled after the duration even if the skill has no impact VFX or no frontend result reveal.
- Missing trail setup should be a safe no-op. If the catalog entry explicitly names a trail but it cannot be found, log at most one setup warning per missing trail key or otherwise keep the failure non-spammy.
- Weapon trail playback should happen during the cast/attack portion of the sequence, before result reveal.
- For sequenced instant hit skills, the trail should be able to play as part of the cast sequence and then allow impact/reveal to continue.
- For approach skills, the trail should play at the committed attack/cast moment, not while the player is still selecting a target.
- For projectile skills, weapon trail should not replace `projectileVfx`; use projectile presentation only when an object actually travels from caster to target.
- A skill may validly define trail plus SFX and no world VFX.
- A skill may validly define trail plus impact SFX/reveal and no impact VFX.
- Result reveal remains owned by the existing frontend reveal flow. Weapon trail playback must not create, duplicate, or suppress damage/heal/status reveal.
- Start with physical active skills as the target use case. Passive/deferred presentation from PRD 013 can use the same capability later only when its real trigger needs it.
- Recommended first validation skill is `Slash`, because it is a physical attack-like skill and already uses sequenced result reveal.
- `Rush`, stance skills, basic melee attack presentation, and other weapon actions are follow-up candidates after the first trail path works.
- Do not edit Unity prefabs, scenes, materials, Animator Controllers, animation clips, generated Unity files, `.inputactions`, `.asmdef`, or `.asmref` as part of the code task.
- The user wires actual `TrailRenderer` components or trail assets in Unity after implementation.

## Grill Decisions

1. Should sword trail be a new system or part of skill VFX/SFX presentation?
   Recommended answer: part of the existing skill presentation layer.

2. Should a sword trail count as `projectileVfx`?
   Recommended answer: no. Projectile remains only for visuals that travel from
   caster to target.

3. Should every physical skill still require impact VFX?
   Recommended answer: no. Trail-only or SFX-only entries are valid.

4. Should the first implementation instantiate a trail prefab or toggle an
   existing trail?
   Recommended answer: toggle an existing `TrailRenderer` under the caster view
   first. The user confirmed this is the desired first implementation model.
   Prefab attach is out of scope for PRD 014.

5. Where should the trail be resolved?
   Recommended answer: from the caster's `TosterView` hierarchy, using a
   catalog-authored trail name/key.

6. Should missing trail setup warn or silently do nothing?
   Recommended answer: safe no-op, with at most one setup warning when the entry
   explicitly requested a named trail.

7. Should gameplay wait for trail duration?
   Recommended answer: no beyond the existing presentation sequencing. Do not
   change damage, cooldown, targeting, or turn timing.

8. Should this PRD add animation events?
   Recommended answer: no. Animation events require editing animation assets.
   Use duration/timing fields for the first implementation.

## Minimal Model

SkillPresentationEntry:

- `skillId`
- existing VFX/SFX fields
- `useTrail`
- `weaponTrailDurationSeconds`

TosterView:

- can find attached child `TrailRenderer` components
- can enable all weapon trails for a duration
- disables trails deterministically

SkillPresentationManager:

- reads the trail fields from the existing catalog entry
- starts the trail during cast/attack presentation
- keeps existing impact/projectile/reveal sequencing

Example:

- `Slash`
- no `projectileVfx`
- optional `castSfx`
- `useTrail = true`
- `weaponTrailDurationSeconds = 0.35`
- optional `impactSfx`
- result reveal still plays once on the hit target

## Testing Decisions

- The user compiles and tests inside Unity. No command-line Unity build, dotnet test, package restore, external build script, or SDK command is required.
- A good test validates visible behavior: whether the trail appears, follows the weapon/view, disables afterward, and does not interfere with SFX or result reveal.
- Do not test private coroutine internals unless a small pure C# lookup helper is extracted.
- Manual Play Mode testing should cover one configured physical skill with trail and SFX.
- Manual Play Mode testing should cover the same skill with missing trail setup and confirm no crash, no blocked result reveal, and no warning spam.
- Manual Play Mode testing should cover one existing projectile skill and confirm projectile VFX still travels normally.
- Manual Play Mode testing should cover one existing VFX-only or SFX-only skill and confirm old catalog behavior still works.
- Manual Play Mode testing should confirm the trail disables after its configured duration.
- Manual Play Mode testing should confirm repeated uses of the same skill do not leave the trail permanently enabled.
- Manual Play Mode testing should confirm damage/hit/death reveal still plays exactly once for a damaging physical skill.

## Out of Scope

- Rewriting the skill system.
- Moving skill definitions out of XML.
- Renaming skill ids.
- Changing damage, healing, cooldowns, ranges, movement, targeting rules, status values, or turn consumption.
- Editing Unity scenes, prefabs, materials, Animator Controllers, animation clips, `.inputactions`, generated Unity files, `.asmdef`, or `.asmref`.
- Adding animation events to existing animation clips.
- Spawning or attaching trail prefabs at runtime.
- Building a full socket/bone authoring system for all unit models.
- Adding random trail variants.
- Adding weapon trail asset generation.
- Migrating every physical skill to trails in this PRD.
- Changing projectile skill behavior.
- Changing basic ranged attack behavior.
- Building catalog/XML validation tooling.

## Further Notes

This PRD intentionally keeps the first slice small: one catalog-driven path that
enables and disables an already attached `TrailRenderer`, validated on a
physical skill such as `Slash`. Once that works, later PRDs can expand the
visual operation model to prefab attachment, named sockets, multiple trail
channels, or animation-window timing.

The main architectural constraint is that weapon trail presentation must remain
additive over existing gameplay. The catalog decides optional presentation; XML
and `skillstrings` still decide which skills a unit owns.

## Implementation - 2026-06-13

### What Changed

- `SkillPresentationCatalog.cs` / `SkillPresentationEntry`: added `useTrail`, a
  bool Inspector field. Default `true` emits all child `TrailRenderer` weapon
  trails under the caster `TosterView`; set `false` to disable trails for a
  specific skill.
- `SkillPresentationCatalog.cs` / `SkillPresentationEntry`: added
  `weaponTrailDurationSeconds`, a seconds Inspector field for trail emission.
  Useful range is roughly `0.15-0.8`; lower values make quick slashes, higher
  values make heavier/longer smears. Values `<= 0` use the code default
  `0.35`. Start tuning at `0.35`.
- `SkillPresentationCatalog.cs` / `SkillPresentationEntry`: renamed
  `spellPresentationDelay` to `castVfxDelay` with `FormerlySerializedAs` so
  existing catalog values migrate safely. `castVfxDelay` controls when cast VFX
  and projectile release start; cast SFX and weapon trails start with the
  animation.
- `TosterView.cs`: added runtime lookup and timed playback for child
  `TrailRenderer` components. Matching is case-insensitive, includes inactive
  children, clears stale trail data, restarts repeated casts, and restores the
  original active/enabled state after the trail stops.
- `SkillPresentationManager.cs`: cast presentation now starts an optional weapon
  trail at the same moment as existing cast VFX/SFX. Missing configured trails
  are safe no-ops with one warning per missing key.
- Removed fields: none.

### Automatic Test

- No EditMode test file was added. The implemented behavior depends on Unity
  `TrailRenderer`, child GameObject activation, coroutines, catalog setup, and
  Play Mode timing rather than isolated pure C# logic.
- Automatic checks performed: targeted `rg` search for the new weapon trail
  symbols and simple brace-count checks on the three changed scripts. All checks
  passed.
- Tests were not run automatically. In Unity, open `Window > General > Test
  Runner`, choose `EditMode`, and run the existing test set if needed; no new
  PRD 014 test entry is expected.

### Unity Test

#### Unity Setup

- Open the `SkillPresentationCatalog` asset used by the scene
  `SkillPresentationManager`.
- On a physical skill entry such as `Slash`, keep `useTrail` enabled.
- Set `weaponTrailDurationSeconds` to a small value such as `0.35`.
- If cast VFX/projectile release should happen immediately, set that entry's
  `castVfxDelay` to `0`.
- Under the caster model/view hierarchy, create or select the weapon trail
  GameObjects and ensure each one has a configured `TrailRenderer` with material,
  width, and time values.

#### Play Mode Test

- Use the configured physical skill and confirm the weapon trail appears on the
  weapon/view.
- Confirm the trail disables after `weaponTrailDurationSeconds`.
- Cast the same skill repeatedly and confirm the trail does not stay permanently
  enabled.
- Temporarily test a caster with no child trail renderers and confirm the skill
  still completes, result reveal still plays, and warning spam does not repeat.
- Use one existing projectile skill and confirm projectile presentation still
  travels normally.
- Use one existing VFX-only or SFX-only skill and confirm old catalog behavior
  still works.

### QA Verdict

- Final QA verdict: Pass.
- QA report: `_codex/tasks/QA/2026-06-13_1254_014_QA_ArchitectureReview.md`.
- Actionable findings: none.
- Non-blocking observations: Unity compile and Play Mode validation were not
  run and remain manual.
- Follow-up fixes applied after QA: none needed.

### Notes

- No gameplay damage, cooldown, targeting, movement, turn, XML skill ownership,
  scene, prefab, material, Animator Controller, `.asset`, `.asmdef`, `.asmref`,
  or generated Unity file was changed.
- The first pass toggles an existing `TrailRenderer`; it does not spawn or
  attach trail prefabs at runtime.
- Trail visibility still depends on Unity-side `TrailRenderer` content setup:
  material, width, time, transform placement, and whether the caster model
  actually moves the trail object during the animation.

### Next Steps

- Let Unity compile the scripts in the open Editor.
- Configure one physical skill, preferably `Slash`, with `useTrail = true`,
  `weaponTrailDurationSeconds = 0.35`, and `castVfxDelay = 0` if you want cast
  VFX/projectile release from animation start.
- Run the Play Mode checklist above, prioritizing trail start timing, cleanup
  after repeated casts, missing-trail safety, and projectile-skill regression.

## Fix - 2026-06-13 - Multiple Weapon Trails

- Follow-up decision: a skill should trigger all weapon trail `TrailRenderer`
  components under the caster `TosterView`, not one named child.
- `useTrail` is the single catalog switch. `true` emits all child trail
  renderers during cast presentation; `false` disables trail playback.
- `TosterView.TryPlayWeaponTrails(...)` now finds all child `TrailRenderer`
  components, including inactive children, and starts each one for the configured
  duration.
- `SkillPresentationManager` now warns once if `useTrail` is enabled but no
  child trail renderers exist under the caster view.
- Unity setup update: for skills such as `Slash`, keep `useTrail` enabled, set
  `weaponTrailDurationSeconds`, and keep each weapon trail GameObject disabled
  by default if it should be hidden in idle.

## Fix - 2026-06-13 - Trail Timing

- Follow-up decision: weapon trails belong to the caster animation window, not
  the delayed spell release moment.
- `SkillPresentationManager.PlayCasterAnimationAndCast(...)` now starts weapon
  trails before waiting for `castVfxDelay`.
- Cast VFX still plays after `castVfxDelay`, preserving the existing spell
  visual release timing.
- Projectile travel still starts after cast presentation, so projectile skills
  such as `Axe_Rain` now run as animation + weapon trail first, then cast
  VFX/SFX and projectile launch after the configured delay.
- Direct `PlayCast(...)` calls without a caster animation still start weapon
  trails immediately before cast VFX/SFX.

## Fix - 2026-06-13 - Trail Lifetime In Animated Sequences

- Follow-up issue: after moving trail start to animation start, short trail
  durations could end at or before the delayed cast VFX/release moment.
- Animated skill sequences now use an effective trail duration that is at least
  the skill animation wait window and at least
  `castVfxDelay + weaponTrailDurationSeconds`.
- This keeps weapon trails alive through the animation/release window for
  projectile skills such as `Axe_Rain`, while direct non-animated `PlayCast(...)`
  still uses only `weaponTrailDurationSeconds`.

## Fix - 2026-06-13 - Cast SFX Timing And Cast VFX Delay Naming

- Follow-up decision: cast SFX belongs with caster animation start, while cast
  VFX and projectile release are the delayed spell-release moment.
- Renamed `spellPresentationDelay` to `castVfxDelay` in
  `SkillPresentationEntry`, preserving existing serialized values with
  `FormerlySerializedAs`.
- `SkillPresentationManager.PlayCasterAnimationAndCast(...)` now starts weapon
  trails and cast SFX before waiting for `castVfxDelay`.
- Cast VFX now plays after `castVfxDelay`; projectile travel still starts after
  cast VFX in projectile sequences.
- Direct non-animated `PlayCast(...)` still plays weapon trail, cast SFX, and
  cast VFX immediately.

## Fix - 2026-06-13 - Basic Attack Weapon Trails

- Basic melee attacks and counterattacks now use the same child
  `TrailRenderer` playback mechanism as skill weapon trails.
- `TosterHexUnit` starts all child weapon trails on the attacking unit when the
  combat animation state is `attack`.
- The attack trail duration uses the existing combat animation max wait window,
  so no new Inspector field was added for normal attacks.
- Ranged basic attack presentation remains owned by `SkillPresentationManager`
  and is unchanged.
