# [TARENA] 021 PRD019 Run Map - Coding Completion

## Scope

- Added Offline Run Map domain, path catalog, in-memory store, adapter, and EditMode tests.
- Added task-specific service-backed Unity Run Map prototype.

## Files Changed

- `TArenaUnity3D/Assets/Scripts/RunMetagame/021_RunMap/RunMapModels.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/021_RunMap/RunMapContracts.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/021_RunMap/DefaultRunMapPathCatalog.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/021_RunMap/RunMapService.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/021_RunMap/OfflineRunMapAdapter.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/021_RunMap/PRD19_021_RunMapMockupController.cs`
- Historical PRD019 Run Map prefab builder removed; do not recreate without current path-specific user permission.
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/RunMapServiceTests.cs`

## Notes

- Offline Mode owns local route state.
- Route data is authored/local V1 data, not PlayerPrefs authority.
- Historical Unity Editor-side PRD019 prototype generation is deprecated and removed.
- Route node, Travel, Back, and View Army actions are wired to the controller
  and adapter/service path.
