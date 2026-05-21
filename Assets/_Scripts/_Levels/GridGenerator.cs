using UnityEngine;
using System.Collections.Generic;

public class GridGenerator : MonoBehaviour
{
    public List<LevelBlueprint> levels = new List<LevelBlueprint>(); // Design your levels in the Inspector!

    [Header("Prefabs")]
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject CoinPrefab;
    public GameObject doorPrefab;
    public GameObject spikeTrapFloorPrefab;
    public GameObject gargoylePrefab;
    public GameObject arrowWallPrefab;
    public GameObject pressurePlatePrefab;
    public GameObject mimicPrefab;

    [Header("Enemy Settings")]
    public GameObject enemyPrefab;
    public GameObject trailEnemyPrefab;
    public GameObject patrollerEnemyPrefab;

    [Header("Decor Settings")]
    public GameObject[] decorPrefabs;

    [HideInInspector] public Vector3 doorPosition;
    [HideInInspector] public Vector3 playerSpawnPos = Vector3.up;

    public void GenerateDesignedLevel(int index)
    {
        if (index >= levels.Count) return;

        // --- MISSING FUNCTIONALITY: CLEAN SLATE ---
        BaseEnemy.OccupiedTiles.Clear();

        string[] rows = levels[index].layout.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        System.Array.Reverse(rows);

        // Helper for Arrow Trap pairing (used for 'A' and 'L')
        // This allows multiple arrow shooters in one level if needed
        ArrowTrap lastSpawnedShooter = null;

        for (int z = 0; z < rows.Length; z++)
        {
            for (int x = 0; x < rows[z].Length; x++)
            {
                char c = rows[z][x];
                Vector3 pos = new Vector3(x, 0, z);
                GameObject currentFloor = null;

                // 1. Spawn Floor (Restored logic: Wall 'W' and Arrow 'A' provide their own collision)
                if (c != ' ' && c != 'W' && c != 'A' && c != '3')
                {
                    currentFloor = Instantiate(floorPrefab, pos, Quaternion.identity, transform);
                }

                // 2. Spawn Specific Objects
                switch (c)
                {
                    case 'W':
                        PlaceWall(pos, x, z, rows.Length);
                        break;

                    case 'P':
                        playerSpawnPos = pos + Vector3.up;
                        break;

                    case 'D':
                        Vector3 doorOffset = new Vector3(-0.07f, 0.32f, -0.42f);
                        GameObject doorInstance = Instantiate(doorPrefab, pos + doorOffset, Quaternion.identity, transform);
                        doorInstance.name = "LevelExitDoor";
                        doorPosition = pos;

                        if (currentFloor != null)
                        {
                            // 1. Remove falling logic
                            FallingTile ft = currentFloor.GetComponent<FallingTile>();
                            if (ft != null) Destroy(ft);

                            // 2. Add the Goal logic (It will find the player on its own in Start)
                            currentFloor.AddComponent<LevelGoal>();
                        }
                        break;

                    case 'A': // --- RESTORED: ROTATION ---
                              // Rotate 90 degrees to face Right (as in your old code)
                        GameObject arrowWall = Instantiate(arrowWallPrefab, pos + Vector3.up, Quaternion.Euler(0, 90, 0), transform);
                        lastSpawnedShooter = arrowWall.GetComponent<ArrowTrap>();
                        PlaceWall(pos, x, z, rows.Length);
                        break;

                    case 'L': // --- RESTORED: PARENTING & ASSIGNMENT ---
                        if (currentFloor != null)
                        {
                            GameObject plate = Instantiate(pressurePlatePrefab, new Vector3(pos.x, 0.5f, pos.z), Quaternion.identity, transform);
                            if (lastSpawnedShooter != null)
                                plate.GetComponent<PressurePlate>().wallTrap = lastSpawnedShooter;
                        }
                        break;

                    case 'C': // --- RESTORED: ROTATION & PARENTING ---
                        if (currentFloor != null)
                        {
                            Quaternion coinRot = Quaternion.Euler(90, 0, 0);
                            Instantiate(CoinPrefab, pos + Vector3.up, coinRot, transform);
                        }
                        break;

                    case 'G': // --- RESTORED: LAYER ASSIGNMENT ---
                        if (currentFloor != null)
                        {
                            Instantiate(gargoylePrefab, pos + Vector3.up * 1.25f, Quaternion.identity, transform);
                            currentFloor.layer = LayerMask.NameToLayer("Trap");
                        }
                        break;

                    case 'M': // --- RESTORED: PARENTING ---
                        if (currentFloor != null)
                        {
                            Instantiate(mimicPrefab, pos + Vector3.up * 0.6f, Quaternion.identity, transform);
                        }
                        break;

                    case 'I': // Spike Trap
                        Instantiate(spikeTrapFloorPrefab, pos, Quaternion.identity, transform);
                        break;

                    case 'F': // Basic Enemy
                        Instantiate(enemyPrefab, pos + Vector3.up, Quaternion.identity, transform);
                        break;

                    case 'T': // Patroller
                        Instantiate(patrollerEnemyPrefab, pos + Vector3.up, Quaternion.identity, transform);
                        break;

                    case 'S': // Trail Enemy
                        Instantiate(trailEnemyPrefab, pos + Vector3.up, Quaternion.identity, transform);
                        break;

                    case '0':
                        {
                            GameObject decor = Instantiate(decorPrefabs[0], pos + Vector3.up * 0.5f, Quaternion.identity, transform);
                            if (decor.GetComponent<FallingTile>() == null)
                            {
                                decor.AddComponent<FallingTile>();
                            }
                            currentFloor.layer = LayerMask.NameToLayer("Trap");
                            break;
                        }

                    case '1':
                        {
                            GameObject decor = Instantiate(decorPrefabs[1], pos + Vector3.up * 0.5f, Quaternion.identity, transform);
                            if (decor.GetComponent<FallingTile>() == null)
                            {
                                decor.AddComponent<FallingTile>();
                            }
                            currentFloor.layer = LayerMask.NameToLayer("Trap");
                            break;
                        }

                    case '2':
                        {
                            GameObject decor = Instantiate(decorPrefabs[2], pos + Vector3.up * 0.5f, Quaternion.identity, transform);
                            if (decor.GetComponent<FallingTile>() == null)
                            {
                                decor.AddComponent<FallingTile>();
                            }
                            currentFloor.layer = LayerMask.NameToLayer("Trap");
                            break;
                        }

                    case '3':
                        // 1. Place the wall first using the helper
                        PlaceWall(pos, x, z, rows.Length);

                        // 2. Determine lantern position/rotation based on which wall it's on
                        Vector3 lanternOffset;
                        Quaternion lanternRotation;

                        if (x == 0 && z != rows.Length - 1) // Side wall (Facing Right)
                        {
                            // Push it slightly further out than the wall (0.7f) and up to eye level
                            lanternOffset = new Vector3(0.7f, 1f, 0);
                            lanternRotation = Quaternion.Euler(0, -90, 0);
                        }
                        else // Back wall (Facing Forward/Down)
                        {
                            // Push it slightly forward from the back wall (-0.7f)
                            lanternOffset = new Vector3(0, 1f, -0.7f);
                            lanternRotation = Quaternion.identity;
                        }

                        // 3. Spawn the lantern (Assuming lanternPrefabs[3] is your lantern)
                        GameObject lantern = Instantiate(decorPrefabs[3], pos + Vector3.up + lanternOffset, lanternRotation, transform);
                        break;

                    case '4':
                        {
                            GameObject decor = Instantiate(decorPrefabs[4], pos + Vector3.up * 0.5f, Quaternion.identity, transform);
                            if (decor.GetComponent<FallingTile>() == null)
                            {
                                decor.AddComponent<FallingTile>();
                            }
                            currentFloor.layer = LayerMask.NameToLayer("Trap");
                            break;
                        }
                    case '5':
                        {
                            GameObject decor = Instantiate(decorPrefabs[5], pos + Vector3.up * 0.5f, Quaternion.identity, transform);
                            if (decor.GetComponent<FallingTile>() == null)
                            {
                                decor.AddComponent<FallingTile>();
                            }
                            currentFloor.layer = LayerMask.NameToLayer("Trap");
                            break;
                        }
                    case '6':
                        {
                            GameObject decor = Instantiate(decorPrefabs[6], pos + Vector3.up * 0.5f, Quaternion.identity, transform);
                            if (decor.GetComponent<FallingTile>() == null)
                            {
                                decor.AddComponent<FallingTile>();
                            }
                            currentFloor.layer = LayerMask.NameToLayer("Trap");
                            break;
                        }
                }
            }
        }
    }

    private void PlaceWall(Vector3 pos, int x, int z, int rowCount)
    {
        if (x == 0 && z != rowCount - 1)
        {
            // Offset: Move slightly Right (X+) and forward (Z+) to close the back gap
            Vector3 wallOffset = new Vector3(0.3f, 0.25f, 0.2f);
            Instantiate(wallPrefab, pos + wallOffset, Quaternion.Euler(0, -90, 0), transform);
        }
        // BACK WALLS (Top of the room)
        else
        {
            // Offset: Move slightly Down (Z-) so it sits ON the floor, not on the line
            Vector3 wallOffset = new Vector3(-0.07f, 0.3f, -0.3f);
            Instantiate(wallPrefab, pos + wallOffset, Quaternion.identity, transform);
        }
    }
}