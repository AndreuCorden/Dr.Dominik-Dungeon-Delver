using UnityEngine;
using System.Collections.Generic;

public class GridGenerator : MonoBehaviour
{
    [Header("Grid Dimensions")]
    public int width = 10;
    public int depth = 15;

    [Header("Prefabs")]
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject CoinPrefab;
    public GameObject doorPrefab;
    public GameObject spikeTrapFloorPrefab;
    public GameObject gargoylePrefab;
    public GameObject arrowWallPrefab;
    public GameObject pressurePlatePrefab;
    private Dictionary<Vector2Int, ArrowTrap> pendingPressurePlates = new Dictionary<Vector2Int, ArrowTrap>();

    [Header("Spawn Rates (0.0 to 1.0)")]
    public float coinSpawnRate = 0.1f;
    public float spikeSpawnRate = 0.05f;
    public float gargoyleSpawnRate = 0.02f;
    public float arrowTrapSpawnRate = 0.25f;

    // This is now accessible by other scripts
    [HideInInspector] public Vector3 doorPosition;

    public GameObject GenerateLevel()
    {
        // Important: Clear the dictionary at the start of generation
        pendingPressurePlates.Clear();

        int doorX = Random.Range(0, width);
        GameObject door = null;
        doorPosition = new Vector3(doorX, 0, depth);

        for (int x = -1; x <= width; x++)
        {
            for (int z = -1; z <= depth; z++)
            {
                Vector3 spawnPos = new Vector3(x, 0, z);

                // Define exactly what counts as the "Left Wall"
                bool isLeftWall = (x == -1 && z >= 0 && z < depth);
                bool isEdge = (x == -1 || z == depth);

                // --- DOOR LOGIC ---
                if (x == doorX && z == depth)
                {
                    // 1. Spawn a Floor tile underneath the door
                    GameObject floorUnderDoor = Instantiate(floorPrefab, spawnPos, Quaternion.identity);
                    floorUnderDoor.transform.parent = this.transform;

                    // 2. Spawn the Door itself 1 unit up
                    door = Instantiate(doorPrefab, spawnPos + Vector3.up, Quaternion.identity);
                    door.name = "LevelExitDoor";
                    door.transform.parent = this.transform;

                    continue;
                }

                if (isEdge)
                {
                    // Only spawn Arrow Traps on the Left Wall
                    if (isLeftWall && z > 2 && Random.value < arrowTrapSpawnRate)
                    {
                        // Position at wall height, rotated 90 degrees to face Right
                        Vector3 wallPos = spawnPos + Vector3.up;
                        Quaternion wallRot = Quaternion.Euler(0, 90, 0);

                        GameObject wall = Instantiate(arrowWallPrefab, wallPos, wallRot);
                        wall.transform.parent = this.transform;

                        ArrowTrap trap = wall.GetComponent<ArrowTrap>();

                        // Pick a tile in the same row (Z) but further in (X)
                        // We pick a random X between 1 and the middle of the room
                        int triggerX = Random.Range(width * 1/3, width * 2/3);
                        Vector2Int plateCoord = new Vector2Int(triggerX, z);

                        // Store it so the floor loop can find it
                        if (!pendingPressurePlates.ContainsKey(plateCoord))
                        {
                            pendingPressurePlates.Add(plateCoord, trap);
                        }
                    }
                    else
                    {
                        // Spawn a normal wall for all other edges
                        GameObject wall = Instantiate(wallPrefab, spawnPos + Vector3.up, Quaternion.identity);
                        wall.name = $"Wall_{x}_{z}";
                        wall.transform.parent = this.transform;
                    }
                }
                else
                {
                    // --- FLOOR / TRAP / COIN LOGIC ---
                    GameObject tileToSpawn = floorPrefab;
                    Vector2Int currentCoord = new Vector2Int(x, z);

                    if (z > 2 && Random.value < spikeSpawnRate && !pendingPressurePlates.ContainsKey(currentCoord))
                    {
                        tileToSpawn = spikeTrapFloorPrefab;
                    }

                    GameObject tile = Instantiate(tileToSpawn, spawnPos, Quaternion.identity);
                    tile.transform.parent = this.transform;

                    // CHECK FOR PRESSURE PLATE
                    
                    if (pendingPressurePlates.ContainsKey(currentCoord))
                    {
                        // Spawn the plate slightly above floor height
                        GameObject plate = Instantiate(pressurePlatePrefab,new Vector3(spawnPos.x, 0.55f, spawnPos.z), Quaternion.identity);
                        plate.transform.parent = tile.transform;

                        // Connect the plate to the specific wall trap instance
                        plate.GetComponent<PressurePlate>().wallTrap = pendingPressurePlates[currentCoord];
                    }

                    // Only spawn coins on regular floors, not on traps
                    if (tileToSpawn == floorPrefab && Random.value < coinSpawnRate)
                    {
                        Vector3 coinPos = new Vector3(spawnPos.x, 1.0f, spawnPos.z);
                        Quaternion coinRotation = Quaternion.Euler(90, 0, 0);
                        GameObject coin = Instantiate(CoinPrefab, coinPos, coinRotation);
                        coin.transform.parent = tile.transform; // Parent to tile for organization
                    }
                    else if (z > 2 && tileToSpawn == floorPrefab && Random.value < gargoyleSpawnRate)
                    {
                        Vector3 gargoylePos = new Vector3(spawnPos.x, 1.25f, spawnPos.z);
                        GameObject gargoyle = Instantiate(gargoylePrefab, gargoylePos, Quaternion.identity);
                        gargoyle.transform.parent = tile.transform; // Parent to tile for organization
                        tile.layer = LayerMask.NameToLayer("Trap");
                    }
                }
            }
        }
        return door;
    }

    public Vector3 GetTilePosition(int x, int z)
    {
        return new Vector3(x, 0.5f, z);
    }
}