# [TARENA] PRD: Mod Unit Data Import And Validation

- Status: future
- Type: PRD
- Area: Mods, Unit Data, Data Import
- Triage: not-ready-for-agent

## Problem Statement

TArenaUnity3D now uses a runtime unit catalog built from concrete
`ScriptableObject` unit definitions. This is good for Unity authoring, but it
does not yet give future mod authors a safe way to add or override unit data
outside the Unity editor.

In the future, players or mod authors may want to create new units through
plain data files such as XML or JSON. Without an import and validation module,
mod files could silently change stat shape, lose skill slot order, reference
missing skill ids, duplicate unit ids, or create units that cannot be loaded by
the existing battle, UI, and run-metagame data flow.

## Solution

Build a future mod unit data import and validation flow that accepts supported
plain-text unit data formats, validates them against the current unit catalog
shape, and produces reviewable unit definitions that can be used by the game.

The runtime unit catalog should remain the canonical in-game unit source. Mod
XML or JSON should be treated as input data that must pass validation before it
can become playable unit content.

## User Stories

1. As a mod author, I want to define a new unit in XML, so that I can create
   unit content without editing Unity assets directly.
2. As a mod author, I want to define a new unit in JSON, so that I can use a
   common data format that is easy to generate and diff.
3. As a mod author, I want the supported unit schema to match the current unit
   catalog shape, so that I understand which fields are required.
4. As a mod author, I want validation errors to name the bad unit and field, so
   that I can fix data quickly.
5. As a mod author, I want skill slot order to be preserved exactly, so that UI,
   animation slot logic, and skill execution stay predictable.
6. As a mod author, I want duplicate unit ids to be rejected, so that one mod
   cannot accidentally replace another unit without an explicit override rule.
7. As a mod author, I want missing required stats to be rejected, so that broken
   unit definitions do not reach runtime.
8. As a mod author, I want invalid numeric values to be reported, so that typos
   do not create unusable or impossible unit data.
9. As a mod author, I want unknown skill ids to be reported, so that playable
   units do not appear with missing `CastManager` or skill-info coverage.
10. As a mod author, I want sprite references to be validated separately from
    gameplay stats, so that missing art does not hide gameplay data errors.
11. As a mod author, I want the importer to support repeated validation without
    creating duplicate Unity assets, so that iteration is safe.
12. As a player, I want invalid mods to fail with clear errors, so that the game
    does not break silently.
13. As a player, I want modded units to use the same skill identity rules as
    built-in units, so that UI and skill execution remain consistent.
14. As a developer, I want mod import to sit behind a small module interface, so
    that XML and JSON parsing do not spread into battle, UI, or run-metagame
    modules.
15. As a developer, I want validation to test external behavior, so that future
    schema changes are caught without asserting parser internals.
16. As a developer, I want mod import to produce the same unit definition shape
    as the built-in catalog, so that existing callers do not need mod-specific
    branches.
17. As a developer, I want the current editor XML-to-SO migration path treated
    as prior art, so that future mod import does not reintroduce runtime XML
    coupling.
18. As a developer, I want mod data loading to be clearly separate from the
    canonical built-in catalog, so that recovery work can continue safely.
19. As a designer, I want built-in units to remain inspectable as
    `ScriptableObject` assets, so that Unity authoring stays ergonomic.
20. As a designer, I want mod units to be reviewable before they become playable,
    so that balance and identity problems can be caught before runtime.

## Implementation Decisions

- Do not implement this now. This PRD records future mod support only.
- Keep the runtime unit catalog as the canonical built-in unit source.
- Treat XML and JSON as import formats for mods, not as direct runtime sources.
- Build a mod unit import module with a small interface: load text input,
  parse supported format, validate unit definitions, and return accepted units
  plus structured errors.
- Keep XML and JSON parsing behind adapters. The rest of the game should consume
  validated unit definitions, not parser-specific data structures.
- Preserve the current unit definition shape: unit id/name, combat stats, cost,
  sprite reference, and ordered skill string list.
- Preserve exact skill strings as stable ids. Do not introduce a second skill id
  system for mods.
- Reject duplicate unit ids by default. If explicit override support is wanted,
  make it a separate decision with clear precedence rules.
- Validate required fields, numeric parsing, duplicate ids, skill slot order,
  missing skill ids, missing skill info, missing skill execution coverage, and
  missing presentation/icon resources as separate error categories.
- Do not let mod import silently change built-in unit assets.
- Do not let the skill presentation catalog grant, remove, or reorder unit
  skills.
- Use the current editor XML-to-SO migration as prior art only. Future mod
  import should not restore `Units.xml` as runtime truth.

## Testing Decisions

- Test the mod import module through its public behavior: input data in,
  accepted unit definitions and validation errors out.
- Add focused tests for valid XML input, valid JSON input, malformed XML,
  malformed JSON, missing fields, invalid numbers, duplicate ids, unknown skill
  ids, and preserved skill order.
- Add tests that accepted XML and JSON definitions produce the same unit shape
  for equivalent data.
- Add tests that invalid mod input does not mutate the built-in catalog.
- Use existing EditMode service-test style as prior art: deterministic tests over
  plain C# data where possible, without scene or prefab setup.
- Unity-side manual verification should cover importing a small sample mod,
  seeing accepted units in a catalog-like preview, and confirming rejected units
  show clear errors.

## Out of Scope

- Implementing mod support now.
- Loading mods at runtime in a shipped build.
- Designing a mod folder layout.
- Designing mod packaging, signing, dependency resolution, or load order.
- Balancing modded units.
- Changing built-in unit stats, skill ids, skill order, cooldowns, damage,
  targeting, or gameplay float values.
- Replacing the built-in `ScriptableObject` unit catalog.
- Rewriting `CastManager`, skill info, skill presentation, or UI icon lookup.
- Supporting skill creation through mods.
- Supporting non-unit mod content such as maps, rewards, shops, AI, VFX, or SFX.

## Further Notes

The current project state is intentionally simple: built-in unit runtime data is
in a `ScriptableObject` catalog. Future mod import should deepen that module by
adding a clean import and validation seam around plain-text mod data, while
keeping battle, UI, and run-metagame callers isolated from XML or JSON details.
