# [TARENA] PRD046-F: Tactical AI CastManager Skill Bridge

- Status: draft
- Type: PRD
- Area: Tactical Battle, AI, Skills, CastManager Bridge
- Label: needs-grill
- Parent: `_codex/tasks/046_PRD_TacticalBattleAI_V1.md`
- Depends on: `_codex/tasks/046_B_PRD_TacticalAI_ActionIntentCandidates.md`
- Depends on: `_codex/tasks/046_C_PRD_TacticalAI_LifecycleExecutionBridge.md`
- Related: `_codex/Documentation/ADR_013_TacticalAI_CastManagerSkillBridge.md`
- Related: `_codex/tasks/034_PRD_FutureSkillLogicAndTargetingExtraction.md`

## Problem Statement

Tactical AI V1 must use skills, but the project is not doing a full skill
prediction rewrite now. Current skill legality and execution are still owned by
legacy `CastManager`, `MouseControler`, skill ids, cooldowns, targeting flags,
and runtime scene state.

Build a bridge that lets AI consider every currently legal active skill as a
candidate, score skills approximately in pure planning, and execute selected
skills through the existing live skill path.

## Scope

This PRD covers all current active skills as AI candidates. It does not create
a new skill prediction catalog and does not migrate skill execution out of
`CastManager`.

## Implementation Decisions

- All current active skills should be eligible as AI candidates when legal.
- Passive skills are not direct action candidates.
- Stance/toggle skills may be candidates when legal and useful.
- The AI must not mutate live Unity objects during skill planning.
- Skill prediction in V1 is approximate.
- Live execution is authoritative.
- Selected skill intents must revalidate live state immediately before
  execution.
- No separate `SkillPredictionCatalog` is allowed in V1.
- Existing skill ids remain the join key.

## Planning Rule

Planning may use:

- skill id,
- skill slot,
- cooldown,
- action flags such as `NI` and `AM`,
- active/passive type from current skill data,
- approximate targeting shape from current bridge behavior where safe,
- conservative heuristics for complex skills.

Planning must not:

- call skill execution methods,
- call `CastManager.startSpell` as prediction,
- mutate `CastManager` mode state in a way that affects live play,
- mutate `TosterHexUnit`,
- alter cooldowns,
- send chat messages,
- play VFX/SFX/animations.

## Execution Rule

Execution flow:

```text
Selected skill intent
-> resolve live actor
-> sanity-check skill slot and skill id
-> revalidate cooldown/action state/target state
-> use existing MouseControler/CastManager skill flow
-> complete through BattleActionLifecycle
```

If live skill execution rejects the intent, fallback to the next legal intent.

## Approximate Skill Scoring

V1 may score skills with categories such as:

- direct enemy damage,
- AoE damage opportunity,
- self/ally defensive value,
- movement/approach value,
- disabling/debuff value,
- stance/toggle value,
- trap/summon/complex value using conservative heuristics.

Unknown or hard-to-predict skills should not crash planning. They can receive a
bounded heuristic score and rely on live execution for truth.

## Testing Decisions

Add deterministic tests where pure seams exist for:

- passive skills are excluded,
- cooldown-blocked skills are excluded,
- used non-toggle skills are excluded,
- skill slot and skill id stay paired,
- approximate scoring does not mutate snapshot or live objects,
- selected skill intent is revalidated before execution.

Manual Play Mode validation should cover:

- several direct damage skills,
- AoE skill,
- move/approach skill,
- self/stance or buff skill,
- illegal target rejection,
- cooldown rejection,
- lifecycle completion after skill execution.

## Acceptance Criteria

Done when:

- every current active skill can be represented as a candidate,
- AI planning does not mutate live skill/runtime state,
- selected skill intents execute through current `CastManager` and lifecycle
  paths,
- failed live skill execution falls back safely,
- no second skill prediction catalog is introduced,
- existing skill ids, cooldowns, targeting, damage, statuses, movement, and
  presentation behavior are not intentionally changed.

## Out Of Scope

- Rewriting `CastManager`.
- Extracting full skill targeting/validation/execution.
- Creating a separate skill prediction catalog.
- Changing skill ids, cooldowns, range, damage, targeting, movement, status,
  VFX/SFX, animation, or turn rules.

