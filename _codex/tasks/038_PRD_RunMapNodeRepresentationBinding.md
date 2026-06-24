# [TARENA] PRD 038: Run Map Node Representation Binding

- Status: ready-for-agent
- Type: PRD
- Area: Run Metagame, Run Map, UI Binding
- Label: ready-for-agent
- Related:
  - `_codex/tasks/019_PRD_RunMetagameRewardFramework.md`
  - `_codex/tasks/archive/021_PRD019_RunMap.md`
  - `_codex/tasks/030_DB-001_OfflineModeDatabasePersistence.md`
  - `_codex/tasks/035_PRD_RandomStartingArmiesRoutes.md`
  - `_codex/tasks/037_PRD_MaterializedRunGenerationRewardsAndMapPersistence.md`

## Problem Statement

The Run Map screen currently requires the controller to know too much about the
internal UI structure of every route node. Each static node slot exposes button,
background, selection frame, locked overlay, and text references directly on the
controller. This makes the screen fragile to iterate in Unity: changing the node
prefab or removing debug labels requires controller-level rewiring.

The user wants the map layout to remain static for now. Designers should place
node GameObjects manually on the map, but the controller should only receive a
list of node representations, similar to the existing stack representation
pattern. The representation owns the UI internals; the controller owns run-map
meaning and player actions.

## Solution

Keep the Run Map layout authored manually in Unity, but replace per-field node
wiring with a reusable node representation component.

The Run Map controller receives an ordered list of node representation
components. At render time it flattens the generated run-map node data in path
order and binds each UI slot by list index. No node id is entered manually in the
Inspector. The representation stores the runtime node id after binding and emits
hover/click events back to the controller.

Hovering a node should inspect/focus it and update the existing route detail
panel. Clicking a node should attempt travel to that node. Optional visual fields
inside the representation may be left unassigned, so the same prefab can be used
first with debug labels and later as an icon-only production node.

## User Stories

1. As a designer, I want to place node GameObjects manually on the Run Map, so
   that the map composition remains under Unity scene/prefab control.
2. As a designer, I want to drag only the node representation component into the
   controller list, so that I do not wire every label and image on the controller.
3. As a designer, I want the first UI node slot to receive the first generated
   node, so that setup is based on visible order rather than manual node id
   strings.
4. As a designer, I want temporary debug labels on node prefabs, so that I can
   verify which generated node is displayed in each map position.
5. As a designer, I want to remove or leave unassigned debug labels later, so
   that the production node can become icon-only without code changes.
6. As a player, I want hovering a route node to show its details, so that I can
   inspect risk and rewards before committing.
7. As a player, I want clicking a route node to travel when it is available, so
   that the map behaves like a direct route-choice surface.
8. As a player, I want unavailable nodes to look inactive and not accept travel,
   so that locked progression is clear.
9. As a developer, I want node UI internals encapsulated in a representation, so
   that Run Map controller code does not depend on individual text and image
   fields.
10. As a developer, I want node binding to follow generated data order, so that
    generated ids such as `node-1` and `node-4a` do not need to be copied into
    Unity Inspector fields.
11. As a developer, I want hover and click to be separate callbacks, so that
    preview and travel behavior can match the Reward Map card interaction model.
12. As a developer, I want mismatched node counts handled gracefully, so that
    temporary layouts can be tested without crashing the screen.
13. As a QA reviewer, I want extra UI slots disabled when there is no generated
    node for them, so that stale placeholder nodes are not accidentally shown.
14. As a QA reviewer, I want missing UI slots reported when generated nodes
    exceed the configured representations, so that incomplete map layouts are
    easy to diagnose.
15. As a future UI designer, I want node representation visuals to be optional,
    so that the same data binding supports both debug and polished node prefabs.

## Implementation Decisions

- The Run Map layout remains static and manually positioned in Unity.
- The controller receives an ordered array/list of node representation
  components instead of per-node UI field groups.
- No authored node id is entered in the controller Inspector.
- Runtime node ids are assigned to representations during binding from the
  generated run-map data.
- Generated run-map nodes are flattened in existing path order and bound to UI
  representations by index.
- The expected V1 order is main path nodes, then safe branch nodes, then risk
  branch nodes, then shared finale nodes.
- The representation owns optional visual fields such as button, background,
  icon, selection frame, locked overlay, and debug text.
- Optional visual fields may be null without failing the screen.
- The representation exposes hover and click events/callbacks that include the
  bound runtime node id.
- Hover means inspect/focus: the controller updates focused node state and the
  existing detail panel.
- Click means commit/travel: the controller invokes the existing travel flow for
  the bound node.
- The controller remains the only owner of run-map decisions such as focus,
  availability, selected node, and travel result handling.
- If there are more representations than generated nodes, the extra
  representations are hidden or deactivated.
- If there are fewer representations than generated nodes, the controller renders
  what it can and reports a warning.
- Dynamic node instantiation, automatic layout, and dynamic connection drawing
  are explicitly not part of this PRD.

## Testing Decisions

- Tests should assert behavior visible through the controller/representation
  contract, not private field implementation.
- Representation tests should cover binding a node, updating optional visuals,
  leaving optional visuals unassigned, and emitting hover/click callbacks with
  the bound runtime node id.
- Controller tests should cover binding generated nodes by order, hiding extra
  representations, tolerating too few representations, hover-driven focus, and
  click-driven travel.
- Existing Run Map service tests remain the prior art for node state,
  availability, selected node, and travel behavior.
- Existing Reward Map card interaction is prior art for hover-preview and
  click-commit separation.
- Manual Unity validation should confirm that a prefab with debug labels shows
  generated node data correctly, and that removing debug label references does
  not break interaction.

## Out of Scope

- Dynamic creation of node UI GameObjects.
- Automatic map layout from stage/path data.
- Dynamic drawing of node connections.
- Changes to run generation, node ids, route persistence, rewards, battles, or
  offline database schema.
- Changes to gameplay balance, node count, route structure, enemy generation, or
  reward generation.
- Editing Unity scene or prefab assets as part of the code task unless the user
  explicitly asks for asset-side setup.

## Further Notes

- This PRD is intentionally a UI binding cleanup, not a route-generation change.
- The desired mental model is the existing stack representation pattern: a
  prefab owns its UI references, while the screen controller supplies data and
  decides what user interaction means.
- The current generator produces more nodes than the older static map screenshot,
  so the authored Run Map should have enough node representation slots for the
  generated V1 route shape when validating the feature.

## Implementation - 2026-06-18

### What Changed

- `RunMapController`: replaced the obsolete serialized `routeNodes` per-field binding group with `routeNodeRepresentations`, an ordered `RunMapNodeRepresentation[]`. Value range is 0-N manually placed node components; lower than generated node count renders what exists and logs a warning, higher hides extras. Tuning hint: order the array exactly as the visible map order should receive generated nodes.
- `RunMapNodeRepresentation`: added optional Inspector fields for `button`, `background`, `icon`, `selectionFrame`, `lockedOverlay`, `titleText`, `typeText`, `stateText`, and `nodeIdText`. Each field may be assigned or left empty; assigning more fields gives richer visuals, leaving fields empty supports icon-only production nodes. Tuning hint: keep debug labels assigned while validating route order, then remove them for final art.
- `PRD19_021_RunMapBuilder`: added generation for `Assets/Resources/UI/PRD_19/021_RunMap/Prefabs/PRD_19_021_RunMap_RouteNode_Polished.prefab` and a menu item `TArena/Run Metagame/Rebuild PRD 19 021 Polished Route Node Prefab`. The prefab root owns `RunMapNodeRepresentation`, `Button`, styled images, selected/locked states, and TMP debug labels.
- Removed: controller-level node id, button, background, selection frame, locked overlay, and text fields from the old nested binding class, so those internals no longer appear on `RunMapController`.

### Automatic Test

- Added `RunMapNodeRepresentationTests`: checks binding node data, hiding null slots, null-safe optional visuals, hover callback, available click callback, and locked click suppression.
- Added `RunMapControllerBindingTests`: checks flattened path-order binding and tolerance for null/missing path data.
- Run manually in Unity Test Runner: `Window > General > Test Runner > EditMode`, then run `RunMapNodeRepresentationTests` and `RunMapControllerBindingTests`. Expected result: all tests pass. I did not run Unity tests automatically per project rule.

### Unity Test

#### Unity Setup

- Let Unity recompile/import scripts. The polished prefab should be generated by `PRD19_021_RunMapBuilder`; if needed click `TArena/Run Metagame/Rebuild PRD 19 021 Polished Route Node Prefab`.
- Open `Assets/Resources/UI/PRD_19/021_RunMap/Prefabs/PRD_19_021_RunMap_RouteNode_Polished.prefab`.
- Confirm root `Script_PRD_19_021_RunMap_RouteNode_Polished` has `RunMapNodeRepresentation` with button, background, icon, selected, locked, and TMP labels assigned.
- In the Run Map screen/prefab, place node instances manually and assign their `RunMapNodeRepresentation` components to `RunMapController.routeNodeRepresentations` in the intended generated-node order.
- Backend gaps: brak.

#### Play Mode Test

- Start a persisted run, open Run Map, and confirm generated node names/types appear on the assigned node slots.
- Hover a node: the detail panel should focus that node without travelling.
- Click an available node: travel should commit through the existing Run Map flow and update current node/progress.
- Hover a locked/completed node: details should inspect it. Clicking should not commit travel.
- Add one extra representation slot with no generated node: it should hide. Remove one required slot: visible nodes should still render and Unity Console should warn about too few representations.

### QA Verdict

- Pass.
- QA report: `_codex/tasks/QA/2026-06-18_1252_038_QA_ArchitectureReview.md`.
- Actionable findings: none.
- Non-blocking observations: small duplicated node label formatting between controller and representation; existing builder has some unused helper methods from the previous wiring style; existing prefab assets are not edited until Unity runs the builder.
- Follow-up fixes applied: none required.

### Notes

- Unity was not run here, so the polished prefab file did not exist at text-level verification time. It is generated by the Unity Editor builder after import or via the new menu item.
- No scenes, existing prefab YAML, gameplay balance, route generation, persistence, rewards, battles, `.asmdef`, or `.asmref` files were edited.
- `ClickRequested` is gated by `CanTravel`; locked/completed nodes remain hover-inspectable.
- The bottom-bar Travel button still works for the focused available node.

### Next Steps

- In Unity, let scripts import and generate `PRD_19_021_RunMap_RouteNode_Polished.prefab`.
- Run `EditMode > RunMapNodeRepresentationTests`, `EditMode > RunMapControllerBindingTests`, plus adjacent `RunMapServiceTests` and `OfflineStartRunRunMapDbTests`.
- Wire manually placed Run Map node instances into `RunMapController.routeNodeRepresentations` in generated-node order and do the Play Mode checks above.
