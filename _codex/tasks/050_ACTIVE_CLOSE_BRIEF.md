# [TARENA] 050 Active Close Brief - Battle Action API Full Migration Purge

- Status: closed - superseded by final closure report and follow-up validation PRD
- Parent PRD stub: `_codex/tasks/050_PRD_BattleActionAPI_FullMigrationPurge.md`
- Historical full PRD: `_codex/tasks/archive/050_PRD_BattleActionAPI_FullMigrationPurge_HISTORICAL.md`
- Completed-state summary: `_codex/tasks/050_COMPLETED_STATE.md`
- Owner: Coding Agent
- Purpose: close the remaining PRD050 runtime migration without loading the full historical PRD unless a conflict appears.

## Read This First

For the next coding pass, use this file as the active task source.

Do not reread the historical full PRD by default. Open it only when this brief
is ambiguous or conflicts with code.

Required local context:

- `AGENTS.md`
- `_codex/agents/coding-agent.md`
- `_codex/agents/runbooks/unity-coding.md`
- `_codex/agents/runbooks/testing.md`
- this file
- `_codex/tasks/050_COMPLETED_STATE.md`

Do not load `_codex/tasks/archive/050_PRD_BattleActionAPI_FullMigrationPurge_HISTORICAL.md`
unless this brief conflicts with code or lacks a required original decision.

Do not load PRD019/PRD030 run-metagame maps for this close pass unless the
actual code change touches run metagame screens, persistence, database mapper
flow, or PRD019 UI.

## Goal

PRD050 close scope was to remove the remaining runtime split between Battle
Action and the legacy skill / automatic-action paths.

Done means every tactical runtime action path is submitted, validated, scored,
simulated, and executed through:

- `BattleActionUse`
- `BattleAction`
- `BattleActionResult`

Final code closure report:

- `_codex/tasks/QA/2026-06-26_050_FinalClose_CodingAgentCompletion.md`

Remaining Unity validation follow-up:

- `_codex/tasks/051_PRD_PRD050_UnityValidationAndLegacySceneCleanup.md`

## Already Done

Use `_codex/tasks/050_COMPLETED_STATE.md` for detail. In short:

- Battle Action models/rules/result events exist.
- Tactical AI non-skill planning no longer uses legacy intent DTOs.
- legacy AI intent/candidate/revalidator files were deleted.
- AI search candidate expansion uses `BattleActionRules.GenerateLegalActions(...)`.
- AI search scoring simulates through `BattleActionRules.Apply(...)`.
- non-skill live apply no longer delegates to `MouseControler.TryStart*`.
- `MouseControler.TryStart*` non-skill methods are now compatibility adapters that submit `BattleActionUse`.

Do not redo this work unless code inspection proves it regressed.

## Remaining Scope To Close

### 1. Remove Runtime Skill DTO Split

Audit and remove or retire runtime use of:

- `SkillUse`
- `SkillCast`
- `SkillResult`
- `SkillQuery`
- skill-only `SkillRules` runtime authority

These may remain only if converted into internal data owned by Battle Action
implementation and no longer compile as independent runtime action DTOs.

### 2. Remove `CastManager` As Runtime Authority

`CastManager` must not remain the active skill execution system.

Allowed end states:

- deleted, if no longer referenced and safe to remove;
- or reduced to a non-authoritative holder/adapter only if deletion would
  require Unity asset or scene edits not permitted by this task.

If kept temporarily, document exactly why it remains and prove tactical runtime
execution does not depend on it as an authority.

### 3. Migrate Active Skill Execution

Active skill execution must flow through Battle Action validation/result/apply.

Known remaining area from previous notes:

- `TacticalAISkillRulesExecutor`
- `SkillCast` internals
- active skill execution after `BattleActionRules` validation

The close pass should make AI and player skill execution consume
`BattleActionResult` events, not a separate skill result pipeline.

### 4. Migrate Passive / Trap / Automatic Actions

Passive, trap, and automatic tactical mutations should become Battle Action
result events or automatic Battle Action applications.

Legacy hooks in tactical unit/hex/turn logic must not remain separate gameplay
authorities for action outcomes.

Keep gameplay float values unchanged unless the user gives explicit permission.

### 5. Reduce Player Compatibility Surface

`MouseControler.TryStart*` compatibility methods may remain only as player input
submission adapters.

They must not contain independent validation, mutation, damage, wait/defend, or
skill execution authority that bypasses Battle Action.

### 6. Remove Disabled Legacy Blocks

Previous follow-up noted one `#if false` legacy defense fallback block in
`MouseControler`.

Remove disabled legacy runtime blocks if code encoding permits safe deletion.
If not, document the exact file/region and why it remains.

## Acceptance Criteria

- `CastManager` is not a runtime authority.
- `SkillUse`, `SkillCast`, `SkillResult`, `SkillQuery`, and skill-only
  `SkillRules` are removed, renamed, or made non-runtime/internal to Battle
  Action.
- active skills execute through `BattleActionUse` -> `BattleAction` ->
  `BattleActionResult` -> apply.
- passive/trap/automatic actions no longer bypass Battle Action result/apply
  authority.
- player and AI skill paths use the same validation/result/apply model.
- no legacy fallback path remains for action validation or execution.
- no gameplay float values are changed without explicit permission.
- no scenes, prefabs, materials, animator controllers, `.inputactions`,
  `.asmdef`, or `.asmref` files are edited unless the user separately permits
  the exact path.
- focused EditMode tests are added or updated for the migrated runtime surfaces.
- manual Unity compile, EditMode tests, and Play Mode parity checks are listed
  in completion notes.

## Coding Agent Completion Notes Required

Append the final close report to a new QA/protocol file under `_codex/tasks/QA/`
instead of expanding the parent PRD.

The report must list:

- changed files;
- deleted files;
- remaining legacy names found by source audit, if any;
- migrated action kinds;
- tests added/updated;
- tests not run;
- manual Unity checks required;
- whether PRD050 can be marked closed.
