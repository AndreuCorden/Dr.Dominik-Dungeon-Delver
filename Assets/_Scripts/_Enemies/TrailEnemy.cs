using UnityEngine;

public class TrailEnemy : EnemyFollower
{
    [Header("Trail Settings")]
    public GameObject trailPrefab;

    protected override void DetermineNextStep()
    {
        Vector3 spawnPos = transform.position;

        base.DetermineNextStep();

        if (isMoving && trailPrefab != null)
        {
            Instantiate(trailPrefab, new Vector3(spawnPos.x, 0.55f, spawnPos.z), Quaternion.identity);
        }
    }
}