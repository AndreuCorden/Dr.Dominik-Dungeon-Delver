using UnityEngine;
using System.Collections.Generic;

public class EnemyFollower : BaseEnemy 
{
    // A simple container to track path nodes while running our grid search
    private class PathNode
    {
        public Vector2 gridPos;
        public PathNode parent;
        public PathNode(Vector2 pos, PathNode p) { gridPos = pos; parent = p; }
    }

    protected override void DetermineNextStep() 
    {
        // 1. Get the current and target grid spots matching GetGridKey format
        Vector2 startGrid = GetGridKey(transform.position);
        Vector2 targetGrid = GetGridKey(player.position);

        // 2. Compute the smart direction to take
        Vector3 pathfindingDirection = CalculateSmartPathStep(startGrid, targetGrid);

        // 3. Execute the step! If pathfinding yields nothing or gets blocked dynamically, default back to the old logic
        if (pathfindingDirection != Vector3.zero)
        {
            if (!TryMove(pathfindingDirection))
            {
                ExecuteGreedyFallback();
            }
        }
        else
        {
            ExecuteGreedyFallback();
        }
    }

    private Vector3 CalculateSmartPathStep(Vector2 start, Vector2 target)
    {
        Queue<PathNode> queue = new Queue<PathNode>();
        HashSet<Vector2> visited = new HashSet<Vector2>();

        queue.Enqueue(new PathNode(start, null));
        visited.Add(start);

        // Grid-locked cardinal movements matching the layout
        Vector2[] gridDirections = {
            new Vector2(1f, 0f),  // Right
            new Vector2(-1f, 0f), // Left
            new Vector2(0f, 1f),  // Forward
            new Vector2(0f, -1f)  // Backward
        };

        PathNode destinationNode = null;
        int tilesSearched = 0;
        int maxSearchLimit = 400; // Limits search depth to keep game loop performance lightning fast

        while (queue.Count > 0 && tilesSearched < maxSearchLimit)
        {
            tilesSearched++;
            PathNode current = queue.Dequeue();

            // If we are directly adjacent to or sharing the target tile, we found the path
            if (Vector2.Distance(current.gridPos, target) < 0.1f)
            {
                destinationNode = current;
                break;
            }

            foreach (Vector2 dir in gridDirections)
            {
                Vector2 neighbor = current.gridPos + dir;

                if (!visited.Contains(neighbor))
                {
                    // Check if the floor exists there and if it isn't claimed by another enemy
                    if (IsTileWalkableSimulation(neighbor) || Vector2.Distance(neighbor, target) < 0.1f)
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(new PathNode(neighbor, current));
                    }
                }
            }
        }

        // Trace the steps backward to extract the single next step coordinates
        if (destinationNode != null)
        {
            PathNode movementTracker = destinationNode;
            while (movementTracker.parent != null && Vector2.Distance(movementTracker.parent.gridPos, start) > 0.1f)
            {
                movementTracker = movementTracker.parent;
            }

            // Convert the grid differences into a standard 3D direction vector
            Vector2 stepDirection2D = movementTracker.gridPos - start;
            return new Vector3(stepDirection2D.x, 0f, stepDirection2D.y);
        }

        return Vector3.zero; // No direct path available
    }

    /// <summary>
    /// Simulates your BaseEnemy structural validation rules without changing runtime variables.
    /// </summary>
    private bool IsTileWalkableSimulation(Vector2 tileGridCoords)
    {
        // Reconstruct the potential 3D position based on your rounding methods
        Vector3 simulatedDest3D = new Vector3(tileGridCoords.x, transform.position.y, tileGridCoords.y);

        // Rule A: Does a floor exist under this simulated coordinate spot?
        bool hasFloor = Physics.Raycast(simulatedDest3D + Vector3.up, Vector3.down, 2f, floorLayer);
        if (!hasFloor) return false;

        // Rule B: Is another enemy currently claiming this tile?
        bool isOccupied = OccupiedTiles.Contains(tileGridCoords);
        if (isOccupied) return false;

        return true;
    }

    /// <summary>
    /// Your original line-of-sight tracking algorithm acting as an immutable fallback routine.
    /// </summary>
    private void ExecuteGreedyFallback()
    {
        Vector3 diff = player.position - transform.position;
        Vector3 primary = Mathf.Abs(diff.x) > Mathf.Abs(diff.z) ? 
            new Vector3(Mathf.Sign(diff.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(diff.z));
        
        if (!TryMove(primary)) 
        {
            Vector3 secondary = (primary.x != 0) ? new Vector3(0, 0, Mathf.Sign(diff.z)) : new Vector3(Mathf.Sign(diff.x), 0, 0);
            TryMove(secondary);
        }
    }
}