# TArenaUnity3D Task, PRD, And QA Index

Status: canonical
Last verified: 2026-06-26

## Purpose

This file defines how agents should classify local task, PRD, and QA files.
It does not replace individual task files.

## Current Folder Meaning

- `_codex/tasks/` - active or candidate task/PRD files. Read only the task
  named by the prompt or the smallest matching task.
- `_codex/tasks/archive/` - historical or completed task/PRD files. Do not use
  as current truth unless explicitly requested or routed by a history/risk map.
- `_codex/tasks/Analysis/` - pre-implementation analysis reports. Use only when
  implementing or reviewing the matching task.
- `_codex/tasks/QA/` - Coding Agent completion protocols and QA architecture
  reviews. Use only for the matching task/protocol/review workflow.
- `_codex/tasks/templates/` - task templates.
- `_codex/tasks/RunMetaGame_Tests/` - run-metagame test notes/tasks.

Nested archive folders, including obsolete mockups and generated UI notes, are
historical unless a current prompt explicitly asks for them.

HTML files under `_codex/tasks/` are explicit user artifacts, usually generated
questionnaires or visual reports. Do not use them as current agent knowledge
unless the user or selected task explicitly names the HTML file.

## PRD Rule

PRD files are not global project truth. A PRD is active only when the current
prompt, selected task, or domain map explicitly routes to it.

If a PRD describes current architecture, agents should prefer the active
context/code map or current code. Use the PRD to understand intent, acceptance
criteria, or historical scope, not as a competing source of truth.

## QA Rule

QA files are evidence for a specific completed protocol/review. They should not
become hidden architecture documents. If a QA report reveals current system
truth that future agents need, update or recommend updating the relevant
canonical context/code map.

## Completion Hygiene

After a task changes behavior, architecture, workflow, or source routing, check:

- relevant context map,
- relevant codebase map,
- canonical source document,
- task status,
- QA status,
- agent source list if routing changed.

Do not mass-edit old tasks just to make them match current wording. Mark or move
historical material only when a focused task asks for it.
