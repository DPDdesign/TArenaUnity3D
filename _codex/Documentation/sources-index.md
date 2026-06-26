# TArenaUnity3D Sources Index

Status: canonical
Last verified: 2026-06-26

## Purpose

This file classifies documentation sources for agent retrieval. Use it when an
agent needs to decide whether a document is canonical, support, task/PRD, QA,
archive, deprecated, or explicit-only.

Agents should not scan `_codex/Documentation/` broadly. Start from
`_codex/Context/CONTEXT-MAP.md`, then use this index only when the task needs a
documentation source outside the domain context maps.

## Source Types

- `canonical` - current source of truth for a topic.
- `support` - useful supporting information, not the sole truth.
- `task` - implementation/planning task.
- `PRD` - requirement/planning document; not default truth unless the task
  explicitly targets it.
- `QA` - review/check result; evidence, not canonical truth by itself.
- `archive` - historical; do not use as current truth unless explicitly asked.
- `deprecated` - outdated; use only when explicitly needed for comparison.
- `explicit-only` - for the user or for explicit requests; agents must not load
  it by default.

## Canonical And Support Markdown

| File | Type | Use When | Notes |
| --- | --- | --- | --- |
| `_codex/Documentation/sources-index.md` | canonical | Classifying documentation sources | This file. |
| `_codex/Documentation/CurrentSkills.md` | support | Skill catalog/presentation context is needed | Prefer active skill maps first. |
| `_codex/Documentation/ADR_015_SkillActionDefinitionOwnsSkillTextAndRules.md` | support | Skill definition ownership decisions are relevant | Link from `skill-api-map.md`. |
| `_codex/Documentation/User_Setup_Guide.md` | support | Manual Unity setup, SFX, presentation setup | Do not treat as code truth without inspection. |
| `_codex/Documentation/PRD030_OfflineDatabase_Map.md` | PRD/support | Explicit PRD030/offline database work | Task-scoped only; not global startup context. |
| `_codex/Documentation/Cleanup_Report_PRD_Tasks_046_052.md` | archive/support | Historical combat/API cleanup investigation | Use through `combat-skill-ai-history-risks-map.md`. |
| `_codex/Documentation/Prompt_RunArmyGeneratorDesign.md` | support | Explicit run army generator design prompt work | Prompt/reference, not canonical truth. |

## ADR Markdown

ADR files under `_codex/Documentation/ADR_*.md` are `support` sources. Use them
when the current task touches the decision they name. ADRs should not override
current code or a newer canonical domain map.

## HTML Documentation And Artifacts

HTML files under `_codex/Documentation/`, `_codex/tasks/`, or other `_codex/`
subfolders are `explicit-only` unless a current prompt asks to create or edit
that exact HTML artifact.

They are for the user, screenshots, generated visual reports, setup pages,
questionnaires, or manual reference. Agents must not load HTML documentation or
HTML task artifacts unless the user explicitly asks for that file or the current
task specifically names it.

Current explicit-only HTML files:

- `_codex/Documentation/2026-06-16_RunMetagame_DB_UI_Cleanup_Summary.html`
- `_codex/Documentation/AI_Setup_guide.html`
- `_codex/Documentation/CapsuleArt_CharacterRenderWorkflow.html`
- `_codex/Documentation/chat-ui-settings.html`
- `_codex/Documentation/PRD030_ArmySnapshotFields.html`
- `_codex/Documentation/PRD030_OfflineDatabaseTabs.html`
- `_codex/Documentation/RunStackRepresentationPrefabFlow_Summary.html`
- `_codex/Documentation/SkillIconSuggestions.html`
- `_codex/Documentation/SkillPresentationSetup.html`
- `_codex/Documentation/StartRun_DataMapper_Flow_Graph.html`

## Conflict Rule

When documentation conflicts:

1. Current user instruction.
2. Specific task/brief.
3. Canonical domain map.
4. Canonical context/source document.
5. Current code, when implementation truth is needed.
6. Support docs and ADRs.
7. PRD/task/QA history.
8. Explicit-only HTML or legacy references.

If conflict remains, report it instead of silently choosing a stale source.
