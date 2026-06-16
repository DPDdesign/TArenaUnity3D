# [TARENA] 035 QA Architecture Review Follow-Up - Random Starting Armies And Route Maps

- Date: 2026-06-16 14:50
- Protocol: `_codex/tasks/QA/2026-06-16_035_FollowupCodingCompletion_RandomStartingArmiesRoutes.md`
- Verdict: pass-manual-test-pending

## Findings

- No blocking findings.

## Follow-Up Verification

- The prior finding about possible third-race generated stacks is addressed.
  `BuildArmyOffer(...)` now selects a race pool before stack generation,
  filters the candidate units through `FilterUnitsByRace(...)`, selects the
  tier composition from that filtered pool, and calls `PickUnitForTier(...)`
  only with the filtered units.
- The generator no longer has a `usedRaces` fallback path that can silently add
  a third race to satisfy a requested tier.

## Non-Blocking Observations

- Seed persistence and reroll interaction remain documented slice limitations
  from the first QA report.
- Focused EditMode tests are still required after this final QA verdict, per
  the implement-task workflow.

## Checks

- Ownership: pass.
- Persistence boundary: pass with documented seed-storage caveat.
- UI text rule: pass.
- Asset safety: pass.

## Required Follow-Up

- Add focused EditMode tests for deterministic generation, starting asset
  values, legal skill count, race limit, route topology, encounter ids, and DB
  reload behavior.
- User must run Unity compilation, EditMode tests, and Play Mode checks manually
  in Unity.
