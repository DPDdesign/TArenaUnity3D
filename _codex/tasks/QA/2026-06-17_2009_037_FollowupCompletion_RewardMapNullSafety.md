# [TARENA] Follow-up Coding Completion - PRD037 Reward Map Null Safety

Task: `_codex/tasks/037_PRD_MaterializedRunGenerationRewardsAndMapPersistence.md`

Previous QA report: `_codex/tasks/QA/2026-06-17_2008_037_QA_ArchitectureReview.md`

## Summary

Applied one focused follow-up fix found during the final static scan:

- `RewardMapScreenController.BindRewardCards()` now handles `choice == null` before reading `choice.Cards`.

## Changed Files

- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/RewardMapScreenController.cs`

## Reason

`RenderUnavailable()` can call `BindRewardCards()` before a reward choice exists. The PRD037 implementation already made Reward Map stricter about missing materialized rows, so the unavailable path must safely clear card views instead of throwing a null reference.

## Verification

- Static scan confirmed no remaining `BindCommandButtons`, obsolete command-button fields, or `GetBeforeSnapshotId` references in active runtime files.
- Unity tests were not run automatically per project rule.
