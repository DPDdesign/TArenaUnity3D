# ADR 001: UI Portrait RenderTexture Experiment

Status: recorded experiment
Date: 2026-06-11
Project: TArenaUnity3D

## Context

During new battle UI work, we tested showing the active unit portrait through a
secondary Unity camera rendering into a `RenderTexture`, then displaying that
texture in UI through a `Raw Image`.

The intended use case was a circular hero/active-unit portrait area in the
footer HUD.

## Experiment Result

The RenderTexture approach works technically:

- a dedicated camera can render to a `RenderTexture`,
- UI can display it with `Raw Image`,
- a circular mask can clip the rendered image when the `Raw Image` is a child of
  an `Image + Mask` object,
- the decorative frame should stay outside the mask as a sibling rendered above
  the image.

Correct hierarchy for the experiment:

```text
Hero
+-- BG
+-- Mask                  // Image + Mask
|   +-- CameraImage        // Raw Image, Texture = active unit RenderTexture
+-- Frame                 // decorative frame above the masked image
```

## Decision

Do not continue the camera/RenderTexture portrait path for the current pass.

For the near-term HUD, use a normal `UnityEngine.UI.Image` field on `UICanvas`
and assign the active unit sprite directly from existing unit data.

## Rationale

The sprite path is simpler, easier to author, and matches current legacy unit
data:

- unit sprite id is loaded from `Resources/Data/Units.xml`,
- runtime unit stores it in `TosterHexUnit.TosterSpriteName`,
- existing turn-queue UI already loads it with
  `Resources.Load<Sprite>(toster.TosterSpriteName)`.

The camera portrait remains a possible future presentation upgrade, but it
would need code to follow the active unit and more careful scene setup.

## Current Implementation Direction

`UICanvas` owns:

- `CurrentUnitPortrait`

Current behavior:

- if `MouseControler.SelectedToster` exists, `UICanvas` loads
  `Resources.Load<Sprite>(SelectedToster.TosterSpriteName)`,
- this means the portrait displays the active turn unit for now,
- later UI work can replace this with a separate inspected/selected unit.
