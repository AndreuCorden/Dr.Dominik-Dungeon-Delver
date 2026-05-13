using UnityEngine;

public class MimicEnemy : EnemyFollower {
    public float wakeRange = 2f;

    public GameObject chestModel; // Assign in Inspector
    public GameObject mimicModel; // Assign in Inspector
    private bool isAwake = false;
    protected override void DetermineNextStep() {
        if (!isAwake) {
            if (Vector3.Distance(transform.position, player.position) <= wakeRange)
            {
              isAwake = true;
                chestModel.SetActive(false);
                mimicModel.SetActive(true);  
            } 
            return;
        }
        base.DetermineNextStep(); // Acts as a Follower once awake
    }
}