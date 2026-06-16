# ADR 009: PRD035 Runtime Reroll Start Offers

Status: accepted
Date: 2026-06-16
Project: TArenaUnity3D

## Context

PRD 035 added generated starting armies and generated route previews for the
Run Metagame. The first implementation used a fixed seed so generated offers
were deterministic across repeated screen opens.

During playtest, the Start Run screen became too static: repeated Play Game
entries showed the same armies, and the early catalog could produce low-variety
offers such as repeated Wisps, Trappers, or Rushers.

## Decision

For now, the offline runtime Start Run composition creates a fresh PRD 035 seed
when a new Start Run offer session begins. This means entering the Start Run
screen through normal offline UI flow can show new generated starting armies and
route preview ids.

The current runtime offer session is shared by Start Run and Campaign Selection
so a selected generated army id still resolves while the player moves forward
through the PRD 019 screens.

Generated offers still become concrete when the player begins a run: the chosen
starting army snapshot, route choice, and run record are stored by the existing
offline persistence flow.

The deterministic generator config remains available for tests and explicit
tooling. Tests should call `StartingArmyGeneratorConfig.CreateDefault()` when
stable repeatable output is required.

## Rationale

- Current goal is fast local playtest variety.
- The offline Start Run screen is currently the place where the player compares
  generated offers.
- Persisting the selected run state is more important now than preserving the
  unselected offer list across screen entries.
- The generator needs deterministic mode for regression tests, QA, and future
  known-seed work.

## Future Revision Trigger

Revisit this when one of these becomes product truth:

- generated offers must be replayable from a visible known seed,
- unselected offers must persist across menu exits,
- campaign selection owns run seed selection,
- online authority or analytics must validate generated offers,
- reroll tokens become the only allowed way to change offers.

## Consequences

- Playtesters see fresh armies each new Start Run screen entry.
- The same player action can produce different unselected offer lists between
  visits until the run starts.
- Existing deterministic tests stay stable by using explicit default configs.
- Future online or analytics work should not rely on this runtime-random seed as
  final authority.
