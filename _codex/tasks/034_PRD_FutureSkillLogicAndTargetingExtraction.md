# [TARENA] PRD: Future Skill Logic And Targeting Extraction

- Status: future
- Type: PRD
- Area: Skills, CastManager, Targeting, Action Validation, Architecture
- Triage: not-ready-for-agent
- Related: `_codex/tasks/033_PRD_SkillCatalogScriptableObjectMigration.md`
- Related: `_codex/Context/09_CurrentSkills.md`
- Related: `_codex/Context/10_Skill_Design_Rules.md`
- Related: `_codex/Context/BattleActionRules.md`
- Related: `_codex/Documentation/ADR_004_BattleActionLifecycleTurnSafety.md`
- Related: `_codex/Documentation/ADR_005_ActionValidationFuturePRD.md`

## Problem Statement

PRD 033 intentionally moves only skill metadata from XML into
`ScriptableObject` skill definitions. It does not move skill targeting,
cooldowns, passive triggers, or execution logic out of the legacy skill flow.

After PRD 033, `CastManager` still owns most skill implementation details:

- `{SkillName}M` methods configure targeting mode through public legacy flags,
- `{SkillName}` methods execute gameplay logic,
- some skill bodies directly start movement, damage, projectile, trap, or
  presentation flows,
- passive/autocast behavior is still split between `TosterHexUnit`,
  `SpellOverTime`, and empty `CastManager` methods,
- stance toggles still mutate runtime skill strings.

That structure is acceptable for the metadata migration, but it remains a
large architecture hotspot for future skill work.

## Solution

In a future task, design a deeper skill module model that gradually moves
targeting, validation-relevant skill data, passive triggers, and eventually
execution out of `CastManager`.

This future work should build on the skill catalog created by PRD 033, but it
must be scoped separately. The first future slice should probably move targeting
profile data out of `{SkillName}M` methods while keeping skill execution bodies
unchanged. Later slices can migrate execution by skill category.

## User Stories

1. As a designer, I want skill targeting shape to be authored in data, so that
   range, AoE, self-cast, ally target, enemy target, and empty-hex target rules
   are inspectable.
2. As a designer, I want cooldown authoring to live with skill definitions, so
   that cooldowns are not hidden in mode methods.
3. As a designer, I want passive trigger type to be explicit, so that passive
   skills are easier to reason about.
4. As a designer, I want stance toggles to be represented intentionally, so that
   runtime skill string mutation is not the long-term model.
5. As a developer, I want `CastManager` to stop exposing many public mode flags
   as its interface, so that skill targeting has better locality.
6. As a developer, I want skill execution to be migrated by category, so that
   projectile, movement, trap, pull, buff, debuff, and passive skills can be
   tested in smaller slices.
7. As a developer, I want player input, AI, and future network commands to use
   the same skill validation shape, so that skill legality does not drift across
   callers.
8. As QA, I want a skill-by-skill migration checklist, so that no skill silently
   loses targeting, cooldown, passive, or execution behavior.

## Implementation Decisions

- Do not implement this as part of PRD 033.
- Use the skill id string preserved by PRD 033 as the join key.
- Keep unit skill ownership in the unit catalog.
- Keep presentation data in the skill presentation catalog unless a future PRD
  explicitly changes that model.
- Treat targeting, validation, action lifecycle, and execution as related but
  separate modules.
- Respect ADR 004: action lifecycle owns turn safety and model-safe completion.
- Respect ADR 005: a full action validation module should be a separate PRD.
- Prefer migrating skill execution by category rather than rewriting all skills
  in one task.

## Out of Scope

- PRD 033 XML-to-ScriptableObject metadata migration.
- Renaming skill ids.
- Changing gameplay values, cooldowns, damage, ranges, AoE radii, movement, or
  status values without explicit approval.
- Editing Unity scenes, prefabs, Animator Controllers, materials,
  `.inputactions`, generated Unity files, `.asmdef`, or `.asmref` without a
  later explicit task.

## Further Notes

Suggested future order:

1. Skill targeting profile data.
2. Skill cooldown and action flag authoring.
3. Passive trigger metadata.
4. Legacy `CastManager` execution adapters.
5. Category-by-category execution extraction.
