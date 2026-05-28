using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossEnemy : EnemyFollower
{
    [Header("Boss Health Settings")]
    public int health = 3;
    [SerializeField] [Range(0f, 1f)] private float volume = 0.9f;

    // A helper list to track all 3 coordinates this Boss currently spans
    private List<Vector2> currentOccupiedKeys = new List<Vector2>();
    private List<Vector2> targetOccupiedKeys = new List<Vector2>();

    protected override void Start()
    {
        // Find player and round center position using BaseEnemy initialization rules
        player = GameObject.FindGameObjectWithTag("Player").transform;
        targetPosition = RoundToGrid(transform.position);
        transform.position = targetPosition;

        // Force a starting forward direction so our side-tile offsets calculate correctly
        if (transform.forward == Vector3.zero) transform.forward = Vector3.forward;

        // Explicitly map all 3 tiles we are starting on
        UpdateOccupiedTilesMap(transform.position, transform.forward, currentOccupiedKeys);

        // Register all 3 blocks into the global system
        foreach (Vector2 key in currentOccupiedKeys)
        {
            OccupiedTiles.Add(key);
        }
    }

    // --- OVERRIDDEN MULTI-TILE VALIDATION AND MOVEMENT ---

    protected new bool TryMove(Vector3 direction)
    {
        // --- FIX: ATTACK RANGE CHECK BEFORE MOVEMENT CALCULATIONS ---
        // Calculate the grid tile that is exactly 2 blocks away from our *current* center
        Vector3 attackCheckPos = RoundToGrid(transform.position + (direction * 2f));
        Vector2 attackCheckKey = GetGridKey(attackCheckPos);
        Vector2 playerKey = GetGridKey(player.position);

        // If the player is standing on the center line 2 blocks ahead, attack!
        if (playerKey == attackCheckKey)
        {
            PerformAttack(direction);
            return true;
        }

        // Also check if the player is standing 2 blocks ahead but diagonally touching our wings
        Vector3 rightOffset = Vector3.Cross(Vector3.up, direction).normalized;
        Vector2 attackLeftWingKey = GetGridKey(attackCheckPos - rightOffset);
        Vector2 attackRightWingKey = GetGridKey(attackCheckPos + rightOffset);

        if (playerKey == attackLeftWingKey || playerKey == attackRightWingKey)
        {
            PerformAttack(direction);
            return true;
        }
        // -------------------------------------------------------------

        Vector3 centerDest3D = RoundToGrid(transform.position + direction);

        // Calculate what our 3-tile footprint will look like at the destination
        UpdateOccupiedTilesMap(centerDest3D, direction, targetOccupiedKeys);

        // Multi-Tile Validation Check Loop
        foreach (Vector2 targetKey in targetOccupiedKeys)
        {
            Vector3 worldCheckPos = new Vector3(targetKey.x, centerDest3D.y, targetKey.y);

            // Does a physical floor tile exist under this specific footprint segment?
            bool segmentHasFloor = Physics.Raycast(worldCheckPos + Vector3.up, Vector3.down, 2f, floorLayer);
            if (!segmentHasFloor) return false; // Entire move fails if any part hangs over a void

            // Is this tile blocked by another enemy? 
            if (OccupiedTiles.Contains(targetKey) && !currentOccupiedKeys.Contains(targetKey))
            {
                return false; // Path blocked by another entity
            }
        }

        // Movement Execution: Clear old footprint coordinates, write new ones
        foreach (Vector2 oldKey in currentOccupiedKeys) OccupiedTiles.Remove(oldKey);
        foreach (Vector2 newKey in targetOccupiedKeys) OccupiedTiles.Add(newKey);

        // Swap local trackers
        List<Vector2> temp = currentOccupiedKeys;
        currentOccupiedKeys = targetOccupiedKeys;
        targetOccupiedKeys = temp;

        // --- PLAY BOSS WALK SFX ---
        // Trigger right when a step is successfully calculated and accepted
        if (AudioManager.Instance != null && moveSFX != null)
        {
            AudioManager.Instance.PlaySFX(moveSFX, transform.position, volume);
        }

        targetPosition = centerDest3D;
        isMoving = true;
        transform.forward = direction;
        return true;
    }

    protected override void FinishMovement()
    {
        transform.position = targetPosition;
        isMoving = false;
        nextMoveTime = Time.time + timeBetweenSteps;

        // Double-check alignment precision for all 3 tracked sub-tiles
        UpdateOccupiedTilesMap(transform.position, transform.forward, currentOccupiedKeys);
    }

    protected override void DetermineNextStep()
    {
        Vector3 diff = player.position - transform.position;
        Vector3 primary = Mathf.Abs(diff.x) > Mathf.Abs(diff.z) ?
            new Vector3(Mathf.Sign(diff.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(diff.z));

        // Call our localized multi-tile TryMove instead of base.TryMove
        if (!TryMove(primary))
        {
            Vector3 secondary = (primary.x != 0) ? new Vector3(0, 0, Mathf.Sign(diff.z)) : new Vector3(Mathf.Sign(diff.x), 0, 0);
            TryMove(secondary);
        }
    }

    // --- OVERRIDDEN VOID DETECTION ---

    protected new void CheckForVoid()
    {
        // The boss only falls if its central foundation tile breaks away out from under it
        if (!Physics.Raycast(transform.position + Vector3.up, Vector3.down, 2f, floorLayer))
        {
            isFalling = true;
            ClearEntireFootprint();
        }
    }

    // --- DAMAGE AND LIFECYCLE MANAGEMENT ---

    public override void Die()
    {
        health -= 1;
        Debug.Log($"Boss took damage! Health remaining: {health}");

        if (health <= 0 || isFalling)
        {
            ClearEntireFootprint();
            if (NavigationManager.Instance != null)
            {
                NavigationManager.Instance.OpenCreditsScene();
            }
            else
            {
                // Fallback for scene simulation in the editor
                UnityEngine.SceneManagement.SceneManager.LoadScene("Credits");
            }
            Destroy(gameObject);
        }
    }

    protected override void OnDestroy()
    {
        ClearEntireFootprint();
    }

    // --- FOOTPRINT CALCULATOR UTILITIES ---

    // Fills a target list with the 3 grid coordinate keys mapped to Center, Left, and Right wings
    private void UpdateOccupiedTilesMap(Vector3 centerPos, Vector3 forwardDir, List<Vector2> listToFill)
    {
        listToFill.Clear();

        // Establish structural lookups perpendicular to our current facing view direction
        Vector3 rightOffset = Vector3.Cross(Vector3.up, forwardDir).normalized;

        Vector3 centerTile = centerPos;
        Vector3 leftTile = centerPos - rightOffset;
        Vector3 rightTile = centerPos + rightOffset;

        listToFill.Add(GetGridKey(centerTile));
        listToFill.Add(GetGridKey(leftTile));
        listToFill.Add(GetGridKey(rightTile));
    }

    private void ClearEntireFootprint()
    {
        foreach (Vector2 key in currentOccupiedKeys) OccupiedTiles.Remove(key);
        foreach (Vector2 key in targetOccupiedKeys) OccupiedTiles.Remove(key);
    }
}