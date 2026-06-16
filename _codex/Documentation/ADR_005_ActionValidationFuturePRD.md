# ADR 005: Action Validation Is A Future PRD

Status: accepted direction, future PRD required
Date: 2026-06-13
Project: TArenaUnity3D

## Context

ADR 004 accepts a future **Battle Action Lifecycle Module** as the seam that
owns turn safety and action completion. During design grilling, action
validation was identified as a related but separate architectural concern.

Current validation is not centralized. It is spread across legacy call sites:

- `MouseControler` checks input state, highlights, selected unit, active skill
  mode, and clicked hex conditions.
- `CastManager` skill mode methods configure targeting flags, cooldowns,
  availability, AoE, self-cast, movement, and range behavior.
- Individual `CastManager` skill methods often perform their own target or
  highlight checks before resolving.
- `TosterHexUnit` owns movement/path availability checks such as
  `IsPathAvaible(...)`.
- `TeamClass` owns turn eligibility checks for dead, zero-amount, or moved
  units.

The lifecycle migration needs to accept actions that are legal under the
current game rules, but replacing every legacy validation rule at the same time
would combine two large refactors: action sequencing and rules validation.

## Decision

The Battle Action Lifecycle migration should not build a full new action
validation layer.

For the lifecycle work, `ValidatedAction` means:

```text
An action intent that passed the existing legacy validation checks closely
enough to be executed by the lifecycle.
```

This is an adapter contract, not the final rules architecture.

Future work should create a separate PRD for a dedicated **Action Validation
Module**. That PRD should define one explicit seam for validating player, AI,
passive, and future network action intents before they become executable
actions.

## Rationale

Keeping validation separate preserves scope control:

- ADR 004 can focus on sequencing, completion, blocking presentation, automatic
  actions, and turn safety.
- A future validation PRD can focus on legal action rules, target rules,
  cooldown rules, path rules, passive trigger rules, and multiplayer authority.

This avoids changing gameplay behavior accidentally while moving action flow
into the lifecycle. It also prevents a half-built validator from becoming a
second source of truth beside the legacy checks.

## Future Validation PRD Direction

The future PRD should explore a deep **Action Validation Module** with an
interface that can validate:

- player-selected move, attack, skill, wait, and defense intents,
- AI-selected intents,
- automatic passive/deferred action intents,
- movement sub-action intents,
- combat sub-action intents,
- future multiplayer/network command intents.

The likely flow:

```text
ActionIntent
-> Action Validation Module
-> ValidatedAction
-> Battle Action Lifecycle
```

For future multiplayer, the validator should be designed so local player input,
AI decisions, and network commands can all converge on the same validated action
shape before execution.

## Boundaries

This ADR does not approve:

- adding a full validation module during the first lifecycle migration,
- changing skill names, slot order, cooldowns, targeting, ranges, movement,
  damage, status values, initiative, or turn rules,
- editing Unity assets, prefabs, scenes, materials, Animator Controllers,
  `.inputactions`, generated Unity files, `.asmdef`, or `.asmref`,
- treating current highlight checks as final architecture.

## Implementation Guidance For ADR 004

During Battle Action Lifecycle implementation:

- use existing validation checks as legacy adapters,
- do not duplicate large target/range/cooldown logic into the lifecycle,
- preserve current validation behavior unless a specific bug is in scope,
- keep `skillSlot` and `skillId` on skill actions so future validation can
  reason about slot order, cooldowns, UI, animation, and XML skill identity,
- document every place where legacy validation remains outside the lifecycle.

## Verification Direction

The lifecycle implementation should verify that legal actions still execute and
illegal actions still fail in the same player-facing way as before.

The future validation PRD should add stronger verification for:

- legal versus illegal movement,
- legal versus illegal skill targets,
- cooldown and passive trigger legality,
- dead or zero-amount unit action rejection,
- AI and player parity,
- future network command rejection before execution.
