using System;
using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [HideInInspector] public static AudioManager I;

    // BGM //
    internal static AudioClip titleBGM;
    internal static AudioClip editorBGM;
    internal static AudioClip remixBGM;
    internal static AudioClip voidBGM;
    internal static AudioClip W1BGM;
    internal static AudioClip W2BGM;
    internal static AudioClip W3BGM;
    private Coroutine switchCoro = null;

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
    internal static AudioClip ego1;
    internal static AudioClip ego2;

    // AudioSources //
    [Header("Audio Sources")]
    [SerializeField] private AudioSource master;
    [SerializeField] private AudioSource sfx;
    [SerializeField] private AudioSource sfxPitch;

    void Awake()
    {
        // Singleton (AudioManager has persistence)
        if (!I) { I = this; }
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);

        // Get audio references
        titleBGM = Resources.Load<AudioClip>("Audio/BGM/Test1");
        editorBGM = Resources.Load<AudioClip>("Audio/BGM/Test2");
        W1BGM = Resources.Load<AudioClip>("Audio/BGM/Test3");
        W2BGM = Resources.Load<AudioClip>("Audio/BGM/Test4");
        W3BGM = Resources.Load<AudioClip>("Audio/BGM/Test5");
        remixBGM = Resources.Load<AudioClip>("Audio/BGM/Test6");
        voidBGM = Resources.Load<AudioClip>("Audio/BGM/Test0");
        tileDeath = Resources.Load<AudioClip>("Audio/SFX/Tile Death");
        tilePush = Resources.Load<AudioClip>("Audio/SFX/Tile Push");
        areaOverlap = Resources.Load<AudioClip>("Audio/SFX/Area Overlap");
        inverseOverlap = Resources.Load<AudioClip>("Audio/SFX/Inverse Overlap");
        outboundOverlap = Resources.Load<AudioClip>("Audio/SFX/Outbound Overlap");
        uiDeny = Resources.Load<AudioClip>("Audio/SFX/UI Deny");
        select = Resources.Load<AudioClip>("Audio/SFX/Select");
        undo = Resources.Load<AudioClip>("Audio/SFX/Undo");
        cba = Resources.Load<AudioClip>("Audio/SFX/CBA");
        ego1 = Resources.Load<AudioClip>("Audio/Dialog/Ego 1");
        ego2 = Resources.Load<AudioClip>("Audio/Dialog/Ego 2");

        // Default title BGM
        try
        {
            if (GameManager.save != null) master.volume = GameManager.save.preferences.masterVolume;
            PlayBGM(titleBGM);
        }
        catch(Exception e) { Debug.LogWarning(e); } // Errors out on editor ocassionaly
    }

    private void FixedUpdate()
    {
        // if (master == null || TransitionManager.I.inTransition || switchCoro != null) return;

        // if (DialogManager.I.inDialog) master.volume = GameManager.save.preferences.masterVolume - (GameManager.save.preferences.masterVolume * 0.50f);
        // else master.volume = GameManager.save.preferences.masterVolume;
    }

    // Pause BGM's controller
    public void ToggleBGM(bool toggle)
    {
        if (toggle) master.Pause();
        else master.UnPause();
    }

    // Pause SFX's controller
    public void ToggleSFX(bool toggle)
    {
        if (toggle) sfx.Pause();
        else sfx.UnPause();
    }

    // Plays BGM
    public void PlayBGM(AudioClip clip)
    {
        if (clip == master.clip || clip == null) return;

        // Coroutine to switch song
        if (master.isPlaying)
        {
            if (switchCoro != null) StopCoroutine(switchCoro);
            SetMasterVolume(GameManager.save.preferences.masterVolume);
            switchCoro = StartCoroutine(TransitionSong(clip));
            return;
        }

        // Runs one-time
        master.clip = clip;
        master.Play();
    }

    // Plays an SFX
    public void PlaySFX(AudioClip clip, float volume = 1f, bool pitchShift = false)
    {
        if (GameManager.I.chessbattleadvanced) clip = cba;

        if (pitchShift)
        {
            sfxPitch.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
            sfxPitch.volume = volume * GameManager.save.preferences.SFXVolume;
            sfxPitch.PlayOneShot(clip);
            return;
        }

        sfx.volume = volume * GameManager.save.preferences.SFXVolume;
        sfx.PlayOneShot(clip);
    }

    // Volume setters
    public void SetMasterVolume(float volume) { master.volume = volume; }

    // Transition into a different clip
    private IEnumerator TransitionSong(AudioClip clip)
    {
        bool down = true;

        // Lower volume until 0 is reached
        while (down)
        {
            master.volume -= 0.02f;
            yield return new WaitForSeconds(0.01f);
            if (master.volume <= 0) { master.volume = 0; down = false; }
        }

        // Change with the new clip
        master.clip = clip;
        if (!master.isPlaying) master.Play();
        
        // Up volume up until the user's choice
        while (true)
        {
            master.volume += 0.01f;
            yield return new WaitForSeconds(0.01f);
            if (master.volume >= GameManager.save.preferences.masterVolume)
            {
                master.volume = GameManager.save.preferences.masterVolume;
                switchCoro = null;
                yield break;
            }
        }
    }
}
