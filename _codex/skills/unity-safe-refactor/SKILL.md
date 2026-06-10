---
name: unity-safe-refactor
description: Apply an explicitly requested safe Unity C# refactor in TArenaUnity3D.
---

# Unity Safe Refactor

Use only when the user explicitly asks for refactor or cleanup. Prefer one clear
current runtime path and avoid compatibility wrappers unless requested.

Preserve serialized/public field names unless explicitly allowed.
