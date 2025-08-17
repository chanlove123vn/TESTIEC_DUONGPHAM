using UnityEngine;

public class SoundManager : MonoBehaviour
{
    private static SoundManager s_instance;
    public static bool HasInstance => s_instance != null;
    public static SoundManager Instance
    {
        get
        {
            if (s_instance == null)
            {
                var go = new GameObject("SoundManager");
                s_instance = go.AddComponent<SoundManager>();
            }
            return s_instance;
        }
    }

    [Header("Clips (optional, will fallback to Resources)")]
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private AudioClip sfxCollectClip;
    [SerializeField] private AudioClip sfxWinClip;
    [SerializeField] private AudioClip sfxLoseClip;

    [Header("Volumes")]
    [Range(0,1f)] [SerializeField] private float musicVolume = 1f;
    [Range(0,1f)] [SerializeField] private float sfxVolume   = 1f;

    private AudioSource m_music;
    private AudioSource m_sfx;

    private void Awake()
    {
        if (s_instance != null && s_instance != this) { Destroy(gameObject); return; }
        s_instance = this;
        DontDestroyOnLoad(gameObject);

        m_music = gameObject.AddComponent<AudioSource>();
        m_sfx   = gameObject.AddComponent<AudioSource>();
        m_music.loop = true;

        if (!bgmClip)        bgmClip        = Resources.Load<AudioClip>(Constants.BG_SOUND);
        if (!sfxCollectClip) sfxCollectClip = Resources.Load<AudioClip>(Constants.ITEM_COLLECT_SOUND);
        if (!sfxWinClip)     sfxWinClip     = Resources.Load<AudioClip>(Constants.WIN_SOUND);
        if (!sfxLoseClip)    sfxLoseClip    = Resources.Load<AudioClip>(Constants.LOSE_SOUND);
    }

    public void PlayBGM(float volume = -1f)
    {
        if (!bgmClip) return;
        m_music.clip = bgmClip;
        m_music.volume = volume >= 0f ? Mathf.Clamp01(volume) : musicVolume;
        if (!m_music.isPlaying) m_music.Play();
    }

    public void StopBGM()           { if (m_music) m_music.Stop(); }
    public void PauseBGM(bool flag) { if (!m_music) return; if (flag) m_music.Pause(); else m_music.UnPause(); }

    public void PlayCollect(float volume = -1f) { PlayOneShot(sfxCollectClip, volume); }
    public void PlayWin(float volume = -1f)     { StopBGM(); PlayOneShot(sfxWinClip,  volume); }
    public void PlayLose(float volume = -1f)    { StopBGM(); PlayOneShot(sfxLoseClip, volume); }

    public void SetMusicVolume(float v) { musicVolume = Mathf.Clamp01(v); if (m_music) m_music.volume = musicVolume; }
    public void SetSfxVolume(float v)   { sfxVolume   = Mathf.Clamp01(v); }

    private void PlayOneShot(AudioClip clip, float volumeOverride)
    {
        if (!clip) return;
        float v = (volumeOverride >= 0f) ? Mathf.Clamp01(volumeOverride) : sfxVolume;
        m_sfx.PlayOneShot(clip, v);
    }
}
