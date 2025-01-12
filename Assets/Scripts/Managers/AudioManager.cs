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
    internal static AudioClip tileDeath;
    internal static AudioClip tilePush;
    internal static AudioClip areaOverlap;
    internal static AudioClip inverseOverlap;
    internal static AudioClip outboundOverlap;
    internal static AudioClip uiDeny;
    internal static AudioClip select;
    internal static AudioClip undo;
    internal static AudioClip cba;

    // AudioSources //
    [SerializeField] private AudioSource master;
    [SerializeField] private AudioSource sfx;
    [SerializeField] private AudioSource sfxPitch;

    void Awake()
    {
        // Singleton (AudioManager has persistence)
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
        areaOverlap = Resources.Load<AudioClip>("Audio/SFX/Area Overlap");
        inverseOverlap = Resources.Load<AudioClip>("Audio/SFX/Area Overlap"); // Change to new sound
        outboundOverlap = Resources.Load<AudioClip>("Audio/SFX/Area Overlap"); // Change to new sound
        uiDeny = Resources.Load<AudioClip>("Audio/SFX/UI Deny");
        select = Resources.Load<AudioClip>("Audio/SFX/Select");
        undo = Resources.Load<AudioClip>("Audio/SFX/Undo");
        cba = Resources.Load<AudioClip>("Audio/SFX/CBA");

        // Default looping BGM
        // PlayBGM(cba);
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
    public void PlaySFX(AudioClip clip, float volume = 1f, bool pitchShift = false)
    {
        if (GameManager.Instance.chessbattleadvanced) clip = cba;

        if (pitchShift)
        {
            sfxPitch.pitch = Random.Range(0.95f, 1.05f);
            sfxPitch.volume = volume * GameManager.save.preferences.SFXVolume;
            sfxPitch.PlayOneShot(clip);
            return;
        }

        sfx.volume = volume * GameManager.save.preferences.SFXVolume;
        sfx.PlayOneShot(clip);
    }

    // Volume setters
    public void SetMasterVolume(float volume) { master.volume = volume; }
}
