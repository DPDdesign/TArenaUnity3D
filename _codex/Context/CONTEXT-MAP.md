# TArenaUnity3D Context Router

Status: active
Last updated: 2026-06-26

## Purpose

This file is only the context router for TArenaUnity3D. Keep it short.

This workspace is TArenaUnity3D, not Retsot Horde. Do not use context, tasks,
agents, or markdown files from another project unless the user explicitly asks
for comparison or migration. Chat/task titles must start with `[TARENA]`.

Do not read every source document by default. Pick the smallest matching domain
map, then read only the concrete context files named by that map.

When the user asks to "add a context map", create a new focused map file under
`_codex/Context/maps/`. Do not add another large domain section to this router.

## Domain Maps

- Project state, production rules, milestones, task workflows:
  `_codex/Context/maps/project-production-map.md`
- GDD, identity, feel, design grill, gameplay/level/difficulty:
  `_codex/Context/maps/design-gameplay-map.md`
- UI, UI visuals, Run Map UI, responsive battle HUD, mockup routing:
  `_codex/Context/maps/ui-map.md`
- Run metagame, rewards, shops, saved armies, async defence, offline DB:
  `_codex/Context/maps/run-metagame-map.md`
- Current mechanics, code architecture, backend/multiplayer cleanup:
  `_codex/Context/maps/architecture-cleanup-map.md`
- Combat/skill/AI routing:
  `_codex/Context/maps/combat-skill-ai-map.md`
- Skill API:
  `_codex/Context/maps/skill-api-map.md`
- Battle Action API:
  `_codex/Context/maps/battle-action-api-map.md`
- Tactical AI:
  `_codex/Context/maps/tactical-ai-map.md`
- Combat presentation:
  `_codex/Context/maps/combat-presentation-map.md`
- Mods and future player-authored content:
  `_codex/Context/maps/mods-map.md`
- Heavy legacy, binary, generated-image, backup, and reference-only material:
  `_codex/Context/maps/legacy-reference-map.md`
- Documentation source classification:
  `_codex/Documentation/sources-index.md`
- Task, PRD, and QA classification:
  `_codex/tasks/README.md`

## PRD Loading Rule

PRD files and PRD-specific maps are not global startup context. Load a PRD, a
PRD task, or a PRD-specific map only when the current prompt, selected task, or
brief explicitly requires that PRD scope.

For PRD019/PRD030 run-metagame or offline-database work, route through
`_codex/Context/maps/run-metagame-map.md` first. That map names the relevant
PRD-specific maps and explains when they are task-scoped inputs.

## Documentation Hygiene Rule

Before creating or changing context maps, code maps, agent files, or source
routing docs, read `_codex/agents/runbooks/instruction-hygiene.md`. Keep maps as
routers to canonical sources, not duplicated knowledge dumps.

## Source Priority

1. Current user instruction.
2. Specific task file.
3. Requested role file.
4. `AGENTS.md`.
5. Relevant runbook.
6. This router.
7. Relevant domain map.
8. Specific context document.
9. Existing C# code for implementation details.
