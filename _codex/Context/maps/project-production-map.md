# TArenaUnity3D Project And Production Context Map

Status: active
Last updated: 2026-06-26

## Use When

Use this map when reviewing progress, deciding next work, controlling scope,
checking production rules, or using the local task tracker.

## Project Documentation Layout

Canonical project markdown lives under `_codex/`:

- `_codex/Context/` - design, production, gameplay, level, AI, feel, and
  technical context templates.
- `_codex/Documentation/` - setup guides and practical project documentation.
- `_codex/tasks/` - active, archived, analysis, and QA task files.
- `_codex/agents/` - project agent briefs and support docs.
- `_codex/skills/` - project-local workflow skills.

## Current Project Goal

TArenaUnity3D is currently a legacy recovery project. Work should serve these
goals:

- excavate and document legacy code,
- cut non-working PlayFab, PUN, Photon, and multiplayer functionality,
- replace assets where needed,
- improve code architecture through small, safe, testable steps.

Do not treat this as a feature-growth project until the local gameplay loop and
legacy dependency map are stable.

## Production Context

Read only the files needed for the current decision:

- `_codex/Context/02_Current_State.md`
- `_codex/Context/03_Production_Rules.md`
- `_codex/Context/04_Milestones.md`
- `_codex/Documentation/sources-index.md` when classifying documentation sources
- `_codex/tasks/README.md` when classifying task/PRD/QA files

If a document is still a template, say so briefly and work from the latest local
facts.

## Task Tracker Context

For local task workflows, use:

- `_codex/agents/runbooks/task-tracker.md`
- `_codex/tasks/README.md`
- `_codex/skills/analyze-task/SKILL.md`
- `_codex/skills/implement-task/SKILL.md`
- `_codex/skills/fix-task/SKILL.md`
- `_codex/skills/qa-review/SKILL.md`
- `_codex/skills/close-task/SKILL.md`

Do not mass-rewrite tasks unless the current user instruction explicitly asks
for task migration.

## Default Implementation Context

Use for normal Unity/C# work:

- `AGENTS.md`
- `_codex/agents/coding-agent.md`
- `_codex/agents/runbooks/unity-coding.md`
- `_codex/agents/runbooks/testing.md`
- `_codex/agents/docs/codebase-map.md`
- the specific task file from `_codex/tasks/`, when provided
- relevant C# files under `TArenaUnity3D/Assets`

Use `_codex/Context/CONTEXT-MAP.md` only when the task needs design,
production, gameplay, AI, skill, level, difficulty, UI, or architecture context.

PRD-specific maps are task-scoped only. Do not load them as default
implementation context unless the current prompt/task/brief explicitly requires
that PRD scope.
