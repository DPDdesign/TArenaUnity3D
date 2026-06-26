# [TARENA] 050 Final Close - Battle Action API Full Migration Purge

- Date: 2026-06-26
- Agent: Coding Agent
- Scope source: `_codex/tasks/050_ACTIVE_CLOSE_BRIEF.md`
- Historical PRD050 full file was not loaded.

## Changed Files

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionModels.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionRules.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionLiveApplier.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionAutomaticResultApplier.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/HexMap.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/MouseControler.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/SpellOverTime.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIAsyncDecisionPipeline.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIExecutionBridge.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAILiveTurnIntegrator.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIPlannedAction.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIRevalidatedAction.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISearchScoring.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISkillRulesExecutor.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillQuery.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillResult.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/SkillRules.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/BattleActionAutomaticResultApplierTests.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAICandidateGeneratorTests.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAIExecutionBridgeTests.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAISearchScoringTests.cs`

## Deleted Files

- None. `SkillQuery.cs` could not be safely deleted in this environment, so it was reduced to a comment-only retired file.

## Migration Summary

- Active skill execution now flows through `BattleActionUse -> BattleAction -> BattleActionResult -> BattleActionLiveApplier`.
- `TacticalAISkillRulesExecutor` no longer exists as an independent executor; the file now contains `BattleActionSkillResultRuntime`, which applies `BattleActionResult` events for skill effects.
- `MouseControler.TryStartSkillAction` and selected-skill click flow submit Battle Action payloads and no longer call `CastManager.startSpell`.
- Passive/status ticks, trap triggers, trap lifetime ticks, Fire Movement trap placement, start autocasts, and passive aura status applications now build and apply `BattleActionResult` events through `BattleActionAutomaticResultApplier`.
- Legal action generation now hard-rejects passive skills and cooldown-blocked skill slots at `ValidateSkill`, and skill candidates are deduplicated by `StableOrderKey`.

## Migrated Action Kinds

- Active: `Move`, `MoveAndAttack`, `BasicMeleeAttack`, `BasicRangedAttack`, `Wait`, `Defend`, `Skill`, `Stance`.
- Automatic/passive/trap: `Passive`, `Trap`, `Automatic` via `BattleActionAutomaticResultApplier`.

## Remaining Legacy Names From Source Audit

- `CastManager` remains because it is scene-wired legacy surface; active tactical runtime callsites use it only for cleanup (`SetFalse`, `CancelPreparedSkillWithoutCommit`) and do not call `startSpell` or `getMode`.
- `CastManager.startSpell` remains as a method definition only; source audit found no active callsite.
- `SkillUse`, `SkillCast`, and `SkillResult` remain as internal implementation data consumed by `BattleActionRules` and `BattleAction` payloads.
- `SkillRules` remains as validator/preview implementation behind Battle Action validation; `SkillRules.Apply` and `ISkillRuntime` are removed.
- `SkillQuery.cs` remains as a retired comment-only file.

## Focused Tests Added Or Updated

- Added `BattleActionAutomaticResultApplierTests`:
  - `RopeTrapTrigger_BuildsTrapAndStatusResultEvents`
  - `OwnerWalkingIntoOwnFireTrap_BuildsNoMutationEvents`
  - `AutocastStatus_BuildsAutomaticStatusResultEvent`
- Updated `TacticalAICandidateGeneratorTests.PassiveAndCooldownBlockedSkills_AreNotLegalActions` with unique action key assertion.
- Existing Battle Action skill execution/search tests were updated earlier in this close pass to assert Battle Action payloads instead of independent skill executor payloads.

## Source Audit

- No matches for removed runtime bridge symbols:
  - `ValidatedSkillCast`
  - `ITacticalAISkillActionExecutor`
  - `TacticalAISkillRulesExecutor`
  - `TacticalAISkillRuntime`
  - `TryExecuteSkillAction`
  - `ISkillRuntime`
  - `SkillRules.Apply(`
  - `SkillQuery.`
- No active `castManager.startSpell` or `castManager.getMode` callsites found.
- No `#if false` or `#if 0` legacy runtime blocks found under `Assets/Scripts/Lesisz`.
- Direct `AddNewTimeSpell`, `AddTrap`, `RemoveTrap`, and `SpellOverTime.DoTurn` callsites now remain in:
  - `BattleActionAutomaticResultApplier`
  - `BattleActionSkillResultRuntime`
  - retired/non-called `CastManager`
  - primitive holder methods such as `HexClass.AddTrap` and `TosterHexUnit.AddNewTimeSpell`

## Tests Not Run

- Unity compile was not run.
- Unity EditMode tests were not run.
- Unity Play Mode parity was not run.
- `dotnet`, Unity builds, package restore, and git commands were not run, per project restrictions.

## Manual Unity Checks Required

- Unity compile after script reload.
- EditMode tests, including:
  - `BattleActionLegalActionGenerationTests.PassiveAndCooldownBlockedSkills_AreNotLegalActions`
  - `BattleActionAutomaticResultApplierTests`
  - `BattleActionRulesTests`
  - `TacticalAIExecutionBridgeTests`
  - `TacticalAISearchScoringTests`
- Play Mode parity:
  - Slash full movement highlight and second skill attack highlight.
  - Slash on empty hex still ends the unit action.
  - Basic melee counterattack still applies.
  - Trap trigger behavior for Rope, Fire, Spike.
  - Fire Movement trap placement/reveal.
  - New-turn passive/status tick sequence and autocasts.

## Closure Verdict

PRD050 can be marked code-closed pending manual Unity compile, EditMode test pass, and Play Mode parity checks above.

