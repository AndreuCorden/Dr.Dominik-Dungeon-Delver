using UnityEngine;
using System.Collections;

public class LevelHandler : MonoBehaviour
{
    [Header("References")]
    public GridGenerator gridGen;
    public GameObject playerPrefab; // Drag your Player Prefab here

    [Header("Level Settings")]
    public bool shouldFloorFall = true;
    public int levelIndex = 0;

    private GameObject activePlayer;

    IEnumerator Start()
    {
        if (gridGen == null) gridGen = GetComponent<GridGenerator>();

        // 1. Setup the Grid
        GameObject door = gridGen.GenerateDesignedLevel(levelIndex);
        SpawnPlayer(gridGen.playerSpawnPos);

        yield return new WaitForEndOfFrame();

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

    void SpawnPlayer(Vector3 spawnPos)
    {
        // Check if a persistent player already exists
        if (PlayerController.Instance != null)
        {
            activePlayer = PlayerController.Instance.gameObject;
            PlayerController.Instance.ResetState(spawnPos);
        }
        else
        {
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

    void Awake() { BaseEnemy.OccupiedTiles.Clear(); }
}
