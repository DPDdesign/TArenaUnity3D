---
name: implement-task
description: Run the TArenaUnity3D task implementation workflow. Use when the user writes "/implement taskID", "implement task taskID", "zaimplementuj task taskID", or asks for Coding Agent to implement a markdown task and then run QA Review. The workflow is Coding Agent implementation, QA Review, focused Coding Agent follow-up fixes when QA finds issues, final QA verdict, focused EditMode tests for manual Unity execution, and a final Implementation summary written into the task markdown.
---

# Implement Task

## Purpose

Use this skill to run one complete TArenaUnity3D implementation loop for a local markdown task.

This is a workflow skill, not a gameplay design skill.

## Project UI Text Rule

TArenaUnity3D uses TextMesh Pro only. In coding, QA, and optional UI mockup
steps, use TMP types such as `TMP_Text` and `TextMeshProUGUI`, never legacy
`UnityEngine.UI.Text`.

It coordinates:

1. Coding Agent implementation.
2. QA Architecture Review through `_codex/skills/qa-review/SKILL.md`.
3. Coding Agent response to QA findings.
4. Final QA check after follow-up fixes when fixes were needed.
5. Coding Agent unit/EditMode test writing after the final QA verdict.
6. Optional Unity UI mockup prompt for UI-facing tasks.
7. Writing the final implementation summary into the task markdown.
8. A compact final implementation summary for the user.

## Accepted User Forms

Treat these as valid:

```text
/implement 123
/implement task 123
implement task 123
zaimplementuj task 123
zaimplementuj 123
/implement OBJ-001
/implement _codex/tasks/123_STATUS-001_EnemyDisplacedStatus_Coding.md
```

If the task id is ambiguous, search only in `_codex/tasks/` by:

- exact path,
- filename prefix,
- numeric id anywhere in filename,
- task code such as `OBJ-001`.

If multiple task files match, stop and ask the user to choose one.

## Workflow

### 1. Resolve Task

Read:

- `AGENTS.md`
- `_codex/agents/coding-agent.md`
- `_codex/Context/CONTEXT-MAP.md`
- the selected task file under `_codex/tasks/`
- the latest matching pre-implementation report in `_codex/tasks/Analysis/`,
  if one exists

Do not scan the whole project.

If the task is not a coding task, stop and explain which agent/workflow should handle it instead.

If a matching report from `_codex/skills/analyze-task/SKILL.md` exists, read
it before inspecting C# files. Treat it as orientation, not as authority:
verify the task and current code have not drifted before implementing.

### 2. Implement As Coding Agent

Use Coding Agent rules:

- inspect only relevant files under `TArenaUnity3D/Assets`,
- implement the requested runtime/code change first,
- do not write unit/EditMode tests yet; tests are written after the QA review/follow-up loop,
- preserve public and serialized field names,
- do not edit prefabs, scenes, input assets, Unity assets, generated files, or unrelated files,
- exception: for UI-facing tasks, use `_codex/skills/make-ui-mockup/SKILL.md`
  only when the user explicitly requested a mockup in the prompt/task, or after
  asking the user whether they want one. In TArena, "mockup" means a Unity UGUI
  prefab unless the task explicitly says it is only a product sketch,
- do not create or update HTML, JavaScript, browser prototypes, or legacy
  `_codex/Gen_Im/RETSOT ONLINE/` output as the task mockup deliverable; those
  do not satisfy TArena UI mockup work,
- do not run Git, `dotnet`, Unity builds, external build scripts, package restore commands, or SDK installation commands,
- do not create or edit `.asmdef` / `.asmref` without explicit user permission,
- run Unity EditMode tests from command line only when the Unity executable path is known and the user allows that command,
- if the same project is already open in Unity Editor, do not start a second Unity instance by default because batchmode may fail on the project lock or interfere with the open Editor,
- make the smallest playable implementation,
- create exactly one Coding Agent completion protocol in `_codex/tasks/QA/`.

`unity-small-task` is not a substitute for this workflow. If the user invoked
`/implement` or asked to implement a markdown task with QA review, stay in the
full `implement-task` workflow even when the code change itself is small.

Practical boundary:

- `unity-small-task` = one Coding Agent pass, no protocol, no QA review loop.
- `implement-task` = markdown task workflow with protocol, QA review, optional
  focused follow-up, and final QA verdict.

### 3. Run QA Review

After the completion protocol exists, use:

- `_codex/skills/qa-review/SKILL.md`

Run QA Review against the current task/protocol, not an unrelated latest protocol if a specific task was implemented.

QA Review must:

- read `_codex/agents/qa-architecture-review-agent.md`,
- read the matching protocol in `_codex/tasks/QA/`,
- inspect only files named by the protocol and nearby related systems when needed,
- save one QA Architecture Review report into `_codex/tasks/QA/`,
- avoid code edits.

After QA Review finishes, read the generated QA report and carry its feedback into the implementation workflow. Do not only link the file.

Capture:

- QA verdict,
- actionable findings,
- non-blocking observations if they affect future work,
- whether QA requested follow-up fixes,
- report path.

### 4. Respond To QA As Coding Agent

Read the QA report.

If QA has no actionable findings:

- treat the first QA verdict as the final QA verdict,
- skip the duplicate second QA pass,
- continue to unit/EditMode test writing.

If QA finds actionable issues:

- implement only focused fixes tied to those findings,
- do not broaden scope,
- do not redesign systems,
- create a follow-up Coding Agent completion protocol in `_codex/tasks/QA/`,
- run `_codex/skills/qa-review/SKILL.md` one more time for the follow-up protocol.
- read the second QA report and use it as the final QA verdict.

Stop after one follow-up QA loop unless the user explicitly asks for another iteration.

### 5. Write Unit/EditMode Tests

After the final QA verdict:

- read `_codex/agents/runbooks/testing.md` before authoring tests,
- add focused EditMode NUnit tests under `TArenaUnity3D/Assets/Scripts/Tests/EditMode/` when the final implementation has deterministic logic that can be isolated,
- do not use Unity `LogAssert` in TArena EditMode tests; assert state/behavior instead, or use the local `UnityLogGuard.Expect(...)` helper only when a known warning must be accepted,
- do not create or edit `.asmdef` / `.asmref` without explicit user permission,
- do not run `dotnet test`,
- do not start Unity test execution automatically in this workflow,
- tell the user to run the tests manually in the already open Unity Editor.

The final response must explain:

- what each new test checks,
- why the tested logic does not require scene or prefab setup,
- that the tests were not run automatically,
- exactly where the user should run them in Unity Test Runner.

### 6. Ask About Unity UI Mockup When Relevant

If the implemented task affects UI, HUD, menu flow, task mockups, screen
prototypes, route/army/reward/shop previews, or any user-visible UI structure,
do not automatically create a mockup unless the current user prompt or task file
explicitly asks for one.

In TArena, a task UI mockup means a Unity UGUI prefab mockup. Historical
HTML/JS prototype pages are not valid mockup deliverables for new task work.

If the prompt/task already asks for a mockup, use:

- `_codex/skills/make-ui-mockup/SKILL.md`

Follow that skill before writing the final task summary.

If the prompt/task did not already ask for a mockup, stop and ask the user:

```text
Czy chcesz, zebym wygenerowal Unity UI mockup prefab dla tego taska?
```

Only create the mockup if the user says yes.

This step should:

- create or update the task mockup prefab,
- use approved UI assets from the UI context,
- use layout groups on natural lists,
- create prefab assets for repeated UI elements,
- prefix script-owner GameObjects with `Script_`,
- wire script references through serialized fields,
- validate prefab YAML/reference integrity,
- clearly state what still needs Unity import/manual verification.

This step must not create or update HTML/JS/browser mockups as a substitute for
the Unity prefab. If prefab creation is blocked, report the blocker instead of
falling back to web output.

Do not broaden runtime gameplay scope during mockup work.

### 7. Write Implementation Summary Into Task Markdown

Before sending the final response to the user, update the implemented task file
under `_codex/tasks/`.

Append a dated implementation section:

```text
## Implementation - YYYY-MM-DD
```

Under that heading, write the same final implementation summary that will be
sent to the user, in the same section order and with the same content:

- What Changed
- Automatic Test
- Unity Test
- QA Verdict
- Notes
- Next Steps

Use markdown heading levels that fit inside the task section, but do not change
the wording, omit sections, or shorten the content compared with the final
response.

If the task already has an older `Implementation` section, do not overwrite it.
Append a new dated section so the task keeps its history.

## Output

Final response must use compact, easy-to-scan formatting. Keep spacing tight: short headings, dense bullets, no decorative prose, no unnecessary blank lines.

Final response must use exactly these top-level sections:

- What Changed
- Automatic Test
- Unity Test
- QA Verdict
- Notes
- Next Steps

Section requirements:

- What Changed: plain-language summary, not a code diff. Include new, changed, and removed fields grouped by file and class. For each field, explain what object/system it affects, useful value range, what lower/higher values do in Play Mode, and one tuning hint. If no fields changed, state `No Inspector fields changed`.
- Automatic Test: describe unit/EditMode test files added or not added, what each test checks, where to click in Unity Test Runner, and what result the user should expect. State that tests are run manually by the user in Unity.
- Unity Test: split this section into two required subsections: `Unity Setup` and `Play Mode Test`. `Unity Setup` must include only concrete Inspector/scene setup: assignments, components to add, objects to place in scene, fields to set, and buttons/components to click before pressing Play. `Play Mode Test` must include only runtime actions and expected observations after pressing Play. Do not mix Play Mode actions into setup. Do not assume the user knows which new fields must be wired.
- QA Verdict: final QA pass/fail status, QA report path, actionable findings, non-blocking observations, and whether follow-up fixes were applied.
- Notes: important caveats, what to watch during testing, and intentional exclusions.
- Next Steps: only real next actions for the user, especially manual Unity Test Runner and Play Mode checks.

If a blocker appears, report the exact blocker and the smallest next decision needed.
