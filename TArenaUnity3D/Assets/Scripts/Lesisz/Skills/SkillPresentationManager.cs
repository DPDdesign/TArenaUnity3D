using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class SkillPresentationManager : MonoBehaviour
{
    public static SkillPresentationManager Instance;

//hardcodowane zmnijeszanie VFXów, bo magic arsxneal za duży miejscami, a nie chciało mi się ręcznie XD
    const float MagicArsenalDomeScaleMultiplier = 0.5f;
    const float MagicArsenalNovaScaleMultiplier = 0.5f;
    const float MagicArsenalSprayScaleMultiplier = 1.5f;
    const float MagicArsenalAuraScaleMultiplier = 0.4f;

    const float MagicArsenalRainScaleMultiplier = 0.3f;

    const float MagicArsenalMuzzleScaleMultiplier = 2f;

    [SerializeField] SkillPresentationCatalog catalog;
    [SerializeField] AudioSource audioSource;

    static bool missingManagerWarningShown;
    bool missingCatalogWarningShown;
    bool missingAudioSourceWarningShown;
    bool missingWeaponTrailsWarningShown;
    static int blockingPresentationCount;
    const float SkillAnimationMaxWaitSeconds = 1.25f;
    const float DefaultWeaponTrailDurationSeconds = 0.35f;

    public static bool HasBlockingPresentation
    {
        get { return blockingPresentationCount > 0; }
    }

    public static IEnumerator WaitForBlockingPresentation(float maxWaitSeconds)
    {
        float startedAt = Time.time;
        while (blockingPresentationCount > 0)
        {
            if (maxWaitSeconds > 0f && Time.time - startedAt > maxWaitSeconds)
            {
                Debug.LogWarning("SkillPresentationManager blocking presentation wait timed out.");
                blockingPresentationCount = 0;
                yield break;
            }

            yield return null;
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public static void PlayCast(string skillId, TosterHexUnit caster)
    {
        SkillPresentationManager manager = GetManager();
        if (manager == null)
        {
            return;
        }

        SkillPresentationEntry entry = manager.GetEntry(skillId);
        if (entry == null)
        {
            return;
        }

        manager.PlayCast(entry, caster);
    }

    public static void PlayCastSfxOnly(string skillId)
    {
        SkillPresentationManager manager = GetManager();
        if (manager == null)
        {
            return;
        }

        SkillPresentationEntry entry = manager.GetEntry(skillId);
        if (entry == null)
        {
            return;
        }

        manager.PlaySfx(entry.castSfx);
    }

    public static void PlayImpact(string skillId, TosterHexUnit caster, TosterHexUnit targetUnit, HexClass targetHex)
    {
        SkillPresentationManager manager = GetManager();
        if (manager == null)
        {
            return;
        }

        SkillPresentationEntry entry = manager.GetEntry(skillId);
        if (entry == null)
        {
            return;
        }

        Vector3 position = ResolveImpactPosition(entry, caster, targetUnit, targetHex);
        manager.StartBlockingCoroutine(manager.PlayImpactAfterDelay(entry, position, entry.impactDelaySeconds, true));
    }

    public static void PlayImpactVfxOnly(string skillId, TosterHexUnit caster, TosterHexUnit targetUnit, HexClass targetHex)
    {
        SkillPresentationManager manager = GetManager();
        if (manager == null)
        {
            return;
        }

        SkillPresentationEntry entry = manager.GetEntry(skillId);
        if (entry == null)
        {
            return;
        }

        Vector3 position = ResolveImpactPosition(entry, caster, targetUnit, targetHex);
        manager.StartBlockingCoroutine(manager.PlayImpactAfterDelay(entry, position, entry.impactDelaySeconds, false));
    }

    public static GameObject SpawnPersistentModel(string skillId, HexClass targetHex)
    {
        SkillPresentationManager manager = GetManager();
        if (manager == null)
        {
            return null;
        }

        SkillPresentationEntry entry = manager.GetEntry(skillId);
        if (entry == null || entry.spawnModel == null)
        {
            return null;
        }

        return manager.SpawnPersistentModel(entry, targetHex);
    }

    public static void PlayProjectile(string skillId, TosterHexUnit caster, TosterHexUnit targetUnit, HexClass targetHex)
    {
        SkillPresentationManager manager = GetManager();
        if (manager == null)
        {
            return;
        }

        SkillPresentationEntry entry = manager.GetEntry(skillId);
        if (entry == null)
        {
            return;
        }

        manager.PlayProjectile(entry, caster, targetUnit, targetHex, true);
    }

    public static void PlayProjectileTravelOnly(string skillId, TosterHexUnit caster, TosterHexUnit targetUnit, HexClass targetHex)
    {
        SkillPresentationManager manager = GetManager();
        if (manager == null)
        {
            return;
        }

        SkillPresentationEntry entry = manager.GetEntry(skillId);
        if (entry == null)
        {
            return;
        }

        manager.PlayProjectile(entry, caster, targetUnit, targetHex, false);
    }

    public static void PlayBasicRangedAttack(TosterHexUnit caster, TosterHexUnit target)
    {
        PlayBasicRangedAttack(caster, target, null);
    }

    public static void PlayBasicRangedAttack(TosterHexUnit caster, TosterHexUnit target, FrontendResultReveal reveal)
    {
        SkillPresentationManager manager = GetManager();
        if (manager == null)
        {
            FrontendResultRevealPlayer.Play(reveal);
            return;
        }

        SkillPresentationEntry entry = manager.GetBasicRangedAttackEntry();
        if (entry == null)
        {
            FrontendResultRevealPlayer.Play(reveal);
            return;
        }

        manager.StartBlockingCoroutine(manager.PlayProjectileImpactRevealSequence(
            entry,
            caster,
            target,
            target != null ? target.Hex : null,
            SingleReveal(reveal),
            null));
    }

    public static void PlaySequencedProjectileHits(
        string skillId,
        TosterHexUnit caster,
        HexClass targetHex,
        List<FrontendResultReveal> reveals,
        string casterAnimationState)
    {
        SkillPresentationManager manager = GetManager();
        if (manager == null)
        {
            PlayRevealsImmediately(reveals);
            return;
        }

        SkillPresentationEntry entry = manager.GetEntry(skillId);
        if (entry == null)
        {
            PlayRevealsImmediately(reveals);
            return;
        }

        manager.StartBlockingCoroutine(manager.PlayProjectileImpactRevealSequence(
            entry,
            caster,
            targetHex != null && targetHex.Tosters.Count > 0 ? targetHex.Tosters[0] : null,
            targetHex,
            reveals,
            casterAnimationState));
    }

    public static void PlaySequencedProjectileHexImpactThenReveals(
        string skillId,
        TosterHexUnit caster,
        HexClass targetHex,
        List<FrontendResultReveal> reveals,
        string casterAnimationState,
        Action afterImpact)
    {
        SkillPresentationManager manager = GetManager();
        if (manager == null)
        {
            afterImpact?.Invoke();
            PlayRevealsImmediately(reveals);
            return;
        }

        SkillPresentationEntry entry = manager.GetEntry(skillId);
        if (entry == null)
        {
            afterImpact?.Invoke();
            PlayRevealsImmediately(reveals);
            return;
        }

        manager.StartBlockingCoroutine(manager.PlayProjectileHexImpactThenRevealSequence(
            entry,
            caster,
            targetHex,
            reveals,
            casterAnimationState,
            afterImpact));
    }

    public static void PlaySequencedInstantHits(
        string skillId,
        TosterHexUnit caster,
        List<FrontendResultReveal> reveals,
        string casterAnimationState)
    {
        SkillPresentationManager manager = GetManager();
        if (manager == null)
        {
            PlayRevealsImmediately(reveals);
            return;
        }

        SkillPresentationEntry entry = manager.GetEntry(skillId);
        if (entry == null)
        {
            PlayRevealsImmediately(reveals);
            return;
        }

        manager.StartBlockingCoroutine(manager.PlayInstantImpactRevealSequence(
            entry,
            caster,
            reveals,
            casterAnimationState));
    }

    public static void PlaySequencedProjectileHitsToUnits(
        string skillId,
        TosterHexUnit caster,
        List<FrontendResultReveal> reveals,
        string casterAnimationState)
    {
        SkillPresentationManager manager = GetManager();
        if (manager == null)
        {
            PlayRevealsImmediately(reveals);
            return;
        }

        SkillPresentationEntry entry = manager.GetEntry(skillId);
        if (entry == null)
        {
            PlayRevealsImmediately(reveals);
            return;
        }

        manager.StartBlockingCoroutine(manager.PlayProjectilesImpactRevealSequence(
            entry,
            caster,
            reveals,
            casterAnimationState));
    }

    public static void PlaySequencedCasterEffect(string skillId, TosterHexUnit caster, string casterAnimationState)
    {
        PlaySequencedLocationEffect(skillId, caster, caster, caster != null ? caster.Hex : null, casterAnimationState);
    }

    public static void PlaySequencedHexEffect(string skillId, TosterHexUnit caster, HexClass targetHex, string casterAnimationState)
    {
        PlaySequencedLocationEffect(
            skillId,
            caster,
            targetHex != null && targetHex.Tosters.Count > 0 ? targetHex.Tosters[0] : null,
            targetHex,
            casterAnimationState);
    }

    public static void PlaySequencedHexEffectWithoutCastSfx(string skillId, TosterHexUnit caster, HexClass targetHex, string casterAnimationState)
    {
        PlaySequencedLocationEffect(
            skillId,
            caster,
            targetHex != null && targetHex.Tosters.Count > 0 ? targetHex.Tosters[0] : null,
            targetHex,
            casterAnimationState,
            false);
    }

    public static void PlaySequencedHexEffectWithReveals(
        string skillId,
        TosterHexUnit caster,
        HexClass targetHex,
        List<FrontendResultReveal> reveals,
        string casterAnimationState)
    {
        SkillPresentationManager manager = GetManager();
        if (manager == null)
        {
            PlayRevealsImmediately(reveals);
            return;
        }

        SkillPresentationEntry entry = manager.GetEntry(skillId);
        if (entry == null)
        {
            PlayRevealsImmediately(reveals);
            return;
        }

        manager.StartBlockingCoroutine(manager.PlayLocationImpactRevealSequence(
            entry,
            caster,
            targetHex != null && targetHex.Tosters.Count > 0 ? targetHex.Tosters[0] : null,
            targetHex,
            reveals,
            casterAnimationState));
    }

    public static void PlaySequencedHexCastUnitImpactThenReveals(
        string skillId,
        TosterHexUnit caster,
        HexClass castHex,
        TosterHexUnit impactUnit,
        List<FrontendResultReveal> reveals,
        string casterAnimationState,
        Action afterImpact)
    {
        SkillPresentationManager manager = GetManager();
        if (manager == null)
        {
            afterImpact?.Invoke();
            PlayRevealsImmediately(reveals);
            return;
        }

        SkillPresentationEntry entry = manager.GetEntry(skillId);
        if (entry == null)
        {
            afterImpact?.Invoke();
            PlayRevealsImmediately(reveals);
            return;
        }

        manager.StartBlockingCoroutine(manager.PlayHexCastUnitImpactThenRevealSequence(
            entry,
            caster,
            castHex,
            impactUnit,
            reveals,
            casterAnimationState,
            afterImpact));
    }

    public static void PlaySequencedUnitEffect(string skillId, TosterHexUnit caster, TosterHexUnit target, string casterAnimationState)
    {
        PlaySequencedLocationEffect(skillId, caster, target, target != null ? target.Hex : null, casterAnimationState);
    }

    static void PlaySequencedLocationEffect(
        string skillId,
        TosterHexUnit caster,
        TosterHexUnit targetUnit,
        HexClass targetHex,
        string casterAnimationState,
        bool playCastSfx = true)
    {
        SkillPresentationManager manager = GetManager();
        if (manager == null)
        {
            return;
        }

        SkillPresentationEntry entry = manager.GetEntry(skillId);
        if (entry == null)
        {
            return;
        }

        manager.StartBlockingCoroutine(manager.PlayLocationEffectSequence(
            entry,
            caster,
            targetUnit,
            targetHex,
            casterAnimationState,
            playCastSfx));
    }

    static List<FrontendResultReveal> SingleReveal(FrontendResultReveal reveal)
    {
        List<FrontendResultReveal> reveals = new List<FrontendResultReveal>();
        if (reveal != null)
        {
            reveals.Add(reveal);
        }

        return reveals;
    }

    static void PlayRevealsImmediately(List<FrontendResultReveal> reveals)
    {
        if (reveals == null)
        {
            return;
        }

        for (int i = 0; i < reveals.Count; i++)
        {
            FrontendResultRevealPlayer.Play(reveals[i]);
        }
    }

    static void PlayRevealsImmediately(SkillPresentationEntry entry, List<FrontendResultReveal> reveals)
    {
        if (reveals == null)
        {
            return;
        }

        for (int i = 0; i < reveals.Count; i++)
        {
            FrontendResultRevealPlayer.Play(ApplyTargetReaction(entry, reveals[i]));
        }
    }

    IEnumerator PlayRevealsAndWait(SkillPresentationEntry entry, List<FrontendResultReveal> reveals)
    {
        if (reveals == null)
        {
            yield break;
        }

        int pendingReveals = 0;
        for (int i = 0; i < reveals.Count; i++)
        {
            pendingReveals++;
            StartCoroutine(PlayRevealAndSignal(entry, reveals[i], () => pendingReveals--));
        }

        while (pendingReveals > 0)
        {
            yield return null;
        }
    }

    IEnumerator PlayRevealAndSignal(SkillPresentationEntry entry, FrontendResultReveal reveal, Action onComplete)
    {
        IEnumerator revealRoutine = PlayRevealAndWait(entry, reveal);
        while (revealRoutine != null)
        {
            bool hasNext;
            object current = null;

            try
            {
                hasNext = revealRoutine.MoveNext();
                if (hasNext)
                {
                    current = revealRoutine.Current;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                break;
            }

            if (!hasNext)
            {
                break;
            }

            yield return current;
        }

        if (onComplete != null)
        {
            onComplete();
        }
    }

    IEnumerator PlayRevealAndWait(SkillPresentationEntry entry, FrontendResultReveal reveal)
    {
        FrontendResultReveal revealWithReaction = ApplyTargetReaction(entry, reveal);
        if (revealWithReaction == null || !revealWithReaction.ShouldReveal)
        {
            yield break;
        }

        yield return revealWithReaction.TargetUnit.RevealFrontendResult(revealWithReaction);
    }

    static FrontendResultReveal ApplyTargetReaction(SkillPresentationEntry entry, FrontendResultReveal reveal)
    {
        if (entry == null || reveal == null)
        {
            return reveal;
        }

        return reveal.WithTargetReaction(entry.targetReaction);
    }

    static SkillPresentationManager GetManager()
    {
        SkillPresentationManager manager = Instance;
        if (manager == null)
        {
            manager = FindObjectOfType<SkillPresentationManager>();
            if (manager != null)
            {
                Instance = manager;
            }
        }

        if (manager == null && !missingManagerWarningShown)
        {
            Debug.LogWarning("SkillPresentationManager is missing from the scene. Skill VFX/SFX will not play.");
            missingManagerWarningShown = true;
        }

        return manager;
    }

    SkillPresentationEntry GetEntry(string skillId)
    {
        if (catalog == null)
        {
            WarnMissingCatalog();
            return null;
        }

        return catalog.GetEntry(skillId);
    }

    SkillPresentationEntry GetBasicRangedAttackEntry()
    {
        if (catalog == null)
        {
            WarnMissingCatalog();
            return null;
        }

        return catalog.defaultBasicRangedAttackEntry;
    }

    Coroutine StartBlockingCoroutine(IEnumerator routine)
    {
        return StartCoroutine(TrackBlockingPresentation(routine));
    }

    IEnumerator TrackBlockingPresentation(IEnumerator routine)
    {
        blockingPresentationCount++;

        while (routine != null)
        {
            bool hasNext;
            object current = null;
            try
            {
                hasNext = routine.MoveNext();
                if (hasNext)
                {
                    current = routine.Current;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                break;
            }

            if (!hasNext)
            {
                break;
            }

            yield return current;
        }

        blockingPresentationCount = Mathf.Max(0, blockingPresentationCount - 1);
    }

    void PlayCast(SkillPresentationEntry entry, TosterHexUnit caster)
    {
        PlayCast(entry, caster, true);
    }

    void PlayCast(SkillPresentationEntry entry, TosterHexUnit caster, bool playSfx)
    {
        PlayWeaponTrail(entry, caster);
        PlayCastSfx(entry, playSfx);
        PlayCastVfx(entry, caster);
    }

    void PlayCastSfx(SkillPresentationEntry entry, bool playSfx)
    {
        if (playSfx)
        {
            PlaySfx(entry.castSfx);
        }
    }

    void PlayCastVfx(SkillPresentationEntry entry, TosterHexUnit caster)
    {
        Vector3 position = ResolveUnitPosition(caster);
        SpawnVfx(entry.castVfx, position, entry.effectLifetimeSeconds);
    }

    void PlayWeaponTrail(SkillPresentationEntry entry, TosterHexUnit caster)
    {
        PlayWeaponTrail(entry, caster, GetWeaponTrailDurationSeconds(entry));
    }

    void PlayWeaponTrail(SkillPresentationEntry entry, TosterHexUnit caster, float durationSeconds)
    {
        if (entry == null || caster == null || caster.tosterView == null)
        {
            return;
        }

        if (!entry.useTrail)
        {
            return;
        }

        if (!caster.tosterView.TryPlayWeaponTrails(durationSeconds))
        {
            WarnMissingWeaponTrails();
        }
    }

    float GetWeaponTrailDurationSeconds(SkillPresentationEntry entry)
    {
        if (entry != null && entry.weaponTrailDurationSeconds > 0f)
        {
            return entry.weaponTrailDurationSeconds;
        }

        return DefaultWeaponTrailDurationSeconds;
    }

    float GetAnimatedWeaponTrailDurationSeconds(SkillPresentationEntry entry, string casterAnimationState)
    {
        float durationSeconds = GetWeaponTrailDurationSeconds(entry);
        if (entry == null || string.IsNullOrEmpty(casterAnimationState))
        {
            return durationSeconds;
        }

        float animationWindowSeconds = SkillAnimationMaxWaitSeconds;
        float presentationDelaySeconds = Mathf.Clamp01(entry.castVfxDelay) * animationWindowSeconds;
        if (entry.castVfxDelay >= 1f)
        {
            return Mathf.Max(durationSeconds, animationWindowSeconds);
        }

        return Mathf.Max(durationSeconds, animationWindowSeconds, presentationDelaySeconds + durationSeconds);
    }

    void PlayProjectile(SkillPresentationEntry entry, TosterHexUnit caster, TosterHexUnit targetUnit, HexClass targetHex, bool playImpact)
    {
        Vector3 start = ResolveUnitPosition(caster);
        Vector3 end = ResolveImpactPosition(entry, caster, targetUnit, targetHex);

        StartBlockingCoroutine(PlayProjectileSequence(entry, start, end, playImpact));
    }

    IEnumerator PlayProjectileSequence(SkillPresentationEntry entry, Vector3 start, Vector3 end, bool playImpact)
    {
        yield return PlayProjectileTravel(entry, start, end);

        if (playImpact)
        {
            yield return PlayImpactAfterDelay(entry, end, entry.projectileImpactDelaySeconds, true);
        }
    }

    IEnumerator PlayProjectileTravel(SkillPresentationEntry entry, Vector3 start, Vector3 end)
    {
        AudioSource projectileAudio = StartProjectileSfx(entry.projectileSfx);
        if (entry.projectileVfx == null)
        {
            StopProjectileSfx(projectileAudio);
            yield break;
        }

        GameObject projectile = Instantiate(entry.projectileVfx, start, Quaternion.identity);
        yield return MoveProjectile(projectile, start, end, entry);
        StopProjectileSfx(projectileAudio);
    }

    IEnumerator MoveProjectile(GameObject projectile, Vector3 start, Vector3 end, SkillPresentationEntry entry)
    {
        if (projectile == null)
        {
            yield break;
        }

        projectile.transform.position = start;
        Vector3 direction = end - start;
        if (direction.sqrMagnitude > 0.0001f)
        {
            projectile.transform.rotation = Quaternion.LookRotation(direction.normalized);
        }

        if (entry.projectileSpeed <= 0f)
        {
            projectile.transform.position = end;
        }
        else
        {
            while (projectile != null && Vector3.Distance(projectile.transform.position, end) > 0.01f)
            {
                projectile.transform.position = Vector3.MoveTowards(
                    projectile.transform.position,
                    end,
                    entry.projectileSpeed * Time.deltaTime);
                yield return null;
            }
        }

        if (projectile != null)
        {
            Destroy(projectile);
        }
    }

    IEnumerator PlayProjectileImpactRevealSequence(
        SkillPresentationEntry entry,
        TosterHexUnit caster,
        TosterHexUnit targetUnit,
        HexClass targetHex,
        List<FrontendResultReveal> reveals,
        string casterAnimationState)
    {
        yield return PlayCasterAnimationAndCast(entry, caster, casterAnimationState);

        Vector3 start = ResolveUnitPosition(caster);
        Vector3 end = ResolveImpactPosition(entry, caster, targetUnit, targetHex);
        yield return PlayProjectileTravel(entry, start, end);
        yield return PlayImpactRevealsAfterDelay(entry, caster, reveals, entry.projectileImpactDelaySeconds);
    }

    IEnumerator PlayInstantImpactRevealSequence(
        SkillPresentationEntry entry,
        TosterHexUnit caster,
        List<FrontendResultReveal> reveals,
        string casterAnimationState)
    {
        yield return PlayCasterAnimationAndCast(entry, caster, casterAnimationState);
        yield return PlayImpactRevealsAfterDelay(entry, caster, reveals, entry.impactDelaySeconds);
    }

    IEnumerator PlayProjectileHexImpactThenRevealSequence(
        SkillPresentationEntry entry,
        TosterHexUnit caster,
        HexClass targetHex,
        List<FrontendResultReveal> reveals,
        string casterAnimationState,
        Action afterImpact)
    {
        yield return PlayCasterAnimationAndCast(entry, caster, casterAnimationState);

        Vector3 start = ResolveUnitPosition(caster);
        Vector3 end = ResolveImpactPosition(entry, caster, null, targetHex);
        yield return PlayProjectileTravel(entry, start, end);

        if (entry.projectileImpactDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(entry.projectileImpactDelaySeconds);
        }

        SpawnVfx(entry.impactVfx, end, entry.effectLifetimeSeconds);
        PlaySfx(entry.impactSfx);
        afterImpact?.Invoke();
        yield return PlayRevealsAndWait(entry, reveals);
    }

    IEnumerator PlayProjectilesImpactRevealSequence(
        SkillPresentationEntry entry,
        TosterHexUnit caster,
        List<FrontendResultReveal> reveals,
        string casterAnimationState)
    {
        yield return PlayCasterAnimationAndCast(entry, caster, casterAnimationState);

        if (reveals == null)
        {
            yield break;
        }

        bool canPlayImpactSfx = true;
        for (int i = 0; i < reveals.Count; i++)
        {
            FrontendResultReveal reveal = reveals[i];
            if (reveal == null || reveal.TargetUnit == null)
            {
                continue;
            }

            StartBlockingCoroutine(PlaySingleProjectileImpactRevealSequence(entry, caster, reveal, canPlayImpactSfx));
            canPlayImpactSfx = false;
        }
    }

    IEnumerator PlaySingleProjectileImpactRevealSequence(
        SkillPresentationEntry entry,
        TosterHexUnit caster,
        FrontendResultReveal reveal,
        bool playImpactSfx)
    {
        Vector3 start = ResolveUnitPosition(caster);
        Vector3 end = ResolveImpactPosition(
            entry,
            caster,
            reveal.TargetUnit,
            reveal.TargetUnit != null ? reveal.TargetUnit.Hex : null);

        yield return PlayProjectileTravel(entry, start, end);

        if (entry.projectileImpactDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(entry.projectileImpactDelaySeconds);
        }

        SpawnVfx(entry.impactVfx, end, entry.effectLifetimeSeconds);
        if (playImpactSfx)
        {
            PlaySfx(entry.impactSfx);
        }

        yield return PlayRevealAndWait(entry, reveal);
    }

    IEnumerator PlayLocationEffectSequence(
        SkillPresentationEntry entry,
        TosterHexUnit caster,
        TosterHexUnit targetUnit,
        HexClass targetHex,
        string casterAnimationState,
        bool playCastSfx = true)
    {
        yield return PlayCasterAnimationAndCast(entry, caster, casterAnimationState, playCastSfx);

        Vector3 position = ResolveImpactPosition(entry, caster, targetUnit, targetHex);
        yield return PlayImpactAfterDelay(entry, position, entry.impactDelaySeconds, true);
    }

    IEnumerator PlayLocationImpactRevealSequence(
        SkillPresentationEntry entry,
        TosterHexUnit caster,
        TosterHexUnit targetUnit,
        HexClass targetHex,
        List<FrontendResultReveal> reveals,
        string casterAnimationState)
    {
        yield return PlayCasterAnimationAndCast(entry, caster, casterAnimationState);

        Vector3 position = ResolveImpactPosition(entry, caster, targetUnit, targetHex);
        yield return PlayImpactAfterDelay(entry, position, entry.impactDelaySeconds, true);
        yield return PlayRevealsAndWait(entry, reveals);
    }

    IEnumerator PlayHexCastUnitImpactThenRevealSequence(
        SkillPresentationEntry entry,
        TosterHexUnit caster,
        HexClass castHex,
        TosterHexUnit impactUnit,
        List<FrontendResultReveal> reveals,
        string casterAnimationState,
        Action afterImpact)
    {
        PlayWeaponTrail(entry, caster, GetAnimatedWeaponTrailDurationSeconds(entry, casterAnimationState));
        PlayCastSfx(entry, true);
        yield return PlayCasterAnimation(entry, caster, casterAnimationState);

        SpawnVfx(entry.castVfx, ResolveHexPosition(castHex, ResolveUnitPosition(caster)), entry.effectLifetimeSeconds);

        if (entry.impactDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(entry.impactDelaySeconds);
        }

        SpawnVfx(entry.impactVfx, ResolveUnitPosition(impactUnit), entry.effectLifetimeSeconds);
        PlaySfx(entry.impactSfx);
        afterImpact?.Invoke();
        yield return PlayRevealsAndWait(entry, reveals);
    }

    IEnumerator PlayCasterAnimationAndCast(SkillPresentationEntry entry, TosterHexUnit caster, string casterAnimationState)
    {
        yield return PlayCasterAnimationAndCast(entry, caster, casterAnimationState, true);
    }

    IEnumerator PlayCasterAnimationAndCast(SkillPresentationEntry entry, TosterHexUnit caster, string casterAnimationState, bool playCastSfx)
    {
        PlayWeaponTrail(entry, caster, GetAnimatedWeaponTrailDurationSeconds(entry, casterAnimationState));
        PlayCastSfx(entry, playCastSfx);
        yield return PlayCasterAnimation(entry, caster, casterAnimationState);
        PlayCastVfx(entry, caster);
    }

    IEnumerator PlayCasterAnimation(SkillPresentationEntry entry, TosterHexUnit caster, string casterAnimationState)
    {
        if (!string.IsNullOrEmpty(casterAnimationState) && caster != null && caster.tosterView != null)
        {
            float presentationDelay = Mathf.Clamp01(entry.castVfxDelay);
            if (entry.AnimationPlayPath == SkillPresentationAnimationPlayPath.Trigger)
            {
                if (presentationDelay >= 1f)
                {
                    yield return caster.tosterView.PlayAnimatorTriggerAndWait(casterAnimationState, SkillAnimationMaxWaitSeconds);
                }
                else
                {
                    yield return caster.tosterView.PlayAnimatorTriggerAndWaitForProgress(
                        casterAnimationState,
                        presentationDelay,
                        SkillAnimationMaxWaitSeconds);
                }
            }
            else
            {
                if (presentationDelay >= 1f)
                {
                    yield return caster.tosterView.PlayAnimatorStateAndWaitForDefault(casterAnimationState, SkillAnimationMaxWaitSeconds);
                }
                else
                {
                    yield return caster.tosterView.PlayAnimatorStateAndWaitForPresentationProgress(
                        casterAnimationState,
                        presentationDelay,
                        SkillAnimationMaxWaitSeconds);
                }
            }
        }
    }

    IEnumerator PlayImpactRevealsAfterDelay(
        SkillPresentationEntry entry,
        TosterHexUnit caster,
        List<FrontendResultReveal> reveals,
        float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (reveals == null)
        {
            yield break;
        }

        bool playedImpactSfx = false;
        for (int i = 0; i < reveals.Count; i++)
        {
            FrontendResultReveal reveal = reveals[i];
            if (reveal == null || reveal.TargetUnit == null)
            {
                continue;
            }

            Vector3 position = ResolveImpactPosition(
                entry,
                caster,
                reveal.TargetUnit,
                reveal.TargetUnit.Hex);

            SpawnVfx(entry.impactVfx, position, entry.effectLifetimeSeconds);
            if (!playedImpactSfx)
            {
                PlaySfx(entry.impactSfx);
                playedImpactSfx = true;
            }
        }

        yield return PlayRevealsAndWait(entry, reveals);
    }

    IEnumerator PlayImpactAfterDelay(SkillPresentationEntry entry, Vector3 position, float delay, bool playSfx)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        SpawnVfx(entry.impactVfx, position, entry.effectLifetimeSeconds);
        if (playSfx)
        {
            PlaySfx(entry.impactSfx);
        }
    }

    void SpawnVfx(GameObject prefab, Vector3 position, float lifetime)
    {
        if (prefab == null)
        {
            return;
        }

        GameObject instance = Instantiate(prefab, position, Quaternion.identity);
        ApplySpawnScalePreset(instance, prefab);
        if (lifetime > 0f)
        {
            Destroy(instance, lifetime);
        }
    }

    GameObject SpawnPersistentModel(SkillPresentationEntry entry, HexClass targetHex)
    {
        if (entry == null || entry.spawnModel == null)
        {
            return null;
        }

        Vector3 position = ResolveHexPosition(targetHex, Vector3.zero);
        GameObject instance = Instantiate(entry.spawnModel, position, Quaternion.identity);
        ApplySpawnScalePreset(instance, entry.spawnModel);

        if (targetHex != null && targetHex.MyHex != null)
        {
            instance.transform.SetParent(targetHex.MyHex.transform, true);
        }

        return instance;
    }

    void ApplySpawnScalePreset(GameObject instance, GameObject sourcePrefab)
    {
        if (instance == null || sourcePrefab == null)
        {
            return;
        }

        float scaleMultiplier = GetSpawnScaleMultiplier(sourcePrefab.name);
        if (Mathf.Approximately(scaleMultiplier, 1f))
        {
            return;
        }

        instance.transform.localScale *= scaleMultiplier;
    }

    float GetSpawnScaleMultiplier(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName))
        {
            return 1f;
        }
        
    if (prefabName.IndexOf("Aura", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return MagicArsenalAuraScaleMultiplier;
        }

          if (prefabName.IndexOf("Muzzle", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return MagicArsenalMuzzleScaleMultiplier;
        }

        if (prefabName.IndexOf("Dome", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return MagicArsenalDomeScaleMultiplier;
        }

           if (prefabName.IndexOf("Rain", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return MagicArsenalRainScaleMultiplier;
        }


        if (prefabName.IndexOf("Nova", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return MagicArsenalNovaScaleMultiplier;
        }

        if (prefabName.IndexOf("Spray", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return MagicArsenalSprayScaleMultiplier;
        }

        return 1f;
    }

    void PlaySfx(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            if (!missingAudioSourceWarningShown)
            {
                Debug.LogWarning("SkillPresentationManager does not have an AudioSource. Skill SFX will not play.");
                missingAudioSourceWarningShown = true;
            }
            return;
        }

        audioSource.PlayOneShot(clip);
    }

    AudioSource StartProjectileSfx(AudioClip clip)
    {
        if (clip == null)
        {
            return null;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            if (!missingAudioSourceWarningShown)
            {
                Debug.LogWarning("SkillPresentationManager does not have an AudioSource. Skill SFX will not play.");
                missingAudioSourceWarningShown = true;
            }
            return null;
        }

        AudioSource projectileAudio = gameObject.AddComponent<AudioSource>();
        projectileAudio.clip = clip;
        projectileAudio.loop = true;
        projectileAudio.playOnAwake = false;
        projectileAudio.volume = audioSource.volume;
        projectileAudio.pitch = audioSource.pitch;
        projectileAudio.spatialBlend = audioSource.spatialBlend;
        projectileAudio.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
        projectileAudio.Play();
        return projectileAudio;
    }

    void StopProjectileSfx(AudioSource projectileAudio)
    {
        if (projectileAudio == null)
        {
            return;
        }

        projectileAudio.Stop();
        Destroy(projectileAudio);
    }

    void WarnMissingCatalog()
    {
        if (!missingCatalogWarningShown)
        {
            Debug.LogWarning("SkillPresentationManager does not have a SkillPresentationCatalog. Skill VFX/SFX will not play.");
            missingCatalogWarningShown = true;
        }
    }

    void WarnMissingWeaponTrails()
    {
        if (missingWeaponTrailsWarningShown)
        {
            return;
        }

        Debug.LogWarning("SkillPresentationManager could not find any TrailRenderer weapon trails under the caster TosterView. Skill presentation will continue without weapon trails.");
        missingWeaponTrailsWarningShown = true;
    }

    static Vector3 ResolveImpactPosition(SkillPresentationEntry entry, TosterHexUnit caster, TosterHexUnit targetUnit, HexClass targetHex)
    {
        switch (entry.impactAnchor)
        {
            case SkillPresentationImpactAnchor.TargetUnit:
                if (targetUnit != null)
                {
                    return ResolveUnitPosition(targetUnit);
                }
                return ResolveHexPosition(targetHex, ResolveUnitPosition(caster));

            case SkillPresentationImpactAnchor.Caster:
                return ResolveUnitPosition(caster);

            case SkillPresentationImpactAnchor.AreaCenter:
            case SkillPresentationImpactAnchor.TargetHex:
            default:
                if (targetHex != null)
                {
                    return ResolveHexPosition(targetHex, ResolveUnitPosition(caster));
                }

                if (targetUnit != null)
                {
                    return ResolveUnitPosition(targetUnit);
                }

                return ResolveUnitPosition(caster);
        }
    }

    static Vector3 ResolveUnitPosition(TosterHexUnit unit)
    {
        if (unit != null)
        {
            if (unit.tosterView != null)
            {
                return unit.tosterView.transform.position;
            }

            if (unit.Hex != null)
            {
                return ResolveHexPosition(unit.Hex, Vector3.zero);
            }
        }

        return Vector3.zero;
    }

    static Vector3 ResolveHexPosition(HexClass hex, Vector3 fallback)
    {
        if (hex != null && hex.MyHex != null)
        {
            return hex.MyHex.transform.position;
        }

        return fallback;
    }
}
