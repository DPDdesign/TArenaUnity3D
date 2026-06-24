# [TARENA] QA Architecture Review - PRD046-D

Task: `_codex/tasks/046_D_PRD_TacticalAI_SearchScoring.md`
Protocol: `_codex/tasks/QA/2026-06-24_2200_046_D_CodingAgentCompletion.md`
Date: 2026-06-24

## Verdict

Follow-up required. The new module is correctly isolated from live Unity runtime objects, but one budget-control issue should be fixed before accepting the PRD046-D implementation.

## Reviewed Files

- `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISearchScoring.cs`
- nearby related systems:
  - `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotModels.cs`
  - `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/BattleSnapshotBuilder.cs`
  - `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIActionIntent.cs`
  - `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAICandidateGenerator.cs`
  - `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIProfile.cs`
  - `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAIExecutionBridge.cs`
  - `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAICastManagerSkillIntentExecutor.cs`
  - `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TurnManager.cs`
  - `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TeamClass.cs`

## Findings

### Follow-Up Required: Skill target expansion and root search can exceed fixed profile budget

File: `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISearchScoring.cs`

`TacticalAISearchCandidateExpander.BuildSearchCandidates(...)` starts from profile-capped candidates, but then expands each skill candidate into up to `profile.MaxSkillCandidates` enemy-target variants. With multiple legal skills, total skill candidates can exceed both `MaxSkillCandidates` and `MaxCandidatesPerActionType`.

The root search loop in `TacticalAISearchEngine.Search(...)` then evaluates every expanded root candidate instead of applying the own-side beam width before full recursive search. Deeper plies use `ClampBeamWidth(...)`, but the root ply currently does not.

Why this matters:

- PRD046-D requires deterministic candidate ordering and profile-limited pruning.
- PRD046-E defines fixed profile budgets as AI strength, not CPU-scaled exploration.
- A kit-heavy unit on a board with many enemies could search wider than intended at the root, even though deeper branches are beam-limited.

Expected fix:

- Cap the expanded candidate list after skill target expansion using profile limits.
- Apply root-level deterministic scoring/pruning through the own-side beam before recursive search.
- Preserve deterministic ordering for equal scores.

## Non-Blocking Observations

- The module correctly avoids live runtime dependencies in planning. It does not reference `UnityEngine`, `MouseControler`, `CastManager`, `TosterHexUnit`, `HexClass`, `Random`, presentation, chat, or live execution APIs.
- The separation between approximate planning and live execution remains intact: the new code returns ordered intents, while PRD046-C/F still own live revalidation and CastManager execution.
- The turn-order estimator is intentionally approximate but follows the relevant `TurnManager` and `TeamClass` decision shape: moved units are skipped, non-waited units act before waited units, initiative/speed/roster order determine ordering, and a new round resets action flags.
- Advisory cache use in `TacticalAISearchPlanner` is acceptable because it only returns ordered intents and still relies on later live revalidation. Cache-hit metadata is thinner than a fresh search result, but that is not currently a blocker.

## Test Review

No new EditMode tests were added yet, matching the local `/implement` workflow where tests are written after final QA verdict.

Post-fix tests should include at least:

- root search obeys own-side beam/candidate caps after skill expansion;
- expanded skill candidates remain deterministic and bounded;
- 3-ply search reaches completed depth 3;
- opponent-response coverage is true when reachable;
- average damage prediction is deterministic and pure.

Tests were not executed during QA because project rules leave Unity Test Runner execution to the user unless explicitly allowed.
