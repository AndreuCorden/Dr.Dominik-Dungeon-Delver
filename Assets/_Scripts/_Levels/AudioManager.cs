using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Components")]
    [SerializeField] private AudioSource musicSource;

    [Header("Global Menu Tracks")]
    [SerializeField] private AudioClip mainMenuTrack;
    [SerializeField] private AudioClip creditsTrack;

    [Header("Soundtrack List")]
    [Tooltip("Assign your music clips here. Element 0 = Level 1 music, Element 1 = Level 2, etc.")]
    [SerializeField] private AudioClip[] levelTracks;

    [Header("Fallback Settings")]
    [SerializeField] private AudioClip defaultTrack;

    // ==========================================
    // NEW: VOLUME RUNTIME CONTROLLERS
    // ==========================================
    public float musicVolume = 1f;
    private float sfxVolume = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
            if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.loop = true;
        musicSource.playOnAwake = false;

        // Load preferences on startup (Defaults to max volume '1.0f' if empty)
        musicVolume = PlayerPrefs.GetFloat("MusicVolumeSettingsKey", 0.7f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolumeSettingsKey", 0.8f);

        // Instantly push saved configuration into the background track player
        musicSource.volume = musicVolume;
    }

    // ==========================================
    // NEW: SLIDER INTEGRATION INTERFACES
    // ==========================================
    /// <summary>
    /// Hook this directly to an OnValueChanged() event of a Music Slider (Range: 0 to 1)
    /// </summary>
    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        musicSource.volume = musicVolume;
        PlayerPrefs.SetFloat("MusicVolumeSettingsKey", musicVolume);
    }

    /// <summary>
    /// Hook this directly to an OnValueChanged() event of an SFX Slider (Range: 0 to 1)
    /// </summary>
    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("SFXVolumeSettingsKey", sfxVolume);
    }

    // Helper properties to initialize Slider UI components at startup
    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;


    // --- Play Main Menu Music ---
    public void PlayMainMenuMusic()
    {
        SwitchTrack(mainMenuTrack);
    }

    // --- Play Credits Music ---
    public void PlayCreditsMusic()
    {
        SwitchTrack(creditsTrack);
    }

    // Your existing gameplay music function
    public void PlayMusicForLevel(int levelIndex)
    {
        AudioClip selectedClip = defaultTrack;

        if (levelTracks != null && levelIndex >= 0 && levelIndex < levelTracks.Length)
        {
            if (levelTracks[levelIndex] != null) selectedClip = levelTracks[levelIndex];
        }

        SwitchTrack(selectedClip);
    }

    // --- HELPER: Safely switches tracks without restarting if already playing ---
    private void SwitchTrack(AudioClip newClip)
    {
        if (musicSource.clip == newClip && musicSource.isPlaying) return;

        musicSource.clip = newClip;
        
        // Always match the current master music volume cap whenever switching tracks
        musicSource.volume = musicVolume;

        if (newClip != null)
        {
            musicSource.Play();
        }
        else
        {
            musicSource.Stop();
        }
    }

    public void StopMusic()
    {
        musicSource.Stop();
        musicSource.clip = null;
    }

    public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;

        // Create a temporary GameObject for the 3D sound
        GameObject sfxObj = new GameObject($"SFX_{clip.name}");
        sfxObj.transform.position = position;

        AudioSource source = sfxObj.AddComponent<AudioSource>();
        source.clip = clip;

        // ==========================================
        // FIXED: SCALED BY AUDIO MANAGER MASTER LEVEL
        // ==========================================
        // Multiplies individual asset volume parameter offsets (e.g. 0.8f) 
        // by our global master system slider value (e.g. 0.5f max)
        source.volume = volume * sfxVolume;

        // Configure for game-world spatial audio
        source.spatialBlend = 1f; 
        source.minDistance = 2f;
        source.maxDistance = 15f;
        source.rolloffMode = AudioRolloffMode.Linear;

        source.Play();

        // Destroy the object automatically once the clip finishes playing
        Destroy(sfxObj, clip.length);
    }
}