# [TARENA] PRD055B QA Architecture Review

- Task: `_codex/tasks/055B_PRD_SkillDamageMigration_CombatDamageService.md`
- Protocol: `_codex/tasks/QA/2026-06-30_055B_CodingAgent_Completion.md`
- Date: 2026-06-30
- Reviewer: QA Architecture Review Agent
- Verdict: follow-up required

## Reviewed Files

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleActionRules.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISkillRulesExecutor.cs`
- Nearby check:
  `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISearchScoring.cs`

## Findings

### Follow-Up Required: snapshot simulation can reject migrated skill damage

`BattleActionRules.Apply(snapshot, action)` now resolves skill
`BasicAttackDamage` / `RangedBasicAttackDamage` through the default
`CombatDamageService`, which is `DataMapper` backed. That is correct for live
runtime, but `TacticalAISnapshotSimulator.ApplyAction(...)` calls the default
overload while simulating copied `BattleSnapshot` data. In EditMode and AI
simulation contexts, `DataMapper.Instance` may be absent even when the snapshot
has deterministic catalog ids and base stats.

Impact:

- AI snapshot simulation can turn a legal skill action into an `ActionRejected`
  result because catalog lookup uses the wrong source for that context.
- Existing skill search/scoring tests that call `BattleActionRules.Apply(...)`
  on snapshot-only fixtures are likely to fail for migrated combat-style skill
  damage.
- This does not reintroduce legacy damage math, but it weakens deterministic
  replay/simulation readiness for PRD055B.

Recommended focused fix:

- Keep `BattleActionRules.Apply(...)` defaulting to `CombatDamageService.Default`
  for live callers.
- In snapshot simulation/scoring contexts, pass an explicit
  snapshot-backed `ICombatUnitCatalog` through `CombatDamageService`, using the
  same deterministic snapshot catalog concept already present in
  `TacticalAIDamagePredictor`.
- Do not add a hidden fallback inside `CombatDamageService`.

## Passed Checks

- Combat-style skill damage no longer falls through to
  `TosterHexUnit.CalculateDamageBetweenTosters(...)` in
  `TacticalAISkillRulesExecutor`.
- `BattleActionRules` uses a distinct skill roll purpose including skill id,
  effect index, and target index.
- Missing catalog/snapshot damage data still rejects through the existing
  `ActionRejected` flow instead of silently producing `0 damage`.
- Non-combat skill effects remain outside `CombatDamageService`.

## Non-Blocking Observations

- Legacy damage calls remain in `BattleActionAutomaticResultApplier`,
  `MostStupidAIEver`, and `TosterHexUnit`; these match the protocol's stated
  PRD055C or separate legacy scope and are not a PRD055B blocker.
- `Stone_Throw` remains `FixedDamageThroughDefense`; treating it as out of
  combat-style scope is acceptable unless a design decision says otherwise.

## Required Follow-Up

Apply the focused snapshot simulation catalog injection, then add EditMode tests
covering:

- direct combat-style skill damage through `CombatDamageService`,
- scaled combat-style skill damage,
- deterministic repeated skill damage for the same inputs,
- missing catalog data rejection without legacy fallback.
