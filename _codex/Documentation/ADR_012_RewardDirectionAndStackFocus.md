# ADR 012: Reward Choices Can Reinforce A Run Direction

Status: accepted
Date: 2026-06-24
Project: TArenaUnity3D

## Context

PRD019 establishes post-battle rewards as quick 1-of-3 card choices that apply
concrete army transactions. PRD037 and ADR011 move reward generation toward
materialized run data, where reward opportunities can be known before the
battle while concrete targets and amounts are selected from the post-battle
army state.

The Minimal Reward Flow V1 should keep implementation small: three reward cards
are generated from three different operation types, each card is already fully
selected before display, and clicking a legal card applies it immediately. In
that version, more than one legal reward card may target the same stack.

However, the long-term run experience should not feel like random isolated
transactions. Route choices and reward hints should let the player steer a run
toward a recognizable army direction, for example reinforcing a Rusher-centered
army or another stack/unit identity.

## Decision

For Minimal Reward Flow V1, reward cards may target the same stack when their
operation type is legal and no additional target-picking UI exists.

For `AddNewStack` in V1, the reward must resolve to the first free formation
slot during materialization/preview/apply. If no free slot exists, the card is
not legal and should remain disabled/burned instead of creating a new UI choice
or forcing a stack replacement.

For the product direction, reward generation and route hints should preserve
space for intentional run steering:

- reward opportunities may carry operation-type hints at run generation time,
- concrete reward cards resolve target, amount, and preview after battle from
  the current army snapshot,
- future reward generation may bias toward a chosen stack, unit, role, or army
  archetype,
- route UI may communicate this as high-level reward direction before battle,
  without promising an exact target or amount,
- reward cards remain concrete transactions, not an army editor.

## Rationale

V1 needs a small, reliable flow that proves battle completion, reward display,
preview, apply, and return-to-map routing. Allowing repeated target stacks keeps
that flow simple and avoids adding a target-distribution problem before reward
materialization is stable.

The strategic promise of the run is still bigger than V1. The player should be
able to read route hints and choose paths that push the army toward a coherent
identity, such as "make my Rusher stack matter", rather than only consuming
independent random cards.

## Consequences

- V1 tests should not fail only because two cards target the same stack.
- Future PRDs should revisit reward bias, operation selection, target
  distribution, and stack/unit focus once the minimal flow is working.
- Future PRDs may add remove-stack or sell-stack interactions so `AddNewStack`
  can become available even when the formation is full.
- Run Map reward hints should avoid exact target/amount promises before the
  battle, but can still communicate operation type or direction.
- A fully impossible set of three reward cards can burn those cards and expose
  an emergency RunGold fallback, preserving route-choice consequence while
  avoiding a dead end.
