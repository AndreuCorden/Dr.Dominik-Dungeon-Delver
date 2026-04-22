using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelGoal : MonoBehaviour
{
    private GridGenerator gridGen;
    private Transform player;

    void Start()
    {
    }

    public void Initialize(GridGenerator generator, GameObject playerObj)
    {
        gridGen = generator;
        player = playerObj.transform;
        Debug.Log("LevelGoal Initialized with Player and Generator.");
    }

    void Update()
    {
        if (player == null) return;

        // 1. Check if all enemies are dead
        // We look for objects with the "Enemy" tag
        int enemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;
        if (enemyCount == 0)
        {
            // 2. Calculate distance to the door
            // One tile away on a grid means the distance is roughly 1.0
            float distToDoor = Vector3.Distance(player.position, gridGen.doorPosition);

            // We use 1.1f to account for small floating point errors
            if (distToDoor <= 1.1f)
            {
                CompleteLevel();
            }
        }
    }

    void CompleteLevel()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        // Check if the next index actually exists in your Build Settings
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            Debug.Log($"Level Complete! Moving to Scene Index: {nextSceneIndex}");
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.LogWarning("No more levels in Build Settings! Returning to Main Menu or Boss?");
            // Optional: SceneManager.LoadScene("MainMenu");
        }
    }
}