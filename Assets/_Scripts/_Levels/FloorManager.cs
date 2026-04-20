using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Needed for easy list sorting

public class FloorManager : MonoBehaviour
{
    public float timeBetweenRows = 2f; // How fast the floor disappears
    private List<GameObject> allTiles = new List<GameObject>();

    public void StartFallingLogic()
    {
        // 1. Find all objects tagged "Floor" (Make sure your tiles have the tag!)
        allTiles = GameObject.FindGameObjectsWithTag("Floor").ToList();

        // 2. Sort them by Z position so we drop the "back" rows first
        // If your level goes from Z=0 to Z=20, this starts at Z=0
        var groupedByRow = allTiles.GroupBy(t => Mathf.RoundToInt(t.transform.position.z))
                                   .OrderBy(g => g.Key);

        StartCoroutine(DropRowsSequentially(groupedByRow));
    }

    IEnumerator DropRowsSequentially(IEnumerable<IGrouping<int, GameObject>> rows)
    {
        foreach (var row in rows)
        {
            yield return new WaitForSeconds(timeBetweenRows);

            foreach (var tile in row)
            {
                if (tile.TryGetComponent<FallingTile>(out FallingTile ft))
                {
                    ft.StartFalling(0.2f);
                }
            }
        }
    }
}