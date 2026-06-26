# Instruction Hygiene Runbook

Use this when creating or changing TArenaUnity3D agent instructions, runbooks,
role files, skills, or context routing docs.

## Workspace Identity Guard

This workspace is TArenaUnity3D, not Retsot Horde.

Keep project-specific agents, tasks, skills, and context routed to local
TArenaUnity3D files under `_codex/` unless the user explicitly asks for
comparison or migration.

Do not add shared cross-project context as a default route.

## Rules

- Keep always-loaded files short.
- Put task-specific rules in task-specific runbooks or role files.
- Before adding a new rule, read nearby existing instructions and update them if
  a wording change is enough.
- Do not duplicate the same rule in many files unless it is a hard safety rule.
- Use one-line routing from broad files to specific files.
- Prefer concrete triggers.
- Remove stale or contradictory wording when you replace it.

## Documentation And RAG Hygiene

- Do not duplicate large blocks of knowledge across files. Link to the
  canonical source instead.
- Keep one source of truth per topic. If a canonical source already exists,
  update it instead of creating a competing source.
- Agent files should define role, responsibility, allowed actions, escalation,
  and required sources. Project knowledge belongs in context/source files.
- Update documentation only when code structure, behavior, workflow,
  responsibility, or source routing changes.
- Do not create summary-of-summary files. If a summary is needed, it must point
  clearly to the canonical source.
- HTML files under `_codex/Documentation/` are explicit-only user/reference
  files. Do not route agents to them unless the user or task names the HTML
  file.
- Classify documentation through `_codex/Documentation/sources-index.md`.
- Classify task/PRD/QA files through `_codex/tasks/README.md`.

## Map Hygiene

- Main maps must stay short routers.
- When a new context area is needed, create a focused file under
  `_codex/Context/maps/` and link it from `_codex/Context/CONTEXT-MAP.md`.
- When a new code area is needed, create a focused file under
  `_codex/agents/docs/codebase/` and link it from
  `_codex/agents/docs/codebase-map.md`.
- Do not put historical notes into active maps. Put them in an archive,
  history, or explicit-only risk file.
- Before finishing a documentation change, check for broken paths, stale names,
  duplicate truth, and whether retrieval became more precise.

## Final Documentation Check

Before reporting a documentation/routing task as complete, answer:

- Did I change code structure? If yes, did I update the code map?
- Did I change behavior or source truth? If yes, did I update the canonical
  context source?
- Did I create a knowledge file? If yes, did I link it from the relevant router?
- Did I find outdated docs? If yes, did I mark them archive/deprecated or report
  them?
- Did I avoid duplicating knowledge?
- Did I leave fewer ambiguities than before?
