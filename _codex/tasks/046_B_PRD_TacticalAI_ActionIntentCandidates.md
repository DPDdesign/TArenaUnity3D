# [TARENA] PRD046-B: Tactical AI Action Intents And Candidates

- Status: draft
- Type: PRD
- Area: Tactical Battle, AI, Action Intents, Candidate Generation
- Label: needs-grill
- Parent: `_codex/tasks/046_PRD_TacticalBattleAI_V1.md`
- Depends on: `_codex/tasks/046_A_PRD_TacticalAI_BattleSnapshotV1.md`
- Related: `_codex/Context/BattleActionRules.md`
- Related: `_codex/Documentation/ADR_005_ActionValidationFuturePRD.md`

## Problem Statement

Tactical AI needs an explicit action-intent model so planning can remain
separate from live scene mutation. Current player and AI actions are routed
through legacy methods on `MouseControler`, `TosterHexUnit`, and `CastManager`.
The AI must not directly call those paths during planning.

Create a pure intent and candidate-generation layer that can enumerate legal or
potentially legal actions from a `BattleSnapshot`.

## Scope

This PRD defines the action intent model and candidate generation for:

- wait,
- defend,
- move,
- move-and-attack,
- basic ranged attack,
- every currently available active skill as a skill intent.

Skill prediction remains approximate and is detailed in PRD046-F. This PRD
only requires that skill candidates can be represented and considered.

## Implementation Decisions

- AI outputs an action intent, not direct board mutation.
- Candidate generation reads `BattleSnapshot`, not live scene objects.
- Live execution revalidates an intent before execution.
- Move-and-attack is one composite action.
- Candidate generation should produce a stable ordering before scoring.
- Stance/toggle skills are represented as skill intents only if they are legal
  and useful to consider.
- Passive skills are not direct action candidates.
- Candidate generation should not create a second action validation authority.
  It is allowed to approximate legality for planning, but live execution is
  authoritative.

## Intent Model

Minimal model:

```text
TacticalAIActionIntent
- actionType
- actorUnitId
- sourceHex
- destinationHex optional
- targetUnitId optional
- targetHex optional
- skillSlot optional
- skillId optional
- predictedPriority / candidate metadata optional

Action types
- Wait
- Defend
- Move
- MoveAndAttack
- BasicRangedAttack
- Skill
```

## Candidate Rules

Candidate generation must consider:

- active unit only for the current ply,
- current movement/action flags,
- wait/defend restrictions from `BattleActionRules`,
- occupied and empty hexes,
- movement range approximation,
- melee adjacency for move-and-attack,
- basic ranged attack availability for ranged units,
- active skill cooldowns and used-skill rules,
- skill ids by slot.

The first candidate generator should keep movement bounded by the profile
limits defined in PRD046-E, such as max move candidates and max candidates per
action type.

## Skill Candidate Rule

All current active skills may be candidates. This does not mean pure prediction
for every skill is perfect in V1.

Planning:

```text
Skill candidate legal enough for planning
-> approximate score
-> selected only if it wins search
```

Execution:

```text
Selected skill intent
-> live revalidation
-> CastManager / MouseControler / BattleActionLifecycle path
```

## Testing Decisions

Add deterministic tests for:

- wait candidate available only before movement and before non-stance skills,
- defend candidate available only before movement and before non-stance skills,
- move candidates exclude occupied destinations,
- move-and-attack candidates include legal adjacent attack positions,
- basic ranged attack candidates target enemies,
- passive skills are not candidates,
- cooldown-blocked skills are not candidates,
- candidate ordering is stable.

## Acceptance Criteria

Done when:

- a pure `TacticalAIActionIntent` model exists,
- a candidate generator can enumerate basic action candidates from a snapshot,
- all current active skills can be represented as candidates,
- candidate generation does not mutate live Unity objects,
- generated candidates are stable and profile-limit aware,
- live execution remains the authority for final legality.

## Out Of Scope

- Executing intents.
- Full action validation rewrite.
- Perfect skill simulation.
- Changing skill ids, cooldowns, targeting, range, damage, movement, status, or
  turn rules.

