using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SkillPresentationEntry))]
public class SkillPresentationEntryDrawer : PropertyDrawer
{
    static readonly Dictionary<string, bool> sectionExpanded = new Dictionary<string, bool>();

    static readonly string[] PreviewFields =
    {
        "indicatorType",
        "indicatorPlacement",
        "indicatorSprite",
        "indicatorMaterial",
        "indicatorFillTexture",
        "indicatorEffectSpeed",
        "indicatorPrefabScaleXY"
    };

    static readonly string[] CastFields =
    {
        "animationStateOverride",
        "holdAnimationState",
        "AnimationPlayPath",
        "castVfxDelay",
        "castVfx",
        "castSfx",
        "useTrail",
        "weaponTrailDurationSeconds"
    };

    static readonly string[] ProjectileFields =
    {
        "projectileVfx",
        "projectileSfx",
        "projectileSpeed",
        "projectileImpactDelaySeconds"
    };

    static readonly string[] ImpactFields =
    {
        "impactAnchor",
        "targetReaction",
        "impactVfx",
        "impactSfx",
        "impactDelaySeconds",
        "effectLifetimeSeconds",
        "spawnModel"
    };

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Rect line = NextLine(ref position, EditorGUIUtility.singleLineHeight);
        SerializedProperty skillId = property.FindPropertyRelative("skillId");
        GUIContent entryLabel = BuildEntryLabel(label, skillId);
        property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, entryLabel, true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            DrawProperty(ref position, skillId);
            DrawSection(ref position, property, "Preview", PreviewFields, true);
            DrawSection(ref position, property, "Cast", CastFields, false);
            DrawSection(ref position, property, "Projectile", ProjectileFields, false);
            DrawSection(ref position, property, "Impact", ImpactFields, false);
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;
        if (!property.isExpanded)
        {
            return height;
        }

        height += EditorGUIUtility.standardVerticalSpacing;
        height += PropertyHeight(property.FindPropertyRelative("skillId"));
        height += SectionHeight(property, "Preview", PreviewFields, true);
        height += SectionHeight(property, "Cast", CastFields, false);
        height += SectionHeight(property, "Projectile", ProjectileFields, false);
        height += SectionHeight(property, "Impact", ImpactFields, false);
        return height;
    }

    static GUIContent BuildEntryLabel(GUIContent fallback, SerializedProperty skillId)
    {
        if (skillId == null || string.IsNullOrEmpty(skillId.stringValue))
        {
            return fallback;
        }

        return new GUIContent(fallback.text + " - " + skillId.stringValue, fallback.tooltip);
    }

    static void DrawSection(
        ref Rect position,
        SerializedProperty root,
        string title,
        string[] fields,
        bool defaultExpanded)
    {
        Rect header = NextLine(ref position, EditorGUIUtility.singleLineHeight);
        string key = SectionKey(root, title);
        bool expanded = IsSectionExpanded(key, defaultExpanded);
        expanded = EditorGUI.Foldout(header, expanded, title, true);
        sectionExpanded[key] = expanded;

        if (!expanded)
        {
            return;
        }

        EditorGUI.indentLevel++;
        for (int i = 0; i < fields.Length; i++)
        {
            DrawProperty(ref position, root.FindPropertyRelative(fields[i]));
        }
        EditorGUI.indentLevel--;
    }

    static void DrawProperty(ref Rect position, SerializedProperty property)
    {
        if (property == null)
        {
            return;
        }

        float height = EditorGUI.GetPropertyHeight(property, true);
        Rect line = NextLine(ref position, height);
        EditorGUI.PropertyField(line, property, true);
    }

    static float SectionHeight(
        SerializedProperty root,
        string title,
        string[] fields,
        bool defaultExpanded)
    {
        float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        if (!IsSectionExpanded(SectionKey(root, title), defaultExpanded))
        {
            return height;
        }

        for (int i = 0; i < fields.Length; i++)
        {
            height += PropertyHeight(root.FindPropertyRelative(fields[i]));
        }

        return height;
    }

    static float PropertyHeight(SerializedProperty property)
    {
        if (property == null)
        {
            return 0f;
        }

        return EditorGUI.GetPropertyHeight(property, true) + EditorGUIUtility.standardVerticalSpacing;
    }

    static Rect NextLine(ref Rect position, float height)
    {
        Rect line = new Rect(position.x, position.y, position.width, height);
        position.y += height + EditorGUIUtility.standardVerticalSpacing;
        return line;
    }

    static bool IsSectionExpanded(string key, bool defaultExpanded)
    {
        bool expanded;
        if (sectionExpanded.TryGetValue(key, out expanded))
        {
            return expanded;
        }

        sectionExpanded[key] = defaultExpanded;
        return defaultExpanded;
    }

    static string SectionKey(SerializedProperty property, string section)
    {
        int targetId = property.serializedObject.targetObject != null
            ? property.serializedObject.targetObject.GetInstanceID()
            : 0;

        return targetId + ":" + property.propertyPath + ":" + section;
    }
}
