---
name: feature-grill
description: Create or extend a feature-planning grill as a local autosaving HTML PRD. Use when the user asks to grill a feature, prepare feature questions, create a PRD questionnaire, make an HTML form for decisions, add follow-up/dopytki questions, or continue a previous HTML grill with new tabs.
---

# Feature Grill

Use this skill to turn feature planning into an HTML questionnaire artifact for
the user. Generated HTML grills are not canonical knowledge sources for agents
unless the user explicitly asks to use a specific HTML file.

## Workflow

1. Read the relevant local project context maps, task files, code, schemas, and data before writing questions.
2. Create or update one local HTML file under the project task/documentation area.
3. The first tab must contain exactly 20 primary grill questions.
4. Each question must include:
   - a concise question,
   - the relevant area/tag,
   - a recommended answer,
   - an editable answer field.
5. The HTML must autosave all fields in `localStorage`.
6. If the user asks for follow-up grilling, add a new tab in the same HTML file. Do not create a second HTML unless explicitly requested.
7. Follow-up tabs should contain only unresolved, risky, contradictory, or implementation-blocking questions.
8. Preserve the existing `localStorage` key when updating an existing HTML so the user's saved answers still load.
9. Include export/copy controls when practical:
   - copy Markdown,
   - download JSON,
   - import JSON if answers may need to move between browser/localStorage contexts.

## Question Style

Ask concrete implementation-shaping questions, not generic product prompts.

Prefer questions about:

- source of truth,
- persistence boundary,
- deterministic seed behavior,
- data model shape,
- validation rules,
- scope exclusions,
- test seams,
- migration/compatibility risks,
- UI contract only when it affects implementation.

## HTML Requirements

- Use a single self-contained `.html` file with embedded CSS and JavaScript.
- Keep answers in a versioned `localStorage` key.
- Render tabs from data arrays so later follow-up tabs are easy to add.
- Generate a Markdown preview from every tab.
- Keep the file useful when opened directly from disk.

## Follow-Up Tab Rules

When adding dopytki:

- Name the tab clearly, for example `Dopytki 1`.
- Keep the original 20-question tab unchanged unless correcting a factual/code-context error.
- Add follow-ups that point at the decision they refine.
- If user answers are not readable from the filesystem because they live only in browser `localStorage`, say that briefly and add an import JSON control or instructions inside the HTML.

## TArena Defaults

For TArenaUnity3D:

- Place PRD grill HTML files in `_codex/tasks/` unless the user names another local path.
- Start titles with `[TARENA]`.
- Inspect `_codex/Context/CONTEXT-MAP.md`, the smallest relevant domain map,
  and relevant C# files before generating questions.
- Load PRDs only when the user prompt, selected task, or domain map explicitly
  requires that PRD scope.
- Treat generated HTML grill files as explicit user artifacts, not as current
  project truth for future agents.
- Do not implement code from the grill unless the user explicitly asks for implementation.
