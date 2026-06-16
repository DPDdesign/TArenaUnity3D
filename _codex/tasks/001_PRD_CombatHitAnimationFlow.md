# [TARENA] PRD: Combat Hit Animation Flow

## Problem Statement

Combat currently triggers attack animations from code, but there is no explicit
`hit` animation when a TosterHexUnit receives damage. The melee attack flow also
needs to be visually ordered so the player can read the exchange: attacker
attacks, defender reacts, defender counterattacks when available, and the
original attacker reacts.

## Solution

Add a code-driven `hit` animation for TosterHexUnit damage and make melee combat
animation calls follow this order:

1. Attacker plays `attack`.
2. Defender receives damage and plays `hit` around the midpoint of the attack
   animation, or `death` instead of `hit` when the damage kills the unit.
3. Defender plays `attack` when a counterattack is available.
4. Attacker receives counterattack damage and plays `hit` around the midpoint of
   the counterattack animation, or `death` instead of `hit` when the damage kills
   the unit.
5. Each `attack` and `hit` animation returns to the Animator Entry/default state
   after the played state finishes. `death` does not return to default.

## User Stories

1. As a player, I want the attacking unit to play `attack`, so that I can see who initiated combat.
2. As a player, I want the defending unit to play `hit` when it takes damage, so that damage has a visible reaction.
3. As a player, I want a counterattacking defender to play `attack`, so that the retaliation is readable.
4. As a player, I want the original attacker to play `hit` after counterattack damage, so that the counterattack impact is visible.
5. As a content author, I want animation state names to match the Animator exactly, so that changing clips in Unity does not require broader gameplay changes.
6. As a developer, I want hit reactions attached to the damage path, so that normal attacks, ranged attacks, and skills can share the same behavior where they use the same damage function.

## Implementation Decisions

- Use Animator state names `walk`, `attack`, `hit`, and `death` as code-facing
  combat animation names.
- Trigger `hit` from the real damage path when positive damage is applied.
- Trigger `death` instead of `hit` when the positive damage makes the unit die.
- Do not reset a dead unit back to default after `death`; keep death as the final
  visual state for that unit.
- After `death`, hold the final pose from code so Animator Controller transitions
  on individual unit assets cannot return dead units to default/idle.
- Lock the animated root transform during combat/death playback so imported
  clips cannot drift a unit away from its hex position.
- Return combat animations to the Animator Entry/default state with the same
  controller reset approach used after movement.
- Time melee hit reactions from attack animation progress, currently at roughly
  50% of the `attack` state, with a short fallback timeout.
- Keep the existing damage calculation, chat messages, counterattack availability, and unit loss logic unchanged.
- Keep facing updates code-driven before attack animations so combatants still turn toward their targets.
- Do not edit Animator Controllers, prefabs, scenes, materials, generated Unity files, or serialized Unity assets as part of this PRD.

## Testing Decisions

- The primary manual test is a Unity Play Mode melee attack where the defender can counterattack.
- A successful test shows the visual order: attacker `attack`, defender `hit`
  near mid-swing, defender `attack`, attacker `hit` near mid-swing.
- A lethal hit should play `death` instead of `hit`.
- After each `attack` or `hit`, the unit should return to its Animator default
  state instead of staying in the combat state.
- After `death`, the unit should not return to default/idle.
- Test more than one unit family because some imported Animator Controllers have
  different death state and transition setup.
- Include Rusher/Kobold Knight in manual testing because its imported animation
  clips can visibly drift the model if root transform motion is not locked.
- Also test a melee attack where counterattack is unavailable; the expected visual order is attacker `attack`, defender `hit`.
- Also test any direct-damage skill or ranged attack using the same damage path; the damaged unit should play `hit`.
- Follow-up testing should cover ranged attacks and skills once they are moved
  from immediate `hit` playback to a sequenced presentation flow.
- No automated Unity test is required for this PRD unless a deterministic combat animation seam is introduced later.

## Out of Scope

- Editing Animator Controllers or animation clips.
- Full sequenced presentation for ranged attacks and skills. They should be
  handled in a later pass using the same principle as melee combat.
- Reworking combat into a full animation timeline or event queue.
- Renaming serialized fields or changing gameplay float/stat values.
- Changing death, counterattack eligibility, or damage math.

## Further Notes

This PRD captures the current code-driven approach. Ranged attacks and skills
also need sequenced animation presentation, but they are intentionally deferred
until the melee flow is validated in Unity.
