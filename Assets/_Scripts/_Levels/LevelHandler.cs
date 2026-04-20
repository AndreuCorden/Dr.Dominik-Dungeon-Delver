using UnityEngine;

public class LevelHandler : MonoBehaviour
{
    [Header("References")]
    public GridGenerator gridGen;
    public GameObject playerPrefab; // Drag your Player Prefab here

    [Header("Spawn Settings")]
    public int spawnX = 0;
    public int spawnZ = 2;

    private GameObject activePlayer;

    void Start()
    {
        // Automatically find the GridGenerator on the same object
        if (gridGen == null) gridGen = GetComponent<GridGenerator>();

        gridGen.GenerateLevel();
        SpawnPlayer();
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