# [TARENA] 035 QA Architecture Review - Random Starting Armies And Route Maps

- Date: 2026-06-16 14:30
- Protocol: `_codex/tasks/QA/2026-06-16_035_CodingCompletion_RandomStartingArmiesRoutes.md`
- Verdict: follow-up-required

## Findings

1. Follow-up required - Generated armies can still violate the PRD035 two-race
   limit.
   - File: `TArenaUnity3D/Assets/Scripts/RunMetagame/035_Generation/DeterministicRunGenerationCatalog.cs`
   - Evidence: `PickUnitForTier(...)` first filters by already-used races, but
     when no matching unit exists it falls back to any unit of the requested
     tier. The caller only records new races while `usedRaces.Count < 2`, so a
     third-race stack can be selected without being tracked.
   - Risk: The generated Start Run screen can show an army that fails the PRD
     rule "Starting army V1 must not include more than two races", especially
     when a tier composition requires a tier that exists only outside the two
     races already selected.
   - Required fix: Choose the two-race pool before selecting a tier
     composition, then select only compositions satisfiable inside that pool.
     If no two-race pool can satisfy the PRD constraints, use fallback with a
     clear warning/description rather than silently exceeding two races.

## Non-Blocking Observations

- Seed persistence is intentionally limited to generated route ids plus
  persisted route rows in this slice. That is acceptable as a V1 implementation
  note, but a future schema migration should add an explicit run seed column if
  PRD035 grows into authoritative seeded-run rules.
- The Start Run UI exposes the reroll token count but no reroll command. This
  is acceptable for this slice because the existing UI contract has no reroll
  action surface, but it should be a follow-up task if reroll interaction is
  part of the next playable milestone.

## Checks

- Ownership: pass. Generator logic is behind existing Start Run and Run Map
  source interfaces, not inside UI controllers.
- Persistence boundary: pass with caveat. Selected army snapshots and route
  nodes continue to persist through existing PRD030 stores.
- UI text rule: pass. Changed UI-facing code continues to use TextMesh Pro.
- Asset safety: pass. No Unity assets, prefabs, scenes, controllers, or
  generated Unity files were edited.

## Required Follow-Up

- Fix the generator race-pool selection so generated four-stack armies cannot
  exceed two inferred races.
- Add focused EditMode tests after the final QA verdict for deterministic
  generation, budget bands, legal skill count, race limit, route topology, and
  DB reload behavior.
