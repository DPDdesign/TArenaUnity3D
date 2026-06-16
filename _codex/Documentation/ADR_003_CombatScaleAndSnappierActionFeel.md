# ADR 003: Combat Scale And Snappier Action Feel

Status: design direction recorded
Date: 2026-06-13
Project: TArenaUnity3D

## Context

Recent play observation from the project director:

- the current map feels somewhat too large,
- the game looks better when there are fewer units concentrated in the center,
- animations feel too slow,
- spells should enter their effect faster from animation,
- the current default animation transition time around `0.7` is too high for
  spell entry feel.

This is a feel and readability problem before it is a balance problem. The
current combat presentation reads better when the player sees fewer simultaneous
actors competing for attention and when actions resolve quickly enough to keep
the tactical board moving.

## Decision

Treat TArena combat as needing a tighter, more readable tactical scale and a
snappier action cadence.

Future correction work should explore:

- reducing effective map scale or playable engagement distance,
- reducing early/central unit crowding,
- making animation presentation faster where it currently delays readability,
- shortening spell entry transitions so cast intent reaches the player sooner.

Do not silently change gameplay float values, serialized fields, animation
controllers, prefabs, scenes, or asset settings while only documenting this
direction. Implementation requires an explicit follow-up task.

## Rationale

TArena's current tactical readability depends on the player quickly parsing:

- which unit is acting,
- where attention should be focused,
- when a skill has committed,
- when the result has landed,
- what changed on the board.

Oversized arenas and crowded centers dilute attention. Slow animation timing and
long spell-entry transitions delay the moment where the player understands the
committed action. A faster, tighter feel should make the same underlying rules
read more clearly without requiring immediate combat-system changes.

## Implementation Guidance

When this becomes an implementation task, keep changes small and separately
verifiable:

1. Map scale / engagement readability pass.
2. Unit-count or spawn-density review for center fights.
3. Animation speed and transition audit.
4. Spell entry transition audit, including defaults near `0.7`.

Each pass should record before/after observations from Unity playtesting.

## Boundaries

This ADR does not approve:

- changing damage, cooldowns, targeting, movement, initiative, or turn rules,
- renaming public or serialized fields,
- editing Unity assets, prefabs, scenes, materials, controllers, or generated
  Unity files without explicit permission,
- treating faster feel as a reason to rewrite combat flow.

## Follow-up Rule

Create a small implementation task before changing values. The first task should
name the exact map, animation controller, transition, or spawn/unit-count source
being changed and define a Unity-side verification step.
