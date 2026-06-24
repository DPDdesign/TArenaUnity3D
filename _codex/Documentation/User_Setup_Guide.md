# TArenaUnity3D User Setup Guide

Status: active
Project: TArenaUnity3D
Last updated: 2026-06-24

Use this for Inspector wiring, scene setup, manual Unity checks, and repeatable
setup recipes discovered in this project.

Do not copy setup assumptions from another Unity project unless the user
explicitly asks for migration and the setup has been verified here.

## Offline Mode Reward Map Manual Check

Use this after PRD37 or PRD41 reward-flow changes.

### Unity Setup

1. Use the existing Offline Mode screen setup with `GameSceneManager`, Start
   Run, Run Map, tactical battle handoff, Reward Map, and Summary Value wired.
2. No new prefabs, scenes, GameObjects, components, or Inspector assignments are
   required by PRD41.
3. If the local Offline Mode database predates PRD37, rebuild or reset it before
   testing because PRD37 added run seed and materialized map/reward tables.
4. Keep Reward Map card button references assigned on the reward card views.
   The old separate select/continue reward command buttons are not part of the
   current runtime flow.

### EditMode Tests

Run these manually in Unity Test Runner:

- `PRD37MaterializedRunGenerationTests`
- `RunBattleServiceTests`
- `OfflineRunBattleRewardDbTests`
- `PRD41RewardValueParityTests`

Expected result: focused tests pass. These were documented by implementation
tasks but were not run automatically by Codex.

### Play Mode Smoke

1. Start an Offline run.
2. Travel to a normal battle node.
3. Complete the tactical battle with a win.
4. Confirm Reward Map opens instead of Run Map.
5. Confirm three materialized reward cards are visible or disabled according to
   legal target availability.
6. Hover a legal reward card and confirm the preview/focused summary updates.
7. Click a legal reward card.
8. Confirm the reward applies immediately and the UI returns to Run Map.
9. On a high-value army, compare Add Stack, More Units, Promote, and Downgrade
   deltas; Add Stack and More Units should not be tiny early-run values.
10. Complete a final battle with a win and confirm Summary Value opens.

## Combat SFX Setup

Combat SFX are script-driven and follow the existing combat animation states:
`attack`, `hit`, and `death`.

### Scene Setup

1. In the combat scene, create or reuse a GameObject named `AudioManager`.
2. Add an `AudioSource` component.
3. Add `CombatSfxManager`.
4. Assign the same `AudioSource` to the `CombatSfxManager` `Audio Source` field.
5. Keep the `AudioSource` `Spatial Blend` at `0` for global/2D combat SFX.
6. Disable `Play On Awake`; combat SFX are played through code with
   `PlayOneShot`.

`CombatSfxManager` is expected to be placed manually in the scene. If combat
tries to play assigned clips and no manager exists, the code logs one setup
warning.

### Unit Model Setup

1. Open the unit prefab/model hierarchy.
2. Find the child GameObject that owns the unit `Animator`.
3. Add `TosterSfxSet` to that same GameObject.
4. Assign clips to:
   - `Attack Sfx`
   - `Hit Sfx`
   - `Death Sfx`

Each field is an array. Add one clip for a fixed sound, or multiple clips for
random variants. Empty arrays are valid and remain silent.

Example hierarchy:

- `FireElemental`
- `GameObject`
- `MechaGolem_Rd`
- `Animator`
- `TosterSfxSet`

### Animator State Names

The runtime uses exact lowercase animator state names:

- basic attack: `attack`
- hit reaction: `hit`
- death: `death`
- defend: `defense`
- skill cast from slot 1: `skill1`
- skill cast from slot 2: `skill2`
- skill cast from slot 3: `skill3`
- skill cast from slot 4: `skill4`

Skill cast animations are slot-based, not skill-id-based. Example: if
`Fire_Ball` is stored in `Skill2` for a unit, the animator state must be
`skill2`.

By default skill presentation uses direct state playback for the caster
animation. For individual skills that need Animator transitions or several
chained animation states, set that skill entry's `Animation Play Path` to
`Trigger`. The trigger name is still the same lowercase slot name, for example
`skill2`.

### Manual Check

In Unity Play Mode:

1. Trigger a normal melee attack.
2. Confirm `attack` SFX plays at attack animation start.
3. Confirm `hit` SFX plays when positive damage lands and the target survives.
4. Confirm lethal damage plays `death` SFX with the death animation instead of
   also playing `hit`.
5. Confirm counterattacks play the same sequence for both combat directions.
6. Confirm overlapping events do not cut each other off.

## Background Music Setup

Background music uses a separate scene-level `AudioSource` from combat SFX.

### Scene Setup

1. In the combat scene, create a GameObject named `BackgroundMusicManager`, or
   add the component to an existing scene audio object.
2. Add an `AudioSource` component dedicated to music.
3. Add `BackgroundMusicManager`.
4. Assign the music `AudioSource` to the `BackgroundMusicManager`
   `Audio Source` field.
5. Assign the music clip to `Music Clip`, or assign it directly on the
   `AudioSource`.
6. Keep `Loop` enabled for normal background music.
7. Keep the music `AudioSource` separate from the `CombatSfxManager`
   `AudioSource`, so SFX `PlayOneShot` calls do not interrupt or replace the
   music clip.

### Manual Check

In Unity Play Mode:

1. Start the combat scene.
2. Confirm background music starts automatically and loops.
3. Trigger combat SFX.
4. Confirm combat SFX play over the music without stopping or changing the
   music track.

## Skill VFX/SFX Setup

Skill presentation is driven by one scene manager and one Inspector-authored
catalog asset. Do not edit `Units.xml`, prefabs, scenes, or generated files to
assign skill VFX/SFX data through code.

### Catalog Asset

1. In the Project window, create a catalog asset:
   `Create > TArena > Skill Presentation Catalog`.
2. Name it `SkillPresentationCatalog`.
3. Store it anywhere under `Assets/` that is convenient for authored content.
4. Open the asset in the Inspector.
5. Use `Entries` for XML skill ids.
6. Use `Default Basic Ranged Attack Entry` for ordinary ranged shots after a
   unit enters ranged stance.

Catalog `Entries` use the exact skill string from `Units.xml` /
`TosterHexUnit.skillstrings`. Example ids:

- `Slash`
- `Defence_Ritual`
- `Range_Stance_Barb`
- `Melee_Stance_Barb`
- `Range_Stance_Lizard`
- `Melee_Stance_Lizard`
- `Double_Throw`
- `Axe_Rain`
- `Fire_Ball`
- `Spike_Trap`
- `Stone_Throw`

The basic ranged attack is not an XML skill. Configure it only in
`Default Basic Ranged Attack Entry`.

### Scene Setup

1. In the combat scene, create or reuse a GameObject named
   `SkillPresentationManager`.
2. Add an `AudioSource`.
3. Add `SkillPresentationManager`.
4. Assign the catalog asset to `Catalog`.
5. Assign the same `AudioSource` to `Audio Source`.
6. Keep the `AudioSource` `Spatial Blend` at `0` for global/2D skill SFX.
7. Disable `Play On Awake`; skill SFX are played through code with
   `PlayOneShot`.

If the scene has no `SkillPresentationManager`, or the manager has no catalog or
audio source, the code logs one setup warning and continues without crashing.

### Entry Fields

For each skill entry:

- `Skill Id`: exact skill string, for example `Fire_Ball`.
- `Animation Play Path`: `PlayAnimation` directly plays the caster state;
  `Trigger` calls an Animator trigger with the same slot name.
- `Cast Vfx`: optional prefab spawned at the caster.
- `Cast Sfx`: optional clip played when the skill commits.
- `Projectile Vfx`: optional prefab moved from caster to target/hex.
- `Projectile Sfx`: optional clip played when projectile presentation starts.
- `Projectile Speed`: movement speed for controlled projectile VFX.
- `Impact Anchor`: where impact feedback resolves.
- `Target Reaction`: target animation played during result reveal. Use `Hit`
  for normal hits, `Buff` for positive effects, `Debuff` for negative effects,
  or `None` when the target should not react.
- `Impact Vfx`: optional prefab spawned at impact.
- `Impact Sfx`: optional clip played at impact.
- `Impact Delay Seconds`: delay for non-projectile impact feedback.
- `Effect Lifetime Seconds`: optional destroy delay for spawned cast/impact VFX.
- `Projectile Impact Delay Seconds`: optional delay after projectile arrival.

Use `0` lifetime when the prefab destroys itself. Use a positive lifetime for
simple one-shot prefabs that do not self-destroy.

### Impact Anchor Rules

- `TargetHex`: selected hex or projectile destination.
- `TargetUnit`: affected unit.
- `Caster`: acting unit.
- `AreaCenter`: selected/central AoE hex.

For AoE skills that affect units, use `TargetUnit`. The code now sends impact
VFX to each actually affected unit. If the AoE hits no unit, no impact fallback
is spawned on the selected hex. Use `AreaCenter` or `TargetHex` only for
location-only effects such as traps, summon points, terrain, or movement
destinations.

Recommended first entries:

- `Range_Stance_Barb`: `Caster`.
- `Melee_Stance_Barb`: `Caster`.
- `Range_Stance_Lizard`: `Caster`.
- `Melee_Stance_Lizard`: `Caster`.
- `Slash`: `TargetUnit`.
- `Defence_Ritual`: `TargetUnit`.
- `Fire_Ball`: `TargetUnit` with projectile VFX.
- `Axe_Rain`: `TargetUnit` with projectile VFX.
- `Double_Throw`: `TargetUnit` with projectile VFX.
- `Spike_Trap`: `TargetHex`.
- `Stone_Throw`: `TargetHex`.
- `Default Basic Ranged Attack Entry`: `TargetUnit` with projectile VFX.

### Manual Check

In Unity Play Mode:

1. Select a unit with a stance toggle and switch to ranged stance.
2. Confirm stance cast/impact feedback plays on the caster.
3. Perform a basic ranged attack.
4. Confirm the catalog projectile/impact feedback plays and the old Rigidbody
   projectile is not duplicated.
5. Cast one direct target skill, for example `Slash`, `Force_Pull`, or
   `Tough_Skin`.
6. Cast one AoE skill, for example `Defence_Ritual`, `Chope`, or `Insult`.
7. Confirm AoE impact VFX appears on each affected unit and does not appear on
   the selected hex when no unit is hit.
8. Cast one projectile skill, for example `Fire_Ball`, `Axe_Rain`, or
   `Double_Throw`.
9. Place one trap or use one summon/split skill, for example `Spike_Trap` or
   `Stone_Throw`.
10. Select one passive/info skill and confirm it does not play active cast
   feedback.
11. Remove one optional entry or clip and confirm missing presentation silently
    no-ops.
