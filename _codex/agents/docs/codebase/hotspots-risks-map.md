# TArenaUnity3D Hotspots, Risks, And Next Tasks Codebase Map

Status: active
Last updated: 2026-06-26

## Largest Game-Code Files

These are the main architecture hotspots:

- `Scripts/Lesisz/HexMap/MouseControler.cs` - input, turn action state machine,
  RPC wrappers, skill targeting, movement, attacks.
- `Scripts/Lesisz/Skills/CastManager.cs` - skill mode and skill execution
  monolith.
- `Scripts/Lesisz/HexMap/TosterHexUnit.cs` - unit stats, combat, pathing,
  statuses, messages.
- `Scripts/Lesisz/HexMap/HexMap.cs` - map generation, teams, PUN startup path,
  highlighting.
- `Scripts/Multiplayer/PlayFabControler.cs` - backend account/stats/shop/cloud.
- `Symulator.cs` - simulation UI/prototype; contains `NotImplementedException`
  methods.
- `Scripts/Lesisz/HexMap/TurnManager.cs` - initiative and queue logic.
- `Scripts/Lesisz/HexMap/TeamClass.cs` - team build load/generation.

## Known Runtime Risk Markers

Game-code markers found during the scan:

- `NotImplementedException` in `Symulator.cs`, `HexClass.cs`,
  `TosterHexUnit.cs`, and early overloads in `MouseControler.cs`.
- hard-coded old product text in `PlayFabControler.cs`: "Retsot account".
- `BinaryFormatter` used for local build files in menu/team/selection code.
- `Resources.Load` string paths for units, skills, icons, and toster models.
- skill assignment, skill UI, skill definitions, presentation, and remaining
  compatibility execution are coupled by string ids across the unit catalog,
  skill catalog, icon paths, presentation entries, and legacy `CastManager`
  method names.
- many public fields used by Unity Inspector; do not rename without permission.
- root and `Scripts` files are mostly in the global namespace, with only small
  pockets such as `HPath`, `Priority_Queue`, `TimeSpells`, `Traps`, `Tostery`,
  and outline/plugin namespaces.

## Recommended Next Tasks

1. Create a dedicated cleanup task to design a local-only game session path that
   bypasses Photon connection requirements without deleting SDKs.
2. Create a PlayFab replacement/stub task for account, inventory, shop, and
   profile reads used by `Shop`, `OverlayMainMenu`, `Generator`, and
   `MouseControler`.
3. Create a small task to decide whether `NetworkConMenager.cs` is unused and
   can be removed after Unity scene reference checks.
4. Create an architecture task to extract `PanelArmii.BuildG` into a plain data
   type only after serialized compatibility is understood.
5. Continue PRD49ED before skill cleanup: route Tactical AI selection and live
   skill execution through the shared `SkillRules` / validated action /
   SO-driven executor path.
6. When adding skill VFX/SFX, keep `UnitCatalog.asset` as the skill ownership
   source and use the existing skill string as the join key for any model-local
   presentation data.
