using UnityEngine;

public class TrailEnemy : EnemyFollower
{
    [Header("Trail Settings")]
    public GameObject trailPrefab;
    public GameObject slimePrefab;
    [SerializeField] private float trailYOffset = 0.02f;
    [SerializeField] private float trailSlowAmount = 0.5f;
    [SerializeField] private float trailLifetime = 5f;
    [SerializeField] private float slimeYOffset = -0.45f;
    [SerializeField] private Vector3 slimeRotationEuler = Vector3.zero;
    [SerializeField] private bool randomizeSlimeYaw = true;

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

    protected override void FinishMovement()
    {
        if (slimePrefab != null)
        {
            Vector3 spawnPos = new Vector3(
                Mathf.Round(transform.position.x),
                transform.position.y,
                Mathf.Round(transform.position.z));
            float yaw = randomizeSlimeYaw ? Random.Range(0f, 360f) : 0f;
            Quaternion rotation = Quaternion.Euler(slimeRotationEuler + new Vector3(0f, yaw, 0f));
            Instantiate(slimePrefab, spawnPos + Vector3.up * slimeYOffset, rotation);
        }

        base.FinishMovement();
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
