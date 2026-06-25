# 049D PRD Tactical AI Legal Action API Integration

Superseded by:
`_codex/tasks/049ED_PRD_TacticalAIActionSelectionAndExecutionMigration.md`

049D is no longer an active standalone PRD. AI legal action selection and
SO-driven execution are now planned together in 049ED so the new AI path can
choose and execute `ValidatedTacticalAction` without a transition adapter or
`CastManager` fallback.

- Status: superseded
- Type: PRD
- Area: Tactical AI, battle actions, legal action generation
- Owner: TBD

## Goal

Migrate Tactical AI skill/action selection to the shared tactical action API.
AI must stop guessing targets from skill slots or legacy `CastManager` state.
AI should choose from legal `ValidatedTacticalAction` candidates generated from
`BattleSnapshot` plus `SkillActionDefinition` data.

## Core Rule

The validator checks legality only.

AI scoring decides tactical value.

AI must not have a private target-generation fallback that bypasses the shared
API.

## Problem Statement

Current Tactical AI can generate skill intents from available skill slots, then
expand or revalidate targets through limited logic. This causes invalid or
ambiguous skill usage, such as traps placed on occupied enemy hexes or line
skills without complete normalized action data.

The future AI path should use the same legal action source as player UI and
future server validation.

## Goals

- Replace AI skill target guessing with shared legal action generation.
- Score `ValidatedTacticalAction` candidates, not raw skill-slot intents.
- Keep AI scoring separate from validation.
- Preserve deterministic planning and snapshot-only worker-thread rules.
- Reject or skip skills without complete action specs.
- Remove fallback paths that try legacy skill targeting.

## Non-Goals

- Do not rebalance AI.
- Do not make validator judge action quality.
- Do not build server authority in this PRD.
- Do not rewrite skill execution in this PRD.
- Do not depend on live Unity objects during AI planning.

## Desired Flow

Current direction:

- capture/copy `BattleSnapshot`,
- load immutable skill/action definition data,
- generate full legal action candidates first,
- run AI scoring/pruning only after legal candidates exist,
- generate legal actions for the actor,
- score those legal actions,
- pick ordered candidates,
- before execution, revalidate against current snapshot,
- execute through the current execution bridge until 049E replaces execution.

Important:

- AI receives already legal, normalized candidates.
- AI may rank and prune.
- AI may simulate future outcomes.
- AI may not invent skill targets outside the API.
- AI heuristics may evaluate tactical quality, such as trap placement value or
  movement safety, but may not define legality.
- New AI execution path should consume `ValidatedTacticalAction`; do not add a
  legacy `TacticalAIActionIntent` fallback for migrated action API skills.

## Required API Behavior

The shared action API must provide AI-facing legal generation:

- input: battle snapshot, actor stack id, action definitions,
- output: legal `ValidatedTacticalAction` candidates,
- no live Unity references,
- deterministic order,
- stable ids,
- reason/logging for unsupported legacy-only skills.

Legal action generation should be able to run asynchronously on immutable
snapshot/spec data. It must not depend on live Unity objects.

The default conceptual output is all legal actions. Technical safety guards may
exist to prevent candidate explosion, but those guards are runtime protection,
not tactical scoring.

## Candidate Scoring Boundary

Validation output may include structural consequences:

- destination hex,
- impact hex,
- affected unit ids,
- affected hexes,
- effect family,
- action cost.

Validation output must not include "good/bad" tactical judgment.

AI scoring may consider:

- damage prediction,
- unit value,
- trap placement value,
- threat/safety,
- movement position,
- objective pressure later.

## Migration Risks

- Some skills may not yet have complete `SkillActionDefinition` data.
- Early AI may lose access to legacy-only skills until migration catches up.
- Search simulation may need new effect prediction data from 049AC.
- Candidate counts may grow for multi-target skills.
- Implementation should be sliced, but the new API path should stay clean: if a
  skill is not migrated, it is skipped/logged rather than executed through an
  old targeting fallback.

## Initial Grill Questions For 049D

Use these when this PRD is grilled:

1. Should AI wait until all active skills have specs, or integrate family by
   family?
2. How should AI log unsupported legacy-only skills?
3. Should legal action generation return all legal actions or capped/pruned
   candidates?
4. Where should deterministic ordering live: validator, AI candidate layer, or
   both?
5. How should AI simulate traps, pulls, movement, and spawn effects before 049E?
6. How does cached/async planning validate definition version changes?
7. What is the fallback if no legal skill actions exist: move/attack/wait/defend
   from the same API?

## Acceptance Criteria

Done when:

- Tactical AI no longer generates skill targets from slot availability alone.
- Tactical AI consumes legal action candidates from the shared API.
- AI scoring operates after legality generation.
- Unsupported skills are logged and skipped, not executed through fallback.
- Async/cached plans remain advisory and are revalidated before execution.
- No AI planning path reads `CastManager`, `MouseControler`, or live Unity
  objects.
