using UnityEngine;

public class TrailEnemy : EnemyFollower
{
    [Header("Trail Settings")]
    public GameObject trailPrefab;
    public GameObject slimePrefab;
    [SerializeField] private float trailYOffset = 0.02f;
    [SerializeField] private float trailSlowAmount = 0.5f;
    [SerializeField] private float trailSlowDuration = 2.5f;
    [SerializeField] private float trailLifetime = 5f;
    [SerializeField] private float slimeYOffset = -0.45f;
    [SerializeField] private Vector3 slimeRotationEuler = Vector3.zero;
    [SerializeField] private bool randomizeSlimeYaw = true;

    protected override void DetermineNextStep()
    {
        Vector3 spawnPos = GetGroundedTilePosition(
            Mathf.Round(transform.position.x),
            Mathf.Round(transform.position.z),
            trailYOffset);

        base.DetermineNextStep();

        if (isMoving && trailPrefab != null)
        {
            GameObject spawnedTrail = Instantiate(
                trailPrefab,
                spawnPos,
                Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));

            EnsureTrailDamageSetup(spawnedTrail);
        }
    }

    protected override void FinishMovement()
    {
        if (slimePrefab != null)
        {
            Vector3 spawnPos = GetGroundedTilePosition(
                Mathf.Round(transform.position.x),
                Mathf.Round(transform.position.z),
                slimeYOffset);
            float yaw = randomizeSlimeYaw ? Random.Range(0f, 360f) : 0f;
            Quaternion rotation = Quaternion.Euler(slimeRotationEuler + new Vector3(0f, yaw, 0f));
            Instantiate(slimePrefab, spawnPos, rotation);
        }

        base.FinishMovement();
    }

    private void EnsureTrailDamageSetup(GameObject spawnedTrail)
    {
        TrailDamage trailDamage = spawnedTrail.GetComponent<TrailDamage>();
        if (trailDamage == null)
            trailDamage = spawnedTrail.AddComponent<TrailDamage>();

        trailDamage.slowAmount = trailSlowAmount;
        trailDamage.slowDuration = trailSlowDuration;
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

    private Vector3 GetGroundedTilePosition(float x, float z, float yOffset)
    {
        Vector3 rayOrigin = new Vector3(x, transform.position.y + 3f, z);
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 10f, floorLayer))
            return new Vector3(x, hit.point.y + yOffset, z);

        return new Vector3(x, transform.position.y + yOffset, z);
    }
}
