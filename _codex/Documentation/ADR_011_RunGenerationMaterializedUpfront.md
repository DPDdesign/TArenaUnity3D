# ADR 011: Run Generation Is Materialized Upfront

Status: accepted
Date: 2026-06-17
Project: TArenaUnity3D

## Context

PRD019/PRD030 and PRD035 define an Offline Mode run that is built from
generated starting run state, route nodes, rewards, and future event content.

The earlier vertical slices still left some generation behavior at screen time:
Reward Map could select cards from a fallback authored catalog, and generated
content could be recreated by service calls if no persisted runtime row existed.
That makes reload behavior harder to reason about and blurs the ownership of a
run.

## Decision

A run is generated deterministically from the run seed and then materialized
upfront when the run begins.

This applies to run-owned generated content, including:

- selected starting army snapshot and run assets,
- route map, paths, and nodes,
- reward choices for reward-producing nodes,
- future event choices and generated event payloads,
- future generated enemy or encounter-owned armies when those systems exist.

Generated runtime content must be stored in minimal DB tables keyed by `run_id`
and the relevant runtime node/catalog position identity. Runtime screens should
load the materialized rows for the current run/node instead of rolling new
content or falling back to hardcoded catalogs.

For rewards, the Reward Map screen must not generate the reward choice at screen
time. It loads the materialized reward choice for the current run/node, previews
cards on hover, applies the clicked card, and then returns to Run Map through
`GameSceneManager.ShowRunMap()`.

Catalog/ruleset data remains source configuration. The database stores runtime
materialized results and references such as reward id, run id, node id, catalog
position id, generated card type, operation payload, selected/applied state,
and resulting snapshot references.

## Rationale

- Reloading a run should never reroll armies, rewards, nodes, or events.
- Debugging is simpler when the complete generated run exists as data.
- Reward Map, Run Map, and future event screens stay display/apply surfaces
  rather than generator owners.
- Future online authority can validate or supply generated results at the same
  seam: the client displays materialized run content, not trusted client RNG.
- Minimal runtime tables preserve PRD030's rule that authored/catalog truth does
  not become SQLite-owned truth.

## Constraints

- Generation must be deterministic from the run seed and catalog/ruleset data.
- Runtime screens must not create fallback generated content when materialized
  rows are missing; missing rows are errors or explicit dev/test setup gaps.
- Do not silently overload unrelated DB fields to store generated content.
- Add schema changes through explicit migration/version planning.
- UI controllers must load generated run content through adapters/services and
  DB stores, not direct SQLite queries.
- Online mode must not trust Unity-client RNG as authoritative generated state.

## Consequences

- Existing screen-time fallback reward catalogs should be removed from the
  production Reward Map path.
- Reward generation becomes part of Begin Run/run generation persistence work.
- Reward Map tests should verify loading and applying materialized choices, not
  creating fresh choices at screen open.
- Future event/shop/encounter generation should follow the same upfront
  materialization rule.
