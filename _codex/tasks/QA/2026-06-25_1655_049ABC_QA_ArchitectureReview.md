# 049ABC QA Architecture Review

Date: 2026-06-25
Protocol: `_codex/tasks/QA/2026-06-25_1648_049ABC_PRD_SkillAPIAndFullMigration_Completion.md`
Reviewer: QA Architecture Review Agent

## Verdict

Follow-up required.

The implementation adds the correct PRD49 data/API foundation and migrates the skill assets, but it does not yet satisfy the task file's full migration definition of done because live active skill commits and passive trigger mutation still depend on legacy paths.

## Findings

### High - Active Skill Commit Still Requires `CastManager` Reflection

Files:

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/MouseControler.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/Skills/CastManager.cs`

`MouseControler.startSpell(...)` now validates clicked targets through `SkillRules`, but the final live commit still calls `castManager.startSpell(skillId, targetHex)`, which invokes the named skill method by reflection. This means migrated active demo skills still require the legacy reflection commit path for actual live mutation.

This violates the strict PRD49ABC DoD items:

- active demo skills route through the new skill API,
- `CastManager` is no longer the authority for active demo skill commit,
- migrated reflection paths are removed or unreachable.

The validation boundary is a good intermediate step, but this task cannot be marked fully complete until live apply consumes `SkillCast` plus ordered `SkillEffect` data through `ISkillRuntime` or an equivalent non-reflection runtime adapter.

### High - Passive Trigger Mutation Still Lives In Legacy Hooks

Files:

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TosterHexUnit.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/SpellOverTime.cs`
- `TArenaUnity3D/Assets/Resources/0_Data/Skills/*.asset`

Passive skills are represented in the new asset schema as activation metadata/effect outlines, but the actual trigger behavior remains hidden in legacy hooks such as movement, new-turn status resolution, and trap/status side effects.

This violates the PRD49ABC passive migration requirement:

- passive behavior must not remain hidden only in `TosterHexUnit`, `SpellOverTime`, and related hooks.

The current data migration is useful for future AI/server reasoning, but passive runtime extraction remains a separate implementation step.

### Medium - Full Manual/Unity Validation Is Still Outstanding

Files:

- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/SkillRulesTests.cs`
- `TArenaUnity3D/Assets/Resources/0_Data/Skills/*.asset`

Focused EditMode tests were added, but Unity Test Runner was not executed automatically per project rules. Unity import, compilation, and Play Mode validation for all migrated skills remain required before this can be treated as production-verified.

## Positive Checks

- `SkillDefinitionAsset` was extended in place instead of replaced.
- `skillName` remains the canonical skill id.
- The new DTO/API names match the short PRD naming direction.
- `DataMapper` still preserves old `SkillDefinition` compatibility while exposing `SkillDefinitionAsset`.
- UI code touched still uses TMP types; no legacy `UnityEngine.UI.Text` was introduced.
- All 33 active skill assets have migrated activation, targeting, resolution, and effect fields.
- `Long_Lick` was changed to the approved two-step destination-selection contract.
- `Stone_Throw` asset data now requires enemy unit targeting.

## Required Follow-Up

1. Replace `MouseControler` final skill commit with `SkillRules.Validate(...)` + `SkillRules.Apply(...)` using a live runtime adapter that consumes `SkillCast` and ordered `SkillEffect` data.
2. Migrate passive trigger behavior out of legacy-only hooks into the new skill/status/trap rule model or a narrow trigger runtime that emits `SkillResult` events.
3. Run Unity Test Runner EditMode tests and the PRD manual Play Mode checklist.
