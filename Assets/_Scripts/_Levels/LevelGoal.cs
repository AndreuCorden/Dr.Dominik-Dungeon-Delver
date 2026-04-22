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

        // Only allow level completion if enemies are gone
        if (GameObject.FindGameObjectsWithTag("Enemy").Length == 0)
        {
            if (Vector3.Distance(player.position, gridGen.doorPosition) <= 1.1f)
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