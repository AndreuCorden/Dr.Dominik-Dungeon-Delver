using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    [Header("Grid Dimensions")]
    public int width = 10;
    public int depth = 15;

    [Header("Prefabs")]
    public GameObject floorPrefab;
    public GameObject wallPrefab;

    void Start()
    {
    }

    public void GenerateLevel()
    {
        // We iterate from -1 to width/depth to include the walls
        for (int x = -1; x <= width; x++)
        {
            for (int z = -1; z <= depth; z++)
            {
                Vector3 spawnPos = new Vector3(x, 0, z);

                // Determine if this is a Wall or Floor
                bool isEdge = (x == -1 || z == depth);

                if (isEdge)
                {
                    // Create Wall
                    GameObject wall = Instantiate(wallPrefab, spawnPos + Vector3.up, Quaternion.identity);
                    wall.name = $"Wall_{x}_{z}";
                    wall.transform.parent = this.transform;
                }
                else
                {
                    // Create Floor
                    GameObject tile = Instantiate(floorPrefab, spawnPos, Quaternion.identity);
                    tile.name = $"Tile_{x}_{z}";
                    tile.transform.parent = this.transform;
                }
            }
        }
    }

    public Vector3 GetTilePosition(int x, int z)
    {
        // Returns the world position of a specific coordinate
        return new Vector3(x, 0.5f, z); // 0.5f height so the player sits ON the cube
    }
}