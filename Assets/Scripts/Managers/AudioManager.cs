using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [HideInInspector] public static AudioManager Instance;

    // BGM //
    [HideInInspector] public static AudioClip titleBGM;
    [HideInInspector] public static AudioClip editorBGM;
    [HideInInspector] public static AudioClip W1BGM;
    [HideInInspector] public static AudioClip W2BGM;
    [HideInInspector] public static AudioClip W3BGM;

    // SFX //
    [HideInInspector] public static AudioClip tileDeath;
    [HideInInspector] public static AudioClip tilePush;

    // AudioSources //
    [SerializeField] private AudioSource master;
    [SerializeField] private AudioSource sfx;

    void Awake()
    {
        // Singleton (GameManager has persistence)
        if (!Instance) { Instance = this; }
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);

        // Get audio references
        titleBGM = null;
        editorBGM = null;
        W1BGM = null;
        W2BGM = null;
        W3BGM = null;
        tileDeath = Resources.Load<AudioClip>("Audio/SFX/Tile Death");
        tilePush = Resources.Load<AudioClip>("Audio/SFX/Tile Push");

        // Default looping BGM
        // PlayBGM(tileDeath);
    }

    // Dunno yet
    private void FixedUpdate()
    {
        if (master != null) return;
    }

    // Pause music volume
    public void ToggleBGM(bool toggle)
    {
        if (toggle) master.Pause();
        else master.UnPause();
    }

    // Pause sfx volume
    public void ToggleSFX(bool toggle)
    {
        if (toggle) sfx.Pause();
        else sfx.UnPause();
    }

    // Plays BGM
    public void PlayBGM(AudioClip clip)
    {
        master.clip = clip;
        master.Play();
    }

    // Plays an SFX
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        sfx.volume = volume * GameManager.save.preferences.SFXVolume;
        sfx.PlayOneShot(clip);
    }
}
