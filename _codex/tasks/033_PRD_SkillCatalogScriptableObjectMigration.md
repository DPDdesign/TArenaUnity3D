# [TARENA] PRD: Skill Catalog ScriptableObject Migration

- Status: implemented
- Type: PRD
- Area: Skills, Skill Data, DataMapper, ScriptableObject Catalog
- Triage: qa-pass
- Related: `_codex/tasks/032_PRD_UnitCatalogScriptableObjectMigration.md`
- Related: `_codex/Context/09_CurrentSkills.md`
- Related: `_codex/Context/10_Skill_Design_Rules.md`
- Related: `_codex/Context/BattleActionRules.md`

## Problem Statement

TArenaUnity3D already moved built-in unit runtime data into Unity-authored
`ScriptableObject` unit definitions. Skill ownership now lives on unit assets as
ordered skill strings, but basic skill metadata still comes from
`Resources/0_Data/skills.xml`.

That means the project has two different built-in data authoring models:

- unit stats and legal unit skill lists are authored as `ScriptableObject`
  assets,
- skill name, type, flags, and right-click description are still authored in
  XML.

The current XML skill data is not skill logic. It is metadata used by UI,
right-click skill info, passive detection, and action flags such as `NI` and
`AM`. Keeping this metadata in XML makes skill authoring inconsistent with the
unit catalog and leaves a stale runtime data source beside the new Unity asset
workflow.

## Solution

Migrate only the current XML skill metadata into Unity-authored
`ScriptableObject` skill definition assets collected by one skill catalog.

This first migration must preserve behavior. It should not move skill logic out
of `CastManager`, should not change targeting mode, should not change cooldowns,
should not add new skill categories, and should not change passive/autocast
behavior. Existing callers should keep using `DataMapper.FindSkill(...)`, but
`DataMapper` should read built-in skill metadata from the new skill catalog
instead of from XML.

The current stable skill id remains the exact skill string already used by unit
assets, runtime `skillstrings`, UI icon lookup, skill presentation, and
`CastManager` reflection.

## User Stories

1. As a designer, I want each built-in skill metadata record to have its own
   Unity asset, so that skill info is inspectable in the editor.
2. As a designer, I want all built-in skill metadata collected in one skill
   catalog, so that the current skill list is easy to review.
3. As a designer, I want skill id, type, flags, and description preserved
   exactly from XML, so that gameplay and UI behavior do not change.
4. As a designer, I want unit skill ownership to remain on unit definition
   assets, so that units still define their legal skill list.
5. As a designer, I want skill slot order to remain unchanged, so that UI and
   animation slot behavior stay predictable.
6. As a player, I want skill buttons to behave exactly as before, so that this
   data migration does not change battle actions.
7. As a player, I want passive skills to remain greyed/non-castable exactly as
   before, so that passive readability does not change.
8. As a player, I want right-click skill info to show the same type and
   description as before, so that the UI does not regress.
9. As a developer, I want `DataMapper.FindSkill(...)` to remain the skill
   metadata seam, so that existing callers do not need a broad rewrite.
10. As a developer, I want XML removed as runtime skill metadata truth, so that
    stale XML cannot silently drift from Unity-authored skill data.
11. As a developer, I want missing skill catalog setup to fail visibly, so that
    broken data setup does not fall back to stale XML.
12. As a developer, I want the skill id string preserved, so that unit assets,
    UI icons, skill presentation, and legacy execution continue to join on the
    same key.
13. As a future developer, I want this migration to leave room for later skill
    logic extraction, so that future `CastManager` work can start from a clean
    metadata source.
14. As QA, I want a small verification checklist, so that XML-to-SO migration
    can be confirmed without retesting the entire skill system.

## Implementation Decisions

- This PRD is only about moving current XML skill metadata to
  `ScriptableObject` assets.
- Preserve the current stable skill id exactly.
- Do not rename skill ids.
- Do not change unit skill ownership or skill slot order.
- Add a `SkillDefinitionAsset` with fields equivalent to current XML metadata:
  skill id/name, type, info, and flags.
- Add a `SkillCatalog` that collects built-in `SkillDefinitionAsset` entries.
- Add a serialized `SkillCatalog` reference on `DataMapper`.
- Keep `DataMapper.FindSkill(...)` and `DataMapper.SkillDefinition` as the
  compatibility seam for existing callers.
- Change `DataMapper` skill cache loading to read from `SkillCatalog` instead
  of `skills.xml`.
- Disable runtime XML fallback after the catalog migration.
- Preserve `skills.xml` only as historical reference or editor migration input.
- Keep `DataMapper.SkillDefinition.HasFlag(...)` behavior compatible with the
  current XML flags text.
- Keep `DataMapper.LoadSkillIcon(...)` unchanged for this PRD. Skill icons
  still use the current Resources path and skill id leaf name.
- Keep `SkillPresentationCatalog` unchanged. Presentation remains separate and
  additive.
- Keep `CastManager` unchanged except where compile compatibility requires no
  behavior change.
- Keep `MouseControler`, `UICanvas`, and `RightClickInfo` behavior unchanged by
  preserving the `DataMapper.FindSkill(...)` interface.
- Generate one built-in skill definition asset for each current XML skill entry.
- Populate the skill catalog with all generated built-in skill definition
  assets.

## XML Fields To Preserve

The first-pass skill definition asset should preserve only the current XML
metadata shape:

```text
Name -> SkillId / Name
Typ -> Type
Flags -> Flags
Info -> Info
```

Do not add targeting, delivery, anchor, cooldown, icon, presentation, or legacy
execution fields in this PRD.

## Testing Decisions

- Test through the public skill metadata seam: skill id in,
  `DataMapper.SkillDefinition` out.
- Add focused tests for finding known skills by id.
- Add focused tests that active and passive skill types match the old XML
  values.
- Add focused tests that `NI` and empty flags are interpreted the same way as
  before.
- Add focused tests that unknown skill ids return null.
- Add a test or static verification that every unit-catalog skill id resolves
  to a skill definition.
- Add a test or static verification that every current XML skill was migrated
  into the catalog.
- Manual Unity verification should confirm skill buttons, passive greying,
  right-click skill info, and representative active skill casts still behave as
  before.
- The user compiles and tests inside Unity unless a later task explicitly
  allows a Unity test command.

## Out of Scope

- Moving skill logic out of `CastManager`.
- Replacing `{SkillName}M` targeting mode methods.
- Replacing `{SkillName}` execution methods.
- Adding a targeting profile model.
- Adding delivery, anchor, target team, target shape, cooldown, or execution id
  fields to skill definitions.
- Changing cooldowns, damage, healing, status values, movement distances,
  target ranges, AoE radii, or gameplay float values.
- Changing `NI`, `AM`, passive, or stance behavior.
- Changing unit skill ownership.
- Changing skill icon lookup.
- Changing `SkillPresentationCatalog`.
- Editing scenes, prefabs, Animator Controllers, materials, `.inputactions`,
  generated Unity files, `.asmdef`, or `.asmref`.
- Implementing mod-created skills.
- Implementing new skills or new balance behavior.

## Further Notes

Future work may extract targeting and execution logic from `CastManager`, but
that is deliberately not part of this PRD. This PRD should leave the project in
a cleaner data state first:

```text
current skills.xml metadata
-> SkillDefinitionAsset files
-> SkillCatalog
-> DataMapper.FindSkill(...)
-> existing UI and battle callers
```

After this migration is complete and verified, a later PRD can decide whether
to move targeting, cooldown authoring, passive triggers, or execution logic into
deeper skill modules.

## Implementation - 2026-06-15

### What Changed

- `SkillDefinitionAsset`: added a new Unity-authored skill metadata asset type.
  New Inspector fields: `skillName` identifies the stable skill id; `type`
  stores the current XML `Typ` value such as `Active` or `Passive`; `info`
  stores right-click/UI description text; `flags` stores current flag text such
  as `NI`. These are metadata fields only. Lower/higher numeric tuning does not
  apply because no gameplay numbers were added; tuning hint: preserve exact
  skill ids and current flag text.
- `SkillCatalog`: added a new Unity-authored catalog for built-in skill
  metadata. New Inspector field: `skills`, the list of skill definition assets.
  It affects `DataMapper.FindSkill(...)` lookup. Keep every built-in skill asset
  listed once; missing entries make `FindSkill(...)` return null.
- `DataMapper`: added a serialized `skillCatalog` reference and changed skill
  metadata loading from XML parsing to `SkillCatalog`. Existing callers still
  use `DataMapper.FindSkill(...)`. No gameplay Inspector tuning fields changed.
- Assets: generated 33 `SkillDefinitionAsset` files under
  `Resources/0_Data/Skills`, created `SkillCatalog.asset`, and assigned it on
  `DataMapper.asset`.
- New PRD: added `_codex/tasks/034_PRD_FutureSkillLogicAndTargetingExtraction.md`
  for the deferred targeting/execution extraction work.

### Automatic Test

- No EditMode test files were added because this project does not currently
  expose an existing game-code test assembly and this task must not create or
  edit `.asmdef`/`.asmref`.
- Performed static/data verification with `py -3`: XML skill count is 33,
  generated skill asset count is 33, `SkillCatalog.asset` has 33 references,
  and every unit-catalog skill id resolves to a generated skill asset.
- Tests are run manually by the user in Unity. In Unity Test Runner, there are
  no new tests expected from this task.

### Unity Test

#### Unity Setup

- Let Unity import the new scripts and assets.
- Open `Resources/0_Data/DataMapper.asset`.
- Confirm `Skill Catalog` references `Resources/0_Data/SkillCatalog.asset`.
- Open `Resources/0_Data/SkillCatalog.asset`.
- Confirm the `Skills` list contains 33 entries.
- Open a few assets under `Resources/0_Data/Skills` and confirm `Skill Name`,
  `Type`, `Info`, and `Flags` are populated.

#### Play Mode Test

- Start a battle with units that have active and passive skills.
- Select a unit with active skills and confirm skill buttons still appear.
- Right-click or inspect skill info and confirm type/description text still
  appears.
- Select a unit with a passive skill and confirm the passive is not directly
  castable.
- Cast representative active skills such as `Hate`, `Fire_Ball`, and
  `Stone_Stance`; expected result is the same behavior as before this migration.

### QA Verdict

- QA status: Pass with manual Unity verification required.
- QA report: `_codex/tasks/QA/2026-06-15_1146_033_QA_ArchitectureReview.md`.
- Actionable findings: none.
- Non-blocking observations: Unity import and Play Mode verification are still
  required.
- Follow-up fixes applied: none needed.

### Notes

- `CastManager`, `{SkillName}M` methods, `{SkillName}` methods, cooldowns,
  targeting, damage, movement, passive behavior, stance behavior, unit skill
  ownership, icon lookup, and `SkillPresentationCatalog` were intentionally not
  changed.
- `skills.xml` remains in the project as historical/reference data, but
  `DataMapper` no longer uses it as runtime skill metadata truth.

### Next Steps

- Run Unity import/compile.
- Run the Play Mode checks listed above.
- Use PRD 034 later when ready to design targeting profiles or extract skill
  logic from `CastManager`.
