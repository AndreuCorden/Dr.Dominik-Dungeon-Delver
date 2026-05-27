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
    }

    // --- NEW: Play Main Menu Music ---
    public void PlayMainMenuMusic()
    {
        SwitchTrack(mainMenuTrack);
    }

    // --- NEW: Play Credits Music ---
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
        source.volume = volume;

        // Configure for game-world spatial audio
        source.spatialBlend = 1f; // 1.0 = Fully 3D sound
        source.minDistance = 2f;
        source.maxDistance = 15f;
        source.rolloffMode = AudioRolloffMode.Linear;

        source.Play();

        // Destroy the object automatically once the clip finishes playing
        Destroy(sfxObj, clip.length);
    }
}