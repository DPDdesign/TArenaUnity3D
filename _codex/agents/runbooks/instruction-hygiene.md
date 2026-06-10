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
