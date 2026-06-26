# Cleanup Report - PRD / Tasks 46-52

Date: 2026-06-25
Status: completed documentation/task cleanup; Unity validation remains user-side

## 1. Co zostalo uporzadkowane

- Reviewed PRD/tasks 046-052, their child tasks, completions, and QA reports.
- Updated task status headers for 048, 049ED, 049F, 050, 051, and 052.
- Archived completed or superseded task files:
  - 048 completed with QA pass.
  - 049A, 049AC, 049D, 049E superseded by 049ABC/049ED.
  - 051 and 052 completed with QA pass.
- Left active:
  - 049ED as pass-with-residual-manual-validation-risk.
  - 049F as future cleanup / PRD050 debt.
  - 050 as partially implemented; larger continuation required.
  - 049 program map as the active PRD49 routing document.
- Updated the context map and codebase map with current Combat API, action
  validation, Tactical AI, Battle Scene readiness, skill data, and PRD050 debt
  routing.

## 2. Zaktualizowane dokumenty

- mapa kontekstow: `_codex/Context/CONTEXT-MAP.md`
- mapa kodu: `_codex/agents/docs/codebase-map.md`
- taski:
  - `_codex/tasks/049ED_PRD_TacticalAIActionSelectionAndExecutionMigration.md`
  - `_codex/tasks/049F_PRD_LegacySkillSystemCleanup.md`
  - `_codex/tasks/050_PRD_BattleActionAPI_FullMigrationPurge.md`
  - archived task files listed below
- QA: no QA report files were rewritten; open/closed QA state is summarized
  here from subagent verification and existing QA reviews.

## 3. Taski zarchiwizowane

- `_codex/tasks/archive/048_PRD_RewardGeneratorRuleSets.md`
- `_codex/tasks/archive/049A_PRD_TacticalActionValidationSO_UI_VerticalSlice.md`
- `_codex/tasks/archive/049AC_PRD_SkillTargetAndEffectDataModel.md`
- `_codex/tasks/archive/049D_PRD_TacticalAILegalActionAPIIntegration.md`
- `_codex/tasks/archive/049E_PRD_SODrivenTacticalActionExecutionMigration.md`
- `_codex/tasks/archive/051_CombatAPIValidatorAI_AuditHardening.md`
- `_codex/tasks/archive/052_CombatActionsSkills_AuditHardening.md`

## 4. QA zamkniete jako nieaktualne / zamkniete dowodem

- PRD051 battle readiness / premature first AI action: closed by readiness
  guards in `MostStupidAIEver`, `HexMap`, `BattleSnapshotLiveAdapter`, and
  `BattleActionRules`; Play Mode validation still recommended.
- PRD050 deterministic basic attack hash: closed by stable hash follow-up and
  `BattleActionRulesTests`.
- 049ED AI skill path consuming `TacticalAIActionIntent`: closed by
  `TacticalAIPlannedAction` skill payload plus execution bridge revalidation.
- 049ED async planning carrying `SkillDefinitionAsset`: closed by copied
  `SkillDefinitionSpec`.
- Rusher illegal first Rush: currently obsolete at code-structure level because
  shared Rush targeting/validation rejects off-line illegal targets.
- Rusher wrong Rush destination/not reaching previous straight hex: currently
  obsolete at code-structure level; `SkillRules` has Rush-line validation and
  tests. Live animation still can be manually checked.

## 5. QA nadal aktualne

- PRD050 legacy purge: runtime/test surfaces still reference
  `TacticalAIActionIntent`, `TacticalAICandidateGenerator`,
  `TacticalAISearchCandidateExpander`, `TacticalAIIntentRevalidator`,
  `LegacyIntent`, and related probe/execution fields.
- PRD050 live application: `BattleActionLiveApplier` still delegates non-skill
  mutation through `MouseControler.TryStart*`.
- PRD050 skill DTO transition: `BattleAction` skill handling still embeds the
  PRD49 `SkillUse` / `SkillCast` / `SkillResult` / `SkillRules` layer.
- PRD050 AI simulation: search/scoring internals still generate and simulate
  legacy intents instead of pure `BattleAction` apply.
- Player skill commit: player skills validate through `SkillRules` but can
  still execute through `CastManager.startSpell(...)` compatibility.
- Passive/trap trigger execution: still uses legacy hooks in `TosterHexUnit`,
  `SpellOverTime`, `HexClass`, and `Traps`.
- `Stone_Throw`: actor stack split/spawn parity remains high-risk because
  current effect data does not fully express dynamic "half current stack".
- `Double_Throw`: validation is covered, but live VFX/projectile/animation
  parity still requires manual Play Mode validation.
- Rusher choosing `Chope` in place instead of moving/Rushing: no code proof of
  illegal action; keep as manual AI behavior validation before any scoring fix.

## 6. Bugfixy przekazane Coding Agentom

Prepared handoff scopes. Do not start these as broad refactors without a
focused implementation task:

- PRD050 continuation 1: remove legacy AI intent/candidate runtime surfaces
  after replacing non-skill plans with `BattleAction` candidates.
- PRD050 continuation 2: replace non-skill `BattleActionLiveApplier` delegation
  to `MouseControler.TryStart*` with direct `BattleActionResult` live apply.
- PRD050 continuation 3: move player skill commit from `CastManager`
  compatibility to `BattleActionUse` / `SkillRules` / SO-result live apply.
- PRD050 continuation 4: migrate passive/trap triggers into automatic
  Battle Action or SkillResult events, leaving legacy hooks only as detectors
  until removal is proven.
- Stone_Throw parity fix: only after Unity repro or owner approval, add dynamic
  stack fraction support or a narrow SO-execution calculation for half-stack
  split parity.
- Double_Throw VFX fix: only after Unity repro, inspect
  `SkillPresentationCatalog.asset` first, then projectile reveal sequencing.
- Rusher scoring fix: only after Unity repro, add a focused AI scoring test for
  Rusher with `Chope` out of range and legal `Rush`/move alternatives.

## 7. Bugfixy wykonane

- No new code bugfixes were executed in this cleanup pass.
- Existing completed fixes from prior tasks remain:
  - PRD051 readiness/turn-state guard hardening.
  - PRD052 legacy non-skill AI intent revalidation through
    `BattleActionRules.Validate(...)`.
  - PRD050 deterministic basic attack stable hashing follow-up.

## 8. Rzeczy, ktorych nie ruszano

- No C# code was edited.
- No Unity assets, prefabs, scenes, materials, animator controllers,
  `.inputactions`, generated Unity files, `.asmdef`, or `.asmref` were edited.
- No gameplay numeric values were changed.
- No public or serialized field names were changed.
- No Unity builds, dotnet commands, package restores, or Unity Test Runner
  commands were run.
- No Git commands were run.

## 9. Ryzyka regresji

- Unity compile/import and EditMode tests remain manual.
- PRD050 is not closed; future work can regress AI if it removes legacy intent
  surfaces before direct `BattleAction` candidate/search/live apply parity.
- Player and AI paths still do not share a fully unified live action applier.
- Skill validation and live execution can still drift where player commit uses
  `CastManager`.
- Passives/traps can still drift from validator/result previews because their
  mutation remains in legacy hooks.
- `Stone_Throw`, `Double_Throw`, Rush/Rusher behavior, and trap/passive timing
  need targeted Play Mode checks.

## 10. Nastepny najmniejszy krok dla Project Directora

Run a manual Unity Play Mode validation pass for the high-risk checklist:

- first enemy turn after Battle Scene load waits for readiness,
- enemy AI legal move/move-and-attack/basic ranged/wait/defend/skill actions,
- `Double_Throw` on two targets and duplicate target,
- Rusher with `Chope` out of range and `Rush`/move alternatives,
- `Stone_Throw` stack split/spawn parity,
- `Spike_Trap`, `Rope_Trap`, and listed passives.

After that, open one narrow Coding Agent task from section 6 based on confirmed
Unity evidence. Do not start the full PRD050 purge as one large rewrite.

## Shutdown

Final process check found no Unity editor, Unity build, dotnet, MSBuild, NUnit,
or VSTest process requiring completion. Only `codex` and `unityvcstray` matched
the process-name check.

Computer shutdown skipped - no permission / unsupported environment.
