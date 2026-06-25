# [TARENA] 051 Combat API / Validator / AI Audit And Hardening

- Status: draft
- Type: implementation task
- Area: tactical battle, Battle Action API, validation, Tactical AI, skill execution boundary
- Owner: Coding Agent
- Source prompt: `C:\Users\piotr\.codex\attachments\6d0c72ab-e6f4-4467-9f6a-79a85bd63f9d\pasted-text.txt`
- Must run before: `_codex/tasks/052_CombatActionsSkills_AuditHardening.md`

## Goal

Perform a production audit and small corrective hardening pass for the current
tactical Combat API / Validator / AI usage after PRD046, PRD049, and PRD050.

Keep the original prompt goal: make sure combat actions flow through a clear
request -> validation -> execution -> result path, AI and player actions do not
bypass validation, illegal actions cannot enter execution, battle scene
initialization blocks premature AI actions, and the validator/API shape remains
ready for future server-side validation.

This is a cleanup and verification task. Do not redesign the combat system, do
not add new gameplay systems, and do not expand into a new full framework.

## Current Project State To Assume

PRD046 / PRD047 created the Tactical Battle AI architecture:

- `BattleSnapshot` is the pure planning state.
- `TacticalAIActionIntent`, `TacticalAICandidateGenerator`,
  `TacticalAISearchScoring`, `TacticalAIProfile`, `TacticalAIPlanCache`,
  `TacticalAILiveTurnIntegrator`, and `TacticalAIAsyncTurnIntegrator` are the
  PRD046/047 AI surfaces.
- `MostStupidAIEver` remains the live enemy-turn entry point and fallback.
- AI planning must not mutate Unity scene objects and async planning must use
  copied immutable snapshot/profile/skill metadata.

PRD049 created and partially migrated the skill API:

- `SkillDefinitionAsset` owns skill id, activation, targeting, resolution, and
  ordered effect data.
- `SkillUse`, `SkillContext`, `SkillRules`, `SkillCast`, `SkillResult`,
  `SkillTarget`, and `SkillQuery` exist.
- `MouseControler` already calls `SkillRules` for skill start legality,
  target highlighting, and clicked-target validation.
- AI skill planning/execution has moved toward `TacticalAIPlannedAction` plus
  `TacticalAISkillRulesExecutor`.
- `TacticalAICastManagerSkillIntentExecutor` was removed in PRD049F.
- Remaining debt: `CastManager` still exists, player/live skill commits may
  still depend on legacy paths, passives may still mutate through
  `TosterHexUnit`, `SpellOverTime`, and `HexClass`, and non-skill AI still uses
  legacy intent surfaces.

PRD050 introduced the broader Battle Action API direction:

- `BattleActionUse`, `BattleAction`, `BattleActionResult`, and
  `BattleActionValidationResult` exist in
  `Assets/Scripts/Lesisz/HexMap/BattleActionModels.cs`.
- `BattleActionRules` exists and validates/generates/applies move,
  move-and-attack, basic ranged attack, wait, defend, skill, and stance actions.
- `BattleActionRules.Validate(...)` already checks active actor, alive state,
  lifecycle blocking, destination legality, occupancy, basic target legality,
  and delegates skill validation into `SkillRules`.
- Current runtime still has visible legacy paths:
  `TacticalAIActionIntent`, `TacticalAIIntentRevalidator`,
  `TacticalAICandidateGenerator`, `TacticalAISearchCandidateExpander`,
  `TacticalAIExecutionBridge` legacy intent fields, and
  `MouseControler.TryStart*` live execution methods.

## Required Sources

Read these documents before changing code:

- `AGENTS.md`
- `_codex/agents/coding-agent.md`
- `_codex/agents/runbooks/unity-coding.md`
- `_codex/agents/runbooks/testing.md`
- `_codex/agents/docs/codebase-map.md`
- `_codex/Context/CONTEXT-MAP.md`
- `_codex/Context/BattleActionRules.md`
- `_codex/Context/AI_Context.md`
- `_codex/Context/09_CurrentSkills.md`
- `_codex/Context/10_Skill_Design_Rules.md`
- `_codex/Documentation/ADR_005_ActionValidationFuturePRD.md`
- `_codex/Documentation/ADR_014_TacticalAI_MultiplayerCompatibleValidation.md`
- `_codex/Documentation/ADR_015_SkillActionDefinitionOwnsSkillTextAndRules.md`
- `_codex/tasks/archive/046_PRD_TacticalBattleAI_V1.md`
- `_codex/tasks/archive/047_PRD_TacticalAI_AsyncDecisionPipeline.md`
- `_codex/tasks/049_PRD_TacticalActionSkillMigrationProgram.md`
- `_codex/tasks/archive/049ABC_PRD_SkillAPIAndFullMigration.md`
- `_codex/tasks/049ED_PRD_TacticalAIActionSelectionAndExecutionMigration.md`
- `_codex/tasks/049F_PRD_LegacySkillSystemCleanup.md`
- `_codex/tasks/050_PRD_BattleActionAPI_FullMigrationPurge.md`
- `_codex/tasks/QA/2026-06-25_2107_049F_QA_ArchitectureReview.md`

Do not read or use another project's context.

## Required Code Inspection

Do not scan the whole `Assets` folder. Start from these concrete files and only
expand to directly referenced battle/skill code when needed:

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionModels.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionRules.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionLifecycle.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotModels.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotBuilder.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotLiveAdapter.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/MouseControler.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/HexMap.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/HexClass.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TeamClass.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TurnManager.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/MostStupidAIEver.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIActionIntent.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAICandidateGenerator.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIIntentRevalidator.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIPlannedAction.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISearchScoring.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIExecutionBridge.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAILiveTurnIntegrator.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIAsyncDecisionPipeline.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISkillRulesExecutor.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISnapshotProbe.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIProfile.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillRules.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillUse.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillCast.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillResult.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillQuery.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillContext.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/CastManager.cs`
- `TArenaUnity3D/Assets/Scripts/SkillDefinitionAsset.cs`
- `TArenaUnity3D/Assets/Scripts/SkillCatalog.cs`
- `TArenaUnity3D/Assets/Scripts/DataMapper.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/SkillRulesTests.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAICandidateGeneratorTests.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAIExecutionBridgeTests.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAISearchScoringTests.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAIAsyncDecisionPipelineTests.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAILiveTurnIntegrationTests.cs`

## Scope

1. Audit the current combat flow.
   - Map the actual request -> validation -> execution -> result flow for
     player move, player basic attack, player skill, AI move, AI basic attack,
     AI skill, wait, and defend.
   - Identify which actions use `BattleActionUse` / `BattleActionRules` /
     `BattleActionResult`, which still use `SkillRules`, and which still use
     legacy intent or `MouseControler.TryStart*` authority methods.
   - Do not require full PRD050 purge unless a narrow fix is needed to stop a
     concrete bypass.

2. Audit the validator.
   - Treat `BattleActionRules.Validate(...)` as the current Battle Action
     validator.
   - Treat `SkillRules.Validate(...)` as the current skill validator used by
     `BattleActionRules` and remaining skill-only surfaces.
   - Check legality coverage for actor existence, actor alive/actionable state,
     active unit ownership, lifecycle blocking, destination validity,
     occupancy, path/move budget, target existence, target alive state, target
     team legality, cooldown, used-skill state, wait/defend state, and stance
     repeatability.
   - If a small obvious validation guard is missing, add it in the existing
     validator surface. Do not create another validator class unless the current
     code has no practical place for the guard.

3. Audit AI usage.
   - AI must not mutate battle state directly from planner/search code.
   - AI selected actions must be revalidated against current live state before
     execution.
   - Check whether AI legal candidate generation now uses
     `BattleActionRules.GenerateLegalActions(...)` where available, or whether
     it still depends on `TacticalAICandidateGenerator` /
     `TacticalAISearchCandidateExpander`.
   - For remaining legacy AI paths, document the exact class/method and mark
     whether it is expected PRD050 debt or a real bypass that must be fixed now.
   - Pay special attention to `TacticalAIExecutionBridge.TryExecuteLiveIntent`
     because current non-skill actions still call `MouseControler.TryStart*`.

4. Audit battle readiness and early AI actions.
   - Check the path from `MostStupidAIEver` into
     `TacticalAILiveTurnIntegrator` / `TacticalAIAsyncTurnIntegrator` and then
     `TacticalAIExecutionBridge`.
   - Ensure AI does not receive or execute a move/attack/skill before
     `HexMap`, `MouseControler`, `TurnManager`, teams, units, hex occupancy,
     and active unit state are ready enough to build a valid `BattleSnapshot`.
   - Add a small readiness guard only if the current checks can let AI act
     before the scene is initialized. Prefer existing snapshot/lifecycle state
     over new scene-specific flags.

5. Prepare for future server-side validation.
   - Do not implement a server.
   - Check whether `BattleActionUse`, `BattleAction`, `BattleActionResult`,
     `SkillUse`, `SkillCast`, and `SkillResult` remain DTO-like and free of
     live Unity object references.
   - Check whether `BattleActionRules` and `SkillRules` depend on live
     `MonoBehaviour`, UI click state, VFX, SFX, animation, or scene objects
     when validating.
   - If Unity-only dependencies appear in validation, either remove the small
     dependency or document exactly why it remains and what later PRD050 work
     must extract.

6. Legacy cleanup.
   - Search only within the required code inspection surface for legacy paths
     that bypass `BattleActionRules` or `SkillRules`.
   - Remove or reroute only small, safe bypasses.
   - If a path is clearly legacy but not safe to delete in this task, add
     `TODO_LEGACY_REVIEW` near the specific runtime path and describe it in the
     report.
   - Do not delete large systems such as `CastManager`,
     `TacticalAIActionIntent`, `TacticalAICandidateGenerator`, or
     `TacticalAIIntentRevalidator` unless the audit proves the exact file is
     unused and deletion is within this task's narrow fix scope.

## Known High-Risk Checks

- Rusher / Rush / Chop opening behavior:
  - AI must not issue a Rush before the battle snapshot and live scene are
    initialized.
  - Rusher must not execute Chop in place when validation says it should move
    first.
  - Rush must be accepted/rejected by shared skill/action validation, not by
    an AI-only target guess.

- Non-skill AI action path:
  - `TacticalAIExecutionBridge` currently still calls
    `MouseControler.TryStartMoveAction`, `TryStartMoveAndAttackAction`,
    `TryStartBasicRangedAttackAction`, `TryStartWaitAction`, and
    `TryStartDefenseAction`.
  - Decide whether each call is a safe legacy adapter guarded by
    `BattleActionRules`/revalidation, or a bypass requiring a small fix.

- Skill API bridge:
  - `BattleActionRules.ValidateSkill(...)` delegates to `SkillRules`.
  - `BattleActionRules.Apply(...)` converts `SkillResult` events into
    `BattleActionResult` events.
  - Verify no runtime path applies skill effects from unvalidated selected
    targets.

- Legacy skill execution:
  - `MouseControler.TryStartSkillAction(...)` still contains CastManager
    compatibility messages and preparation paths.
  - Treat this as known PRD049/050 debt unless it produces a concrete bypass
    in this task's tested path.

- Scene readiness:
  - `BattleSnapshotLiveAdapter.BuildSnapshot(...)` and lifecycle busy checks
    are the preferred readiness boundary.
  - Avoid adding a second broad scene state machine.

## Explicit Non-Goals

- Do not change damage, cooldown, range, cost, movement, action-point,
  initiative, status duration, target count, or balance values.
- Do not redesign Tactical AI scoring, profile weights, search depth, or
  personality.
- Do not implement server authority, networking, rollback, or replay storage.
- Do not perform full PRD050 legacy purge unless a tiny deletion is proven safe
  and necessary.
- Do not rename public or serialized fields without permission.
- Do not edit scenes, prefabs, materials, Animator Controllers,
  `.inputactions`, generated Unity files, `.asmdef`, or `.asmref`.
- Do not run Unity, `dotnet`, package restore, external build scripts, or SDK
  installation commands.

## Testing

If existing EditMode tests cover the touched pure code, add or update focused
tests only for the changed behavior. Prefer existing test files:

- `SkillRulesTests.cs`
- `TacticalAICandidateGeneratorTests.cs`
- `TacticalAIExecutionBridgeTests.cs`
- `TacticalAISearchScoringTests.cs`
- `TacticalAIAsyncDecisionPipelineTests.cs`
- `TacticalAILiveTurnIntegrationTests.cs`

Recommended focused test cases if the touched code supports them:

- illegal move is rejected before execution,
- illegal basic attack out of range is rejected,
- skill target rejected by `SkillRules` cannot execute,
- AI stale/illegal action is rejected before live execution,
- AI cannot execute while lifecycle/snapshot readiness says battle is busy or
  not initialized.

Unity compilation and Unity Test Runner execution are manual unless the user
explicitly authorizes a Unity command.

## Completion Report Required

Write a completion protocol under `_codex/tasks/QA/` with:

1. Current combat flow after changes:
   - request -> validation -> execution -> result.
2. Main classes by responsibility:
   - Battle Action API,
   - Validator,
   - Action request/use,
   - Action result,
   - AI decision/execution,
   - battle initialization/readiness.
3. What was changed.
4. What remains risky.
5. Server-side validation readiness:
   - YES / NO / PARTIAL,
   - what still needs extraction later.
6. Found and removed legacy paths.
7. Legacy paths left intentionally, including any `TODO_LEGACY_REVIEW`.
8. Things deliberately not changed.
9. Tests added/updated and tests not run.
10. Manual Unity checks still required.

## Acceptance Criteria

- AI uses the shared Combat/Battle Action or Skill validation boundary before
  live execution.
- Player actions use the shared validation boundary where current PRD050 code
  supports it, or remaining legacy adapter paths are explicitly documented.
- Basic actions and skills cannot enter execution from an obviously illegal
  unvalidated request.
- Battle scene readiness is checked before AI action execution.
- Validator code remains mostly snapshot/data/request based and is not tied to
  UI/VFX/SFX/animation as truth.
- New work stays compatible with future server-side validation.
- Legacy bypasses are removed when safely narrow, otherwise marked and
  reported.
- Gameplay values are unchanged unless a value was only miswired by the
  refactor and the correction is documented.

## Implementation - 2026-06-25

### What Changed

- `BattleActionRules`: added a shared turn-state blocking check so direct validation and generated action lists reject actions while the snapshot reports `IsActionBlocking` or `IsResolvingNewTurnSequence`.
- `TacticalAICandidateGenerator`: legacy AI candidates now return empty while the snapshot says the battle is action-blocking or resolving the new-turn sequence.
- `TacticalAIIntentRevalidator`: stale legacy AI intents now reject before execution while the live snapshot says the battle is action-blocking or resolving the new-turn sequence.
- `BattleActionLiveApplier`, `TacticalAICandidateGenerator`, and `TacticalAIIntentRevalidator`: added focused `TODO_LEGACY_REVIEW` markers at the remaining legacy runtime boundaries.
- `BattleActionRulesTests`: added `ActionsAndAICandidates_AreRejectedDuringNewTurnSequence`.
- No Inspector-visible fields were added, removed, or renamed.

### Automatic Test

Added a focused EditMode test in `BattleActionRulesTests`.

Manual run path:

- Open Unity.
- Go to `Window > General > Test Runner`.
- Select EditMode.
- Run `BattleActionRulesTests`.

Expected result: `ActionsAndAICandidates_AreRejectedDuringNewTurnSequence` passes, and the rest of `BattleActionRulesTests` continue to pass.

Agent-run status: Unity compilation and Unity Test Runner were not run, per project instruction.

### Unity Test

#### Unity Setup

No new scene, prefab, asset, material, controller, `.asmdef`, `.asmref`, or Inspector setup is required. Use the existing battle scene wiring for `HexMap`, `MouseControler`, `TurnManager`, `BattleActionLifecycle`, `MostStupidAIEver`, and `DataMapper`.

#### Play Mode Test

- Start a normal battle and advance to an enemy turn.
- Confirm AI does not act during new-turn passive/status/initialization resolution.
- Confirm AI resumes after readiness and lifecycle blocking clear.
- Confirm normal AI move, move-and-attack, basic ranged attack, wait, defend, and skill actions still execute.
- Confirm stale or illegal AI actions are rejected before live execution.

### QA Verdict

QA architecture review passed.

- Completion protocol: `_codex/tasks/QA/2026-06-25_2212_051_CodingAgentCompletion.md`
- QA report: `_codex/tasks/QA/2026-06-25_2217_051_QA_ArchitectureReview.md`
- Required follow-up: none for task 051.

### Notes

- This was not a full PRD050 purge.
- Non-skill AI live application still delegates through `MouseControler.TryStart*` adapter methods after validation.
- Player non-skill input still uses the existing `MouseControler` authority paths.
- `CastManager` compatibility and older passive/status/trap mutation surfaces remain documented PRD049/PRD050 debt.
- No gameplay values changed.

### Next Steps

- Let Unity import/compile the changed C# files.
- Run EditMode `BattleActionRulesTests`.
- Perform the Play Mode checks above before starting task 052.

## Compile Fix - 2026-06-25

- Fixed `TacticalAIExecutionBridge.TryRevalidateAction` local variable shadowing by renaming the non-skill `BattleActionValidationResult` local to `actionValidation`.
- Fixed `BattleActionRules.ValidateSkill` enum-to-string conversion by formatting `SkillValidationResult` rejection details before passing them to `BattleActionValidationResult.Invalid`.
- Unity compile was not run by the agent; this responds to the Unity Console errors reported by the user.
