using UnityEngine;

public class MimicEnemy : EnemyFollower {
    public float wakeRange = 2f;
    private bool isAwake = false;
    protected override void DetermineNextStep() {
        if (!isAwake) {
            if (Vector3.Distance(transform.position, player.position) <= wakeRange) isAwake = true;
            return;
        }
        base.DetermineNextStep(); // Acts as a Follower once awake
    }
}