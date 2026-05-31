using UnityEngine;

public class TrailEnemy : EnemyFollower {
    public GameObject slimePrefab;

    protected override void StartMovement() {
        // Spawn cowweb at current position before moving
        Vector3 spawnPos = new Vector3(Mathf.Round(transform.position.x), transform.position.y, Mathf.Round(transform.position.z));
        Instantiate(slimePrefab, spawnPos - Vector3.up * 0.45f, Quaternion.identity);
        base.StartMovement();
    }
}