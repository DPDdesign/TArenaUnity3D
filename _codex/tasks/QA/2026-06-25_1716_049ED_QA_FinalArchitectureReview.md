# 049ED Final QA Architecture Review

Date: 2026-06-25
Task: `_codex/tasks/049ED_PRD_TacticalAIActionSelectionAndExecutionMigration.md`
Initial protocol: `_codex/tasks/QA/2026-06-25_1714_049ED_CodingAgentCompletion.md`
Follow-up protocol: `_codex/tasks/QA/2026-06-25_1716_049ED_CodingAgentFollowup.md`
Reviewer: QA Architecture Review Agent

## Verdict

Follow-up required.

The follow-up correctly moved repeatability into the shared validated action model by adding `SkillCast.RepeatableInTurn` and using it in simulation/live runtime. That removes one local inference problem from the first review.

The full PRD049ED acceptance bar is still not met.

## Resolved Since First QA

- `SkillCast` now carries `RepeatableInTurn` from `ActivationRuleData`.
- AI simulation and live AI skill runtime now use `SkillCast.RepeatableInTurn` rather than inferring repeatability from stance effects.

## Remaining Findings

### Follow-up Required: Migrated runtime still consumes `TacticalAIActionIntent`

The planner and bridge still use `TacticalAIActionIntent` as the ranked action container. Skill candidates now carry `SkillCast`, which is useful, but the PRD explicitly asks the new runtime path to stop consuming `TacticalAIActionIntent` as a transition adapter.

### Follow-up Required: Async planning still carries `SkillDefinitionAsset`

`TacticalAICopiedSkillMetadataProvider` stores `SkillDefinitionAsset` references. This is not a plain immutable spec snapshot, and it keeps Unity asset objects on the async planning path.

### Follow-up Required: Full active skill parity is incomplete

`TacticalAISkillRuntime` does not yet fully apply all active skill families with parity:

- spawn events are warning-only,
- status effects do not carry concrete modifier data,
- damage/effect matching is simplified,
- representative active-skill parity tests are not present.

## Final QA Status

The implementation is acceptable as an architecture slice toward PRD049ED, but not as complete closure of PRD049ED.
