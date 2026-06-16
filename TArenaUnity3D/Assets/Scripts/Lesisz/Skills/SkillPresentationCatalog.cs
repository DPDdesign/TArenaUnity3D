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

[Serializable]
public class SkillPresentationEntry
{
    public string skillId;
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
