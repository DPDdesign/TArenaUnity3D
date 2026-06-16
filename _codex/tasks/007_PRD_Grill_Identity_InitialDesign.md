# [TARENA] PRD: Grill Identity Initial Design

- Status: ready-for-grill
- Type: PRD
- Area: Identity, Player Fantasy, Scope Guardrails
- Label: ready-for-agent
- Target: `19_Identity.md`

## Problem Statement

TArenaUnity3D has an initial identity draft: a turn-based tactical hex arena
where the player builds a limited-point army and wins through positioning,
skill timing, unit roles, and phase pressure. This is a strong direction, but
it still contains unconfirmed assumptions about COBA, player fantasy, unit
factions, online identity, and what should survive legacy cleanup.

The user needs a structured grilling agenda that turns identity from a broad
initial synthesis into sharp production guardrails future agents can apply.

## Solution

Run a focused identity grilling session that challenges every identity claim
until it can be written as:

```text
Identity claim - what the player sees or does, and what production rule follows.
```

Confirmed identity decisions should update the identity document. Rejected or
uncertain ideas should either move to non-identity or remain as open questions.
The identity document should become the place future agents use to decide
whether a proposed feature makes TArena more distinct or more generic.

## User Stories

1. As the project director, I want a final identity sentence, so that future agents can identify off-scope work quickly.
2. As the project director, I want COBA clarified, so that the project has either a clear public genre label or a safe internal shorthand.
3. As the project director, I want the player's role clarified, so that the game is not pulled between commander, hero, summoner, account owner, and shop user fantasies.
4. As the project director, I want the main toy named, so that tactical unit interaction stays central.
5. As the project director, I want the repeated main decision clarified, so that the design can reject generic feature ideas.
6. As the project director, I want identity pillars challenged, so that each pillar is player-facing and actionable.
7. As the project director, I want non-identity boundaries listed, so that backend, monetization, and live-service surfaces do not define recovery work.
8. As the project director, I want unit identity reviewed, so that real TArena unit roles are separated from test leftovers.
9. As the project director, I want faction identity reviewed, so that barbarians, lizards, golems, shadows, city/humans, and mystic units are either confirmed or parked.
10. As the project director, I want legacy preservation rules clarified, so that cleanup work knows what must not be broken.
11. As the project director, I want legacy baggage named, so that old systems can be cut without identity anxiety.
12. As a future coding agent, I want identity rules to be concrete, so that implementation choices do not dilute the game.
13. As a future design agent, I want every identity pillar to include a production rule, so that it can guide decisions.
14. As a future QA agent, I want identity preservation checks, so that reviews can catch regressions beyond compile errors.
15. As the user, I want identity to stay distinct from GDD and Feel, so that each document has a clear job.
16. As the user, I want unresolved identity claims to stay visible, so that they can be grilled later.

## Implementation Decisions

- Treat the current identity file as an `Initial Design` draft.
- Do not expand identity into a full GDD, skill compendium, or lore bible.
- Keep identity claims concrete and player-facing.
- Every confirmed pillar should include a production rule.
- Confirm whether backend, multiplayer, online profile, economy, and mobile
  plans are identity or legacy product history.
- Confirm the player role before confirming progression or economy identity.
- Confirm which unit groups and factions matter to TArena identity.
- Keep the local tactical battle as the default recovery-era identity unless
  the user explicitly restores online-first identity.
- Use non-identity as an active rejection tool, not as a dumping ground.

## Testing Decisions

- A good documentation test checks whether another agent can read the identity
  file and reject a backend-first feature during recovery.
- A good documentation test checks whether every pillar says what the player
  sees or does.
- A good documentation test checks whether every pillar leads to a production
  rule.
- Manual review should verify that the identity file does not copy Retsot
  product goals as TArena identity.
- Manual review should verify that test/placeholder XML units are not promoted
  to final identity content without confirmation.
- Manual review should verify that open questions are still explicit after the
  grill.

## Out of Scope

- Implementing identity decisions in code.
- Writing final marketing copy.
- Writing full lore.
- Rebalancing units.
- Editing Unity assets.
- Deciding all current mechanics.
- Creating progression, economy, or monetization systems.

## Further Notes

Recommended grill order:

1. Is COBA final or internal?
2. What is the player fantasy?
3. What is the one remembered moment after a battle?
4. What is the main toy?
5. What is the repeated decision?
6. Which identity pillars survive challenge?
7. Which factions and units are identity-critical?
8. Which legacy systems are baggage?
9. What is forbidden until local battle recovery is stable?

Key decisions to force:

- Commander vs hero vs summoner vs account owner.
- Hex tactics vs skill combos vs army building as the dominant identity.
- Online PvP as long-term identity or legacy history.
- Phase system as identity pillar or only balance mechanism.
- Unit tier timing as identity pillar or implementation detail.
- Which unit roles define the game at first slice.
