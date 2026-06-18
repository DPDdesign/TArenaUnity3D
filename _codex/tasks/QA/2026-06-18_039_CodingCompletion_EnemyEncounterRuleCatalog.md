# [TARENA] Coding Completion: PRD 039 Enemy Encounter Rule Catalog

- Task: `_codex/tasks/039_PRD_EnemyEncounterRuleCatalog.md`
- Status: ready-for-qa
- Type: Coding Completion Protocol
- Date: 2026-06-18

## Scope

Implemented the PRD 039 authoring/catalog foundation only. This pass adds a
dedicated enemy encounter rule catalog that maps encounter difficulty to either
a generated enemy ruleset or a predefined enemy id.

Out-of-scope items were intentionally not implemented:

- full enemy army generation,
- enemy snapshot materialization,
- database schema changes,
- Start Run / Run Battle flow wiring,
- Unity asset, prefab, or scene edits.

## Changed Files

- `TArenaUnity3D/Assets/Scripts/RunMetagame/035_Generation/EnemyEncounterRuleCatalog.cs`

## What Changed

- Added `EnemyEncounterDifficulty` enum:
  - `Low`
  - `Medium`
  - `High`
  - `Boss`
- Added `EnemyEncounterRuleLookupError` enum for explicit lookup/validation
  failure states.
- Added `EnemyEncounterRuleCatalog` ScriptableObject with Inspector-authored
  entries and public lookup methods:
  - `FindRule(EnemyEncounterDifficulty difficulty)`
  - `Resolve(EnemyEncounterDifficulty difficulty)`
- Added `EnemyEncounterRule` entry model with:
  - `Difficulty`
  - `ArmyGeneratorRuleSet`
  - `PredefinedEnemyId`
  - resolved accessors that ignore rulesets when `PredefinedEnemyId` is
    non-empty.
- Added `EnemyEncounterRuleLookupResult` so callers can distinguish success
  from missing entries, duplicate entries, missing generated rulesets, and
  missing predefined ids.

## Design Notes

- The catalog is independent from `RunMapNodeType`.
- The existing `ArmyGeneratorRuleSet` remains usable by starting army generation
  and is only referenced by this catalog as an assignable ruleset type.
- Non-empty `PredefinedEnemyId` overrides generation. In that case,
  `ArmyGeneratorRuleSet` may be null and is ignored even when assigned.
- The predefined enemy reference is one generic string, as requested.
- Duplicate difficulty entries fail resolution instead of silently choosing one.

## Automatic Tests

No tests were added in this pass because the `/implement` workflow writes
focused EditMode tests after QA review.

Recommended post-QA tests:

- generated entries resolve for `Low`, `Medium`, and `High`,
- `Boss` predefined entry resolves with a null ruleset and non-empty predefined
  id,
- entries with non-empty predefined ids ignore assigned rulesets,
- entries with empty predefined ids use assigned rulesets,
- entries with neither predefined id nor ruleset fail,
- missing and duplicate entries fail clearly.

## Manual Unity Validation Needed

- Let Unity recompile.
- Create an `EnemyEncounterRuleCatalog` asset from:
  `Assets > Create > TArena > Run Metagame > Enemy Encounter Rule Catalog`.
- Confirm entries can be edited in the Inspector.
- Confirm generated entries can reference `ArmyGeneratorRuleSet` assets.
- Confirm predefined entries can leave `ArmyGeneratorRuleSet` empty and use only
  `PredefinedEnemyId`.

## Known Risks

- The catalog is not yet wired into enemy army generation or battle preparation.
- No default catalog asset was created because project rules prohibit editing
  Unity assets without explicit permission.
- Runtime consumers still need a future task to read this catalog and
  materialize enemy armies.
