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
        Vector3 spawnPos = gridGen.GetTilePosition(spawnX, spawnZ);
        spawnPos.y = 1.0f;

        activePlayer = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        activePlayer.name = "Player";

        // RE-FIND THE CAMERA AND TARGET THE NEW PLAYER
        Camera mainCam = Camera.main;
        if (mainCam != null && mainCam.TryGetComponent<CameraFollow>(out CameraFollow follow))
        {
            follow.target = activePlayer.transform;

            // Snap camera to position immediately so it doesn't "slide" from the old spot
            Vector3 targetCamPos = activePlayer.transform.position + follow.offset;
            mainCam.transform.position = targetCamPos;
        }
    }
}