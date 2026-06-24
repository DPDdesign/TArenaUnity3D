# ADR 014: Tactical AI Validation Stays Multiplayer-Compatible

Status: accepted direction
Date: 2026-06-24
Project: TArenaUnity3D

## Context

PRD046 introduces Tactical Battle AI that selects actions from a pure
`BattleSnapshot`, may use cached or background plans, and executes through the
current tactical scene.

The same battle action shape is expected to matter later for replay and online
sync. A selected AI action must therefore not become a special trusted path that
can bypass the live legality checks a player or future network command would
need to pass.

Current validation is still legacy-owned and distributed across
`MouseControler`, `CastManager`, `TosterHexUnit`, `TeamClass`, and the battle
action lifecycle. ADR005 keeps the full action validator as future work.

## Decision

Tactical AI V1 must treat every selected action intent as untrusted until it is
revalidated against the current live battle state.

This applies to:

- best search result,
- cached plan result,
- background-planned result,
- greedy fallback result,
- defend/wait fallback result,
- selected skill intent.

The V1 bridge may use existing legacy validation adapters, but the boundary
must be shaped like the future multiplayer action validation seam:

```text
Action intent
-> current live revalidation adapter
-> BattleActionLifecycle execution
```

Future player input, AI decisions, replay commands, and network commands should
converge on one validated action shape before execution.

## Rationale

This keeps Tactical AI fair and prevents a private AI execution path from
becoming incompatible with replay or online authority.

It also lets PRD046 improve enemy decision-making before the full validation
module exists, while preserving the direction from ADR005.

## Consequences

- AI execution cannot directly mutate tactical scene objects.
- Cache hits never bypass revalidation.
- Fallbacks never bypass revalidation.
- Live execution remains authoritative when snapshot planning and current scene
  state disagree.
- PRD046 snapshot and intent models should avoid Unity object references and be
  suitable for future replay/online sync.
- A future multiplayer/action-validation PRD must replace the legacy adapter
  boundary with one explicit validation module.

## Boundaries

This ADR does not approve:

- building the full multiplayer system now,
- building the full action validation module now,
- changing damage, cooldowns, targeting, movement, initiative, or turn rules,
- bypassing `MouseControler`, `CastManager`, or `BattleActionLifecycle`,
- editing Unity assets, prefabs, scenes, materials, Animator Controllers,
  `.inputactions`, generated Unity files, `.asmdef`, or `.asmref`.

## Verification Direction

PRD046 implementation tasks should verify that:

- selected AI intents pass through live revalidation before execution,
- cached intents are rejected when snapshot/profile/active unit state changed,
- fallback intents pass through the same revalidation path,
- stale actor, target, destination, cooldown, or lifecycle-busy state rejects
  safely,
- the intent shape contains enough stable ids to be reusable by future replay or
  network command validation.
