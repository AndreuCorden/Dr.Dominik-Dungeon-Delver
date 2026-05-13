using UnityEngine;
using System.Collections.Generic;

public class GridGenerator : MonoBehaviour
{
    public List<LevelBlueprint> levels; // Design your levels in the Inspector!

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

    [HideInInspector] public Vector3 doorPosition;
    [HideInInspector] public Vector3 playerSpawnPos = Vector3.up;

    public GameObject GenerateDesignedLevel(int index)
    {
        if (index >= levels.Count) return null;

        // --- MISSING FUNCTIONALITY: CLEAN SLATE ---
        BaseEnemy.OccupiedTiles.Clear();

        string[] rows = levels[index].layout.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        System.Array.Reverse(rows);

        GameObject doorInstance = null;

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
                if (c != ' ' && c != 'W' && c != 'A')
                {
                    currentFloor = Instantiate(floorPrefab, pos, Quaternion.identity, transform);
                }

                // 2. Spawn Specific Objects
                switch (c)
                {
                    case 'W':
                        // SIDE WALLS (Left side of the room)
                        if (x == 0 && z != rows.Length - 1)
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
                            Instantiate(wallPrefab, pos  + wallOffset, Quaternion.identity, transform);
                        }
                        break;

                    case 'P':
                        playerSpawnPos = pos + Vector3.up;
                        break;

                    case 'D':
                        Vector3 doorOffset = new Vector3(-0.07f, 0.32f, -0.42f);
                        doorInstance = Instantiate(doorPrefab, pos + doorOffset, Quaternion.identity, transform);
                        doorInstance.name = "LevelExitDoor";
                        doorPosition = pos;
                        break;

                    case 'A': // --- RESTORED: ROTATION ---
                              // Rotate 90 degrees to face Right (as in your old code)
                        GameObject arrowWall = Instantiate(arrowWallPrefab, pos + Vector3.up, Quaternion.Euler(0, 90, 0), transform);
                        lastSpawnedShooter = arrowWall.GetComponent<ArrowTrap>();
                        break;

                    case 'L': // --- RESTORED: PARENTING & ASSIGNMENT ---
                        if (currentFloor != null)
                        {
                            GameObject plate = Instantiate(pressurePlatePrefab, new Vector3(pos.x, 0.55f, pos.z), Quaternion.identity, transform);
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
                }
            }
        }
        return doorInstance;
    }
}