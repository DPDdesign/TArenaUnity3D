#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class AgentPrefabScreenshotBridge
{
    private const string BridgeVersion = "isolated-ui-layer-v4";
    private const string RequestRelativePath = "../_codex/tmp/unity_prefab_screenshot/request.txt";
    private const string ResponseRelativePath = "../_codex/tmp/unity_prefab_screenshot/response.txt";
    private const string BusyRelativePath = "../_codex/tmp/unity_prefab_screenshot/busy.txt";

    private static bool isProcessing;
    private static double nextPollTime;

    static AgentPrefabScreenshotBridge()
    {
        EditorApplication.update += PollForRequest;
    }

    [MenuItem("TArena/Agent/Process Prefab Screenshot Request")]
    public static void ProcessRequestFromMenu()
    {
        TryProcessRequest();
    }

    private static void PollForRequest()
    {
        if (EditorApplication.timeSinceStartup < nextPollTime)
        {
            return;
        }

        nextPollTime = EditorApplication.timeSinceStartup + 0.5d;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        TryProcessRequest();
    }

    private static void TryProcessRequest()
    {
        if (isProcessing)
        {
            return;
        }

        string requestPath = GetAbsoluteProjectPath(RequestRelativePath);
        if (!File.Exists(requestPath))
        {
            return;
        }

        isProcessing = true;
        string requestId = string.Empty;

        try
        {
            Dictionary<string, string> request = ReadKeyValueFile(requestPath);
            requestId = GetRequired(request, "id");
            string prefabPath = GetRequired(request, "prefab");
            string outputPath = GetRequired(request, "output");
            int width = GetInt(request, "width", 1600);
            int height = GetInt(request, "height", 900);

            WriteKeyValueFile(GetAbsoluteProjectPath(BusyRelativePath), new Dictionary<string, string>
            {
                { "id", requestId },
                { "status", "busy" },
                { "prefab", prefabPath }
            });

            string savedPath = CapturePrefab(prefabPath, outputPath, width, height);
            WriteKeyValueFile(GetAbsoluteProjectPath(ResponseRelativePath), new Dictionary<string, string>
            {
                { "id", requestId },
                { "status", "ok" },
                { "bridge", BridgeVersion },
                { "output", savedPath },
                { "message", "saved" }
            });

            Debug.Log("AGENT_PREFAB_SCREENSHOT_BRIDGE_SAVED " + savedPath);
        }
        catch (Exception exception)
        {
            WriteKeyValueFile(GetAbsoluteProjectPath(ResponseRelativePath), new Dictionary<string, string>
            {
                { "id", requestId },
                { "status", "error" },
                { "bridge", BridgeVersion },
                { "message", exception.GetType().Name + ": " + exception.Message }
            });

            Debug.LogError("Agent prefab screenshot bridge failed: " + exception);
        }
        finally
        {
            TryDelete(GetAbsoluteProjectPath(RequestRelativePath));
            TryDelete(GetAbsoluteProjectPath(BusyRelativePath));
            isProcessing = false;
        }
    }

    private static string CapturePrefab(string prefabPath, string outputPath, int width, int height)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            throw new InvalidOperationException("Prefab not found or not importable: " + prefabPath);
        }

        RenderTexture renderTexture = null;
        Texture2D texture = null;
        RenderTexture previousActive = RenderTexture.active;
        GameObject cameraGo = null;
        GameObject canvasGo = null;
        GameObject instance = null;

        try
        {
            cameraGo = new GameObject("AgentScreenshotCamera");
            cameraGo.hideFlags = HideFlags.HideAndDontSave;
            Camera camera = cameraGo.AddComponent<Camera>();
            int screenshotLayer = GetScreenshotLayer();
            cameraGo.layer = screenshotLayer;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.075f, 0.085f, 1f);
            camera.cullingMask = 1 << screenshotLayer;
            camera.orthographic = true;
            camera.orthographicSize = height * 0.5f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.transform.rotation = Quaternion.identity;

            canvasGo = new GameObject("AgentScreenshotCanvas");
            canvasGo.hideFlags = HideFlags.HideAndDontSave;
            canvasGo.layer = screenshotLayer;
            RectTransform canvasRect = canvasGo.AddComponent<RectTransform>();
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            camera.targetTexture = renderTexture;

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
            canvasRect.sizeDelta = new Vector2(width, height);
            canvasRect.anchoredPosition = Vector2.zero;
            canvasRect.localPosition = Vector3.zero;
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.localScale = Vector3.one;

            instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                instance = UnityEngine.Object.Instantiate(prefab);
            }

            instance.name = prefab.name;
            SetHideAndDontSave(instance);
            SetLayerRecursively(instance, screenshotLayer);
            instance.transform.SetParent(canvasGo.transform, false);
            PositionInstance(instance, width, height);
            EnableVisibleUi(instance);
            Canvas.ForceUpdateCanvases();
            FitCameraToBounds(camera, CalculateRectTransformBounds(instance), width, height);

            RenderTexture.active = renderTexture;
            GL.Clear(true, true, camera.backgroundColor);
            camera.Render();
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();

            string absoluteOutput = ToAbsoluteProjectPath(outputPath);
            string directory = Path.GetDirectoryName(absoluteOutput);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(absoluteOutput, texture.EncodeToPNG());

            string projectRelativeOutput = ToProjectRelativePath(absoluteOutput);
            if (!string.IsNullOrEmpty(projectRelativeOutput))
            {
                AssetDatabase.ImportAsset(projectRelativeOutput);
            }

            return absoluteOutput;
        }
        finally
        {
            RenderTexture.active = previousActive;

            if (texture != null)
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }

            if (renderTexture != null)
            {
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }

            if (instance != null)
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }

            if (canvasGo != null)
            {
                UnityEngine.Object.DestroyImmediate(canvasGo);
            }

            if (cameraGo != null)
            {
                UnityEngine.Object.DestroyImmediate(cameraGo);
            }
        }
    }

    private static void PositionInstance(GameObject instance, int width, int height)
    {
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
            return;
        }

        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
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

    private static Bounds CalculateRectTransformBounds(GameObject root)
    {
        bool hasBounds = false;
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        Vector3[] corners = new Vector3[4];

        foreach (RectTransform rectTransform in root.GetComponentsInChildren<RectTransform>(true))
        {
            rectTransform.GetWorldCorners(corners);
            for (int i = 0; i < corners.Length; i++)
            {
                if (!hasBounds)
                {
                    bounds = new Bounds(corners[i], Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(corners[i]);
                }
            }
        }

        if (!hasBounds)
        {
            bounds = new Bounds(Vector3.zero, new Vector3(1600f, 900f, 0f));
        }

        return bounds;
    }

    private static void FitCameraToBounds(Camera camera, Bounds bounds, int width, int height)
    {
        float aspect = Mathf.Max(0.01f, (float)width / Mathf.Max(1, height));
        float halfHeightFromWidth = bounds.extents.x / aspect;
        float halfHeight = Mathf.Max(bounds.extents.y, halfHeightFromWidth);
        halfHeight = Mathf.Max(0.01f, halfHeight * 1.01f);

        camera.orthographicSize = halfHeight;
        camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z - 10f);
        camera.transform.rotation = Quaternion.identity;
    }

    private static void SetHideAndDontSave(GameObject root)
    {
        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
        {
            transform.gameObject.hideFlags = HideFlags.HideAndDontSave;
        }
    }

    private static int GetScreenshotLayer()
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        return uiLayer >= 0 ? uiLayer : 0;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
        {
            transform.gameObject.layer = layer;
        }
    }

    private static Dictionary<string, string> ReadKeyValueFile(string path)
    {
        Dictionary<string, string> values = new Dictionary<string, string>();
        foreach (string line in File.ReadAllLines(path))
        {
            int separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            string key = line.Substring(0, separator).Trim();
            string value = line.Substring(separator + 1).Trim();
            values[key] = value;
        }

        return values;
    }

    private static void WriteKeyValueFile(string path, Dictionary<string, string> values)
    {
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        List<string> lines = new List<string>();
        foreach (KeyValuePair<string, string> pair in values)
        {
            lines.Add(pair.Key + "=" + SanitizeValue(pair.Value));
        }

        string tempPath = path + ".tmp";
        try
        {
            File.WriteAllLines(tempPath, lines.ToArray());
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(tempPath, path);
        }
        catch (IOException)
        {
            File.WriteAllLines(path, lines.ToArray());
            TryDelete(tempPath);
        }
        catch (UnauthorizedAccessException)
        {
            File.WriteAllLines(path, lines.ToArray());
            TryDelete(tempPath);
        }
    }

    private static string GetRequired(Dictionary<string, string> values, string key)
    {
        string value;
        if (!values.TryGetValue(key, out value) || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("Missing request value: " + key);
        }

        return value;
    }

    private static int GetInt(Dictionary<string, string> values, string key, int fallback)
    {
        string value;
        int parsed;
        if (values.TryGetValue(key, out value) && int.TryParse(value, out parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private static string SanitizeValue(string value)
    {
        return (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
    }

    private static string GetAbsoluteProjectPath(string relativePath)
    {
        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), relativePath));
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

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
#endif
