# [TARENA] PRD: Grill Feel Initial Design

- Status: ready-for-grill
- Type: PRD
- Area: Feel, Feedback, Readability, Presentation
- Label: ready-for-agent
- Target: `GameplayFeelDoctrine.md`

## Problem Statement

TArenaUnity3D has an initial feel draft built around tactical, readable,
skill-forward, pressured, and physical-enough play. It identifies important
feedback topics: active unit, selected skill, valid/invalid cast, hit/death,
phase changes, skill unlocks, persistent effects, traps, DoT/CC, and sudden
death warnings.

The draft is useful but not final. The user needs a structured grilling agenda
to decide what the game should feel like, which feedback is mandatory during
recovery, which feedback can remain placeholder, and how feel work stays
separate from balance or system rewrites.

## Solution

Run a focused feel grilling session that validates the feedback hierarchy and
turns abstract feel words into player-facing requirements. Each confirmed feel
decision should update the feel doctrine. Any feel claim that would alter
damage, cooldowns, targeting, movement values, turn rules, skill ownership, or
serialized/public fields should be moved out of feel-only scope.

The final feel doctrine should help future agents answer:

- what must be readable before a player can play,
- what should become clear soon,
- what can wait,
- how skill casts and results should communicate,
- how phases and unlocks should feel,
- how persistent effects should remain visible,
- where the boundary is between presentation and gameplay change.

## User Stories

1. As the project director, I want target feel words confirmed, so that future presentation work has a shared direction.
2. As the project director, I want each feel word tied to player-facing feedback, so that "polish" does not become vague scope.
3. As the project director, I want the feedback hierarchy grilled, so that the team knows what is mandatory before nice-to-have effects.
4. As the project director, I want selection feedback clarified, so that active and selected units are never ambiguous.
5. As the project director, I want valid and invalid target feedback clarified, so that players do not commit actions blindly.
6. As the project director, I want skill cast feedback clarified, so that skill use feels intentional.
7. As the project director, I want impact feedback clarified, so that damage, healing, buffs, debuffs, pulls, traps, and deaths are readable.
8. As the project director, I want phase-change feedback clarified, so that phases become part of player experience rather than hidden math.
9. As the project director, I want skill-unlock feedback clarified, so that players understand when a unit becomes stronger.
10. As the project director, I want sudden-death warning feedback clarified, so that anti-stall pressure feels fair.
11. As the project director, I want persistent-effect feedback clarified, so that traps, fire, CC, DoT, walls, and auras do not become invisible state.
12. As the project director, I want movement plus skill feel clarified, so that turn flow supports the intended tactics.
13. As a future coding agent, I want feel boundaries to be explicit, so that I do not change balance while adding feedback.
14. As a future content author, I want mandatory vs optional feedback separated, so that asset work can be prioritized.
15. As a future QA agent, I want manual checks for feel, so that readability can be reviewed consistently.
16. As the user, I want feel decisions recorded as doctrine, so that future tasks do not re-litigate the same basics.

## Implementation Decisions

- Treat the current feel doctrine as an `Initial Design` draft.
- Start the grill with target feel words and player-facing examples.
- Keep feel separate from balance, targeting, turn economy, and skill ownership.
- Confirm whether the game should feel fast/snappy, deliberate/heavy, or a
  specific mixture.
- Confirm whether movement plus skill in one turn is intended feel or only a
  design note.
- Confirm the minimum valid/invalid targeting cues.
- Confirm phase and skill-unlock feedback before designing detailed VFX.
- Confirm persistent-effect readability rules before adding many trap/DoT/CC
  effects.
- Keep current skill-id and XML/reflection constraints intact.
- Treat placeholder feedback as acceptable only when it still answers the
  relevant readability question.

## Testing Decisions

- A good feel test checks external player-readable behavior, not internal
  helper implementation.
- Manual review should verify that the player can identify the active unit.
- Manual review should verify that the player can identify the selected skill
  or action.
- Manual review should verify that legal and illegal targets are distinguishable
  before committing.
- Manual review should verify that a committed action has a visible or audible
  confirmation.
- Manual review should verify that damage, healing, status, movement, death,
  and trap triggers are readable.
- Manual review should verify that phase changes and skill unlocks are not
  silent once they become real mechanics.
- Manual review should verify that persistent effects remain visible or
  otherwise trackable while active.
- Manual review should verify that feel tasks do not change damage, cooldowns,
  targeting, movement values, turn rules, skill ids, or unit skill ownership.
- Unity-side Play Mode validation is required for current feel behavior claims.

## Out of Scope

- Implementing new feel systems.
- Editing C# code.
- Editing Unity assets, scenes, prefabs, materials, controllers, animation
  clips, generated Unity files, `.inputactions`, `.asmdef`, or `.asmref`.
- Rebalancing combat.
- Renaming skill ids.
- Rewriting the skill system.
- Creating final VFX/SFX assets.
- Designing a full UI art direction.
- Finalizing current-state mechanics.

## Further Notes

Recommended grill order:

1. Target feel words.
2. First 10 seconds of battle.
3. One complete battle payoff.
4. Active/selected unit feedback.
5. Valid/invalid target feedback.
6. Action commit and result feedback.
7. Skill cast/projectile/impact feedback.
8. Phase and skill-unlock feedback.
9. Persistent effects.
10. Recovery-era placeholder boundaries.

Key decisions to force:

- Fast/snappy vs deliberate/heavy.
- Whether movement plus skill is intended.
- Whether valid/invalid target cues are icon, color, hex overlay, cursor, UI, or
  a combination.
- What the player must never miss.
- Which feedback can be placeholder.
- Which feedback must be production-readable before further feature work.
- How to warn sudden death fairly.
