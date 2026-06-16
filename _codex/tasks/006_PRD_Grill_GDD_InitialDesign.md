# [TARENA] PRD: Grill GDD Initial Design

- Status: ready-for-grill
- Type: PRD
- Area: GDD, Design Direction, Scope Control
- Label: ready-for-agent
- Target: `01_Game_Design_Document.md`

## Problem Statement

TArenaUnity3D now has an initial GDD draft synthesized from local references,
legacy Retsot material, invitational unit descriptions, and runtime XML data.
That draft is useful, but it is not final design truth. It contains assumptions
that must be challenged: COBA identity, army point budgets, phase system,
match modes, skill-forward combat, sudden death, progression boundaries, and
which legacy surfaces should be ignored during recovery.

The user needs a structured grilling agenda that can turn the GDD from an
`Initial Design` draft into a confirmed project charter without accidentally
copying Retsot backend/product assumptions into TArena.

## Solution

Run a focused GDD grilling session that validates the top-level game design in
small decision batches. Each confirmed decision should update the GDD. Each
uncertain or disputed decision should remain in open questions. Current-state
claims must be separated from intended design claims and require local code or
Unity verification.

The output should be a sharper GDD that answers:

- what TArena is,
- what the player repeatedly does,
- what one battle/session contains,
- how army building works,
- how phases work,
- what skill-forward combat means,
- what content/progression is in scope,
- what is explicitly out of scope during recovery.

## User Stories

1. As the project director, I want the GDD to define TArena in one sentence, so that future agents can reject off-scope ideas.
2. As the project director, I want the GDD to separate TArena design from Retsot legacy references, so that old backend goals do not become current scope by accident.
3. As the project director, I want the GDD to define the main battle loop, so that implementation and documentation work knows what to preserve first.
4. As the project director, I want the GDD to define the player's repeated decisions, so that the game does not become a generic stat battler.
5. As the project director, I want army building rules clarified, so that point budgets and unit composition have design meaning.
6. As the project director, I want quick, medium, and long mode assumptions challenged, so that mode scope does not grow before the core loop is stable.
7. As the project director, I want phase-system rules grilled, so that tiers, skill unlocks, stat buffs, and match pacing are not vague.
8. As the project director, I want sudden-death rules grilled, so that match-ending pressure is intentional rather than a leftover note.
9. As the project director, I want skill-forward combat defined, so that movement, attacks, skills, and waits have a coherent turn economy.
10. As the project director, I want level/map assumptions recorded, so that hex battlefield rules can be designed without premature scene work.
11. As the project director, I want progression and reward assumptions separated from recovery work, so that economy and monetization do not block local gameplay.
12. As the project director, I want the GDD to list out-of-scope legacy systems, so that PlayFab, PUN, Photon, online stats, and monetization do not drive current work.
13. As a future coding agent, I want the GDD to distinguish intended design from current mechanics, so that I do not treat unverified ideas as implemented systems.
14. As a future QA agent, I want the GDD to name the manual Unity checks that prove the design slice exists, so that validation is concrete.
15. As a future design agent, I want open questions to stay visible, so that uncertain topics are not silently resolved by assumption.
16. As the user, I want each grill decision to produce a document change, so that the conversation hardens the project documentation.
17. As the user, I want unconfirmed source conflicts to stay marked, so that deck numbers and XML numbers are not merged casually.
18. As the user, I want the GDD to keep feature growth below recovery work, so that the project remains manageable.

## Implementation Decisions

- Treat the current GDD as an `Initial Design` draft.
- Use a grilling session rather than a rewrite-from-scratch session.
- Start with identity and loop decisions before detailed content decisions.
- Keep GDD scope high-level: intended play structure, production boundaries,
  and open design questions.
- Do not turn the GDD into a full code map, skill list, balance spreadsheet, or
  backlog.
- Treat the old Retsot GDD as legacy context only.
- Treat the invitational deck as unit fantasy and early balance intent.
- Treat runtime XML as current data, not final design.
- Preserve source-conflict warnings when deck and XML disagree.
- Keep backend, multiplayer, monetization, mobile, and community plans out of
  current GDD scope unless the user explicitly restores them.
- Confirm whether COBA is final public vocabulary or internal shorthand.
- Confirm the battle/session structure before expanding progression.
- Confirm turn economy before changing any feel or skill documentation.

## Testing Decisions

- A good documentation test checks whether another agent can read only the GDD
  and correctly explain what TArena is trying to become.
- A good documentation test checks whether another agent can separate intended
  design from current runtime behavior.
- A good documentation test checks whether the GDD rejects backend-first or
  multiplayer-first scope during recovery.
- Manual review should verify that every confirmed grill answer appears in the
  GDD.
- Manual review should verify that unresolved questions remain in the open
  questions section.
- Manual review should verify that no Retsot-only production goal was copied as
  TArena truth.
- Unity-side validation is required only for current-mechanics claims, not for
  high-level design intent.

## Out of Scope

- Implementing gameplay systems.
- Editing C# code.
- Editing Unity assets, scenes, prefabs, materials, controllers, or generated
  Unity files.
- Balancing unit floats.
- Renaming XML skill ids.
- Resolving all current-state documentation.
- Creating economy, monetization, backend, or multiplayer plans.
- Writing final lore or full unit bible content.

## Further Notes

Recommended grill order:

1. One-sentence game definition.
2. Player role and repeated decision.
3. Battle/session structure.
4. Turn economy.
5. Army point-budget rules.
6. Phase system.
7. Sudden death and anti-stall pressure.
8. Content/progression boundaries.
9. Legacy systems that are explicitly out of scope.
10. Manual Unity check for the first real GDD slice.

Key decisions to force:

- Is TArena local-first during recovery or online-first long term?
- Is COBA the public genre label?
- Can a unit move and use a skill in the same turn?
- Do phases unlock skills, buff stats, or both?
- Are quick/medium/long modes real near-term design or reference only?
- Which XML units are real design content?
- What is the smallest battle loop worth preserving first?
