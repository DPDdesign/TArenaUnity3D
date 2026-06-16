#!/usr/bin/env python3
"""Capture a Unity UGUI prefab screenshot through an open-Editor bridge or batchmode."""

from __future__ import annotations

import argparse
import os
import shutil
import subprocess
import sys
import time
from pathlib import Path


EDITOR_CLASS = "AgentPrefabScreenshotCapture"
EDITOR_RELATIVE_PATH = Path("Assets/Editor/GeneratedAgentTools/AgentPrefabScreenshotCapture.cs")
BRIDGE_SCRIPT_RELATIVE_PATH = Path("Assets/Editor/AgentPrefabScreenshotBridge.cs")
BRIDGE_DIR_RELATIVE_PATH = Path("../_codex/tmp/unity_prefab_screenshot")


EDITOR_SCRIPT = r'''using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class AgentPrefabScreenshotCapture
{
    public static void Capture()
    {
        string prefabPath = GetArg("--agent-prefab-screenshot-prefab") ?? Environment.GetEnvironmentVariable("AGENT_PREFAB_SCREENSHOT_PREFAB");
        string outputPath = GetArg("--agent-prefab-screenshot-output") ?? Environment.GetEnvironmentVariable("AGENT_PREFAB_SCREENSHOT_OUTPUT");
        int width = GetIntArg("--agent-prefab-screenshot-width", GetIntEnv("AGENT_PREFAB_SCREENSHOT_WIDTH", 1600));
        int height = GetIntArg("--agent-prefab-screenshot-height", GetIntEnv("AGENT_PREFAB_SCREENSHOT_HEIGHT", 900));

        if (string.IsNullOrEmpty(prefabPath))
        {
            throw new Exception("Missing --agent-prefab-screenshot-prefab");
        }

        if (string.IsNullOrEmpty(outputPath))
        {
            throw new Exception("Missing --agent-prefab-screenshot-output");
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            throw new Exception("Prefab not found or not importable: " + prefabPath);
        }

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cameraGo = new GameObject("AgentScreenshotCamera");
        Camera camera = cameraGo.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.07f, 0.075f, 0.085f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = height * 0.5f;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 100f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.transform.rotation = Quaternion.identity;

        GameObject canvasGo = new GameObject("AgentScreenshotCanvas");
        RectTransform canvasRect = canvasGo.AddComponent<RectTransform>();
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1f;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(width, height);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.pivot = new Vector2(0.5f, 0.5f);
        canvasRect.sizeDelta = Vector2.zero;
        canvasRect.anchoredPosition = Vector2.zero;

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, canvasGo.transform) as GameObject;
        if (instance == null)
        {
            instance = UnityEngine.Object.Instantiate(prefab, canvasGo.transform);
        }

        instance.name = prefab.name;
        RectTransform rect = instance.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            rect.localPosition = Vector3.zero;
        }
        else
        {
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
        }

        EnableVisibleUi(instance);
        Canvas.ForceUpdateCanvases();

        RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        try
        {
            camera.targetTexture = renderTexture;
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            GL.Clear(true, true, camera.backgroundColor);
            camera.Render();
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;

            string absoluteOutput = ToAbsoluteProjectPath(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absoluteOutput));
            File.WriteAllBytes(absoluteOutput, texture.EncodeToPNG());
            Debug.Log("AGENT_PREFAB_SCREENSHOT_SAVED " + absoluteOutput);

            string projectRelativeOutput = ToProjectRelativePath(absoluteOutput);
            if (!string.IsNullOrEmpty(projectRelativeOutput))
            {
                AssetDatabase.ImportAsset(projectRelativeOutput);
            }
        }
        finally
        {
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(texture);
            renderTexture.Release();
            UnityEngine.Object.DestroyImmediate(renderTexture);
        }
    }

    private static void EnableVisibleUi(GameObject root)
    {
        foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
        {
            canvas.enabled = true;
        }

        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            graphic.enabled = true;
        }

        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            text.enabled = true;
        }
    }

    private static string GetArg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
            {
                return args[i + 1];
            }
        }

        return null;
    }

    private static int GetIntArg(string name, int fallback)
    {
        string value = GetArg(name);
        if (int.TryParse(value, out int result))
        {
            return result;
        }

        return fallback;
    }

    private static int GetIntEnv(string name, int fallback)
    {
        string value = Environment.GetEnvironmentVariable(name);
        if (int.TryParse(value, out int result))
        {
            return result;
        }

        return fallback;
    }

    private static string ToAbsoluteProjectPath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
    }

    private static string ToProjectRelativePath(string absolutePath)
    {
        string projectRoot = Path.GetFullPath(Directory.GetCurrentDirectory()).Replace('\\', '/').TrimEnd('/');
        string normalized = Path.GetFullPath(absolutePath).Replace('\\', '/');
        if (!normalized.StartsWith(projectRoot + "/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return normalized.Substring(projectRoot.Length + 1);
    }
}
'''


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Run Unity and capture a rendered screenshot of a UGUI prefab."
    )
    parser.add_argument("--project-root", default="TArenaUnity3D", help="Unity project root.")
    parser.add_argument("--prefab", required=True, help="Prefab path, relative to the Unity project root or absolute.")
    parser.add_argument("--output", help="PNG output path, relative to the Unity project root or absolute.")
    parser.add_argument("--unity-exe", help="Path to Unity.exe. Auto-detected when omitted.")
    parser.add_argument("--width", type=int, default=1600)
    parser.add_argument("--height", type=int, default=900)
    parser.add_argument("--timeout", type=int, default=900, help="Unity timeout in seconds.")
    parser.add_argument("--mode", choices=("auto", "bridge", "batch"), default="auto")
    parser.add_argument("--bridge-timeout", type=int, default=180, help="Seconds to wait for an open Unity Editor bridge.")
    parser.add_argument("--bridge-poll", type=float, default=0.5, help="Seconds between bridge response checks.")
    parser.add_argument("--keep-editor-script", action="store_true", help="Leave the temporary Editor script in Assets.")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    workspace = Path.cwd()
    project_root = Path(args.project_root)
    if not project_root.is_absolute():
        project_root = workspace / project_root
    project_root = project_root.resolve()

    if not project_root.exists():
        print(f"ERROR: Unity project root does not exist: {project_root}", file=sys.stderr)
        return 2

    prefab_path = normalize_project_path(args.prefab, project_root)
    output_path = args.output or default_output_path(prefab_path)
    output_path = normalize_output_path(output_path, project_root)

    project_open = is_project_open(project_root)
    if args.mode == "bridge" or (args.mode == "auto" and project_open):
        bridge_code = try_bridge_capture(
            project_root=project_root,
            prefab_path=prefab_path,
            output_path=output_path,
            width=args.width,
            height=args.height,
            timeout_seconds=args.bridge_timeout,
            poll_seconds=args.bridge_poll,
        )
        if bridge_code == 0:
            return 0
        if args.mode == "bridge" or project_open:
            return bridge_code

        print("Open Unity bridge did not answer; falling back to Unity batchmode.")

    unity_exe = Path(args.unity_exe).resolve() if args.unity_exe else detect_unity_exe(project_root)
    if not unity_exe or not unity_exe.exists():
        print("ERROR: Unity.exe not found. Pass --unity-exe.", file=sys.stderr)
        return 2

    editor_script_path = project_root / EDITOR_RELATIVE_PATH
    editor_script_path.parent.mkdir(parents=True, exist_ok=True)
    existing_content = editor_script_path.read_text(encoding="utf-8") if editor_script_path.exists() else None
    previous_content = None if existing_content == EDITOR_SCRIPT else existing_content
    editor_script_path.write_text(EDITOR_SCRIPT, encoding="utf-8")

    log_path = project_root / "Temp" / "AgentPrefabScreenshotCapture.log"
    log_path.parent.mkdir(parents=True, exist_ok=True)

    command = [
        str(unity_exe),
        "-batchmode",
        "-projectPath",
        str(project_root),
        "-executeMethod",
        f"{EDITOR_CLASS}.Capture",
        "-agent-prefab-screenshot-prefab",
        prefab_path,
        "-agent-prefab-screenshot-output",
        output_path,
        "-agent-prefab-screenshot-width",
        str(args.width),
        "-agent-prefab-screenshot-height",
        str(args.height),
        "-quit",
        "-logFile",
        str(log_path),
    ]

    try:
        print("Running Unity screenshot capture...")
        print("Unity:", unity_exe)
        print("Prefab:", prefab_path)
        print("Output:", output_path)
        env = os.environ.copy()
        env["AGENT_PREFAB_SCREENSHOT_PREFAB"] = prefab_path
        env["AGENT_PREFAB_SCREENSHOT_OUTPUT"] = output_path
        env["AGENT_PREFAB_SCREENSHOT_WIDTH"] = str(args.width)
        env["AGENT_PREFAB_SCREENSHOT_HEIGHT"] = str(args.height)

        result = subprocess.run(
            command,
            cwd=project_root,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            timeout=args.timeout,
            env=env,
        )
    except subprocess.TimeoutExpired:
        print(f"ERROR: Unity timed out after {args.timeout} seconds.", file=sys.stderr)
        print(f"Log: {log_path}", file=sys.stderr)
        return 124
    finally:
        if not args.keep_editor_script:
            restore_or_remove(editor_script_path, previous_content)

    output_abs = to_absolute_project_path(output_path, project_root)
    if result.stdout:
        print(result.stdout)
    print(f"Log: {log_path}")

    if result.returncode != 0:
        print(f"ERROR: Unity exited with code {result.returncode}.", file=sys.stderr)
        print_tail(log_path)
        return result.returncode

    if not output_abs.exists() or output_abs.stat().st_size == 0:
        print(f"ERROR: Expected screenshot was not created: {output_abs}", file=sys.stderr)
        print_tail(log_path)
        return 1

    print(f"Saved screenshot: {output_abs}")
    print_tail(log_path, marker="AGENT_PREFAB_SCREENSHOT_SAVED")
    return 0


def try_bridge_capture(
    project_root: Path,
    prefab_path: str,
    output_path: str,
    width: int,
    height: int,
    timeout_seconds: int,
    poll_seconds: float,
) -> int:
    bridge_script = project_root / BRIDGE_SCRIPT_RELATIVE_PATH
    if not bridge_script.exists():
        print(
            f"ERROR: Open Unity bridge script is missing: {bridge_script}",
            file=sys.stderr,
        )
        return 2

    bridge_dir = (project_root / BRIDGE_DIR_RELATIVE_PATH).resolve()
    bridge_dir.mkdir(parents=True, exist_ok=True)
    request_path = bridge_dir / "request.txt"
    response_path = bridge_dir / "response.txt"

    request_id = f"{int(time.time() * 1000)}-{os.getpid()}"
    output_abs = to_absolute_project_path(output_path, project_root)

    write_key_value_file_atomic(
        request_path,
        {
            "id": request_id,
            "prefab": prefab_path,
            "output": output_path,
            "width": str(width),
            "height": str(height),
        },
    )

    print("Waiting for open Unity Editor bridge...")
    print("Prefab:", prefab_path)
    print("Output:", output_path)
    print("Request:", request_path)

    deadline = time.monotonic() + max(1, timeout_seconds)
    while time.monotonic() < deadline:
        if response_path.exists():
            response = read_key_value_file(response_path)
            if response.get("id") == request_id:
                status = response.get("status", "")
                if status == "ok":
                    saved_path = Path(response.get("output", str(output_abs)))
                    if not saved_path.exists() or saved_path.stat().st_size == 0:
                        print(
                            f"ERROR: Bridge reported success but screenshot is missing or empty: {saved_path}",
                            file=sys.stderr,
                        )
                        return 1
                    print(f"Saved screenshot: {saved_path}")
                    return 0

                print(
                    f"ERROR: Bridge screenshot failed: {response.get('message', 'unknown error')}",
                    file=sys.stderr,
                )
                return 1

        time.sleep(max(0.1, poll_seconds))

    print(
        "ERROR: Timed out waiting for open Unity Editor bridge. "
        "Make sure Unity has compiled AgentPrefabScreenshotBridge.cs.",
        file=sys.stderr,
    )
    return 124


def is_project_open(project_root: Path) -> bool:
    return (project_root / "Temp" / "UnityLockfile").exists()


def write_key_value_file_atomic(path: Path, values: dict[str, str]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temp_path = path.with_suffix(path.suffix + ".tmp")
    with temp_path.open("w", encoding="utf-8", newline="\n") as handle:
        for key, value in values.items():
            handle.write(f"{key}={sanitize_key_value(value)}\n")
    try:
        os.replace(temp_path, path)
    except PermissionError:
        shutil.copyfile(temp_path, path)
        try:
            temp_path.unlink(missing_ok=True)
        except PermissionError:
            pass


def read_key_value_file(path: Path) -> dict[str, str]:
    values: dict[str, str] = {}
    for line in path.read_text(encoding="utf-8", errors="replace").splitlines():
        if "=" not in line:
            continue
        key, value = line.split("=", 1)
        values[key.strip()] = value.strip()
    return values


def sanitize_key_value(value: str) -> str:
    return str(value).replace("\r", " ").replace("\n", " ")


def detect_unity_exe(project_root: Path) -> Path | None:
    version = read_unity_version(project_root)
    candidates: list[Path] = []

    if version:
        candidates.extend(
            [
                Path("C:/Program Files/Unity/Hub/Editor") / version / "Editor/Unity.exe",
                Path("D:/Unity") / version / "Editor/Unity.exe",
            ]
        )

    secondary = Path.home() / "AppData/Roaming/UnityHub/secondaryInstallPath.json"
    if secondary.exists() and version:
        raw = secondary.read_text(encoding="utf-8").strip().strip('"')
        if raw:
            candidates.append(Path(raw) / version / "Editor/Unity.exe")

    for candidate in candidates:
        if candidate.exists():
            return candidate

    found = shutil.which("Unity")
    return Path(found) if found else None


def read_unity_version(project_root: Path) -> str | None:
    version_file = project_root / "ProjectSettings/ProjectVersion.txt"
    if not version_file.exists():
        return None
    for line in version_file.read_text(encoding="utf-8-sig").splitlines():
        if line.startswith("m_EditorVersion:"):
            return line.split(":", 1)[1].strip()
    return None


def normalize_project_path(path_value: str, project_root: Path) -> str:
    path = Path(path_value)
    if path.is_absolute():
        try:
            return path.resolve().relative_to(project_root).as_posix()
        except ValueError:
            raise SystemExit(f"ERROR: Prefab must be inside project root: {path}") from None
    return path.as_posix()


def normalize_output_path(path_value: str, project_root: Path) -> str:
    path = Path(path_value)
    if path.is_absolute():
        return str(path.resolve())
    return path.as_posix()


def default_output_path(prefab_path: str) -> str:
    prefab = Path(prefab_path)
    name = prefab.stem
    prd = name if name.upper().startswith("PRD") else "PRD_Unsorted"
    return f"Assets/Resources/UI/{prd}/Screenshots/{name}_pass01.png"


def to_absolute_project_path(path_value: str, project_root: Path) -> Path:
    path = Path(path_value)
    if path.is_absolute():
        return path
    return project_root / path


def restore_or_remove(path: Path, previous_content: str | None) -> None:
    try:
        if previous_content is None:
            try:
                path.unlink()
            except FileNotFoundError:
                pass
            try:
                Path(str(path) + ".meta").unlink()
            except FileNotFoundError:
                pass
        else:
            path.write_text(previous_content, encoding="utf-8")
    except PermissionError as exc:
        print(
            f"WARNING: Could not clean temporary Editor script, likely locked by Unity: {path} ({exc})",
            file=sys.stderr,
        )


def print_tail(log_path: Path, marker: str | None = None, line_count: int = 80) -> None:
    if not log_path.exists():
        return
    lines = log_path.read_text(encoding="utf-8", errors="replace").splitlines()
    selected = [line for line in lines if marker and marker in line]
    if selected:
        print("\n".join(selected))
        return
    print("--- Unity log tail ---")
    print("\n".join(lines[-line_count:]))


if __name__ == "__main__":
    raise SystemExit(main())
