#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OfflinePlayerSettingsBridge))]
public class OfflinePlayerSettingsBridgeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(8f);

        OfflinePlayerSettingsBridge bridge = (OfflinePlayerSettingsBridge)target;
        if (GUILayout.Button("Save"))
        {
            bridge.Save();
            EditorUtility.SetDirty(bridge);
        }
    }
}
#endif
