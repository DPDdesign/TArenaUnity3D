# 049F PRD Legacy Skill System Cleanup

- Status: draft
- Type: PRD
- Area: skill cleanup, legacy removal, validation/execution migration
- Owner: TBD

## Goal

Remove or retire legacy skill-system sources of truth after the new
`SkillActionDefinition`, validator, AI integration, and SO-driven execution
paths are in place.

049F is deliberately last. Cleanup should happen only after replacement paths
are proven.

## Problem Statement

The current skill system has several overlapping sources of truth:

- `CastManager` reflection methods,
- `MouseControler` targeting state,
- legacy highlight flags,
- XML flags such as `NI` and `AM`,
- skill-slot assumptions,
- AI bridge behavior,
- scattered cooldown/turn-cost logic.

Earlier 049 PRDs replace these responsibilities step by step. 049F defines how
to safely remove the old paths without breaking active gameplay.

## Goals

- Remove old legality and targeting sources after replacement.
- Prevent new `CastManager` skill behavior from being added.
- Remove obsolete Inspector fields when legacy wiring is replaced.
- Retire reflection method dependencies for migrated skills.
- Remove duplicated XML flag sources where SO data owns the rule.
- Remove `skills.xml` completely after skill descriptions, rules, and effect
  data are migrated into skill SOs.
- Keep project documentation aligned with the new model.

## Non-Goals

- Do not delete legacy code before migrated parity is verified.
- Do not rebalance skills during cleanup.
- Do not clean unrelated Photon/PlayFab/UI code.
- Do not rename public or serialized fields unless explicitly approved.
- Do not edit prefabs/scenes/assets unless the specific cleanup task allows it.

## Cleanup Preconditions

A legacy path can be removed only when:

- the skill has a complete `SkillActionDefinition`,
- validator/UI target flow uses the new model,
- AI integration uses legal action API if relevant,
- execution has migrated for that skill or family,
- manual Unity scenarios pass,
- automated tests exist where feasible,
- there is no remaining runtime caller that needs the legacy method.

Cleanup should happen per migrated skill family, not as one large final deletion
pass. Before deleting a family legacy path, perform a reference audit for
remaining callers and data dependencies.

After a family cleanup, every skill in that family must receive manual Unity
validation for target selection, cast/commit, result, turn cost, and cooldown
where applicable.

## Cleanup Targets

### CastManager

Targets:

- skill mode methods,
- skill execution methods,
- targeting booleans,
- reflection routing,
- legacy multi-step temporary fields,
- hardcoded effect values once moved to SO.

Rule:

- `CastManager` remains frozen/legacy. Do not add new skill behavior to it.
- Remove retired family methods immediately after replacement passes reference
  audit and validation. Do not leave old methods commented out as a long-term
  transition crutch.

### MouseControler Skill Targeting

Targets:

- legacy skill highlight calls,
- selected skill target state that duplicates validator output,
- old target completion assumptions.

Rule:

- UI target legality must come from validator/resolution rules.

### XML Flags And Skill Metadata

Targets:

- `NI`, `AM`, passive/type/cooldown rules after SO owns them,
- duplicated skill descriptions,
- legacy XML skill text,
- any runtime dependency on `skills.xml`.

Rule:

- `skills.xml` is not kept as a long-term text or import source.
- Skill descriptions and player-facing skill text should be authored in the
  skill SO model.
- Remove XML only after migrated SO data covers the active skill set and runtime
  references have been replaced.
- Treat XML deletion as its own focused cleanup subtask inside the 049F cleanup
  phase.

### Tactical AI Legacy Bridge

Targets:

- skill-slot-only candidate generation,
- CastManager skill executor bridge,
- fallback candidate logic that bypasses action specs.
- old `TacticalAIActionIntent` command/candidate model after 049ED replaces it,
- old intent revalidator once player-equivalent submitted intent validation is
  used by AI,
- old AI execution bridge that calls `MouseControler`/`CastManager`,
- snapshot probe/debug UI that still reports legacy AI intent candidates.

Rule:

- AI should use legal action API candidates.
- After 049ED passes reference audit and manual validation, remove or retire:
  - `TacticalAIActionIntent.cs`
  - `TacticalAICandidateGenerator.cs`
  - `TacticalAISearchCandidateExpander` from `TacticalAISearchScoring.cs`
  - legacy `TacticalAISearchPlan` fields typed as `TacticalAIActionIntent`
  - `TacticalAIExecutionBridge.cs`
  - `TacticalAICastManagerSkillIntentExecutor.cs`
  - `ITacticalAISkillIntentExecutor`
  - `TacticalAIIntentRevalidator.cs`
  - legacy intent usage in `TacticalAIAsyncDecisionPipeline.cs`
  - legacy intent usage in `TacticalAILiveTurnIntegrator.cs`
  - legacy candidate/intent display paths in `TacticalAISnapshotProbe.cs`
- Keep names only if they are repurposed to the new submitted-intent or
  `ValidatedTacticalAction` model; do not keep old slot-target intent behavior
  under renamed wrappers.

## Initial Grill Questions For 049F

Use these when this PRD is grilled:

1. What is the exact definition of "legacy retired" for one skill?
2. What is the family cleanup order?
3. Which `CastManager` fields/methods can be removed first?
4. How do we detect runtime references before deletion?
5. What documentation must be updated after cleanup?
6. What exact runtime/UI references to `skills.xml` must be replaced before
   deletion?
7. What manual regression pass is required after deleting legacy paths?

## Acceptance Criteria

Done when:

- Retired skills no longer use `CastManager` legality, targeting, or execution.
- Legacy paths are removed per migrated family after reference audit.
- Obsolete Inspector fields introduced only for legacy wiring are removed.
- Legacy AI fallback paths are removed.
- XML flags no longer act as source of truth for migrated skills.
- `skills.xml` has no runtime dependency and is removed after SO descriptions
  and rules replace it.
- Documentation points future work to `SkillActionDefinition` and shared
  validator APIs.
- Manual regression confirms every skill in the cleaned family still executes
  correctly.
