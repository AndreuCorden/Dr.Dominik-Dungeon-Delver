using UnityEngine;
using System.Collections;

public class EnemyFollower : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float timeBetweenSteps = 1.0f; // Enemy waits 1 second between moves
    public LayerMask floorLayer;

    protected Vector3 targetPosition;
    protected bool isMoving = false;
    private bool isFalling = false;
    protected float nextMoveTime;
    private Transform player;

    protected void Start()
    {
        // Find the player in the scene
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        targetPosition = transform.position;
    }

    void Update()
    {
        if (isFalling)
        {
            HandleFalling();
            return;
        }

        if (isMoving)
        {
            // Smoothly slide to the next grid tile
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                isMoving = false;
                nextMoveTime = Time.time + timeBetweenSteps; // Set wait time for next step
            }
        }
        else
        {
            CheckForVoid();

            // Only try to move if the cooldown is over and player exists
            if (Time.time >= nextMoveTime && player != null)
            {
                DetermineNextStep();
            }
        }
    }

    protected virtual void DetermineNextStep()
    {
        Vector3 diff = player.position - transform.position;
        Vector3 moveDir = Vector3.zero;

        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.z))
            moveDir.x = Mathf.Sign(diff.x);
        else
            moveDir.z = Mathf.Sign(diff.z);

        Vector3 potentialDest = transform.position + moveDir;

        // 1. Check if the player is already occupying that specific tile
        // We use a small distance check (0.1f) to see if the positions match
        if (Vector3.Distance(potentialDest, player.position) < 0.1f)
        {
            // ATTACK: Don't move into the tile, just face the player and deal damage
            transform.forward = moveDir;

            if (player.TryGetComponent<PlayerController>(out PlayerController pc))
            {
                pc.TakeDamage(); // Deal damage without moving
                StartCoroutine(AttackLunge(moveDir));
            }

            // Put the movement on cooldown so they don't spam damage every frame
            nextMoveTime = Time.time + timeBetweenSteps;
            return;
        }

        // 2. Only move if the tile is unoccupied and has a floor
        if (Physics.Raycast(potentialDest + Vector3.up, Vector3.down, 2f, floorLayer))
        {
            targetPosition = potentialDest;
            isMoving = true;
            transform.forward = moveDir;
        }
    }

    protected IEnumerator AttackLunge(Vector3 dir)
    {
        Vector3 originalPos = transform.position;
        Vector3 lungePos = originalPos + (dir * 0.3f); // Move 30% into the gap

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 15f;
            transform.position = Vector3.Lerp(originalPos, lungePos, Mathf.PingPong(t, 1));
            yield return null;
        }
        transform.position = originalPos;
    }

    void CheckForVoid()
    {
        // If no floor is detected directly beneath the enemy
        if (!Physics.Raycast(transform.position + Vector3.up, Vector3.down, 2f, floorLayer))
        {
            isFalling = true;
        }
    }

    void HandleFalling()
    {
        transform.Translate(Vector3.down * Time.deltaTime * 10f, Space.World);
        transform.Rotate(Vector3.up * Time.deltaTime * 200f);

        if (transform.position.y < -10f)
        {
            Destroy(gameObject);
        }
    }

    protected void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Player"))
    {
        PlayerController pc = other.GetComponent<PlayerController>();
        if (pc != null) pc.TakeDamage(); // Remember: TakeDamage() defaults to false (no reload)
    }
}
}