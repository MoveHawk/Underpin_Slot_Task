using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Background Music")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.4f;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip buttonClickSfx;
    [SerializeField] private AudioClip leverPullSfx;
    [SerializeField] private AudioClip payoutSfx;
    [SerializeField] private AudioClip jackpotSfx;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        musicSource.loop = true;
        musicSource.volume = musicVolume;

        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    public void PlayButtonClick()
    {
        PlaySfx(buttonClickSfx);
    }

    public void PlayLeverPull()
    {
        PlaySfx(leverPullSfx);
    }

    public void PlayPayout()
    {
        PlaySfx(payoutSfx);
    }

    public void PlayJackpot()
    {
        PlaySfx(jackpotSfx);
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip);
    }
}