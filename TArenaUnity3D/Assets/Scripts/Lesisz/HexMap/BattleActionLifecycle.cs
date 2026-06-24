using System;
using System.Collections;
using UnityEngine;

public enum BattleActionLifecycleKind
{
    Movement,
    MoveAndAttack,
    BasicRangedAttack,
    Skill,
    Wait,
    Defense,
    Automatic
}

public class BattleActionLifecycle : MonoBehaviour
{
    const float ActionBodyTimeoutSeconds = 15f;
    const float PresentationTimeoutSeconds = 15f;

    public static BattleActionLifecycle Instance { get; private set; }

    bool isBusy;
    TosterHexUnit activeActor;
    BattleActionLifecycleKind activeKind;
    string activeLabel;

    public bool IsBusy
    {
        get { return isBusy; }
    }

    public TosterHexUnit ActiveActor
    {
        get { return activeActor; }
    }

    public string ActiveKindName
    {
        get { return isBusy ? activeKind.ToString() : string.Empty; }
    }

    public static bool IsActionBlocking
    {
        get
        {
            return (Instance != null && Instance.isBusy) || SkillPresentationManager.HasBlockingPresentation;
        }
    }

    public static BattleActionLifecycle EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        BattleActionLifecycle existing = FindObjectOfType<BattleActionLifecycle>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject lifecycleObject = new GameObject("BattleActionLifecycle");
        return lifecycleObject.AddComponent<BattleActionLifecycle>();
    }

    public static void CancelActiveAction()
    {
        if (Instance != null)
        {
            Instance.CancelActiveActionInstance();
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void CancelActiveActionInstance()
    {
        StopAllCoroutines();
        activeActor = null;
        activeLabel = null;
        isBusy = false;
    }

    public bool TryRunAction(
        TosterHexUnit actor,
        BattleActionLifecycleKind kind,
        string label,
        Action commit,
        Func<IEnumerator> actionBody,
        Action complete,
        float actionBodyTimeoutSeconds = ActionBodyTimeoutSeconds,
        float presentationTimeoutSeconds = PresentationTimeoutSeconds)
    {
        if (isBusy)
        {
            Debug.LogWarning("BattleActionLifecycle rejected overlapping action: " + label);
            return false;
        }

        StartCoroutine(RunAction(actor, kind, label, commit, actionBody, complete, actionBodyTimeoutSeconds, presentationTimeoutSeconds));
        return true;
    }

    IEnumerator RunAction(
        TosterHexUnit actor,
        BattleActionLifecycleKind kind,
        string label,
        Action commit,
        Func<IEnumerator> actionBody,
        Action complete,
        float actionBodyTimeoutSeconds,
        float presentationTimeoutSeconds)
    {
        isBusy = true;
        activeActor = actor;
        activeKind = kind;
        activeLabel = label;

        bool committed = TryInvoke(commit, "commit");
        if (committed && actionBody != null)
        {
            IEnumerator body = null;
            try
            {
                body = actionBody();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            if (body != null)
            {
                yield return RunEnumeratorWithTimeout(body, actionBodyTimeoutSeconds);
            }
        }

        yield return SkillPresentationManager.WaitForBlockingPresentation(presentationTimeoutSeconds);

        TryInvoke(complete, "complete");

        activeActor = null;
        activeLabel = null;
        isBusy = false;
    }

    bool TryInvoke(Action action, string phase)
    {
        if (action == null)
        {
            return true;
        }

        try
        {
            action();
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("BattleActionLifecycle failed during " + phase + " for " + activeKind + " " + activeLabel);
            Debug.LogException(ex);
            return false;
        }
    }

    IEnumerator RunEnumeratorWithTimeout(IEnumerator routine, float timeoutSeconds)
    {
        float startedAt = Time.time;

        while (routine != null)
        {
            if (timeoutSeconds > 0f && Time.time - startedAt > timeoutSeconds)
            {
                Debug.LogWarning("BattleActionLifecycle timed out during " + activeKind + " " + activeLabel);
                yield break;
            }

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
                yield break;
            }

            if (!hasNext)
            {
                yield break;
            }

            yield return current;
        }
    }
}
