using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class CombatSfxManager : MonoBehaviour
{
    public static CombatSfxManager Instance;

    [SerializeField] AudioSource audioSource;

    static bool missingManagerWarningShown;
    static bool missingAudioSourceWarningShown;

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

    public static void PlayRandomSfx(AudioClip[] clips, float volume = 1f)
    {
        if (!HasPlayableClip(clips))
        {
            return;
        }

        CombatSfxManager manager = Instance;
        if (manager == null)
        {
            manager = FindObjectOfType<CombatSfxManager>();
            if (manager != null)
            {
                Instance = manager;
            }
        }

        if (manager == null)
        {
            if (!missingManagerWarningShown)
            {
                Debug.LogWarning("CombatSfxManager is missing from the scene. Combat SFX will not play.");
                missingManagerWarningShown = true;
            }
            return;
        }

        manager.PlayRandom(clips, volume);
    }

    public void PlayRandom(AudioClip[] clips, float volume = 1f)
    {
        AudioClip clip = GetRandomClip(clips);
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
                Debug.LogWarning("CombatSfxManager does not have an AudioSource. Combat SFX will not play.");
                missingAudioSourceWarningShown = true;
            }
            return;
        }

        audioSource.PlayOneShot(clip, volume);
    }

    static bool HasPlayableClip(AudioClip[] clips)
    {
        if (clips == null)
        {
            return false;
        }

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    static AudioClip GetRandomClip(AudioClip[] clips)
    {
        int playableCount = 0;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null)
            {
                playableCount++;
            }
        }

        if (playableCount == 0)
        {
            return null;
        }

        int selectedIndex = Random.Range(0, playableCount);
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] == null)
            {
                continue;
            }

            if (selectedIndex == 0)
            {
                return clips[i];
            }

            selectedIndex--;
        }

        return null;
    }
}
