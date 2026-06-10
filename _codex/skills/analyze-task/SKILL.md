---
name: analyze-task
description: Analyze a TArenaUnity3D markdown task before implementation.
---

# Analyze Task

Resolve a task under `_codex/tasks/`, read local TArenaUnity3D agent/runbook
context, inspect only relevant plain text or C# files, and write a concise
pre-implementation report to `_codex/tasks/Analysis/`.

Do not edit runtime code during analysis.
Do not load another project's tasks or context unless explicitly asked for
comparison or migration.
