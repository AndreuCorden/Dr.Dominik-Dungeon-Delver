using UnityEngine;

public class EnemyFollower : BaseEnemy
{
    protected override void DetermineNextStep()
    {
        if (player == null) return;

        Vector3 diff = player.position - transform.position;

        // Step 1: Figure out which direction is the "Best" (Primary) and "Second Best" (Secondary)
        Vector3 primaryDir = Vector3.zero;
        Vector3 secondaryDir = Vector3.zero;

        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.z))
        {
            primaryDir.x = Mathf.Sign(diff.x);
            secondaryDir.z = Mathf.Sign(diff.z);
        }
        else
        {
            primaryDir.z = Mathf.Sign(diff.z);
            secondaryDir.x = Mathf.Sign(diff.x);
        }

        // Step 2: Check for Attack in the Primary direction
        if (Vector3.Distance(transform.position + primaryDir, player.position) < 0.1f)
        {
            ExecuteAttack(primaryDir);
            return;
        }

        // Step 3: TRY TO MOVE
        // Try the best path first...
        if (!TryMove(primaryDir))
        {
            // If the Gargoyle (or another enemy) blocks the best path, try the secondary path!
            if (secondaryDir != Vector3.zero)
            {
                // Check for attack in secondary direction too
                if (Vector3.Distance(transform.position + secondaryDir, player.position) < 0.1f)
                {
                    ExecuteAttack(secondaryDir);
                    return;
                }

                TryMove(secondaryDir);
            }
        }
    }

    private void ExecuteAttack(Vector3 dir)
    {
        StartCoroutine(AttackLunge(dir));
        if (player.TryGetComponent<PlayerController>(out var pc)) pc.TakeDamage();
        nextMoveTime = Time.time + timeBetweenSteps;
    }
}