using UnityEngine;

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

    [Header("Spawn Rates (0.0 to 1.0)")]
    public float coinSpawnRate = 0.1f;
    public float spikeSpawnRate = 0.05f;
    public float gargoyleSpawnRate = 0.02f;

    // This is now accessible by other scripts
    [HideInInspector] public Vector3 doorPosition;

    public GameObject GenerateLevel()
    {
        // Pick the random X for the door
        int doorX = Random.Range(0, width);
        GameObject door = null;

        // Set the global doorPosition (at floor height for distance checking)
        doorPosition = new Vector3(doorX, 0, depth);

        for (int x = -1; x <= width; x++)
        {
            for (int z = -1; z <= depth; z++)
            {
                Vector3 spawnPos = new Vector3(x, 0, z);
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
                    GameObject wall = Instantiate(wallPrefab, spawnPos + Vector3.up, Quaternion.identity);
                    wall.name = $"Wall_{x}_{z}";
                    wall.transform.parent = this.transform;
                }
                else
                {
                    GameObject tileToSpawn = floorPrefab;

                    // Don't spawn traps too close to the player's start (z=0,1,2)
                    if (z > 2 && Random.value < spikeSpawnRate)
                    {
                        tileToSpawn = spikeTrapFloorPrefab;
                    }

                    GameObject tile = Instantiate(tileToSpawn, spawnPos, Quaternion.identity);
                    tile.name = tileToSpawn == floorPrefab ? $"Tile_{x}_{z}" : $"SpikeTrap_{x}_{z}";
                    tile.transform.parent = this.transform;

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