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

    // This is now accessible by other scripts
    [HideInInspector] public Vector3 doorPosition;

    public void GenerateLevel()
    {
        // Pick the random X for the door
        int doorX = Random.Range(0, width);
        
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
                    GameObject door = Instantiate(doorPrefab, spawnPos + Vector3.up, Quaternion.identity);
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
                    GameObject tile = Instantiate(floorPrefab, spawnPos, Quaternion.identity);
                    tile.name = $"Tile_{x}_{z}";
                    tile.transform.parent = this.transform;

                    if (Random.value < 0.1f)
                    {
                        Vector3 coinPos = new Vector3(spawnPos.x, 1.0f, spawnPos.z);
                        Quaternion coinRotation = Quaternion.Euler(90, 0, 0);
                        Instantiate(CoinPrefab, coinPos, coinRotation);
                    }
                }
            }
        }
    }

    public Vector3 GetTilePosition(int x, int z)
    {
        return new Vector3(x, 0.5f, z);
    }
}