using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelGoal : MonoBehaviour
{
    private Transform player;
    private bool isUnlocked = false;

    [Header("Settings")]
    public string lockedLayer = "Default";
    public string unlockedLayer = "Floor";

    void Start()
    {
        // Automatically find the player by their tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // Start state: Locked physics
        gameObject.layer = LayerMask.NameToLayer(lockedLayer);
        gameObject.tag = "Untagged";
    }

    void Update()
    {
        if (player == null) return;

        // 1. Logic to Unlock
        if (!isUnlocked && GameObject.FindGameObjectsWithTag("Enemy").Length == 0)
        {
            UnlockPath();
        }

        // 2. Logic to Complete Level
        if (isUnlocked)
        {
            // Check if player is standing on this specific tile
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

        // Optional visual cue
        Renderer rend = GetComponentInChildren<Renderer>();
        // if (rend != null) rend.material.color = Color.green;

        Debug.Log("Path to next level is now walkable!");
    }

    void CompleteLevel()
    {
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
    }
}