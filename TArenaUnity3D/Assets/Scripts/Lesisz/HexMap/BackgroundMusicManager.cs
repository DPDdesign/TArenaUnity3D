using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class BackgroundMusicManager : MonoBehaviour
{
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip musicClip;
    [SerializeField] bool playOnStart = true;
    [SerializeField] bool loop = true;
    [SerializeField, Range(0f, 1f)] float volume = 1f;

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        ConfigureAudioSource();
    }

    void Start()
    {
        if (playOnStart)
        {
            PlayMusic();
        }
    }

    public void PlayMusic()
    {
        if (audioSource == null)
        {
            return;
        }

        if (musicClip != null)
        {
            audioSource.clip = musicClip;
        }

        if (audioSource.clip == null)
        {
            return;
        }

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    public void StopMusic()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    public void PauseMusic()
    {
        if (audioSource != null)
        {
            audioSource.Pause();
        }
    }

    public void ResumeMusic()
    {
        if (audioSource != null && audioSource.clip != null && !audioSource.isPlaying)
        {
            audioSource.UnPause();
        }
    }

    void ConfigureAudioSource()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.playOnAwake = false;
        audioSource.loop = loop;
        audioSource.volume = volume;

        if (musicClip != null)
        {
            audioSource.clip = musicClip;
        }
    }
}
