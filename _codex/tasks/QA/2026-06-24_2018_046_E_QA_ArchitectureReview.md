# [TARENA] QA Architecture Review - PRD046-E

Task: `_codex/tasks/archive/046_E_PRD_TacticalAI_ProfileCache.md`
Protocol: `_codex/tasks/QA/2026-06-24_2017_046_E_CodingAgentCompletion.md`
Date: 2026-06-24

## Verdict

Pass for the requested profile/cache slice. No blocking architecture findings in the new `TacticalAIProfile`, fixed-budget helper, advisory cache seam, or Normal profile asset path.

## Reviewed Files

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIProfile.cs`
- `TArenaUnity3D/Assets/Resources/0_Data/TacticalAIProfile_Normal.asset`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAIProfileTests.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotModels.cs`
- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotBuilder.cs`

## Findings

No blocking architecture findings for the requested profile/cache slice.

## Non-Blocking Observations

- The split between `LoadNormalProfileAsset()` and `ResolveAssignedOrRuntimeDefault(...)` is correct for this PRD: the project gets a real asset path for future explicit wiring, but unassigned AI still uses a code-defined runtime default instead of hidden scene/resource coupling.
- `TacticalAIFixedBudget` keeps the watchdog in a safety role only; it does not create a "search until time runs out" surface, which matches PRD046-E and the parent PRD direction.
- `TacticalAIPlanCache` stays advisory by API shape and comments rather than trying to own live validation, which avoids colliding with the future PRD046-C execution bridge and ADR014 validation boundary.
- The profile hash includes budget fields, tie-break settings, weights, and biases, so tuning changes invalidate cached plans without depending on asset instance ids or Unity object identity.

## Test Review

The new `TacticalAIProfileTests` cover:

- runtime Normal default values;
- existence and recommended defaults of the real Normal profile asset;
- profile-hash invalidation when budget or weights change;
- fixed-budget depth/beam/candidate clamps staying profile-driven;
- watchdog fallback returning best completed plan or fallback plan;
- advisory cache hit on matching snapshot/profile and rejection on snapshot/profile mismatch.

Tests were not executed during QA because project rules prohibit command-line Unity, `dotnet`, build, package restore, and Git tooling in this workflow.
