# 049ED QA Architecture Review

Date: 2026-06-25
Task: `_codex/tasks/049ED_PRD_TacticalAIActionSelectionAndExecutionMigration.md`
Protocol: `_codex/tasks/QA/2026-06-25_1714_049ED_CodingAgentCompletion.md`
Reviewer: QA Architecture Review Agent

## Verdict

Follow-up required.

The implementation is a meaningful 049ED architecture slice: AI skill candidates now carry shared `SkillCast` and `SkillResult` data, live revalidation rebuilds a fresh `SkillCast`, and the default skill execution path no longer uses `TacticalAICastManagerSkillIntentExecutor`.

It does not yet satisfy full PRD049ED acceptance because the runtime still depends on the legacy `TacticalAIActionIntent` shell for all action routing, copied async planning still carries `SkillDefinitionAsset` references rather than plain immutable spec data, and live skill execution does not cover all active effect semantics with parity.

## Findings

### Follow-up Required: Full PRD049ED says new runtime path must not consume `TacticalAIActionIntent`

Files:

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIActionIntent.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISearchScoring.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIExecutionBridge.cs`

The implementation extends `TacticalAIActionIntent` with `ValidatedSkillCast` and `PreviewResult`, then continues to route search plans, attempt queues, revalidation, and execution through `TacticalAIActionIntent`.

This improves the skill candidate payload, but it does not meet the PRD language that "the new AI path consumes `ValidatedTacticalAction` directly" and "must stop using `TacticalAIActionIntent` as a transition adapter." For this codebase's implemented PRD49ABC naming, that means the skill runtime should consume `SkillCast` more directly, or a broader action-command/result abstraction should replace the old AI intent shell.

### Follow-up Required: Async planning still captures `SkillDefinitionAsset` references

Files:

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIAsyncDecisionPipeline.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISearchScoring.cs`

`TacticalAICopiedSkillMetadataProvider` now stores `SkillDefinitionAsset` references in `definitionsBySkillId`. The worker planner then uses those assets through `SkillRules`.

This avoids live `DataMapper` reads during the worker plan, but it is not a copied immutable spec model. It still relies on Unity `ScriptableObject` data on a worker path, which is weaker than PRD049ED's snapshot/spec-data boundary and could become thread-safety or drift debt.

### Follow-up Required: Live skill runtime does not cover all active skill behavior with parity

Files:

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISkillRulesExecutor.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillDefinitionMigrationDefaults.cs`

`TacticalAISkillRuntime` applies several `SkillResult` event families, but there are parity gaps:

- `UnitSpawned` logs a warning and does not spawn.
- Status application creates a named `SpellOverTime` shell with zero modifiers instead of preserving legacy effect semantics.
- Damage uses a simplified first-damage-effect lookup and does not distinguish every active multi-effect/multi-target case.
- Cooldown/turn repeatability infers repeatable behavior from stance effects rather than carrying the activation repeatability contract through `SkillCast`.

PRD049ED explicitly says every current active asset-backed/unit-assigned action and skill must execute through the SO-driven executor. This runtime is a useful adapter, but not full active-skill parity.

## Non-Blocking Observations

- The default bridge executor change is in the right direction: `TacticalAIExecutionBridge` now defaults to `TacticalAISkillRulesExecutor.Instance` instead of the CastManager skill bridge.
- The candidate expansion tests now prove `SkillCast` and `SkillResult` are present on skill candidates.
- The revised attempt queue better matches the ranked-action fallback rule by not appending fresh candidates after a non-empty ranked plan.
- Keeping `TacticalAICastManagerSkillIntentExecutor` in the project is acceptable for PRD049F cleanup as long as it is not the default runtime path.

## Required Follow-Up

1. Introduce a non-legacy ranked action item/command model for the migrated AI path, or otherwise remove `TacticalAIActionIntent` consumption from skill planning and execution.
2. Replace copied `SkillDefinitionAsset` references in async planning with a plain immutable skill spec snapshot that `SkillRules` or a pure rule equivalent can consume safely.
3. Expand `SkillCast`/`SkillResult`/runtime data so active status, spawn, movement, damage, cooldown, and turn-cost semantics can be applied with parity from SO data.
4. Add tests that compare planning simulation and live runtime application for the same `SkillCast` over representative active skills, including status and spawn families.
