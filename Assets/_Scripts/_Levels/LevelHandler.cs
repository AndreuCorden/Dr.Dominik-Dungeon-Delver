using UnityEngine;

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

    private GameObject activePlayer;

    void Start()
    {
        if (gridGen == null) gridGen = GetComponent<GridGenerator>();

        // 1. Setup the Grid
        gridGen.GenerateLevel();
        
        // 2. Setup the Player
        SpawnPlayer();

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
        // Calculate the world position based on grid coordinates
        Vector3 spawnPos = gridGen.GetTilePosition(spawnX, spawnZ);
        spawnPos.y = 1.0f;

        // If player doesn't exist, create them. If they do, just move them.
        if (activePlayer == null)
        {
            activePlayer = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            activePlayer.name = "Player";

            // Link the Camera to the new Player
            if (Camera.main.TryGetComponent<CameraFollow>(out CameraFollow follow))
            {
                follow.target = activePlayer.transform;
            }
        }
        else
        {
            activePlayer.transform.position = spawnPos;
        }
    }
}