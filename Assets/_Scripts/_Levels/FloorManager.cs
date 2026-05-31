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
        // 1. Find EVERY object with the FallingTile script using the new modern method
        // FindObjectsSortMode.None is faster because we are doing our own sorting by Z anyway
        FallingTile[] allFallingScripts = Object.FindObjectsByType<FallingTile>(FindObjectsSortMode.None);

        // Convert to GameObjects for your sorting logic
        allTiles = allFallingScripts.Select(script => script.gameObject).ToList();

        // 2. Sort them by Z position so we drop the "back" rows first
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
                // --- THE FIX: CHECK IF THE TILE STILL EXISTS ---
                if (tile == null) continue;

                if (tile.TryGetComponent<FallingTile>(out FallingTile ft))
                {
                    ft.StartFalling(0.2f);
                }
            }
        }
    }

    public void setTimeBetweenRows(float newTime)
    {
        timeBetweenRows = newTime;
    }
}