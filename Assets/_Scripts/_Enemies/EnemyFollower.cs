using UnityEngine;

public class EnemyFollower : BaseEnemy
{
    protected override void DetermineNextStep()
    {
        if (player == null) return;

        Vector3 diff = player.position - transform.position;
        Vector3 moveDir = Vector3.zero;

        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.z)) moveDir.x = Mathf.Sign(diff.x);
        else moveDir.z = Mathf.Sign(diff.z);

        // Distance Check for Attack
        if (Vector3.Distance(transform.position + moveDir, player.position) < 0.1f)
        {
            StartCoroutine(AttackLunge(moveDir));
            if (player.TryGetComponent<PlayerController>(out var pc)) pc.TakeDamage();
            nextMoveTime = Time.time + timeBetweenSteps;
            return;
        }

        TryMove(moveDir);
    }
}