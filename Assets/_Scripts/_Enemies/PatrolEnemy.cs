using UnityEngine;

public class PatrolEnemy : EnemyFollower
{
    [Header("Patrol Settings")]
    public int patrolDistance = 4; // How many tiles to move before turning
    public bool moveOnXAxis = true; // True = Left/Right, False = Forward/Back

    private Vector3 pointA;
    private Vector3 pointB;
    private Vector3 currentPatrolTarget;

    // Use 'new' to hide the base Start so we can set up patrol points
    new void Start()
    {
        base.Start();

        pointA = transform.position;

        // Calculate Point B based on the distance and axis chosen
        if (moveOnXAxis)
            pointB = pointA + new Vector3(patrolDistance, 0, 0);
        else
            pointB = pointA + new Vector3(0, 0, patrolDistance);

        currentPatrolTarget = pointB;
    }

    protected override void DetermineNextStep()
    {
        // 1. Check if we reached the current end of the patrol
        if (Vector3.Distance(transform.position, currentPatrolTarget) < 0.1f)
        {
            currentPatrolTarget = (currentPatrolTarget == pointA) ? pointB : pointA;
        }

        // 2. Calculate direction
        Vector3 diff = currentPatrolTarget - transform.position;
        Vector3 moveDir = new Vector3(Mathf.Sign(diff.x), 0, Mathf.Sign(diff.z));
        if (moveOnXAxis) moveDir.z = 0; else moveDir.x = 0;

        Vector3 potentialDest = transform.position + moveDir;
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null && Vector3.Distance(potentialDest, playerObj.transform.position) < 0.1f)
        {
            transform.forward = moveDir;
            if (playerObj.TryGetComponent<PlayerController>(out PlayerController pc))
            {
                pc.TakeDamage(); // Hits the player
                StartCoroutine(AttackLunge(moveDir)); // Plays the animation
            }
            nextMoveTime = Time.time + timeBetweenSteps;
            return;
        }

        // 3. Standard movement check (Floor detection)
        if (Physics.Raycast(potentialDest + Vector3.up, Vector3.down, 2f, floorLayer))
        {
            targetPosition = potentialDest;
            isMoving = true;
            transform.forward = moveDir;
        }
        else
        {
            currentPatrolTarget = (currentPatrolTarget == pointA) ? pointB : pointA;
        }
    }
}