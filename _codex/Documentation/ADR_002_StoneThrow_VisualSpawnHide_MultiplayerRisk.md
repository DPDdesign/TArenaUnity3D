# ADR 002: Stone_Throw Visual Spawn Hide Multiplayer Risk

Status: temporary local workaround
Date: 2026-06-11
Project: TArenaUnity3D

## Context

`Stone_Throw` now presents as:

- projectile travel to the chosen hex,
- impact / explosion on that hex,
- visible appearance of the split unit after the impact.

The backend unit is still instantiated immediately, because gameplay state,
damage application, and occupancy resolution already depend on the spawned unit
existing during the skill flow.

To preserve the requested local visual order without changing backend timing,
the current implementation hides the spawned unit's renderers until the impact
callback fires, then shows them before the result reveal.

## Decision

Keep the renderer-hide approach as a narrow local presentation workaround for
the current recovery phase.

## Rationale

- It keeps the backend spawn timing unchanged.
- It avoids a larger refactor of split/summon flow during a small skill task.
- It matches the requested player-facing sequence in local play.

## Multiplayer Risk

This pattern is not a safe long-term multiplayer solution:

- renderer visibility is only a visual state,
- visual hide/show is easier to desync than explicit replicated spawn/reveal
  state,
- future network restoration may need a dedicated presentation event or synced
  reveal state so every client shows the same spawn moment.

## Follow-up Rule

If multiplayer or deterministic replay work resumes, audit `Stone_Throw` first
and replace visual-only hiding with an explicit synced spawn/reveal path.
