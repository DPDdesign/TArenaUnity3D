using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TosterView : MonoBehaviour
{
    const float MinimumAnimationSpeedMultiplier = 0.01f;
    Vector3 oldPos;
    Vector3 newPos;
    Vector3 beforeJumpPos;
    Vector3 currentV;
    float smoothTime = 0.1f;
    float speed = 5f;
    public bool isSelected = false;
    HexMap_Highlight map;
    public bool AnimationIsPlaying = false;
    Coroutine returnToDefaultCoroutine;
    TosterSfxSet sfxSet;
    string defaultAnimatorStateOverride;
    readonly Dictionary<TrailRenderer, Coroutine> weaponTrailCoroutines = new Dictionary<TrailRenderer, Coroutine>();
    readonly Dictionary<TrailRenderer, bool> weaponTrailInitialActiveStates = new Dictionary<TrailRenderer, bool>();
    readonly Dictionary<TrailRenderer, bool> weaponTrailInitialEnabledStates = new Dictionary<TrailRenderer, bool>();

    private void Awake()
    {
        sfxSet = GetComponentInChildren<TosterSfxSet>();
    }

    private void Start()
    {
        oldPos = newPos = this.transform.position;
        beforeJumpPos.y = newPos.y;
    }

    public void OnTosterMoved(HexClass oldHex, HexClass newHex)
    {
        // animate move
        
        oldPos = oldHex.Position();
        newPos = newHex.Position();


        newPos.y = this.transform.position.y;
        currentV = Vector3.zero;

        AnimationIsPlaying = true;
           // GameObject.FindObjectOfType<HexMap>().AnimationIsPlaying = true;

    }

    private void OnMouseOver()
    {
        
        
       newPos.y= 2;
      

    }
    public void Destroy()
    {
        Destroy(this);
    }
    private void OnMouseUp()
    {
         
    }
    private void OnMouseExit()
    {
        newPos.y = beforeJumpPos.y;
    }

    public void TeleportTo(HexClass hex)
    {
        this.transform.position = hex.Position();
    }

    public void PlayAnimatorStateAndReturnToDefault(string stateName)
    {
        Animator animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            return;
        }

        if (returnToDefaultCoroutine != null)
        {
            StopCoroutine(returnToDefaultCoroutine);
        }

        Debug.Log(animator);
        PlaySfxForAnimatorState(stateName);
        ApplyAnimationSpeed(animator);
        animator.Play(stateName);
        returnToDefaultCoroutine = StartCoroutine(ReturnAnimatorToDefaultAfterState(animator, stateName));
    }

    public bool PlayAnimatorStateImmediate(string stateName, bool playSfx = true)
    {
        Animator animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            return false;
        }

        if (HasAnimatorState(animator, stateName) == false)
        {
            return false;
        }

        if (playSfx)
        {
            PlaySfxForAnimatorState(stateName);
        }
        ApplyAnimationSpeed(animator);
        animator.Play(stateName);
        animator.Update(0f);
        return true;
    }

    public void SetDefaultAnimatorStateOverride(string stateName)
    {
        defaultAnimatorStateOverride = stateName;
    }

    public void ClearDefaultAnimatorStateOverride()
    {
        defaultAnimatorStateOverride = null;
    }

    public bool HasAnimatorState(string stateName)
    {
        return HasAnimatorState(GetComponentInChildren<Animator>(), stateName);
    }

    public void EnsureDefaultAnimatorStateOverrideApplied()
    {
        if (string.IsNullOrEmpty(defaultAnimatorStateOverride))
        {
            return;
        }

        Animator animator = GetComponentInChildren<Animator>();
        if (animator == null || HasAnimatorState(animator, defaultAnimatorStateOverride) == false)
        {
            return;
        }

        if (animator.IsInTransition(0))
        {
            return;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName(defaultAnimatorStateOverride))
        {
            return;
        }

        PlayAnimatorStateImmediate(defaultAnimatorStateOverride, false);
    }

    public IEnumerator PlayAnimatorStateAndWaitForDefault(string stateName, float maxWaitSeconds)
    {
        yield return PlayAnimatorStateAndWait(stateName, maxWaitSeconds, true);
    }

    public IEnumerator PlayAnimatorStateAndWait(string stateName, float maxWaitSeconds, bool resetToDefault)
    {
        Animator animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.Log("[DEBUG-HITFLOW] PlayAnimatorStateAndWait missing animator state=" + stateName + " view=" + name);
            yield break;
        }

        if (returnToDefaultCoroutine != null)
        {
            StopCoroutine(returnToDefaultCoroutine);
            returnToDefaultCoroutine = null;
        }

        Debug.Log(animator);
        PlaySfxForAnimatorState(stateName);
        ApplyAnimationSpeed(animator);
        if (stateName == "hit" || stateName == "death")
        {
            Debug.Log("[DEBUG-HITFLOW] PlayAnimatorStateAndWait begin state=" + stateName +
                " view=" + name +
                " animator=" + animator.name +
                " speed=" + animator.speed);
        }
        animator.Play(stateName);
        yield return WaitForAnimatorState(animator, stateName, maxWaitSeconds);
        if (resetToDefault)
        {
            ResetAnimatorToDefault(animator);
        }
    }

    public IEnumerator PlayAnimatorTriggerAndWait(string triggerName, float maxWaitSeconds)
    {
        Animator animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            yield break;
        }

        if (returnToDefaultCoroutine != null)
        {
            StopCoroutine(returnToDefaultCoroutine);
            returnToDefaultCoroutine = null;
        }

        Debug.Log(animator);
        PlaySfxForAnimatorState(triggerName);
        int initialStateHash = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        ApplyAnimationSpeed(animator);
        animator.ResetTrigger(triggerName);
        animator.SetTrigger(triggerName);
        yield return WaitForTriggeredAnimation(animator, initialStateHash, maxWaitSeconds);
    }

    public IEnumerator PlayAnimatorTriggerAndWaitForProgress(string triggerName, float normalizedProgress, float maxWaitSeconds)
    {
        Animator animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            yield break;
        }

        if (returnToDefaultCoroutine != null)
        {
            StopCoroutine(returnToDefaultCoroutine);
            returnToDefaultCoroutine = null;
        }

        Debug.Log(animator);
        PlaySfxForAnimatorState(triggerName);
        int initialStateHash = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        ApplyAnimationSpeed(animator);
        animator.ResetTrigger(triggerName);
        animator.SetTrigger(triggerName);

        if (normalizedProgress <= 0f)
        {
            yield break;
        }

        yield return WaitForTriggeredAnimationProgress(
            animator,
            initialStateHash,
            Mathf.Clamp01(normalizedProgress),
            maxWaitSeconds);
    }

    public IEnumerator PlayAnimatorStateAndHoldLastFrame(string stateName, float maxWaitSeconds)
    {
        Animator animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            yield break;
        }

        if (returnToDefaultCoroutine != null)
        {
            StopCoroutine(returnToDefaultCoroutine);
            returnToDefaultCoroutine = null;
        }

        Debug.Log(animator);
        Vector3 anchoredLocalPosition = animator.transform.localPosition;
        Quaternion anchoredLocalRotation = animator.transform.localRotation;
        PlaySfxForAnimatorState(stateName);
        ApplyAnimationSpeed(animator);
        animator.Play(stateName);
        yield return WaitForAnimatorState(animator, stateName, maxWaitSeconds);

        if (animator != null)
        {
            animator.Play(stateName, 0, 1f);
            animator.Update(0f);
            LockAnimatorRootTransform(animator, anchoredLocalPosition, anchoredLocalRotation);
            animator.enabled = false;
        }
    }

    public IEnumerator PlayAnimatorStateAndWaitForProgress(string stateName, float normalizedProgress, float maxWaitSeconds)
    {
        Animator animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            yield break;
        }

        if (returnToDefaultCoroutine != null)
        {
            StopCoroutine(returnToDefaultCoroutine);
            returnToDefaultCoroutine = null;
        }

        Debug.Log(animator);
        PlaySfxForAnimatorState(stateName);
        ApplyAnimationSpeed(animator);
        animator.Play(stateName);
        yield return WaitForAnimatorStateProgress(animator, stateName, normalizedProgress, maxWaitSeconds);
    }

    public IEnumerator PlayAnimatorStateAndWaitForPresentationProgress(string stateName, float normalizedProgress, float maxWaitSeconds)
    {
        Animator animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            yield break;
        }

        if (returnToDefaultCoroutine != null)
        {
            StopCoroutine(returnToDefaultCoroutine);
            returnToDefaultCoroutine = null;
        }

        Debug.Log(animator);
        PlaySfxForAnimatorState(stateName);
        ApplyAnimationSpeed(animator);
        animator.Play(stateName);

        if (normalizedProgress > 0f)
        {
            yield return WaitForAnimatorStateProgress(
                animator,
                stateName,
                Mathf.Clamp01(normalizedProgress),
                maxWaitSeconds);
        }

        if (animator != null)
        {
            returnToDefaultCoroutine = StartCoroutine(ReturnAnimatorToDefaultAfterState(animator, stateName));
        }
    }

    public void PlayAttackSfx()
    {
        TosterSfxSet sfx = GetSfxSet();
        if (sfx != null)
        {
            sfx.PlayAttack();
        }
    }

    public void PlayHitSfx()
    {
        TosterSfxSet sfx = GetSfxSet();
        if (sfx != null)
        {
            sfx.PlayHit();
        }
    }

    public void PlayDeathSfx()
    {
        TosterSfxSet sfx = GetSfxSet();
        if (sfx != null)
        {
            sfx.PlayDeath();
        }
    }

    public bool TryPlayWeaponTrails(float durationSeconds)
    {
        TrailRenderer[] trails = GetComponentsInChildren<TrailRenderer>(true);
        bool playedAnyTrail = false;
        for (int i = 0; i < trails.Length; i++)
        {
            TrailRenderer trail = trails[i];
            if (trail == null)
            {
                continue;
            }

            PlayWeaponTrail(trail, durationSeconds);
            playedAnyTrail = true;
        }

        return playedAnyTrail;
    }

    void PlayWeaponTrail(TrailRenderer trail, float durationSeconds)
    {
        if (weaponTrailCoroutines.TryGetValue(trail, out Coroutine previousCoroutine) && previousCoroutine != null)
        {
            StopCoroutine(previousCoroutine);
        }

        if (!weaponTrailInitialActiveStates.ContainsKey(trail))
        {
            weaponTrailInitialActiveStates.Add(trail, trail.gameObject.activeSelf);
        }

        if (!weaponTrailInitialEnabledStates.ContainsKey(trail))
        {
            weaponTrailInitialEnabledStates.Add(trail, trail.enabled);
        }

        weaponTrailCoroutines[trail] = StartCoroutine(PlayWeaponTrailForDuration(trail, Mathf.Max(0f, durationSeconds)));
    }

    IEnumerator PlayWeaponTrailForDuration(TrailRenderer trail, float durationSeconds)
    {
        if (trail == null)
        {
            yield break;
        }

        trail.gameObject.SetActive(true);
        trail.enabled = true;
        trail.Clear();
        trail.emitting = true;

        if (durationSeconds > 0f)
        {
            yield return new WaitForSeconds(ScaleDurationByAnimationSpeed(durationSeconds));
        }
        else
        {
            yield return null;
        }

        if (trail != null)
        {
            trail.emitting = false;
            trail.Clear();

            if (weaponTrailInitialEnabledStates.TryGetValue(trail, out bool initialEnabled))
            {
                trail.enabled = initialEnabled;
            }

            if (weaponTrailInitialActiveStates.TryGetValue(trail, out bool initialActiveSelf) && !initialActiveSelf)
            {
                trail.gameObject.SetActive(false);
            }

            weaponTrailInitialEnabledStates.Remove(trail);
            weaponTrailInitialActiveStates.Remove(trail);
            weaponTrailCoroutines.Remove(trail);
        }
    }

    void PlaySfxForAnimatorState(string stateName)
    {
        switch (stateName)
        {
            case "attack":
                PlayAttackSfx();
                break;
            case "hit":
                PlayHitSfx();
                break;
            case "death":
                PlayDeathSfx();
                break;
        }
    }

    TosterSfxSet GetSfxSet()
    {
        if (sfxSet == null)
        {
            sfxSet = GetComponentInChildren<TosterSfxSet>();
        }

        return sfxSet;
    }

    public void ResetAnimatorToDefault()
    {
        if (returnToDefaultCoroutine != null)
        {
            StopCoroutine(returnToDefaultCoroutine);
            returnToDefaultCoroutine = null;
        }

        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            ResetAnimatorToDefault(animator);
        }
    }

    IEnumerator ReturnAnimatorToDefaultAfterState(Animator animator, string stateName)
    {
        yield return WaitForAnimatorState(animator, stateName, ScaleMaxWaitByAnimationSpeed(1.25f));

        if (animator != null)
        {
            ResetAnimatorToDefault(animator);
        }

        returnToDefaultCoroutine = null;
    }

    IEnumerator WaitForAnimatorState(Animator animator, string stateName, float maxWaitSeconds)
    {
        yield return null;

        float elapsed = 0f;
        int stateShortNameHash = Animator.StringToHash(stateName);
        Vector3 anchoredLocalPosition = animator.transform.localPosition;
        Quaternion anchoredLocalRotation = animator.transform.localRotation;
        while (animator != null && elapsed < maxWaitSeconds)
        {
            LockAnimatorRootTransform(animator, anchoredLocalPosition, anchoredLocalRotation);
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.shortNameHash != stateShortNameHash)
            {
                break;
            }
            if (!animator.IsInTransition(0) && stateInfo.normalizedTime >= 1f)
            {
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (animator != null)
        {
            LockAnimatorRootTransform(animator, anchoredLocalPosition, anchoredLocalRotation);
        }
    }

    IEnumerator WaitForAnimatorStateProgress(Animator animator, string stateName, float normalizedProgress, float maxWaitSeconds)
    {
        yield return null;

        float elapsed = 0f;
        int stateShortNameHash = Animator.StringToHash(stateName);
        Vector3 anchoredLocalPosition = animator.transform.localPosition;
        Quaternion anchoredLocalRotation = animator.transform.localRotation;
        while (animator != null && elapsed < maxWaitSeconds)
        {
            LockAnimatorRootTransform(animator, anchoredLocalPosition, anchoredLocalRotation);
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.shortNameHash != stateShortNameHash)
            {
                break;
            }
            if (!animator.IsInTransition(0) && stateInfo.normalizedTime >= normalizedProgress)
            {
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (animator != null)
        {
            LockAnimatorRootTransform(animator, anchoredLocalPosition, anchoredLocalRotation);
        }
    }

    IEnumerator WaitForTriggeredAnimation(Animator animator, int initialStateHash, float maxWaitSeconds)
    {
        yield return null;

        float elapsed = 0f;
        bool leftInitialState = false;
        Vector3 anchoredLocalPosition = animator.transform.localPosition;
        Quaternion anchoredLocalRotation = animator.transform.localRotation;
        while (animator != null && elapsed < maxWaitSeconds)
        {
            LockAnimatorRootTransform(animator, anchoredLocalPosition, anchoredLocalRotation);
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            bool isInTransition = animator.IsInTransition(0);

            if (isInTransition || stateInfo.fullPathHash != initialStateHash)
            {
                leftInitialState = true;
            }

            if (leftInitialState && !isInTransition && stateInfo.fullPathHash == initialStateHash)
            {
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (animator != null)
        {
            LockAnimatorRootTransform(animator, anchoredLocalPosition, anchoredLocalRotation);
        }
    }

    IEnumerator WaitForTriggeredAnimationProgress(Animator animator, int initialStateHash, float normalizedProgress, float maxWaitSeconds)
    {
        yield return null;

        float elapsed = 0f;
        bool leftInitialState = false;
        Vector3 anchoredLocalPosition = animator.transform.localPosition;
        Quaternion anchoredLocalRotation = animator.transform.localRotation;
        while (animator != null && elapsed < maxWaitSeconds)
        {
            LockAnimatorRootTransform(animator, anchoredLocalPosition, anchoredLocalRotation);
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            bool isInTransition = animator.IsInTransition(0);

            if (isInTransition || stateInfo.fullPathHash != initialStateHash)
            {
                leftInitialState = true;
            }

            if (leftInitialState && !isInTransition && stateInfo.fullPathHash != initialStateHash && stateInfo.normalizedTime >= normalizedProgress)
            {
                break;
            }

            if (leftInitialState && !isInTransition && stateInfo.fullPathHash == initialStateHash)
            {
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (animator != null)
        {
            LockAnimatorRootTransform(animator, anchoredLocalPosition, anchoredLocalRotation);
        }
    }

    void LockAnimatorRootTransform(Animator animator, Vector3 localPosition, Quaternion localRotation)
    {
        animator.transform.localPosition = localPosition;
        animator.transform.localRotation = localRotation;
    }

    void ApplyAnimationSpeed(Animator animator)
    {
        if (animator == null)
        {
            return;
        }

        animator.speed = GetAnimationSpeedMultiplier();
    }

    float GetAnimationSpeedMultiplier()
    {
        return Mathf.Max(
            MinimumAnimationSpeedMultiplier,
            OfflinePlayerPreferences.GetAnimationSpeedMultiplier());
    }

    float ScaleMaxWaitByAnimationSpeed(float seconds)
    {
        return seconds / GetAnimationSpeedMultiplier();
    }

    float ScaleDurationByAnimationSpeed(float seconds)
    {
        return seconds / GetAnimationSpeedMultiplier();
    }

    void ResetAnimatorToDefault(Animator animator)
    {
        animator.Rebind();
        animator.speed = 1f;
        animator.Update(0f);
        if (string.IsNullOrEmpty(defaultAnimatorStateOverride) == false)
        {
            animator.Play(defaultAnimatorStateOverride, 0, 0f);
            animator.Update(0f);
        }
    }

    static bool HasAnimatorState(Animator animator, string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
        {
            return false;
        }

        return animator.HasState(0, Animator.StringToHash(stateName)) ||
            animator.HasState(0, Animator.StringToHash("Base Layer." + stateName));
    }
    

    private void Update()
    {
        if (newPos != this.transform.position)
        {
            this.transform.position = Vector3.SmoothDamp(this.transform.position, newPos, ref currentV, smoothTime);
        }
        //this.transform.position = Vector3.Lerp(this.transform.position, newPos,smoothTime);
        if (Vector3.Distance(this.transform.position, newPos) < 0.1f)
        {
            AnimationIsPlaying = false;
           // GameObject.FindObjectOfType<HexMap>().AnimationIsPlaying = false;
        }
        if (Vector3.Distance(this.transform.position, newPos) > 2f)
        {
       //     this.transform.position = newPos;
        }
        // this.transform.position = Vector3.SmoothDamp(this.transform.position, newPos, ref currentV, smoothTime);

    }
}
