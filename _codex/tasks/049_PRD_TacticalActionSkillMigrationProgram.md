# 049 PRD Tactical Action Skill Migration Program

- Status: draft
- Type: PRD program map
- Area: battle actions, skill definitions, validation, AI, execution cleanup
- Owner: TBD

## Goal

Track the full tactical action and skill migration program as a set of smaller
PRDs that can be grilled and implemented separately.

This program replaces the current mixed legality model, where skill legality,
targeting, UI highlights, AI candidates, and execution are spread across
`CastManager`, `MouseControler`, reflection methods, XML flags, and ad hoc AI
logic.

## Current State After 049ABC

049ABC is treated as completed for the shared API and skill-data foundation:

- `SkillDefinitionAsset` has the action-capable data fields.
- `SkillRules`, `SkillUse`, `SkillCast`, `SkillResult`, `SkillQuery`, and the
  related API/data model exist.
- Current skill assets have migrated SO data.

049ABC did not fully replace the live/default gameplay script path. Play Mode
skill commits can still execute through legacy `MouseControler` /
`CastManager` reflection bodies, and passive trigger mutation can still live in
legacy hooks.

Therefore remaining 049 work must treat every current active asset-backed and
unit-assigned skill as still in scope for runtime migration until a later task
proves that the skill no longer depends on legacy commit/passive paths. Do not
skip any active skill because it was listed as in-scope for 049ABC.

Legacy `CastManager` methods with no current skill asset and no unit assignment
remain reference-audit only unless implementation finds a live demo reference.

## Core Program Rule

The shared validator is for player, AI, and future server-side validation.
It checks legality only. It does not judge tactical quality.

The long-term source of truth for skills is the existing `SkillDefinitionAsset`
extended with action-capable data:

- stable skill id,
- activation and turn-cost rules,
- target family and target rules,
- resolution rules,
- effect data.

C# code owns validation and execution behavior. SO data describes the skill.
Rules should be enum-driven with simple serialized fields. Do not plan custom
per-skill rule plugins as the default model.

Presentation remains in the existing `SkillPresentationCatalog` and is joined
by skill id. PRD49 does not move VFX/SFX into `SkillDefinitionAsset`.

`skills.xml` is not a long-term source of truth. Skill text, activation rules,
target contracts, and effect data should migrate into skill SO data. See
`_codex/Documentation/ADR_015_SkillActionDefinitionOwnsSkillTextAndRules.md`.

`CastManager` is legacy/frozen. It may exist during transition for
not-yet-migrated skills, but new legality, targeting, AI, and migrated
execution must not depend on it as an authority or fallback.

## Current Accepted Decisions

### Validation And Targeting

- One shared tactical action model covers movement, basic attacks, and skills.
- Player and AI submit minimal intent; validator produces normalized action.
- Player/UI and future server validate submitted intent.
- AI uses legal action generation and does not guess targets.
- Target selections are ordered target hex sequences.
- `targetUnitId`, affected units, destination, and impact are normalized output,
  not trusted input.
- UI highlights and previews should come from the same validation/resolution
  rules.
- Validator reads only snapshot plus skill/action definitions.
- If snapshot lacks needed legality data, extend snapshot instead of reading
  live Unity objects.

### Skill Data

- `SkillDefinitionAsset` is the long-term complete skill description.
- Rules use enums and simple fields.
- No default custom per-skill rule plugins.
- Main target families for migration are:
  - `Self`,
  - `UnitTarget`,
  - `HexTarget`,
  - `Movement`.
- Details such as passive, toggle, AoE, all enemies, trap, line, teleport,
  spawn, status, damage, HP cost are tags/fields, not narrow families.

### Effect Data

- A skill has an ordered `effects[]` list.
- Each effect entry has an `effectType`, one target source, and simple fields.
- Effects execute in list order.
- Cooldown and turn-cost live in activation/cost rules.
- HP cost, movement, damage, status, trap placement, spawn, and stack changes
  are effects.
- Missing effect target is an error by default; `skipIfNoTarget` is explicit.
- Damage modes are domain enum values discovered from real skills, not legacy
  method names and not invented in advance.
- Statuses become separate `StatusDefinition` SOs, migrated with the skills
  that need them.
- Traps become separate `TrapDefinition` SOs; trap trigger effects reuse the
  same effect-entry model with `TriggeringUnit`.
- Stack changes use a shared StackModification domain API also used by rewards.
- Damage should eventually calculate outcome then apply HP/amount changes
  through StackModification.
- StackModification must support amount and temp/front HP state, and be usable
  for future preview/apply flows.
- Preview is part of 049A for the acceptance skills. 049AC keeps effect data
  compatible with preview for the remaining skills.

### AI

- AI legal action generation receives immutable snapshot/spec data and can run
  asynchronously.
- AI receives full legal `ValidatedTacticalAction` candidates.
- AI may score and prune legal actions.
- AI heuristics are allowed for tactical value, not legality.
- Migrated AI execution path consumes `ValidatedTacticalAction` directly.
- Do not add a legacy `TacticalAIActionIntent` adapter for the new action API
  path.
- AI compares skills, movement, basic attacks, wait, and defend together.
- If a non-skill action scores better than a skill, AI may choose it.
- Unsupported legacy-only skills are skipped and logged with warnings.
- This skip rule applies only to legacy methods with no current skill asset and
  no unit assignment. Every active asset-backed/unit-assigned skill must be
  handled by the 049 migration.

### Execution

- 049ED targets a new execution system, not a live legacy adapter.
- Core executor should be a pure state transition where possible:
  input state plus validated action -> output state plus gameplay result events.
- Presentation runs after logic is complete.
- Core executor emits gameplay result events.
- Presentation layer/planner builds presentation cue events separately.
- Use C# events/orchestration where needed, not UnityEvent as domain event
  model.
- Existing `SkillPresentationManager` may be reused as presentation adapter,
  not gameplay executor.

### Cleanup

- Cleanup happens per migrated family.
- Reference audit is required before deleting legacy family paths.
- Delete retired `CastManager` methods immediately after audit and validation;
  do not keep long-term commented legacy code.
- Every skill in a cleaned family receives manual Unity validation.
- `skills.xml` deletion is its own focused cleanup subtask inside 049F.

## PRD Sequence

### 049A - Tactical Action Validation Execution UI Vertical Slice

File: `_codex/tasks/049A_PRD_TacticalActionValidationSO_UI_VerticalSlice.md`

Purpose:

- first working `SkillDefinitionAsset` + validator + UI highlight/preview +
  execution slice,
- snapshot-based validator,
- acceptance skills: traps, Rush, Double Throw, Heavy Fists, Force Pull,
- generic effect handlers and structured result events,
- deterministic damage from one game seed plus action context,
- trap placement and trap trigger migration for Spike Trap and Rope Trap,
- no AI integration,
- no global `CastManager` removal.

Status:

- Merged into closed 049ABC.
- Retained only as reference detail for the validator/UI slice.

Implementation boundaries:

- extend existing `SkillDefinitionAsset`; do not add a separate skill action SO,
- allowed asset edits are the acceptance skill definition assets plus simple
  trap/status definitions needed by those skills,
- presentation stays in `SkillPresentationCatalog`,
- UI legal target state comes from validator, while visual style can remain
  similar to current highlights,
- player commit for acceptance skills does not call `CastManager.startSpell()`,
- validator/executor must be generic and data-driven, not per-skill methods.

### 049AC - Skill Target And Effect Data Model

File: `_codex/tasks/049AC_PRD_SkillTargetAndEffectDataModel.md`

Purpose:

- map all active skills into broad target families,
- keep families broad: `Self`, `UnitTarget`, `HexTarget`, `Movement`,
- define exact effect data categories needed in SO,
- copy existing values unless explicitly changed,
- prepare full skill data migration,
- keep target-family decisions and effect decisions together,
- define ordered `effects[]`, status/trap definitions, StackModification, and
  a minimum effect-type checklist for the current active skill set.

Status:

- Merged into closed 049ABC.
- Retained as the reference target/effect map.
- Supersedes archived 049B and 049C drafts.

### 049ED - Tactical AI Action Selection And Execution Migration

File: `_codex/tasks/049ED_PRD_TacticalAIActionSelectionAndExecutionMigration.md`

Purpose:

- AI stops guessing targets,
- AI receives legal `ValidatedTacticalAction` candidates from the shared API,
- AI scoring remains separate from legality,
- no heuristic skill target fallback,
- move all current active asset-backed/unit-assigned skill execution from
  legacy `CastManager` behavior toward SO-driven action execution,
- consume `ValidatedTacticalAction` directly without a `TacticalAIActionIntent`
  adapter,
- apply movement, damage, status, trap, spawn, cooldown, and turn-cost effects
  from normalized data.

Status:

- Current PRD49 production/planning route after ABC.
- Draft created by merging former 049D and 049E.
- Needs continued implementation grill before coding if implementation details
  are still open.

### 049F - Legacy Skill System Cleanup

File: `_codex/tasks/049F_PRD_LegacySkillSystemCleanup.md`

Purpose:

- remove or retire old sources of truth after migration,
- clean old highlight logic,
- remove obsolete reflection paths,
- remove stale XML flag dependency where replaced,
- prevent new `CastManager` behavior from being added.

Status:

- Draft updated with cleanup-family and XML deletion decisions.
- Needs later grill after migration plans are concrete.

## Dependency Order

Current dependency chain after 049ABC:

1. 049ABC has created the shared API/SO-data/validation-boundary foundation.
2. 049A and 049AC are retained as reference detail inside the closed ABC scope.
3. 049ED integrates AI selection and SO-driven execution after the 049ABC
   API/SO data foundation exists. It must not assume that live/default gameplay
   scripts already execute through the new API.
4. 049F removes legacy paths after replacement and validation.

After 049ABC, do not exclude active skills from 049ED just because their SO data
exists. Do not start 049F deletions before the replacement family has passed
reference audit and manual Unity validation.

## Recommended Grill Order

1. Grill 049ED before AI selection/execution implementation.
2. Grill 049F only when cleanup candidates are concrete.

## How To Grill A Sub-PRD

Use the same structure for each PRD:

- define the problem,
- confirm goals and non-goals,
- choose the minimal domain model,
- list acceptance examples,
- identify open questions,
- end with implementation-slice boundaries.

For each question, keep options short:

- Option 1,
- Option 2,
- recommendation,
- ask for decision.

Do not start implementation from these draft PRDs until the specific PRD has
been grilled enough to be implementation-ready.

## Program Non-Goals

- Do not build a server now.
- Do not use AI scoring as validation.
- Do not use `CastManager` as validator input.
- Do not rebalance skill values during migration unless explicitly approved.
- Do not add new gameplay behavior to `CastManager`.

## Program Completion

The 049 program is complete when:

- every active skill has a complete `SkillDefinitionAsset`,
- every active asset-backed/unit-assigned skill commits through the shared
  SkillRules/validated action/executor path instead of legacy reflection,
- player targeting UI uses shared validator output,
- Tactical AI uses legal action API candidates,
- execution no longer needs `CastManager` as source of skill behavior,
- legacy skill legality and targeting sources are removed or retired,
- `skills.xml` has been removed after SO text/rules/effects replace it,
- rewards, skills, damage, and future server flows share stack modification
  semantics instead of inventing separate stack mutation APIs.
