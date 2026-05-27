using UnityEngine;

public class LevelGoal : MonoBehaviour
{
    private Transform player;
    private bool isUnlocked = false;
    private bool isTransitioning = false; 

    [Header("Settings")]
    public string lockedLayer = "Default";
    public string unlockedLayer = "Floor";

    [Header("Audio Configurations")]
    [SerializeField] private AudioClip doorOpenSFX;
    [SerializeField] [Range(0f, 1f)] private float volume = 0.8f;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        gameObject.layer = LayerMask.NameToLayer(lockedLayer);
        gameObject.tag = "Untagged";
    }

    void Update()
    {
        if (player == null || isTransitioning) return;

        if (!isUnlocked && GameObject.FindGameObjectsWithTag("Enemy").Length == 0)
        {
            UnlockPath();
        }

        if (isUnlocked)
        {
            float distance = Vector2.Distance(
                new Vector2(player.position.x, player.position.z),
                new Vector2(transform.position.x, transform.position.z)
            );

            if (distance < 0.1f)
            {
                CompleteLevel();
            }
        }
    }

    void UnlockPath()
    {
        isUnlocked = true;
        gameObject.tag = "Floor";
        gameObject.layer = LayerMask.NameToLayer(unlockedLayer);

        // --- PLAY DOOR OPEN SFX ---
        if (AudioManager.Instance != null && doorOpenSFX != null)
        {
            AudioManager.Instance.PlaySFX(doorOpenSFX, transform.position, volume);
        }

        GameObject door = GameObject.Find("LevelExitDoor");
        if (door != null)
        {
            if (door.TryGetComponent<DoorOpener>(out var opener)) opener.OpenDoor();
        }
        Debug.Log("Path to next level is now walkable!");
    }

    void CompleteLevel()
    {
        isTransitioning = true;
        LevelHandler handler = FindFirstObjectByType<LevelHandler>();

        if (handler != null)
        {
            int nextLevelIndex = handler.levelIndex + 1;
            handler.StartExitTransition(nextLevelIndex);
        }
        else
        {
            int nextSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex + 1;
            if (nextSceneIndex < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneIndex);
            }
        }
    }
}