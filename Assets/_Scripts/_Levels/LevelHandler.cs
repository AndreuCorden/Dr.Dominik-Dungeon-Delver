using UnityEngine;
using System.Collections;

public class LevelHandler : MonoBehaviour
{
    [Header("References")]
    public GridGenerator gridGen;
    public GameObject playerPrefab; // Drag your Player Prefab here

    [Header("Spawn Settings")]
    public int spawnX = 0;
    public int spawnZ = 2;

    [Header("Level Settings")]
    public bool shouldFloorFall = true;

    [Header("Enemy Settings")]
    public GameObject enemyPrefab;
    public GameObject trailEnemyPrefab;
    public GameObject patrollerEnemyPrefab;
    public int enemyCount = 2;

    private GameObject activePlayer;

    IEnumerator Start()
    {
        if (gridGen == null) gridGen = GetComponent<GridGenerator>();

        // 1. Setup the Grid
        GameObject door = gridGen.GenerateLevel();

        yield return new WaitForEndOfFrame();

        // 2. Setup the Player
        SpawnPlayer();
        SpawnEnemies();

        if (door != null && activePlayer != null)
        {
            if (door.TryGetComponent<LevelGoal>(out LevelGoal goal))
            {
                goal.Initialize(gridGen, activePlayer);
            }
        }

        // 3. Start the Falling Floor (If enabled)
        if (shouldFloorFall)
        {
            if (TryGetComponent<FloorManager>(out FloorManager fm))
            {
                fm.StartFallingLogic();
            }
            else
            {
                Debug.LogWarning("ShouldFloorFall is true, but FloorManager component is missing!");
            }
        }
    }

    void SpawnPlayer()
    {
        Vector3 spawnPos = gridGen.GetTilePosition(spawnX, spawnZ);
        spawnPos.y = 1.0f;

        // Check if a persistent player already exists
        if (PlayerController.Instance != null)
        {
            activePlayer = PlayerController.Instance.gameObject;

            PlayerController.Instance.ResetState(spawnPos);
        }
        else
        {
            // First time spawning (Level 0)
            activePlayer = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            activePlayer.name = "Player";
        }

        // Camera setup remains the same...
        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam.TryGetComponent<CameraFollow>(out CameraFollow follow))
        {
            follow.target = activePlayer.transform;
            mainCam.transform.position = activePlayer.transform.position + follow.offset;
        }
    }

    void SpawnEnemies()
    {
        if (enemyPrefab == null) return;

        for (int i = 0; i < enemyCount; i++)
        {
            // Pick a random spot on your grid
            // We use (gridWidth - 2) to keep them away from the very edges
            int randX = Random.Range(1, gridGen.width - 1);
            int randZ = Random.Range(5, gridGen.depth - 1); // Start at Z=5 so they don't spawn on the player

            Vector3 spawnPos = gridGen.GetTilePosition(randX, randZ);
            spawnPos.y = 1.0f;

            if (Random.value > 0.66f && trailEnemyPrefab != null)
                Instantiate(trailEnemyPrefab, spawnPos, Quaternion.identity);
            else if (Random.value > 0.5f && patrollerEnemyPrefab != null)
                Instantiate(patrollerEnemyPrefab, spawnPos, Quaternion.identity);
            else
                Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        }
    }

    void Awake() { BaseEnemy.OccupiedTiles.Clear(); }
}