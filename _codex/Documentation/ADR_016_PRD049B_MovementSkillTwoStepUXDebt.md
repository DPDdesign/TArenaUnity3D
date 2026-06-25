# ADR 016: PRD049AC Movement Skills Keep Movement-First Targeting For Now

Status: accepted direction
Date: 2026-06-25
Project: TArenaUnity3D

## Context

PRD 049AC maps active skills into target families and effect data before the
full PRD49 execution migration.

Several movement skills currently use a movement-first target flow:

- `Slash`: choose movement destination, then choose impact/direction.
- `Heavy_Fists`: choose movement destination, then choose impact/direction.
- `Toxic_Fume`: choose movement destination, then derive the area around the
  caster automatically.

This matches existing code for `Slash` and `Heavy_Fists`, where the first
selection is a legal movement destination. It also matches the accepted 049AC
direction for `Toxic_Fume`, where the only selected target is the movement
destination. However, this flow can be awkward for players because the player
may think in terms of the intended enemy, cone, or area outcome rather than the
intermediate destination.

## Decision

For PRD 049AC, movement skills are mapped with the first selected target as the
legal movement destination.

The target family map should keep these skills in the `Movement` family with
tags such as:

- `ActorMove`
- `MoveThenHit`
- `MoveThenArea`
- `PathRequired`
- `EmptyDestination`

This is a migration mapping decision, not a final UX endorsement.

## Rationale

Keeping movement destination as the first target preserves current behavior and
keeps the validator model simple for the initial PRD49 migration.

It also avoids hiding movement legality inside auto-pathing or auto-approach
logic before the shared validator, preview, and execution model are stable.

## UX Debt

The movement-first flow is marked as UX debt.

Future PRD49 follow-up work may explore a friendlier targeting mode where the
player chooses the intended enemy, direction, or area first, and the system
derives or suggests legal movement destinations.

Examples:

- choose enemy first, then show possible approach hexes,
- choose cone/area first, then show valid caster destinations,
- preview final affected units before committing the movement step.

Any later UX improvement must still validate through the shared tactical action
validator and must not make `CastManager` or live scene objects the authority.

## Consequences

- PRD 049AC can classify movement skills without inventing per-skill target
  families.
- PRD 049A/049AC/049E can preserve current behavior while keeping room for a
  later targeting UX pass.
- Future UI work should treat movement-first targeting as provisional, not as
  the desired final player experience.

## Related PRDs

- `_codex/tasks/049_PRD_TacticalActionSkillMigrationProgram.md`
- `_codex/tasks/049A_PRD_TacticalActionValidationSO_UI_VerticalSlice.md`
- `_codex/tasks/049AC_PRD_SkillTargetAndEffectDataModel.md`
- `_codex/tasks/049E_PRD_SODrivenTacticalActionExecutionMigration.md`
