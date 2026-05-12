using UnityEngine;

public class EnemyFollower : BaseEnemy {
    protected override void DetermineNextStep() {
        Vector3 diff = player.position - transform.position;
        Vector3 primary = Mathf.Abs(diff.x) > Mathf.Abs(diff.z) ? 
            new Vector3(Mathf.Sign(diff.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(diff.z));
        
        if (!TryMove(primary)) {
            Vector3 secondary = (primary.x != 0) ? new Vector3(0, 0, Mathf.Sign(diff.z)) : new Vector3(Mathf.Sign(diff.x), 0, 0);
            TryMove(secondary);
        }
    }
}