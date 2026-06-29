# [TARENA] Coding Completion - PRD038 Run Map Node Representation Binding

Task: `_codex/tasks/038_PRD_RunMapNodeRepresentationBinding.md`

## Summary

Implemented the Run Map node binding cleanup:

- Added a reusable `RunMapNodeRepresentation` component that owns route-node UI internals.
- Replaced controller-level per-node UI field groups with an ordered `RunMapNodeRepresentation[]`.
- Bound generated `RunMapNodeViewData` to representations by flattened path order at render time.
- Stored runtime node ids inside the representation during binding instead of requiring authored node ids in the controller Inspector.
- Added separate hover and click callbacks:
  - hover focuses/inspects a node and updates the detail panel,
  - click requests direct travel through the existing controller travel flow.
- Hidden extra representation slots when no generated node exists.
- Logged a warning when generated nodes exceed assigned representations.
- Historical Run Map prefab builder/generator flow has been removed. Do not recreate PRD019 generated prefabs without current path-specific user permission.
- Removed the obsolete nested `RouteNodeBinding` serialized field group from `RunMapController`.
- Added focused EditMode tests for representation binding/callbacks and controller node-order flattening.

## Changed Files

- `TArenaUnity3D/Assets/Scripts/RunMetagame/021_RunMap/RunMapNodeRepresentation.cs`
- `TArenaUnity3D/Assets/Scripts/RunMetagame/021_RunMap/RunMapController.cs`
- Historical PRD019 Run Map prefab builder removed; do not recreate without current path-specific user permission.
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/RunMapNodeRepresentationTests.cs`
- `TArenaUnity3D/Assets/Scripts/Tests/EditMode/RunMapControllerBindingTests.cs`

## Implementation Notes

- `RunMapController` still owns run-map meaning: focused node state, availability checks, travel flow, detail panel content, and bottom-bar state.
- `RunMapNodeRepresentation` owns optional node visuals: button, background, icon, selection frame, locked overlay, title/type/state/debug node id labels.
- Optional visual references are null-safe.
- Representation click is gated by `RunMapNodeViewData.CanTravel`, so locked or completed nodes remain inspectable by hover but do not request travel through the representation button.
- `RunMapController.FlattenNodes(...)` preserves existing `RunMapScreenViewData.Paths` order and each path's node order.
- The existing public `OnRouteNodeClicked(string)` UnityEvent method remains for compatibility, but it now routes to direct travel to match PRD038.
- The existing bottom-bar Travel button remains supported for a focused node.
- The historical Unity Editor builder for the polished route-node prefab has been removed; existing PRD019 prefabs are read-only by default.
- No Unity prefabs, scenes, or other Unity asset files were edited.

## Verification

- Text-level scans checked that the obsolete `routeNodes` field and nested `RouteNodeBinding` are no longer referenced by runtime code.
- Text-level scans checked that the builder now serializes `routeNodeRepresentations`.
- Text-level scans checked that `PolishedRouteNodePath` points to `Assets/Resources/UI/PRD_19/021_RunMap/Prefabs/PRD_19_021_RunMap_RouteNode_Polished.prefab`.
- Added `RunMapNodeRepresentationTests` for binding visible node data, hiding null slots, null-safe optional visuals, hover callback, available click callback, and locked click suppression.
- Added `RunMapControllerBindingTests` for flattened path-order binding and missing/null path tolerance.
- Unity EditMode tests were not run automatically because project rules require the user to run Unity tests manually unless explicitly allowed.
- Unity was not run, so the polished prefab file is expected to appear after Unity imports the changed Editor script or after the menu command is clicked.

## Manual Unity Test Target

Run the focused new EditMode tests plus adjacent Run Map regressions in Unity Test Runner:

- `EditMode > RunMapNodeRepresentationTests`
- `EditMode > RunMapControllerBindingTests`
- `EditMode > RunMapServiceTests`
- `EditMode > OfflineStartRunRunMapDbTests`
