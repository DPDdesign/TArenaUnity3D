# TArenaUnity3D Context Map

Status: active
Last updated: 2026-06-10

## Purpose

This file routes agents to the right TArenaUnity3D project context.

This workspace is TArenaUnity3D, not Retsot Horde. Do not use context, tasks,
agents, or markdown files from another project unless the user explicitly asks
for comparison or migration. Chat/task titles must start with `[TARENA]`.

## Project Documentation Layout

Canonical project markdown lives under `_codex/`:

- `_codex/Context/` - design, production, gameplay, level, AI, and technical context templates.
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

## Default Implementation Context

Use for normal Unity/C# work:

- `AGENTS.md`
- `_codex/agents/coding-agent.md`
- `_codex/agents/runbooks/unity-coding.md`
- `_codex/agents/runbooks/testing.md`
- `_codex/agents/docs/codebase-map.md`
- the specific task file from `_codex/tasks/`, when provided
- relevant C# files under `TArenaUnity3D/Assets`

## Production Context

Use when reviewing progress, deciding next work, or controlling scope:

- `_codex/Context/02_Current_State.md`
- `_codex/Context/03_Production_Rules.md`
- `_codex/Context/04_Milestones.md`

## Code Architecture Context

Use for code navigation, cleanup planning, dependency cutting, and agent
handoffs:

- `_codex/agents/docs/codebase-map.md`
- `_codex/tasks/Analysis/001_CODEMAP-001_CodebaseContextMap_Analysis.md`

Code-map headline: apparent core game scripts live in root `Assets/*.cs`,
`Scripts/Lesisz/HexMap`, `Scripts/Lesisz/Menu`, `Scripts/Lesisz/Skills`,
`Scripts/Lesisz/PathFinding`, `Scripts/Cielu`, and `Scripts/Multiplayer`.
Photon/PUN, PlayFab, PhotonChatApi, and plugin/demo folders are heavy
legacy/vendor surfaces and should not be used as default implementation context
unless the task is specifically about dependency removal.

## Backend And Multiplayer Cleanup Context

Use when planning removal or replacement of PlayFab, PUN, Photon, chat,
networking, shop/profile backend, cloud stats, or matchmaking:

- `_codex/Context/02_Current_State.md`
- `_codex/Context/03_Production_Rules.md`
- `_codex/agents/docs/codebase-map.md`
- active cleanup task under `_codex/tasks/`

Do not delete SDK/package folders until game-code references and Unity scene
dependencies have been mapped for that task.

## Gameplay And Level Context

Use when designing or reviewing gameplay, skills, maps, levels, AI, or difficulty:

- `_codex/Context/01_Game_Design_Document.md`
- `_codex/Context/05_Level_Design_Rules.md`
- `_codex/Context/07_Current_Level_Designs.md`
- `_codex/Context/08_Current_Mechanics.md`
- `_codex/Context/10_Skill_Design_Rules.md`
- `_codex/Context/18_Game_Difficulty.md`
- `_codex/Context/19_Identity.md`

## Task Tracker Context

For local task workflows, use:

- `_codex/agents/runbooks/task-tracker.md`
- `_codex/skills/analyze-task/SKILL.md`
- `_codex/skills/implement-task/SKILL.md`
- `_codex/skills/fix-task/SKILL.md`
- `_codex/skills/qa-review/SKILL.md`
- `_codex/skills/close-task/SKILL.md`

## Source Priority

1. Current user instruction.
2. Specific task file.
3. Requested role file.
4. `AGENTS.md`.
5. Relevant runbook.
6. This context map.
7. Specific context document.
8. Existing C# code for implementation details.
