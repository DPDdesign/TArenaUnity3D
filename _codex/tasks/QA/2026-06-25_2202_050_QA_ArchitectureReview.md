# [TARENA] QA Architecture Review - PRD050

Date: 2026-06-25
Task: `_codex/tasks/050_PRD_BattleActionAPI_FullMigrationPurge.md`
Protocol: `_codex/tasks/QA/2026-06-25_2201_050_CodingAgentCompletion.md`
Reviewer: QA Architecture Review Agent

## Verdict

Follow-up required.

The implementation adds the requested `BattleActionUse` / `BattleAction` / `BattleActionResult` model and starts routing Tactical AI ranked root actions through Battle Action validation before live execution. That is a real step toward PRD050, but it does not satisfy the full PRD050 migration/purge acceptance criteria.

## Blocking Findings

### 1. Full PRD050 legacy purge is not achieved

PRD050 explicitly requires no runtime references to the legacy AI intent/candidate stack and no legacy fallback completion path. The implementation still leaves the following runtime surfaces active:

- `TacticalAIActionIntent`
- `TacticalAICandidateGenerator`
- `TacticalAISearchCandidateExpander`
- `TacticalAIIntentRevalidator`
- `TacticalAIExecutionAttempt.Intent`
- `TacticalAIExecutionResult.ExecutedIntent`
- `TacticalAIPlannedAction.LegacyIntent`

This is acknowledged in the protocol, but it means the PRD cannot be accepted as complete.

### 2. Live apply does not consume `BattleActionResult` as authoritative mutation data

`BattleActionLiveApplier` receives a revalidated Battle Action payload, but movement/attack/wait/defend still execute by calling `MouseControler.TryStart*` methods. Those methods recalculate/mutate through the legacy live path instead of applying ordered `BattleActionResult` events.

This conflicts with the PRD050 live-apply rule that runtime mutation applies result events and does not recalculate gameplay.

### 3. The new Battle Action model still embeds skill-only runtime DTOs

`BattleAction` carries `SkillCast`, and skill preview/result work still flows through `SkillRules`, `SkillUse`, `SkillCast`, and `SkillResult`.

That is acceptable for a transitional slice, but PRD050 specifically says the final runtime action model must replace the skill-only DTO stack with Battle Action API responsibilities. The implementation does not yet meet that requirement.

### 4. Basic attack deterministic damage uses `string.GetHashCode()`

`BattleActionRules.ResolveBasicAttackDamage(...)` derives the basic attack seed with `ActorUnitId.GetHashCode()` and `PrimaryTargetUnitId.GetHashCode()`. C# string hash codes should not be treated as a stable deterministic replay/server-authority primitive.

PRD050 requires deterministic seed/action-index damage suitable for AI, replay, and future authority. This should use a stable explicit hash or deterministic integer fold over string characters.

### 5. AI simulation/search is still not fully using the Battle Action pure apply path

The ranked root output is converted into `BattleAction`, but the search internals still use `TacticalAIActionIntent` and `TacticalAISnapshotSimulator.ApplyIntent(...)` paths. That keeps an AI-only predictor beside the new Battle Action result path.

PRD050 requires AI simulation and live commit to share the same pure apply path.

## Non-Blocking Observations

- The new DTO names match the PRD050 target names and are placed in the tactical battle area.
- The revalidation boundary moving toward `BattleActionRules.Validate(...)` is directionally correct.
- Keeping scene/prefab/assets untouched is consistent with project safety rules.
- No TextMesh Pro/UI rule issues were introduced.

## Test Status

- Unity compilation was not run.
- Unity Test Runner was not run.
- Brace-balance checks are useful as a smoke check but do not prove C# compilation.
- No PRD050 EditMode tests exist yet for `BattleActionRules`.

## Required Follow-Up

1. Replace `string.GetHashCode()` in basic attack deterministic damage with a stable deterministic hash.
2. Add focused EditMode tests for Battle Action validation/result output before expanding more migration.
3. Move AI candidate generation/search simulation from `TacticalAIActionIntent` to `BattleAction`.
4. Replace live non-skill mutation with a result-event applier instead of calling `MouseControler.TryStart*` authority methods.
5. Continue the full PRD050 purge only after each action family has parity tests and manual Unity validation.
