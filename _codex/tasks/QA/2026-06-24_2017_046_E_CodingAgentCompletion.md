# [TARENA] Coding Agent Completion Protocol - PRD046-E

Task: `_codex/tasks/archive/046_E_PRD_TacticalAI_ProfileCache.md`
Date: 2026-06-24

## Summary

Implemented the Tactical AI profile/cache foundation as a pure profile-budget module plus a real Normal profile asset:

- Added `TacticalAIProfile` as a `ScriptableObject` with fixed-budget fields, deterministic tie-break settings, scoring weights, and action-type biases.
- Added runtime Normal-profile resolution so AI can use a code-defined default when no asset is assigned, without silently wiring scene/prefab references.
- Added deterministic profile hashing so budget or tuning changes invalidate advisory cache entries.
- Added `TacticalAIFixedBudget` helpers that keep depth, beam, candidate limits, and watchdog behavior profile-driven instead of hardware-scaled.
- Added advisory `TacticalAIPlanCache` key/value types keyed by snapshot hash, active unit id, and profile hash.
- Added a real Normal profile asset under `Resources/0_Data/` for later optional loading/wiring.
- Added focused EditMode tests for runtime defaults, asset existence/defaults, profile-hash invalidation, fixed-budget clamps, watchdog fallback, and advisory cache hit/miss behavior.

## Files Changed

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIProfile.cs`
  - New Tactical AI profile, runtime default resolver, profile hasher, fixed-budget helper, and advisory plan-cache types.
- `TArenaUnity3D/Assets/Resources/0_Data/TacticalAIProfile_Normal.asset`
  - New Normal `TacticalAIProfile` asset with the recommended default values.
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/TacticalAIProfileTests.cs`
  - New focused EditMode tests for profile defaults, fixed-budget behavior, watchdog fallback, and cache matching/rejection.

## Scope Boundaries

- No scenes, prefabs, materials, controllers, `.inputactions`, `.asmdef`, `.asmref`, or generated Unity files changed.
- No battle gameplay values such as unit stats, cooldown logic, targeting rules, damage formulas, movement rules, or turn rules changed.
- No live Tactical AI execution bridge, candidate generator, search engine, or `CastManager` bridge wiring was added in this slice.
- Cache stays advisory only in this slice; live revalidation/execution remains future work under PRD046-C and PRD046-F.

## Verification

Automatic execution was not run because project rules prohibit command-line Unity, `dotnet`, Git, external build scripts, package restore, and SDK installation commands in this workflow.

Manual Unity EditMode tests to run:

- `TacticalAIProfileTests`
- `BattleSnapshotBuilderTests`

Manual Play Mode verification to run:

- let Unity import the new `TacticalAIProfile_Normal.asset` and confirm it opens as `TacticalAIProfile` with the expected Normal defaults;
- inspect any runtime caller or temporary debugger watch that resolves `TacticalAIProfileCatalog.ResolveAssignedOrRuntimeDefault(null)` and confirm it returns the code-defined Normal profile without requiring an assigned asset;
- if you temporarily wire future Tactical AI code to `TacticalAIFixedBudget`, confirm depth cannot exceed 3 plies and own/enemy beams stay capped at 8/5 even on a fast machine.

## Notes For QA

- `ResolveAssignedOrRuntimeDefault(null)` intentionally returns a runtime Normal profile, not the `Resources` asset, because PRD046-E says unassigned AI should use a runtime default rather than implicit asset wiring.
- The Normal asset is provided as a real project asset path for later explicit assignment or optional loading, while remaining out of scene/prefab scope for this slice.
- `TacticalAIPlanCache` is intentionally named and shaped as advisory-only storage; it does not perform execution or legality checks and leaves live revalidation to the future execution bridge.
