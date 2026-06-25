# [TARENA] QA Architecture Review - PRD050 Follow-Up

Date: 2026-06-25
Task: `_codex/tasks/050_PRD_BattleActionAPI_FullMigrationPurge.md`
Protocol: `_codex/tasks/QA/2026-06-25_2203_050_CodingAgentFollowup.md`
Reviewer: QA Architecture Review Agent

## Verdict

Follow-up still required for full PRD050 completion.

The focused follow-up correctly addresses the deterministic basic-attack hash issue and adds Battle Action rule tests. It does not attempt to close the remaining full migration/purge scope, which is the right call for a single follow-up loop.

## Resolved Findings

- `BattleActionRules.ResolveBasicAttackDamage(...)` no longer uses `string.GetHashCode()`.
- `BattleActionRulesTests.cs` now covers:
  - legal and illegal move,
  - wait/defend rejection after movement,
  - deterministic ranged attack damage,
  - planned actions carrying Battle Action payloads without `LegacyIntent`.

## Remaining Blocking Findings

- Full PRD050 legacy purge is not achieved.
- `TacticalAIActionIntent`, `TacticalAICandidateGenerator`, `TacticalAISearchCandidateExpander`, `TacticalAIIntentRevalidator`, `LegacyIntent`, and execution/probe intent fields still remain in runtime/test surfaces.
- Live non-skill application still delegates through `MouseControler.TryStart*` entry points rather than applying `BattleActionResult` events as the mutation authority.
- Skill-only DTO/rule classes remain embedded in Battle Action skill handling.
- Tactical AI search simulation is not yet fully converted to `BattleActionRules.Apply(...)`.
- Player input is not yet migrated to submit all actions through `BattleActionUse`.

## Test Status

- Unity compile was not run.
- Unity Test Runner was not run.
- Source brace-balance checks were reported as passing.
- New focused test file: `TArenaUnity3D/Assets/Scripts/Tests/EditMode/BattleActionRulesTests.cs`.

## QA Status

No additional focused Coding Agent follow-up is recommended in this loop. The remaining findings require a larger continuation of PRD050 rather than a small correction to this slice.
