using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum SkillPresentationImpactAnchor
{
    TargetHex,
    TargetUnit,
    Caster,
    AreaCenter
}

public enum SkillPresentationAnimationPlayPath
{
    PlayAnimation,
    Trigger
}

public enum SkillIndicatorType
{
    None,
    Line,
    Scatter,
    AoE,
    Arc,
    Hex
}

public enum SkillIndicatorPlacement
{
    None,
    CasterToHover,
    UnderHover,
    UnderAffectedHexes,
    UnderTargets,
    UnderAllAllies,
    UnderAllEnemies
}

[Serializable]
public class SkillPresentationEntry
{
    public string skillId;
    [Tooltip("Optional animator state/trigger name. If empty, the skill uses the default state resolved from its slot, e.g. skill1.")]
    public string animationStateOverride;
    [Tooltip("If enabled, the caster stays in the played animator state instead of returning to the default idle state.")]
    public bool holdAnimationState;
    public SkillPresentationAnimationPlayPath AnimationPlayPath = SkillPresentationAnimationPlayPath.PlayAnimation;
    [Range(0f, 1f)]
    [InspectorName("Cast VFX Delay")]
    [FormerlySerializedAs("spellPresentationDelay")]
    [Tooltip("Normalized caster animation progress before cast VFX and projectile release. 0 starts immediately, 1 waits until the animation finishes.")]
    public float castVfxDelay = 0.7f;
    public GameObject castVfx;
    public AudioClip castSfx;
    [Tooltip("If enabled, cast presentation emits all child TrailRenderers under the caster TosterView.")]
    public bool useTrail = true;
    [Tooltip("Seconds to emit the optional weapon trail. If zero or lower, SkillPresentationManager uses its default trail duration.")]
    public float weaponTrailDurationSeconds;
    public GameObject projectileVfx;
    public AudioClip projectileSfx;
    public float projectileSpeed = 8f;
    public SkillPresentationImpactAnchor impactAnchor = SkillPresentationImpactAnchor.TargetHex;
    public FrontendTargetReaction targetReaction = FrontendTargetReaction.Hit;
    public GameObject impactVfx;
    public AudioClip impactSfx;
    public float impactDelaySeconds;
    public float effectLifetimeSeconds;
    public float projectileImpactDelaySeconds;
    [InspectorName("Spawn Model")]
    [Tooltip("Optional persistent board model spawned for traps or other lasting skill-created world objects.")]
    public GameObject spawnModel;
    [Tooltip("Skill hover preview shape.")]
    public SkillIndicatorType indicatorType = SkillIndicatorType.None;
    [Tooltip("Where the hover preview should be drawn.")]
    public SkillIndicatorPlacement indicatorPlacement = SkillIndicatorPlacement.None;
    public Sprite indicatorSprite;
    public Material indicatorMaterial;
    [Tooltip("Optional per-skill override for the material's Energy Fill texture. Leave empty to use the texture assigned on the material.")]
    public Texture2D indicatorFillTexture;
    [Min(0f)]
    [Tooltip("Preview animation speed. For Line/Scatter this is travel speed. For Hex/AoE/Arc this is rotation speed. 0 uses the service default.")]
    public float indicatorEffectSpeed;
    [Tooltip("Preview sprite scale multiplier. X changes sprite length/width on its local X axis, Y changes local Y axis. Zero values are treated as 1.")]
    public Vector2 indicatorPrefabScaleXY = Vector2.one;
}

[CreateAssetMenu(fileName = "SkillPresentationCatalog", menuName = "TArena/Skill Presentation Catalog")]
public class SkillPresentationCatalog : ScriptableObject
{
    public List<SkillPresentationEntry> entries = new List<SkillPresentationEntry>();
    public SkillPresentationEntry defaultBasicRangedAttackEntry = new SkillPresentationEntry();

    public SkillPresentationEntry GetEntry(string skillId)
    {
        if (string.IsNullOrEmpty(skillId) || entries == null)
        {
            return null;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            SkillPresentationEntry entry = entries[i];
            if (entry != null && entry.skillId == skillId)
            {
                return entry;
            }
        }

        return null;
    }
}
