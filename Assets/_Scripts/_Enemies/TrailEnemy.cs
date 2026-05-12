using UnityEngine;

public class TrailEnemy : EnemyFollower {
    public GameObject slimePrefab;
    protected override void FinishMovement() {
        // Drop slime at previous position before calling base
        Vector3 spawnPos = new Vector3(Mathf.Round(transform.position.x), transform.position.y, Mathf.Round(transform.position.z));
        Instantiate(slimePrefab, spawnPos - Vector3.up*0.45f, Quaternion.identity);
        base.FinishMovement();
    }
}