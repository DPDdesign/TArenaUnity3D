# ADR 008: PRD035 Uncertain Generator Decisions Are Implementation-Allowed

Status: accepted
Date: 2026-06-16
Project: TArenaUnity3D

## Context

PRD 035 defines deterministic random starting armies and route maps for the
Run Metagame.

Several PRD 035 areas are intentionally uncertain because the current code and
Offline DB schema do not yet contain the full target domain:

- seed storage and schema migration shape,
- generator-vs-catalog ownership boundary,
- route generation dependency on selected army composition,
- known-seed offline run eligibility for final saved offence/defence armies,
- generated offer history for future balance analysis and data science,
- campaign, mission, random event, empty node, and route seed persistence.

Blocking implementation on all of these details would slow the tracer bullet.
At the same time, silently turning uncertain product rules into permanent
architecture would make future correction harder.

## Decision

PRD 035 implementation may include uncertain areas when needed to deliver a
working vertical slice.

Those decisions must be treated as provisional and documented at the point of
implementation. The implementation note should state:

- which uncertain PRD 035 area was implemented,
- why the choice was needed now,
- what would trigger a later revision,
- whether the behavior is temporary, schema-backed, or persisted as current
  product truth.

This ADR explicitly allows implementation work for:

- explicit run seed storage or a versioned schema migration,
- generator-backed services behind existing Start Run and Run Map interfaces,
- route generation that uses campaign, mission, risk, or selected army context,
- known-seed offline run saved-army eligibility flags or blocking rules,
- generated offer history if the table/migration is explicit,
- route node/schema support for campaigns, missions, random events, empty
  nodes, and seeded deterministic generation.

## Rationale

- PRD 035 is a gameplay-production feature, not only a data model cleanup.
- The first implementation needs a deterministic end-to-end flow that can be
  exercised from Start Run into Run Map.
- Some uncertainty can only be resolved responsibly while adapting the current
  services and Offline DB schema.
- Marking these choices as provisional preserves speed without pretending the
  design is final.

## Constraints

- Do not change unit stats, gameplay floats, cooldowns, skill execution, or
  damage formulas without explicit permission.
- Do not edit Unity scenes, prefabs, materials, controllers, `.inputactions`,
  `.asmdef`, or `.asmref` without explicit permission.
- Schema changes must include an explicit migration/version plan.
- UI controllers should continue to consume services/view models rather than
  query SQLite or generators directly.
- Persisted behavior must be deterministic from the run seed and catalog data.
- Any known-seed saved-army rule must be visible in domain/service behavior,
  not hidden as a UI-only restriction.

## Consequences

- PRD 035 implementation agents can move forward without another grilling
  round for the listed uncertain items.
- Follow-up ADRs may replace or narrow these provisional decisions when
  campaign, online authority, or analytics requirements become concrete.
- Task completion notes for PRD 035 should call out every provisional decision
  taken under this ADR.
