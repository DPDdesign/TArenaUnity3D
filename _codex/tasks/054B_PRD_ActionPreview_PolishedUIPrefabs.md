# [TARENA] PRD054B: Action Preview Polished UI Prefabs

- Status: draft
- Type: PRD / UI prefab task
- Area: Battle HUD, Action Preview UI, Polished UGUI Prefabs
- Label: ready-for-agent
- Created: 2026-06-30
- Parent PRD: `_codex/tasks/054_PRD_SkillIndicators_ActionPreviewUX.md`
- Depends On: `_codex/tasks/054A_PRD_ActionPreview_CombatForecastModel.md` for final data contract names, but visual prefab structure may be prepared with sample data first.
- Related: `_codex/skills/make-ui-mockup/SKILL.md`
- Related: `_codex/skills/polish-ui-mockup/SKILL.md`
- Related: `_codex/Context/11_UI_Context.md`
- Related: `_codex/Context/12_UI_Visual_Context.md`
- Related: `_codex/Context/UI_Visual_Identity.md`

## Explicit Asset Permission

This task may create new UI prefab assets under:

- `TArenaUnity3D/Assets/Resources/UI/PRD_54/ActionPreviewUX/`

If Unity initially creates `Assets/Resources/UI/New Folder`, rename that folder
to the current PRD folder convention above before final output.

This task may copy existing reference prefabs into the PRD054 output folder as
starting points, but must not modify the original reference prefabs.

Reference prefabs:

- `TArenaUnity3D/Assets/Resources/UI/Arena_Footer.prefab`
- `TArenaUnity3D/Assets/Resources/UI/Run_Footer.prefab`
- `TArenaUnity3D/Assets/Resources/UI/Unit_Stats.prefab`

This task must not edit, move, delete, regenerate, or write screenshots into:

- `TArenaUnity3D/Assets/Resources/UI/PRD_19/`

## Goal

Create the polished Unity UGUI prefabs needed by the Action Preview system.

The output should be production-shaped, polished, and Inspector-wireable:
designers can place the Canvas/panel in the battle scene, assign TMP fields,
and later runtime code only binds data.

## Required Skill Usage

The Coding Agent must use:

- `_codex/skills/make-ui-mockup/SKILL.md`
- `_codex/skills/polish-ui-mockup/SKILL.md`

The final prefab output should already be a polished version, not an unstyled
wireframe waiting for a separate polish pass.

## Scope

Do:

- Create a dedicated action preview Canvas/prefab set.
- Create a bottom-right `ActionPreviewPanel` prefab/section.
- Create an `ActionPreviewUnitBadgePresentation` prefab.
- Create an invalid target badge prefab or invalid-state prefab section.
- Create or reuse a mini `StackRepresentation` row/icon prefab for killed unit
  breakdowns.
- Use TMP/TextMesh Pro only.
- Use serialized fields for all TMP, Image, Transform parent, and child view
  references.
- Use repeated prefab parents for killed stack breakdown rows/icons.
- Include visible sample states for:
  - no target selected, values `-`,
  - valid attack preview,
  - retaliation own-loss summary,
  - guaranteed death marker,
  - possible death marker,
  - buff/debuff stat icon section,
  - utility/movement icon section,
  - invalid target badge.
- Copy and adapt visual structure from `Arena_Footer`, `Run_Footer`, and
  `Unit_Stats` where useful.
- Keep copied/adapted outputs under `PRD_54/ActionPreviewUX/`.
- Produce a `Polished_1` or equivalent polished output folder/copy.

Do not:

- Modify original `Arena_Footer`, `Run_Footer`, or `Unit_Stats` prefabs.
- Modify PRD019 assets.
- Add gameplay logic to UI scripts.
- Use legacy `UnityEngine.UI.Text`.
- Rely on runtime `transform.Find` for child binding.
- Flatten repeated prefabs into one giant manually copied hierarchy.

## Required Prefab Concepts

`ActionPreviewPanel` should support:

- skill/action name,
- damage range summary or `-`,
- red skull enemy killed unit count,
- blue skull own lost unit count,
- net economic value range or `-`,
- killed enemy stack representation list,
- own loss stack representation list,
- invalid/no-target state text area,
- optional effect summary rows for status/stat/utility icons.

`ActionPreviewUnitBadgePresentation` should support:

- damage range,
- kill range,
- death certainty overlay over kill area,
- status icon parent/list,
- stat change icon parent/list,
- utility/movement icon parent/list,
- section roots that can be hidden when empty.

`InvalidTargetBadgePresentation` should support:

- text `Invalid target`,
- optional short reason field,
- easy disabling/hiding later.

## Acceptance Criteria

Done when:

- New prefabs exist under `TArenaUnity3D/Assets/Resources/UI/PRD_54/ActionPreviewUX/`.
- A polished output copy/folder exists, not only a rough mockup.
- The bottom preview panel has explicit serialized fields suitable for runtime
  binding.
- The unit badge prefab has explicit serialized fields suitable for runtime
  binding.
- Empty sections can be hidden through assigned root GameObjects.
- The invalid badge prefab/state exists and displays `Invalid target`.
- Killed stack representation supports unit icon, count, and death marker.
- Red skull and blue skull summary visual slots exist.
- Reference prefabs were copied/adapted only under PRD054 output, not modified
  in place.
- No PRD019 files were modified.
- TextMesh Pro is used for all text.
- Prefab validation from `make-ui-mockup` was run if YAML/prefab tooling was
  used outside Unity.
- If Unity screenshot QA cannot be run, the final report says so clearly.

## Testing Decisions

Manual prefab inspection is required:

- Open the polished panel prefab and verify fields are assignable.
- Open the badge prefab and verify all section roots can be hidden.
- Confirm no text uses legacy `Text`.
- Confirm sample data is readable in Scene view.
- Confirm reference prefabs remain unchanged.

If the agent can run the polish screenshot helper safely, capture screenshots
under:

- `TArenaUnity3D/Assets/Resources/UI/PRD_54/Screenshots/`

## Out of Scope

- Runtime hover integration.
- BattleAction/SkillRules forecast calculation.
- Scene wiring if the target battle scene is ambiguous.
- Replacing the full battle HUD.

## Notes For Coding Agent

Use Codex 5.3 Spark subagents for bounded UI reconnaissance if useful:

- one explorer can inspect reference prefab hierarchy,
- one explorer can inspect available icon/stack representation assets,
- one explorer can check PRD019 naming without editing protected assets.

The main agent must still apply final prefab edits and verify the result.
