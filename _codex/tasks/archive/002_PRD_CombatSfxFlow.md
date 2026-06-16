# [TARENA] PRD: Combat SFX Flow

## Problem Statement

Combat has a code-driven animation flow for `attack`, `hit`, and `death`, but
gameplay scripts do not currently provide a project-owned way to play combat
SFX. The project already contains audio assets, but there is no script-level
mechanism that connects per-unit SFX clips to combat presentation.

The desired setup is to attach SFX data to the visual model that owns the
Animator, while keeping actual audio playback centralized through one scene
audio manager.

## Solution

Add a small combat SFX presentation path:

1. Each unit model can define its own combat SFX clips on the same GameObject as
   its Animator.
2. A single scene-level combat SFX manager owns one AudioSource.
3. Combat code asks the unit view to play `attack`, `hit`, or `death` SFX at the
   same moments it already drives combat animations.
4. The manager plays clips with `AudioSource.PlayOneShot`, allowing multiple SFX
   to overlap on the same AudioSource.
5. Missing per-unit clips are silent. Missing scene manager is a setup problem
   and should log a warning once.

## User Stories

1. As a player, I want attacks to have SFX, so that combat actions feel more readable.
2. As a player, I want hit reactions to have SFX, so that damage impact is clear.
3. As a player, I want deaths to have distinct SFX, so that lethal hits are easy to notice.
4. As a player, I want several combat sounds to overlap, so that multiple events do not cut each other off.
5. As a content author, I want to assign SFX on the model GameObject that owns the Animator, so that audio setup lives near the matching visual setup.
6. As a content author, I want each unit model to have its own attack, hit, and death clip lists, so that different units can sound different.
7. As a content author, I want clip lists instead of single clips, so that repeated combat events can use random variants.
8. As a developer, I want one scene-level audio playback point, so that combat SFX output is easy to configure and debug.
9. As a developer, I want missing unit clips to fail silently, so that incomplete content does not spam the console during iteration.
10. As a developer, I want missing scene audio setup to warn once, so that scene configuration mistakes are visible without log spam.
11. As a developer, I want combat SFX calls next to existing combat animation calls, so that audio timing follows the established presentation flow.
12. As a developer, I want the first implementation to stay 2D/global, so that it does not require per-unit AudioSources or spatial audio setup.

## Implementation Decisions

- Add a per-model SFX definition component with random clip arrays for combat
  events.
- Place the per-model SFX component on the same GameObject as the unit Animator,
  not on the dynamically instantiated TosterView parent.
- Keep the first event set limited to `attack`, `hit`, and `death`.
- Use array fields for each event type, even when a unit initially has only one
  clip for that event.
- Use a scene-level combat SFX manager with one AudioSource.
- The combat SFX manager is manually placed in the Unity scene; it should not be
  auto-created by code.
- Use `AudioSource.PlayOneShot` so multiple combat SFX can overlap on the same
  AudioSource.
- Keep SFX global/2D for the first implementation.
- Do not use `Resources.Load` for SFX clips. Clips are assigned through Unity
  Inspector references.
- TosterView discovers the per-model SFX definition in its children and exposes
  simple methods for combat code to request attack, hit, and death SFX.
- Attack SFX plays at the start of the attack animation.
- Hit SFX plays only when positive damage is applied and the target survives.
- Death SFX plays with the death animation, not directly from generic death
  state mutation.
- Lethal damage plays death SFX instead of hit SFX. It should not play both.
- Damage values of zero or less should not trigger hit or death SFX.
- Missing clip arrays or null clips are silent fallback cases.
- Missing combat SFX manager logs a warning once, because that indicates scene
  setup is incomplete.
- Keep skill cast, projectile launch, projectile impact, UI audio, movement SFX,
  footsteps, music, audio mixer setup, and 3D positional audio out of scope for
  this PRD.

## Testing Decisions

- The main validation is Unity Play Mode manual testing in a combat scene.
- A successful normal melee attack plays attack SFX from the attacker at attack
  animation start, then hit SFX from the defender when damage is applied and the
  defender survives.
- A successful lethal melee attack plays attack SFX from the attacker, then
  death SFX from the defender with the death animation.
- A successful counterattack sequence plays attack and hit/death SFX for both
  combat directions in the existing combat presentation order.
- Multiple simultaneous or near-simultaneous SFX should overlap instead of
  cutting each other off.
- Units without assigned clips should produce no SFX and no warning.
- A scene without the combat SFX manager should produce a single setup warning.
- Testing should include at least one unit prefab/model where the SFX component
  is placed on the same GameObject as the Animator and TosterView exists higher
  in the instantiated hierarchy.
- No command-line Unity build or test run is required for this PRD. The user
  compiles and tests in Unity.

## Out of Scope

- Editing prefabs, scenes, Animator Controllers, animation clips, materials, or
  generated Unity asset files as part of the PRD itself.
- Implementing skill, projectile, movement, UI, or music audio.
- Adding positional 3D audio or a pool of AudioSources.
- Building a full audio mixer, volume settings UI, or persistent audio settings.
- Auto-creating scene manager objects at runtime.
- Changing damage math, combat turn rules, animation state names, or gameplay
  float values.

## Further Notes

This PRD extends the current code-driven combat presentation model. The SFX data
lives with the model/Animator, while playback stays centralized at scene level.
That keeps authoring intuitive in Unity and avoids adding AudioSources to every
instantiated unit for the first implementation.

Minimal model:

- CombatSfxManager
- one AudioSource
- PlayRandom(AudioClip[] clips)

- TosterSfxSet
- attackSfx[]
- hitSfx[]
- deathSfx[]

- TosterView
- finds TosterSfxSet in children
- PlayAttackSfx()
- PlayHitSfx()
- PlayDeathSfx()

Example:

- `MechaGolem_Rd`
- Animator
- TosterSfxSet
- attackSfx = metal swing variants
- hitSfx = metal impact variants
- deathSfx = heavy collapse variants
