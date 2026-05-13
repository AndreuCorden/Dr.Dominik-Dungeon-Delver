using UnityEngine;

public class TrailEnemy : EnemyFollower
{
    [Header("Trail Settings")]
    public GameObject trailPrefab;
    [SerializeField] private float trailYOffset = 0.02f;
    [SerializeField] private float trailSlowAmount = 0.5f;
    [SerializeField] private float trailLifetime = 5f;

    protected override void DetermineNextStep()
    {
        Vector3 spawnPos = transform.position;

        base.DetermineNextStep();

        if (isMoving && trailPrefab != null)
        {
            GameObject spawnedTrail = Instantiate(
                trailPrefab,
                new Vector3(spawnPos.x, trailYOffset, spawnPos.z),
                Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

            EnsureTrailDamageSetup(spawnedTrail);
        }
    }

    private void EnsureTrailDamageSetup(GameObject spawnedTrail)
    {
        TrailDamage trailDamage = spawnedTrail.GetComponent<TrailDamage>();
        if (trailDamage == null)
            trailDamage = spawnedTrail.AddComponent<TrailDamage>();

        trailDamage.slowAmount = trailSlowAmount;
        trailDamage.lifetime = trailLifetime;
        trailDamage.floorLayer = floorLayer;

        Collider trailCollider = spawnedTrail.GetComponent<Collider>();
        if (trailCollider == null)
            trailCollider = spawnedTrail.AddComponent<BoxCollider>();
        trailCollider.isTrigger = true;

        Rigidbody rb = spawnedTrail.GetComponent<Rigidbody>();
        if (rb == null)
            rb = spawnedTrail.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }
}
