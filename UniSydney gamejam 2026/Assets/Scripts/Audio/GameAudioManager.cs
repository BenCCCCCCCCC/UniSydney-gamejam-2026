using UnityEngine;

public class GameAudioManager : MonoBehaviour
{
    private static GameAudioManager instance;

    [Header("BGM")]
    [SerializeField] private AudioClip bgmClip;
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.35f;
    [SerializeField] private bool playBgmOnStart = true;

    [Header("Click SFX")]
    [SerializeField] private AudioClip clickClip;
    [SerializeField, Range(0f, 1f)] private float clickVolume = 0.8f;
    [SerializeField] private bool playClickOnLeftMouseDown = true;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource clickSource;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioSources();
        ConfigureAudioSources();
    }

    private void Start()
    {
        if (playBgmOnStart)
        {
            PlayBgm();
        }
    }

    private void Update()
    {
        if (playClickOnLeftMouseDown && IsLeftMouseButtonDownThisFrame())
        {
            PlayClick();
        }
    }

    public void PlayBgm()
    {
        if (bgmSource == null || bgmClip == null)
        {
            return;
        }

        bgmSource.clip = bgmClip;
        bgmSource.volume = bgmVolume;
        bgmSource.loop = true;

        if (!bgmSource.isPlaying)
        {
            bgmSource.Play();
        }
    }

    public void StopBgm()
    {
        if (bgmSource == null)
        {
            return;
        }

        bgmSource.Stop();
    }

    public void SetBgmVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);

        if (bgmSource != null)
        {
            bgmSource.volume = bgmVolume;
        }
    }

    public void SetClickVolume(float volume)
    {
        clickVolume = Mathf.Clamp01(volume);

        if (clickSource != null)
        {
            clickSource.volume = clickVolume;
        }
    }

    public void PlayClick()
    {
        if (clickSource == null || clickClip == null)
        {
            return;
        }

        clickSource.PlayOneShot(clickClip, clickVolume);
    }

    private void EnsureAudioSources()
    {
        AudioSource[] sources = GetComponents<AudioSource>();

        if (bgmSource == null)
        {
            bgmSource = sources.Length > 0
                ? sources[0]
                : gameObject.AddComponent<AudioSource>();
        }

        if (clickSource == null || clickSource == bgmSource)
        {
            foreach (AudioSource source in sources)
            {
                if (source != null && source != bgmSource)
                {
                    clickSource = source;
                    break;
                }
            }

            if (clickSource == null || clickSource == bgmSource)
            {
                clickSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    private void ConfigureAudioSources()
    {
        if (bgmSource != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.volume = bgmVolume;
        }

        if (clickSource != null)
        {
            clickSource.loop = false;
            clickSource.playOnAwake = false;
            clickSource.volume = clickVolume;
        }
    }

    private bool IsLeftMouseButtonDownThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null
            && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonDown(0))
        {
            return true;
        }
#endif

        return false;
    }
}
