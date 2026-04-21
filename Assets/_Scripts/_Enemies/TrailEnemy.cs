using UnityEngine;

public class TrailEnemy : EnemyFollower
{
    [Header("Trail Settings")]
    public GameObject trailPrefab;

    // This "Overrides" the base movement logic
    protected override void DetermineNextStep()
    {
        // Store the position BEFORE we move
        Vector3 spawnPos = transform.position;

        // Run the original movement logic from EnemyFollower
        base.DetermineNextStep();

        // If the enemy successfully started moving, drop a trail at the old spot
        if (isMoving && trailPrefab != null)
        {
            // Spawn trail slightly above floor but below enemy feet
            Instantiate(trailPrefab, new Vector3(spawnPos.x, 0.55f, spawnPos.z), Quaternion.identity);
        }
    }
}