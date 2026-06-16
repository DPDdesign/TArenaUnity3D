# [TARENA] 023 PRD019 Reward Map - Coding Completion

## Scope

- Added Offline Reward Map catalog, resolver, preview/apply flow, in-memory store, adapter, and EditMode tests.
- Added task-specific service-backed Unity Reward Map prototype.

## Files Changed

- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/RewardMapModels.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/RewardMapContracts.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/DefaultRewardMapTemplateCatalog.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/RewardMapService.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/OfflineRewardMapAdapter.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/RewardMapScreenController.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/RewardMapRewardCardView.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/RewardMapArmyPreviewUnitView.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/RewardMapResultGainedPanelView.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/RewardMapCommandButtonView.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/RewardMapUiSpriteResolver.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/023_RewardMap/Editor/PRD19_023_RewardMapPrefabBuilder.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/RewardMapServiceTests.cs`

## Notes

- Reward preview and reward apply use the same resolver operation.
- Catalog has 12 authored templates and all six reward families.
- Resolver is local-authoritative only for Offline Mode.
- Reward card, Select, and Continue actions are wired to the controller and
  adapter/service path.
