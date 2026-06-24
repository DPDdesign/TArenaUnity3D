# [TARENA] PRD046-C: Tactical AI Lifecycle Execution Bridge

- Status: draft
- Type: PRD
- Area: Tactical Battle, AI, Action Lifecycle, Execution Adapter
- Label: needs-grill
- Parent: `_codex/tasks/046_PRD_TacticalBattleAI_V1.md`
- Depends on: `_codex/tasks/046_A_PRD_TacticalAI_BattleSnapshotV1.md`
- Depends on: `_codex/tasks/046_B_PRD_TacticalAI_ActionIntentCandidates.md`
- Related: `_codex/Documentation/ADR_004_BattleActionLifecycleTurnSafety.md`
- Related: `_codex/Documentation/ADR_005_ActionValidationFuturePRD.md`
- Related: `_codex/tasks/018_PRD_BattleActionLifecycleFullMigration.md`

## Problem Statement

AI planning must not execute by mutating tactical scene objects directly. The
selected AI action must enter the same live action lifecycle as player actions
so turn completion, presentation blocking, action consumption, and cleanup stay
centralized.

Build a live execution bridge that maps a selected `TacticalAIActionIntent` to
the existing runtime action paths and revalidates the live state immediately
before execution.

## Scope

This PRD covers execution of selected intents through current live runtime
methods. It does not define search, scoring, profile tuning, or snapshot
building.

## Implementation Decisions

- AI execution must call existing live action paths, not direct model mutation.
- Basic actions should route through existing `MouseControler` lifecycle entry
  points where available:
  - `TryStartMoveAction`,
  - `TryStartMoveAndAttackAction`,
  - `TryStartBasicRangedAttackAction`,
  - `TryStartWaitAction`,
  - `TryStartDefenseAction`.
- Skill actions route through the CastManager bridge detailed in PRD046-F.
- The bridge must re-resolve the live actor from the intent runtime id and
  sanity checks.
- If revalidation fails, the AI must use fallback rather than forcing the
  action.
- The bridge must not create a second full validation module. Existing legacy
  checks remain authoritative per ADR005.

## Live Revalidation

Before execution, check:

```text
Actor:
- runtime unit id maps to a current live unit
- team index matches
- unit name/type matches
- source hex matches
- unit is alive and actionable
- stack amount matches or is compatible with the latest fresh snapshot

Action:
- selected action is still possible in live state
- destination/target still exists
- target is alive when needed
- current action lifecycle is not busy
- skill slot/id/cooldown still match for skill intents
```

Cached plans must always be revalidated. A cache hit never bypasses this bridge.

## Fallback Chain

When selected execution fails:

```text
1. next best intent from the completed plan, if still legal
2. fresh greedy/legal action from current snapshot
3. defend
4. wait
5. safe no-op with warning if no legal action exists
```

Fallback must not bypass live legality checks.

## Testing Decisions

Add deterministic tests where possible for mapping intents to bridge requests.
Use Play Mode/manual validation for scene-owned behavior:

- movement intent executes through lifecycle,
- move-and-attack intent executes through lifecycle,
- basic ranged attack intent executes through lifecycle,
- wait and defend execute through lifecycle,
- stale actor/target/destination rejects and falls back,
- lifecycle busy rejects overlapping AI execution.

## Acceptance Criteria

Done when:

- selected AI intents can be resolved against live tactical state,
- every executed basic AI action enters the existing lifecycle path,
- stale or invalid intents are rejected safely,
- fallback is implemented,
- no AI execution path directly mutates live tactical state outside the
  existing runtime action paths.

## Out Of Scope

- Full action validation rewrite.
- Search/scoring decisions.
- Perfect skill prediction.
- Editing scene or prefab wiring unless later explicitly scoped.

