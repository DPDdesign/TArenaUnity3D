# Mods Context

Status: future
Last updated: 2026-06-15

## Purpose

This file records future TArenaUnity3D modding direction. It is not current
implementation scope.

Use this context when a task mentions mods, modding, user-created unit data,
external unit files, XML unit import, JSON unit import, catalog validation, or
player-authored content.

## Current Boundary

Mods are future scope. Do not implement mod loading, mod folders, runtime import,
packaging, signing, dependencies, or load order unless a future task explicitly
approves that work.

The built-in runtime unit source is the unit `ScriptableObject` catalog:

- `UnitCatalog` collects unit definition assets.
- Each built-in unit has its own `UnitDefinitionAsset`.
- `DataMapper` reads units through the catalog.
- `TosterHexUnit.skillstrings` still stores exact skill strings.

Legacy `Units.xml` is not runtime truth. It may be used only as editor migration
input or historical reference.

## Future Mod Unit Data Direction

Future unit mods may support plain-text data formats such as XML or JSON. Those
formats should be import formats, not direct runtime sources.

The future mod import flow should:

- parse supported XML and JSON unit data,
- validate data against the current unit definition shape,
- preserve skill string order exactly,
- reject duplicate unit ids unless an explicit override policy exists,
- report missing required fields and invalid numeric values,
- report unknown skill ids and missing skill-info or execution coverage,
- avoid mutating built-in unit assets silently,
- return accepted unit definitions plus structured validation errors.

## Architecture Rule

Keep XML and JSON parsing behind a mod import module. Battle, UI, run-metagame,
and skill execution should consume validated unit definitions, not parser-specific
data structures.

The unit catalog remains the canonical built-in authoring model. Mod import may
produce catalog-compatible definitions, but it should not make plain-text files a
second hidden runtime source of truth.

## Related PRDs

- `_codex/tasks/031_PRD_ModUnitDataImportValidation.md`
