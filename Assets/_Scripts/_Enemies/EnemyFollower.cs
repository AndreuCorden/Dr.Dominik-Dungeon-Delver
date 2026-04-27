using UnityEngine;

public class EnemyFollower : BaseEnemy
{
    protected override void DetermineNextStep()
    {
        if (player == null) return;

        Vector3 diff = player.position - transform.position;
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

        // Check Primary Direction for Attack
        if (IsPlayerInAttackRange(GetRoundedPos(transform.position + primaryDir)))
        {
            ExecuteAttack(primaryDir);
            return;
        }

        // Try to move Primary
        if (!TryMove(primaryDir))
        {
            if (secondaryDir != Vector3.zero)
            {
                // Check Secondary Direction for Attack
                if (IsPlayerInAttackRange(GetRoundedPos(transform.position + secondaryDir)))
                {
                    ExecuteAttack(secondaryDir);
                    return;
                }
                TryMove(secondaryDir);
            }
        }
    }

    protected bool IsPlayerInAttackRange(Vector3 targetCheckPos)
    {
        if (player == null) return false;
        Vector3 roundedPlayer = GetRoundedPos(player.position);
        
        return Mathf.Abs(targetCheckPos.x - roundedPlayer.x) < 0.1f && 
               Mathf.Abs(targetCheckPos.z - roundedPlayer.z) < 0.1f;
    }

    protected void ExecuteAttack(Vector3 dir)
    {
        transform.forward = dir;
        StartCoroutine(AttackLunge(dir));
        if (player.TryGetComponent<PlayerController>(out var pc)) pc.TakeDamage();
        nextMoveTime = Time.time + timeBetweenSteps;
    }
}