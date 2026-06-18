# [TARENA] Run Metagame DB/UI Cleanup QA

Date: 2026-06-16

## Verdict

Pass with Unity validation pending.

This review focused on PRD019, PRD030, and PRD035 run-metagame data flow: frontend controllers, offline DB composition, temporary/sample data removal, and mock-store boundaries.

## Findings

No blocking findings in the static review.

## Verified Statically

- Reward Map, Run Shop, Summary Value, Battle Result, and Saved Armies controllers no longer rely on serialized sample run ids, placeholder node ids, or hardcoded sample armies for runtime DB flow.
- Runtime DB store construction is centralized under `OfflineModeDatabaseComposition`.
- `offline_runs` creation and updates are centralized in `OfflineRunContextDbWriter`.
- Start Run now persists through `OfflineRunContextDbWriter` and reloads the created run through `OfflineRunContextDbReader`.
- Run Map DB persistence now requires an existing persisted run instead of inserting placeholder `offline_runs` rows with `mock-start-army`.
- Run Map mockup-only flow is explicitly isolated onto `InMemoryRunMapStore` and excluded from production composition checks.
- Prefab builders no longer write removed serialized placeholder fields.
- Static scans found remaining `InMemory` usage only in tests and the explicit Run Map mockup controller.
- Static scans found `INSERT INTO offline_runs` and `UPDATE offline_runs` only in `OfflineRunContextDbWriter`.

## Unity Validation Pending

- Unity script compilation was not run, per project instruction.
- EditMode tests were not run, per project instruction.
- Manual Play Mode smoke is still required for:
  - Start Run -> Run Map
  - Run Map -> Battle/Reward
  - Run Map -> Shop
  - Summary -> Saved Armies
  - Battle Result latest persisted async result

## Residual Risk

- PRD035 seed metadata is still derived through generated route ids and catalog reconstruction. This is acceptable for the current cleanup, but a future DB migration should make run seed metadata first-class before replay, analytics, or online authority depends on it.
