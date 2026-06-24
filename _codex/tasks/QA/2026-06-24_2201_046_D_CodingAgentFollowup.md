# [TARENA] Coding Agent Follow-Up - PRD046-D

Task: `_codex/tasks/046_D_PRD_TacticalAI_SearchScoring.md`
Initial QA: `_codex/tasks/QA/2026-06-24_2201_046_D_QA_ArchitectureReview.md`
Date: 2026-06-24
Agent: Coding Agent

## QA Finding Addressed

QA found that skill target expansion and root search could exceed fixed profile budget:

- generic skill candidates could expand into more target-aware skill intents than `MaxSkillCandidates` / `MaxCandidatesPerActionType`;
- root search evaluated every expanded root candidate instead of applying own-side beam pruning before recursion.

## Focused Fix

Updated `TArenaUnity3D/Assets/Scripts/Lesisz/HexMap/TacticalAISearchScoring.cs`:

- `TacticalAISearchEngine.Search(...)` now runs root candidates through `ScoreAndPruneCandidates(...)` before recursive search.
- Root pruning uses the same deterministic score ordering and `TacticalAIFixedBudget.ClampBeamWidth(false, ...)` own-side beam as deeper AI plies.
- `TacticalAISearchCandidateExpander.BuildSearchCandidates(...)` now applies `ApplyProfileCandidateCaps(...)` after skill target expansion.
- Expanded skill candidates are capped by `min(MaxCandidatesPerActionType, MaxSkillCandidates)`.
- Move candidates remain capped by `min(MaxCandidatesPerActionType, MaxMoveCandidates)`.
- Ranged attack and move-and-attack candidates remain capped by `min(MaxCandidatesPerActionType, MaxAttackCandidates)`.
- Wait and defend remain capped by `MaxCandidatesPerActionType`.
- Candidate retention preserves the first candidates after deterministic stable-order sorting.

## Scope Notes

- No additional files were edited.
- No Inspector fields changed.
- No scene, prefab, material, controller, `.inputactions`, `.asmdef`, `.asmref`, or generated Unity files were edited.
- The fix did not add live runtime dependencies to the pure planning module.

## Verification

- Re-read the edited sections around root candidate pruning and expanded candidate caps.
- Confirmed the new file still has no references to `UnityEngine`, `Random`, `MouseControler`, `CastManager`, `TosterHexUnit`, `HexClass`, `Debug.Log`, live execution, presentation, or chat APIs.
- Unity compilation and tests were not run automatically per project rules.

## Tests

Per local `/implement` workflow, focused EditMode tests remain deferred until after final QA verdict.
