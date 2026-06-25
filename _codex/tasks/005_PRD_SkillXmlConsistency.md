# [TARENA] PRD: Skill XML Consistency Cleanup

- Status: superseded by SO skill catalog migration and PRD49ABC
- Type: PRD
- Area: Skills, Data Integrity, UI Info
- Label: superseded

## Supersession Note - 2026-06-25

This PRD describes the old XML-era skill consistency problem. It is no longer a
current implementation task.

Current skill context is:

- unit skill ownership: `UnitCatalog.asset` and unit definition assets,
- skill definition data: `SkillCatalog.asset` and `SkillDefinitionAsset`,
- shared API: `SkillRules`, `SkillUse`, `SkillCast`, `SkillResult`,
  `SkillQuery`,
- remaining cleanup: PRD49ED for execution migration and PRD49F for legacy
  removal.

Use this file only as historical context for why string-id consistency mattered
before the SO migration. Do not treat `Units.xml`, `skills.xml`, or
`CastManager` reflection as current skill architecture.

## Problem Statement

At the time this superseded PRD was written, TArenaUnity3D identified skills by
string IDs loaded from unit XML. Those same IDs were expected to connect unit
ownership, UI button text, right click descriptions, icon lookup, and skill
execution. The old XML-era data was not fully consistent: some skills were
assigned to units but had no skill info entry, some skill info entries were not
assigned to any unit, and a backup unit XML contained at least one skill ID not
present in the primary unit XML.

From the user's perspective, this makes skill work risky. A skill may appear on
a unit but have missing or empty UI information, a documented skill may not be
available to any unit, and future presentation work may accidentally configure
assets for IDs that are stale, duplicated, or misspelled.

## Solution

This superseded PRD proposed a small, explicit skill data consistency pass that
audited the old XML-era skill IDs, classified every inconsistency, and then
normalized the data in safe steps. The stable skill ID rule survived the
migration: the exact string assigned to units remains the skill id.

The original implementation was not intended to rewrite the skill system. It
was intended to make the then-existing XML data easier to trust by identifying
and resolving mismatches between unit assignment, skill info definitions,
optional copied XML files, icons, and executable skill methods.

The cleanup should be supported by a repeatable validation report so future
changes can be checked before broader skill presentation or balance work.

## User Stories

1. As a designer, I want to see every skill ID assigned to a unit, so that I know what skills were then playable.
2. As a designer, I want to see every skill info definition, so that I know what skills have player-facing text.
3. As a designer, I want assigned skills with missing info to be listed clearly, so that I can fill in missing UI descriptions.
4. As a designer, I want defined but unassigned skills to be listed clearly, so that I can decide whether they are planned, stale, or should be assigned.
5. As a designer, I want duplicate or suspicious assignments to be visible, so that repeated slots are intentional rather than accidental.
6. As a designer, I want backup or copied XML files to be treated separately from the primary data source, so that stale backup data does not create false game truth.
7. As a player, I want every visible skill button to have a readable name, type, and description, so that I can understand what the skill does.
8. As a player, I want skill UI to avoid blank or broken info panels, so that right-click skill inspection is reliable.
9. As a player, I want skills shown on units to correspond to real executable behavior, so that clicking a skill does not silently fail.
10. As a content author, I want the game to keep using the existing skill ID strings, so that icon, info, and execution references remain stable.
11. As a content author, I want missing skill info entries to be created without renaming assigned skill IDs, so that current unit setup remains intact.
12. As a content author, I want stale skill info entries to be marked or removed only after review, so that planned content is not lost accidentally.
13. As a developer, I want a repeatable skill consistency audit, so that future XML edits can be checked without manual searching.
14. As a developer, I want the audit to compare unit assignments against skill info definitions, so that XML mismatches are caught early.
15. As a developer, I want the audit to compare assigned skill IDs against executable skill methods, so that reflection-based runtime failures are caught early.
16. As a developer, I want the audit to optionally check icon resources by skill ID, so that UI icon gaps are visible before Play Mode.
17. As a developer, I want the audit to report copied or backup XML differences separately, so that only the primary data source drives current truth.
18. As a developer, I want a low-risk cleanup plan, so that data normalization does not change damage, cooldowns, targeting, or gameplay float values.
19. As a QA tester, I want a before/after inconsistency list, so that I can verify the cleanup changed only intended data.
20. As a QA tester, I want a manual Play Mode checklist for affected units, so that corrected skill info can be verified through the actual UI.

## Implementation Decisions

- Treat the primary unit skill assignment XML as the source of which skills were
  then playable.
- Treat the skill info XML as the source of player-facing skill type and
  description text.
- Treat exact skill ID strings as stable identifiers. Do not rename IDs as part
  of the first cleanup unless a specific rename is approved separately.
- Produce an audit report with these categories:
  - assigned skill IDs that are missing skill info entries,
  - skill info entries that are not assigned to any unit,
  - duplicate or suspicious unit skill slot assignments,
  - primary-vs-copy XML differences,
  - assigned skill IDs missing execution method coverage,
  - assigned skill IDs missing UI icon resources.
- Keep copied or backup XML files out of runtime truth. Differences in those
  files should be reported, not automatically merged.
- Resolve assigned-but-missing skill info entries by adding explicit skill info
  rows only after their intended description/type is known.
- Resolve defined-but-unassigned skill info entries by classifying them as
  planned, stale, or intentionally hidden before deleting or assigning them.
- Preserve current unit skill slot order unless the user explicitly approves a
  gameplay/data change.
- Do not change unit stats, skill balance values, cooldowns, damage math,
  targeting ranges, or gameplay floats in this PRD.
- Do not move skill assignment out of XML in this PRD.
- Do not introduce a new unrelated skill ID system in this PRD.
- Keep the audit script or report generation as a small plain-text/data tool
  with a simple output format that can be reviewed by a human.
- If a validation helper is added later, it should be able to run without
  starting Unity and without modifying Unity assets.
- Any actual XML cleanup should be split from audit/reporting if the change
  list becomes large.

## Testing Decisions

- The user compiles and tests inside Unity. No command-line Unity build, dotnet
  test run, package restore, external SDK command, or Unity build command is
  required for this PRD.
- A good validation test checks observable data contracts: which skill IDs are
  assigned, which have info, which have icons, and which have executable method
  coverage.
- If a standalone audit helper is added, test it with small fixture XML data so
  it catches missing info, unassigned info, duplicate assignments, and copied XML
  drift.
- Manual Play Mode testing should inspect one affected unit whose assigned skill
  was missing info before cleanup.
- Manual Play Mode testing should inspect one unit with only already-consistent
  skills to confirm existing UI behavior did not regress.
- Manual Play Mode testing should open skill buttons and right-click info for
  affected skills.
- Manual Play Mode testing should cast or attempt to target affected active
  skills only where the existing game already supports that flow.
- Testing should verify that no unit stats, skill slot order, cooldown behavior,
  damage behavior, or targeting behavior changed as part of data cleanup.

## Out of Scope

- Rewriting the skill system.
- Replacing XML skill assignment with ScriptableObjects.
- Renaming public or serialized fields.
- Renaming skill IDs without a separate approved migration plan.
- Changing gameplay float values, cooldowns, ranges, damage, stats, or balance.
- Editing Unity assets, prefabs, scenes, materials, controllers, generated Unity
  files, `.asmdef`, `.asmref`, or `.inputactions`.
- Building the full skill VFX/SFX presentation catalog.
- Creating new skill mechanics.
- Translating or rewriting every skill description for tone and balance.

## Further Notes

Historical audit snapshot from the old local XML data:

- 42 unique skill IDs are assigned to units in the primary unit XML.
- 33 skill IDs are defined in the skill info XML.
- 44 unique skill IDs exist across both sources.
- Skill info entries not assigned to any unit:
  - `Melee_Stance_Barb`
  - `Melee_Stance_Lizard`
- Assigned skill IDs missing skill info entries:
  - `Rzutnik_Skill1`
  - `Rzutnik_Skill2`
  - `Rzutnik_Skill3`
  - `Skill1`
  - `Skill2`
  - `Skill3`
  - `Tank_Skill1`
  - `Tank_Skill2`
  - `TeleportOT`
  - `Topornik_Skill1`
  - `Topornik_Skill2`
- Suspicious assignment:
  - `Topornik_Skill2` is assigned to both `Lizard:Skill2` and
    `Lizard:Skill3`.
- Backup/copy XML drift:
  - The copied unit XML contains `Range_Stance`, which is not present in the
    primary unit XML.

This PRD was intentionally focused on making old XML-era skill data trustworthy
before larger skill presentation, icon, or data migration work.
