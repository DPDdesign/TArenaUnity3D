# 04 Milestones

Status: active
Project: TArenaUnity3D
Last updated: 2026-06-24

## Recovery Milestones

### M1 - Legacy Code Excavation

Goal: agents can quickly understand where gameplay code lives and which files
are vendor/demo/plugin code.

Done when:

- `_codex/agents/docs/codebase-map.md` lists the major script areas,
  responsibilities, dependencies, and first-risk files.
- `_codex/Context/CONTEXT-MAP.md` routes code, production, gameplay, and cleanup
  questions to the right local documents.
- active tasks use local TArena task files, not another project context.

### M2 - Broken Backend/Multiplayer Isolation

Goal: PlayFab, PUN, Photon, and multiplayer code can be removed or bypassed
without guessing.

Done when:

- all direct game-code references to `PlayFabControler`, `PhotonNetwork`,
  `MonoBehaviourPun*`, `[PunRPC]`, and `IPunObservable` are listed in tasks,
- local gameplay has a documented non-network path,
- backend/shop/profile dependencies are either stubbed, replaced locally, or
  explicitly marked out of scope.

### M3 - Asset Replacement Pass

Goal: old or missing visuals can be replaced without destabilizing code.

Done when:

- asset-dependent scripts and Resources paths are mapped,
- replacement targets are listed by gameplay surface,
- Unity assets are changed only under explicit user permission.

### M4 - Architecture Stabilization

Goal: the project becomes easier to modify safely.

Done when:

- gameplay state, UI, persistence, and backend/network concerns are separated
  enough for small tests or manual checks,
- high-risk monoliths are split only after their behavior is documented,
- future feature work can be done without touching Photon/PlayFab paths.

Current progress notes:

- PRD030 established the Offline Mode SQLite persistence layer and shared run
  context reader/writer pattern.
- PRD37 moved Reward Map toward materialized DB-backed run content and corrected
  battle-completion routing into Reward Map / Summary Value.
- PRD41 added reward value parity at the generator seam so materialized rewards
  are closer to comparable strategic choices.
- PRD046/047 added the Tactical Battle AI V1 architecture: pure battle
  snapshots, legal action intents, profile-budgeted 3-ply planning, live
  revalidation through existing battle action paths, skill bridging through
  `CastManager`, and an initial async worker-task planning pipeline.
- These are architecture stabilization steps for the Offline Mode run loop, but
  manual Unity validation is still required before calling the whole loop or the
  new AI path stable.

## Current Priority

Priority remains M1, then M2. PRD046/047 are acceptable as the current tactical
AI improvement baseline, so the next broad production pressure is still
backend/multiplayer isolation and local-loop verification, not more speculative
AI expansion.
