# 049ED Coding Agent Follow-Up

Date: 2026-06-25
Task: `_codex/tasks/049ED_PRD_TacticalAIActionSelectionAndExecutionMigration.md`
Follow-up to: `_codex/tasks/QA/2026-06-25_1715_049ED_QA_ArchitectureReview.md`

## Files Changed

- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillCast.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillRules.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISearchScoring.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISkillRulesExecutor.cs`

## What Changed

- Added `SkillCast.RepeatableInTurn`.
- Populated `RepeatableInTurn` from `ActivationRuleData.repeatableInTurn` during `SkillRules.ResolveCast(...)`.
- Updated AI snapshot simulation to use `SkillCast.RepeatableInTurn` instead of rereading activation data.
- Updated live AI skill runtime turn/cooldown logic to use `SkillCast.RepeatableInTurn` instead of inferring repeatability from stance effects.

## Automatic Test

Not run automatically. A lightweight source brace-balance check passed for the changed files.

## Remaining QA Scope

This follow-up addresses only the repeatability-contract issue from the QA report. The broader QA findings remain:

- migrated runtime still routes through the legacy `TacticalAIActionIntent` shell,
- async copied planning still carries `SkillDefinitionAsset` references,
- full active-skill live parity for status/spawn/effect details is not complete.
