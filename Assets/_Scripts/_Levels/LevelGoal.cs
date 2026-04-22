using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelGoal : MonoBehaviour
{
    [Header("Progression")]
    public string nextLevelName;
    
    private GridGenerator gridGen;
    private Transform player;

    void Start()
    {
        gridGen = Object.FindFirstObjectByType<GridGenerator>();
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null) player = pObj.transform;
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
        Debug.Log("Enemies cleared and Door reached!");
        // Optional: PlayerController.health = 3;
        SceneManager.LoadScene(nextLevelName);
    }
}