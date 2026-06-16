# ADR 007: Fire_Movement Trap Presentation Consistency

Status: accepted
Date: 2026-06-13
Project: TArenaUnity3D

## Context

`Fire_Movement` leaves behind gameplay trap state named `Fire_Trap`.

After the trap spawn-model rework, persistent board-state visuals are resolved
through `SkillPresentationCatalog` by skill id. That created an inconsistency:

- gameplay trap identity for the fire trail is `Fire_Trap`,
- temporary placement VFX already plays through `Fire_Movement`,
- persistent trap model lookup in `Traps.ShowTrap()` used the gameplay trap name,
- the catalog entry that designers will naturally tune for this passive is
  `Fire_Movement`, not a separate hidden `Fire_Trap` catalog entry.

This split is tolerable as a legacy fallback, but it is the wrong long-term
flow for catalog-authored presentation.

## Decision

Keep gameplay trap ownership and trigger rules on `Fire_Trap`, but allow trap
state to carry a separate presentation skill id.

For fire trail traps, the flow is now:

- gameplay trap name: `Fire_Trap`,
- presentation skill id: `Fire_Movement`,
- temporary placement effect: `Fire_Movement`,
- persistent spawned model: resolved from `Fire_Movement`.

If no spawn model is assigned on the presentation entry, runtime still falls
back to the legacy `Fire_Trap` child object on the hex.

## Rationale

- It preserves current gameplay naming and trigger logic.
- It removes the catalog inconsistency between placement VFX and persistent
  model authoring.
- It avoids inventing a second hidden catalog entry just to compensate for a
  gameplay/presentation naming mismatch.
- It generalizes cleanly for future cases where one gameplay state object is
  created by a differently named skill.

## Architectural Rule

When a lasting board-state object is created by a skill, presentation should be
authored under the creating skill's catalog entry unless there is a deliberate,
documented reason to split it.

Gameplay identifiers may remain domain-specific, but presentation lookup should
be explicit rather than inferred from gameplay state names whenever those names
diverge.

## Consequences

- `HexClass.AddTrap(...)` and `Traps.Traps` now support an optional presentation
  skill id.
- `Fire_Movement` uses that explicit presentation id when leaving a `Fire_Trap`
  behind.
- Unity setup for the persistent fire trail model belongs on the
  `Fire_Movement` entry's `Spawn Model` field, not on a separate `Fire_Trap`
  entry.
