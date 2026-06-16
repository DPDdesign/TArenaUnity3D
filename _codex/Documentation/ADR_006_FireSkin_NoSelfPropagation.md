# ADR 006: Fire Skin Does Not Self-Propagate

Status: accepted direction
Date: 2026-06-13
Project: TArenaUnity3D

## Context

`Fire_Skin` is a passive/end-turn style effect. Its intended feel is good:
the source unit becomes threatening to stand near, and nearby enemies receive a
burning or heat-pressure style debuff.

During bug diagnosis, the current implementation was found to reapply
`"Fire_Skin"` itself onto affected enemy units. That caused a debuffed enemy to
later resolve the same `Fire_Skin` branch as if it were a true source owner.
Player-facing result: the effect appeared to spread by itself from unit to
unit.

That behavior is mechanically interesting, but it is not currently authored as
a named contagion mechanic, a faction identity rule, or a scoped PRD feature.
In the current battle language, `Fire_Skin` reads as an aura-like passive from
the source unit, not as a self-propagating chain effect.

## Decision

`Fire_Skin` should not self-propagate through debuffed target units.

Current intended rule:

```text
Source unit owns Fire_Skin
-> nearby enemy receives Fire_Skin debuff
-> debuffed enemy suffers the debuff only
-> debuffed enemy does not become a new Fire_Skin source by default
```

The same principle applies to similar passive aura/debuff effects unless a
future mechanic explicitly defines propagation.

## Rationale

This keeps the mechanic readable and production-safe:

- The source of the effect remains visually and tactically legible.
- The player can reason about danger radius from the true owner.
- Passive/deferred effects do not silently create chain reactions outside a
  dedicated mechanic definition.
- Balance and AI evaluation remain closer to the authored passive intent.

The self-propagating version is a valid future design idea, but it is a
different mechanic. It would need explicit naming, authored boundaries, and
verification as a contagion/spread system rather than appearing accidentally as
an implementation side effect.

## Future Design Direction

If propagation is explored later, it should be treated as a new mechanic with a
separate PRD or ADR. That future design should answer:

- Which effects are allowed to spread?
- Does spread continue indefinitely, by depth, by duration, or by owner rules?
- Does the new target become a full source or only pass along a weaker version?
- How is source ownership represented in VFX, UX, AI evaluation, and combat
  logs?
- How is chain spread prevented from becoming accidental board-wide explosion?

## Boundaries

This ADR does not approve:

- adding a contagion or chain-aura system now,
- changing `Fire_Skin` balance values as part of this decision,
- reworking all passive/deferred effects under one generalized spread model,
- editing Unity assets, prefabs, scenes, materials, Animator Controllers,
  `.inputactions`, generated Unity files, `.asmdef`, or `.asmref`.

## Verification Direction

Manual verification for the current rule:

- a real `Fire_Skin` owner should debuff nearby enemies,
- a debuffed enemy should not later cast or emit `Fire_Skin` as a new source,
- repeated end-turn processing should preserve the original source ownership of
  the effect.
