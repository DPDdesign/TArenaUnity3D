# ADR 010: Run Metagame GameSceneManager Owns Screen Flow

Status: accepted
Date: 2026-06-16
Project: TArenaUnity3D

## Context

The PRD019 Run Metagame screens are built as separate UI prefabs and the current
Start Run flow creates an offline run but does not switch to the Run Map screen.

We need one small owner for Run Metagame navigation so individual screen
controllers do not each decide which UI roots should be active.

## Decision

Create a `GameSceneManager` for the Run Metagame flow.

For the current MVP it owns:

- a persistent singleton lifetime,
- the current Run Metagame screen enum,
- a default starting screen,
- serialized references to static UI screen roots,
- simple `Show...` methods that activate exactly one UI root,
- Battle entry/return hooks, with Battle treated as a separate Unity scene.

The static Run Metagame UI shell remains loaded in the background for now.
Entering Battle hides the main UI root. Returning from Battle restores the
previous UI screen or the configured default screen.

Gameplay state remains in the offline database and screen services. The manager
does not store armies, rewards, gold, route nodes, or other gameplay data.

## Rationale

- Screen controllers can report player intent without owning global navigation.
- The first implementation only needs GameObject activation, not a larger app
  state machine.
- A default screen enum avoids wiring the same root GameObject twice.
- Keeping Battle as a separate Unity scene leaves room for later performance and
  loading-state decisions.

## Future Revision Trigger

Revisit this when:

- loading screens become visible product behavior,
- the UI shell should unload during Battle,
- Battle Result moves into the Battle scene,
- resume-run flow needs to choose a screen from persisted `next_screen`,
- multiple run slots or explicit run selection become product truth.

## Consequences

- `GameSceneManager` is the single Run Metagame screen-flow owner.
- Screens are still responsible for their own rendering and service calls.
- MVP screen transitions are immediate after successful actions.
- Unity scene/prefab wiring is still manual in the Inspector.
