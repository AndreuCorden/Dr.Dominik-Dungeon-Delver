using UnityEngine;

public class PatrolEnemy : BaseEnemy
{
    [Header("Patrol Settings")]
    public int patrolDistance = 4;
    public bool moveOnXAxis = true;

    private Vector3 pointA;
    private Vector3 pointB;
    private Vector3 currentPatrolTarget;

    protected override void Start()
    {
        base.Start();
        pointA = transform.position;
        pointB = pointA + (moveOnXAxis ? new Vector3(patrolDistance, 0, 0) : new Vector3(0, 0, patrolDistance));
        currentPatrolTarget = pointB;
    }

    protected override void DetermineNextStep()
    {
        if (Vector3.Distance(transform.position, currentPatrolTarget) < 0.1f)
        {
            currentPatrolTarget = (currentPatrolTarget == pointA) ? pointB : pointA;
        }

        Vector3 diff = currentPatrolTarget - transform.position;
        Vector3 moveDir = new Vector3(Mathf.Sign(diff.x), 0, Mathf.Sign(diff.z));
        if (moveOnXAxis) moveDir.z = 0; else moveDir.x = 0;

        // Attack check
        if (player != null && Vector3.Distance(transform.position + moveDir, player.position) < 0.1f)
        {
            transform.forward = moveDir;
            if (player.TryGetComponent<PlayerController>(out PlayerController pc))
            {
                pc.TakeDamage();
                StartCoroutine(AttackLunge(moveDir));
            }
            nextMoveTime = Time.time + timeBetweenSteps;
            return;
        }

        if (!TryMove(moveDir))
        {
            // If blocked, flip target early
            currentPatrolTarget = (currentPatrolTarget == pointA) ? pointB : pointA;
        }
    }
}