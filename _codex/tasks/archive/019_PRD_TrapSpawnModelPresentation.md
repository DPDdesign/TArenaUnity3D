# 019 PRD Trap Spawn Model Presentation

- Status: closed
- Type: PRD / Coding
- Area: Skills, Trap Presentation, Battle UI
- Owner: Coding Agent

## Goal

Migrate trap surface models into the skill presentation model so trap skills can
own both temporary cast/impact presentation and persistent board-state visuals.

The immediate player-facing need is that the player who casts `Rope_Trap` as
the Trapper owner should be able to see a rope trap model on the target hex
surface after the skill resolves.

## Current Behavior

- `Spike_Trap` and `Rope_Trap` are active skills in `CastManager`.
- Both skills create gameplay trap state through `HexClass.AddTrap(...)`.
- Both skills already play temporary hex presentation through
  `SkillPresentationManager.PlaySequencedHexEffect(...)`.
- Persistent trap visibility is still hardcoded in `Traps.ShowTrap()` through
  scene child names such as `trap1` and `Fire_Trap`.
- `Rope_Trap` has no persistent surface model in that hardcoded path.

## Desired Rules

- Trap gameplay state remains owned by `HexClass` and `Traps`.
- Trap visual authoring moves toward `SkillPresentationCatalog`.
- Trap skills can define a persistent `SpawnModel` separate from temporary
  `impactVfx`.
- `Rope_Trap` should support a persistent visible model for the caster/owner.
- `Spike_Trap`, `Rope_Trap`, `Fire_Trap`, and future traps should share the same
  spawn model path.
- Existing scene child trap visuals must keep working as fallback until their
  catalog `SpawnModel` fields are wired in Unity.

## Scope

Do:

- Add a persistent trap model field to `SkillPresentationEntry`.
- Add a `SkillPresentationManager` helper that spawns that model on a hex.
- Update `Traps` to store and destroy the spawned model.
- Preserve legacy child-object trap visuals as fallback.
- Document Unity setup and Play Mode checks.

Do not:

- Change trap damage, durations, cooldowns, targeting, or movement rules.
- Rename existing public or serialized fields.
- Edit prefabs, scenes, materials, or the catalog asset in this task.
- Implement full multiplayer visibility filtering yet.
- Move trap gameplay ownership out of `HexClass` / `Traps`.

## Acceptance Criteria

Done when:

- `SkillPresentationEntry` exposes a `spawnModel` field for persistent board
  objects.
- `Rope_Trap`, `Spike_Trap`, `Fire_Trap`, and future traps can spawn a
  persistent model from `SkillPresentationCatalog` by skill id.
- Removing a trap destroys the spawned persistent model.
- If no `spawnModel` is configured, existing hardcoded child-object visibility
  still works for `Spike_Trap` and `Fire_Trap`.
- The task has a completion protocol and is archived after implementation.

## Implementation - 2026-06-13

### What Changed

`SkillPresentationCatalog.cs` / `SkillPresentationEntry`: added `spawnModel`, a
new optional Inspector object field for persistent board-state models. `null`
means no catalog-driven persistent model is spawned and legacy fallback can
still handle old traps. Assigning a prefab/model makes that model spawn on the
target hex and stay until the owning trap state removes it. Tuning hint: keep
trap model pivots centered and near ground level so the spawned model lands
cleanly on the hex surface.

`SkillPresentationManager.cs`: added `SpawnPersistentModel(skillId, targetHex)`.
It resolves the catalog entry by the same skill id used by XML and `CastManager`,
spawns the configured `spawnModel` at the hex position, applies existing spawn
scale presets, parents the instance under the hex GameObject, and returns the
instance to gameplay state for cleanup.

`Traps.cs`: trap state now stores the spawned persistent model instance.
`ShowTrap()` first tries the catalog-driven spawn model path. If no model is
available, it falls back to the old child-object lookup for `Spike_Trap` and
`Fire_Trap`. `Remove()` destroys the spawned model or hides the legacy child.

`_codex/Context/09_CurrentSkills.md` and
`_codex/Context/10_Skill_Design_Rules.md`: documented that persistent
board-state visuals use `spawnModel`, while gameplay ownership remains in
`HexClass` / `Traps`.

### Automatic Test

No automated EditMode test was added. The changed behavior depends on a scene
`SkillPresentationManager`, a manually assigned `SkillPresentationCatalog`, hex
GameObjects, and prefab references in the Unity Inspector. The user validates it
manually in Unity.

### Unity Test

#### Unity Setup

Open the battle scene with `SkillPresentationManager`. Confirm its catalog field
points to `Resources/0_Data/SkillPresentationCatalog`. In that catalog, open
the `Rope_Trap` entry and assign a rope trap surface prefab/model to `Spawn
Model`. Optionally assign `Spawn Model` for `Spike_Trap` and `Fire_Trap` to
migrate them off legacy hex child objects.

#### Play Mode Test

Enter Play Mode, select a Trapper, cast `Rope_Trap` on an empty target hex, and
confirm the caster sees a persistent rope trap model on the hex surface after
the skill resolves. Move an enemy onto that hex and confirm the trap effect
still applies and the persistent model is removed. Cast `Spike_Trap` and trigger
or expire `Fire_Trap` to confirm their existing visuals still work when no
`Spawn Model` is assigned.

### QA Verdict

Pass. QA report:
`_codex/tasks/QA/2026-06-13_019_QA_ArchitectureReview.md`.

Actionable findings: none. Follow-up fixes applied: none.

Non-blocking observation: full opponent-hidden / owner-visible network filtering
is still out of scope. This implementation adds the persistent model path needed
for owner-visible traps, but multiplayer viewpoint filtering should be a later
explicit task if multiplayer is restored.

### Notes

No gameplay values changed: trap damage, cooldowns, durations, targeting, and
movement effects are unchanged. No prefabs, scenes, materials, or Unity asset
files were edited. `Rope_Trap` will only show a persistent surface model after
the `Spawn Model` field is assigned in Unity.

### Next Steps

Run the Play Mode checks above in Unity. Wire `Rope_Trap` first, then migrate
`Spike_Trap` and `Fire_Trap` by assigning their `Spawn Model` fields when their
final surface models are ready.
