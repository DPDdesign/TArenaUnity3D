# [TARENA] 050 Completed State - Battle Action API Full Migration Purge

- Status: historical summary for context reduction
- Parent PRD stub: `_codex/tasks/050_PRD_BattleActionAPI_FullMigrationPurge.md`
- Historical full PRD: `_codex/tasks/archive/050_PRD_BattleActionAPI_FullMigrationPurge_HISTORICAL.md`
- Active close brief: `_codex/tasks/050_ACTIVE_CLOSE_BRIEF.md`
- Purpose: summarize completed PRD050 work so future agents do not need to
  reread the full implementation history.

## Completed By 2026-06-25 Initial Slice

- Added `BattleActionUse`, `BattleAction`, `BattleActionResult`, result events,
  action kinds, and validation helpers.
- Added snapshot-based validation and result preview for move,
  move-and-attack, basic melee/ranged attack, wait, defend, skill, and stance.
- Fixed basic attack deterministic damage to use a stable string hash instead
  of `string.GetHashCode()`.
- Added `BattleActionLiveApplier` as a Battle Action live-apply bridge.
- Moved Tactical AI root action command/revalidation data toward
  `BattleActionUse`, `BattleAction`, and `BattleActionResult`.
- Added focused EditMode tests in `BattleActionRulesTests`.

QA at this point found the work was an architecture slice, not full PRD050
closure.

## Completed By 2026-06-26 Follow-Up 1

- Removed legacy compatibility fields and factories from:
  - `TacticalAIPlannedAction`
  - `TacticalAISearchPlan`
  - `TacticalAIExecutionAttempt`
  - `TacticalAIExecutionResult`
- `TacticalAIExecutionBridge` now accepts and executes planned `BattleAction`
  payloads for non-skill actions.
- Fallback planning regenerates legal `BattleAction` candidates through
  `BattleActionRules.GenerateLegalActions(...)`.
- `TacticalAISnapshotProbe` builds plans with
  `TacticalAISearchPlanner.BuildPlan(...)` and executes ordered actions.
- Focused EditMode tests were updated to assert `BattleActionUse` /
  `BattleAction` payloads instead of legacy intent payloads.

Source audit passed then for removed runtime symbols:

- `LegacyIntent`
- `ExecutedIntent`
- `BestIntent`
- `TryExecuteOrderedIntents`
- `FromLegacyIntent`
- `FromCandidateIntent`

## Completed By 2026-06-26 Follow-Up 2

Deleted legacy Tactical AI intent/candidate/revalidator runtime files:

- `TacticalAIActionIntent.cs`
- `TacticalAICandidateGenerator.cs`
- `TacticalAIIntentRevalidator.cs`

Replaced or migrated related runtime use:

- `TacticalAISearchCandidateExpander` usage was replaced with direct
  `BattleActionRules.GenerateLegalActions(...)` search candidates.
- `TacticalAISearchScoring` now scores `BattleAction` candidates and simulates
  through `BattleActionRules.Apply(...)` result events.
- `TacticalAIExecutionBridge` revalidates all planned actions, including
  skills, through `BattleActionRules.Validate(BattleActionUse, BattleSnapshot,
  ...)`.
- `TacticalAIPlannedAction` no longer exposes `SubmittedSkillUse`,
  `PreviewResult`, or `FromSkill(...)`.
- `BattleActionLiveApplier` applies non-skill `BattleActionResult` events
  directly instead of delegating to `MouseControler.TryStart*`.
- `MouseControler.TryStartMoveAction`, `TryStartMoveAndAttackAction`,
  `TryStartBasicRangedAttackAction`, `TryStartWaitAction`, and
  `TryStartDefenseAction` act as player/legacy input adapters that submit
  `BattleActionUse` and execute through `BattleActionLiveApplier`.
- EditMode tests were updated to assert `BattleAction` legal action generation,
  search scoring, and bridge revalidation instead of deleted
  intent/generator/revalidator classes.

Source audit passed then for removed AI intent symbols:

- `TacticalAIActionIntent`
- `TacticalAICandidateGenerator`
- `TacticalAISearchCandidateExpander`
- `TacticalAIIntentRevalidator`
- `LegacyIntent`
- `ExecutedIntent`
- `SubmittedSkillUse`
- `PreviewResult`
- `FromSkill(`

## Still Not Closed After Follow-Up 2

These are now tracked in `_codex/tasks/050_ACTIVE_CLOSE_BRIEF.md`:

- `CastManager`, `SkillUse`, `SkillCast`, `SkillResult`, `SkillQuery`, and the
  skill-only `SkillRules` runtime surface still exist.
- active skill execution still flows through `TacticalAISkillRulesExecutor` /
  `SkillCast` internals after `BattleActionRules` validation.
- passive/trap/automatic action migration is not complete.
- `MouseControler.TryStart*` methods still exist as compatibility entry points,
  although non-skill bodies submit `BattleActionUse`.
- manual Unity compile, EditMode tests, and Play Mode parity checks are still
  required before closure.
