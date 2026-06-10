# 002 DEP-CUT-001 QA Architecture Review

## Reviewed Protocol

`_codex/tasks/QA/002_DEP-CUT-001_RemoveOutlinePhotonPlayFabScriptDependencies_Completion.md`

## Verdict

Pass with Unity validation required.

## Findings

- No direct non-vendor game-code references remain for external PlayFab SDK,
  Photon/PUN namespaces, `PhotonNetwork`, `MonoBehaviourPun*`, `IPunObservable`,
  `PhotonStream`, or `PhotonMessageInfo`.
- Outline is intentionally retained. `OutlineM` still uses `cakeslice.Outline`;
  the fixed dependency was only the obsolete `UnityEngine.VR` import in
  `OutlineEffect`.
- Multiplayer UI intent is preserved after user correction. `PlayerPrefs.Multi`
  is still set from the UI, but old PUN runtime paths are disabled until a
  custom transport exists.
- Legacy class names `PlayFabControler` and `PhotonControler` remain as
  compatibility stubs to avoid scene reference churn. This is acceptable for
  this task, but should be removed or renamed when scenes are rewired.
- Local RPC adapter is intentionally transitional. It reduces compile coupling
  but does not provide network semantics; future custom multiplayer should
  replace `LocalRpcView` rather than grow PUN-shaped abstractions.

## Risks

- Unity compile was not run by agent policy; the user must validate in Unity.
- Scene objects still reference old compatibility components. Removing those
  components before rewiring UI events may create missing script/event slots.
- `Assets/Resources/PlayerP*.prefab` still contains UnityOutlineFX components;
  prefab cleanup requires explicit asset edit permission and a replacement
  highlight decision.

## Recommended Next Slice

Create a custom multiplayer design task that defines transport boundaries for:

- room/session lifecycle,
- army/build exchange,
- turn/action replication,
- deterministic action payloads replacing legacy RPC names,
- UI state for host/join without PlayFab/PUN.
