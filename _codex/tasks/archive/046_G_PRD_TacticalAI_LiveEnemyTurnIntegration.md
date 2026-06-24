# [TARENA] PRD046-G: Tactical AI Live Enemy Turn Integration

- Status: implemented-unity-validation-pending
- Type: PRD
- Area: Tactical Battle, AI, Enemy Turn, Live Integration
- Label: needs-implementation
- Parent: `_codex/tasks/archive/046_PRD_TacticalBattleAI_V1.md`
- Depends on: `_codex/tasks/archive/046_A_PRD_TacticalAI_BattleSnapshotV1.md`
- Depends on: `_codex/tasks/archive/046_B_PRD_TacticalAI_ActionIntentCandidates.md`
- Depends on: `_codex/tasks/046_C_PRD_TacticalAI_LifecycleExecutionBridge.md`
- Depends on: `_codex/tasks/archive/046_D_PRD_TacticalAI_SearchScoring.md`
- Depends on: `_codex/tasks/archive/046_E_PRD_TacticalAI_ProfileCache.md`
- Depends on: `_codex/tasks/archive/046_F_PRD_TacticalAI_CastManagerSkillBridge.md`

## Problem Statement

PRD046 now has the main Tactical AI V1 parts in separate slices:

- pure battle snapshots,
- pure action intents and candidates,
- live lifecycle execution bridge,
- 3-ply search and scoring,
- profile/cache,
- CastManager skill execution bridge.

However, the live enemy turn still runs through the old `MostStupidAIEver`
behavior. That old AI does not call `TacticalAISearchPlanner`, so Play Mode
testing does not yet show the new 3-ply planner or skill bridge during real
enemy turns.

Build the small live integration slice that lets the current enemy turn flow
try Tactical AI V1 first, then fall back to the old behavior when planning or
execution cannot start a legal action.

## Scope

This PRD covers:

- wiring Tactical AI V1 into the current enemy turn entry point,
- executing the planner's ordered intents through `TacticalAIExecutionBridge`,
- preserving the old `MostStupidAIEver` logic as a temporary fallback,
- resolving the Normal profile through the current profile system,
- adding simple runtime diagnostics for AI decisions and rejections.

This PRD does not cover:

- rewriting `TurnManager`,
- replacing all enemy AI architecture,
- adding a new AI controller prefab,
- adding manual `TacticalAISnapshotProbe` search-plan context menu,
- changing skill balance or gameplay values.

## Implementation Decisions

- The first integration point is `MostStupidAIEver.AskAIwhattodo()`.
- `AskAIwhattodo()` should try the new Tactical AI V1 path first.
- If the new path cannot capture a snapshot, cannot build a plan, has no
  ordered intents, or `TacticalAIExecutionBridge` cannot start a legal action,
  it should fall back to the existing old AI logic.
- The old AI fallback may remain in V1 as a temporary safety net.
- The integration should use the existing Normal profile resolution path.
- For now, profile resolution should use the current profile system without
  adding new scene or prefab wiring. If no asset is assigned, runtime Normal is
  acceptable.
- The integration should not require a new manual `TacticalAISnapshotProbe`
  workflow for this slice.
- The new path must still execute through:

```text
BattleSnapshotLiveAdapter
-> TacticalAISearchPlanner / TacticalAISearchEngine
-> TacticalAIExecutionBridge
-> TacticalAIIntentRevalidator
-> MouseControler / CastManager / BattleActionLifecycle
```

## Runtime Flow

Target flow:

```text
MostStupidAIEver.AskAIwhattodo()
-> capture current live BattleSnapshot
-> resolve TacticalAIProfile Normal
-> build TacticalAISearchPlan
-> execute plan.OrderedActionIntents through TacticalAIExecutionBridge
-> if Started: return
-> log simple rejection context
-> run existing old AskAIwhattodo behavior as fallback
```

The old behavior should be extracted into a private helper such as
`RunLegacyFallbackAI()` or equivalent, so the new path can return early when it
starts an action.

## Diagnostic Logging

Add simple diagnostics behind clear log messages. The diagnostics should be
enough for Play Mode testing, but should not flood logs every frame.

Log when:

- Tactical AI planning starts for an active unit,
- the best intent is selected,
- the plan has ordered fallback count, score, completed depth, and opponent
  response coverage,
- execution starts successfully,
- execution fails and falls back to legacy AI,
- skill execution rejection is reported by the existing bridge.

Suggested message shape:

```text
[TacticalAI] plan actor=team-1-slot-0 best=Skill target=team-0-slot-2 score=...
[TacticalAI] started Skill actor=team-1-slot-0
[TacticalAI] fallback reason=NoLegalAction attempts=...
```

The exact wording can differ, but it should include actor id, action type, skill
id when present, target id/hex when present, score/depth when available, and
failure status when falling back.

## Safety Rules

- Do not bypass live revalidation.
- Do not directly mutate `TosterHexUnit`, `HexClass`, `CastManager`, or
  `MouseControler` state from planning.
- Do not call skill execution during planning.
- Do not remove the old fallback in this slice.
- Do not add new scene, prefab, material, controller, `.inputactions`,
  `.asmdef`, or `.asmref` edits.
- Do not rename public or serialized fields.
- Do not change gameplay float values.
- Do not add a second skill prediction catalog.

## Testing Decisions

Add focused EditMode tests where pure seams exist, especially for:

- integration helper returns `Started` when the bridge starts an action,
- integration helper falls back when the plan is empty,
- integration helper falls back when the bridge returns `NoLegalAction`,
- diagnostics include selected action type and failure status in testable
  formatting helpers if those helpers are pure.

Use Play Mode/manual validation for live scene behavior:

- enemy turn calls Tactical AI first,
- a legal movement/action starts through `TacticalAIExecutionBridge`,
- a selected skill intent can reach the PRD046-F CastManager bridge when the
  planner emits target-aware skill intents,
- stale or rejected intents fall back to the old AI behavior,
- Unity Console shows simple diagnostics for plan, execution, and fallback.

## Acceptance Criteria

Done when:

- live enemy turn attempts Tactical AI V1 before old AI behavior,
- Tactical AI plan execution uses `TacticalAIExecutionBridge`,
- old `MostStupidAIEver` behavior remains as fallback,
- Normal profile resolution works without new scene wiring,
- simple Play Mode diagnostics show plan, selected intent, execution result,
  and fallback reason,
- no live AI path bypasses revalidation or action lifecycle,
- no Unity assets or serialized scene/prefab references are required for this
  slice.

## Out Of Scope

- Removing `MostStupidAIEver`.
- Creating a new final enemy AI controller architecture.
- Rewriting turn ownership.
- Full skill prediction extraction.
- Manual `TacticalAISnapshotProbe` search-plan buttons.
- Changing damage, cooldowns, movement, initiative, target rules, or skill
  behavior.
