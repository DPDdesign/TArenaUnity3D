# Testing And Response Runbook

Use this after every Unity/C# code change and whenever adding or checking tests.

## Local Environment

The user compiles and validates the project inside Unity.

Do not run or suggest:

- Git commands,
- `dotnet` commands,
- Unity builds,
- external build scripts,
- package restore commands,
- SDK installation commands.

## EditMode Tests

You may add or update Unity Test Framework `.cs` tests when the existing Unity
project setup already supports them.

Prefer tests for deterministic logic that does not require scene or prefab setup.
Do not create or edit `.asmdef` or `.asmref` without explicit permission.

## Final Response After Code Changes

Include: what changed, automatic tests, Unity setup, Play Mode test, QA verdict
for formal task workflows, notes, and next steps.
