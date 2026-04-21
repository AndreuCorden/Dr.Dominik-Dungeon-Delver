using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyFollower : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float timeBetweenSteps = 1.0f; // Enemy waits 1 second between moves

    [Header("Layers")]
    public LayerMask floorLayer;
    public LayerMask blockingLayers;

    protected Vector3 targetPosition;
    protected bool isMoving = false;
    private bool isFalling = false;
    protected float nextMoveTime;
    private Transform player;

    public static HashSet<Vector3> OccupiedTiles = new HashSet<Vector3>();

    protected void Start()
    {
        // Find the player in the scene
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
        if (GameObject.FindObjectsByType<EnemyFollower>(FindObjectsSortMode.None).Length <= 1)
        {
            OccupiedTiles.Clear();
        }

        targetPosition = transform.position;
    }

    private void OnDestroy()
    {
        // If an enemy falls or is killed, make sure it releases its tile claim!
        OccupiedTiles.Remove(targetPosition);
        OccupiedTiles.Remove(transform.position);
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
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                FinishMovement(); // Call the cleanup function here
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

        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.z)) moveDir.x = Mathf.Sign(diff.x);
        else moveDir.z = Mathf.Sign(diff.z);

        Vector3 potentialDest = transform.position + moveDir;
        potentialDest = new Vector3(Mathf.Round(potentialDest.x), potentialDest.y, Mathf.Round(potentialDest.z));

        // 1. Attack check
        if (Vector3.Distance(potentialDest, player.position) < 0.1f)
        {
            // Attack logic... (Lunge/Damage)
            StartCoroutine(AttackLunge(moveDir));
            if (player.TryGetComponent<PlayerController>(out var pc)) pc.TakeDamage();
            nextMoveTime = Time.time + timeBetweenSteps;
            return;
        }

        // 2. Movement Logic
        bool hasFloor = Physics.Raycast(potentialDest + Vector3.up, Vector3.down, 2f, floorLayer);
        bool isClaimed = OccupiedTiles.Contains(potentialDest);

        // Check if ANYONE (Player or Enemy) is physically there
        bool isPhysicallyBlocked = Physics.CheckSphere(potentialDest, 0.4f, blockingLayers);

        if (hasFloor && !isClaimed && !isPhysicallyBlocked)
        {
            // IMPORTANT: Release our current tile so it's free for others
            Vector3 currentTile = new Vector3(Mathf.Round(transform.position.x), transform.position.y, Mathf.Round(transform.position.z));
            OccupiedTiles.Remove(currentTile);

            // Claim the new one
            OccupiedTiles.Add(potentialDest);

            targetPosition = potentialDest;
            isMoving = true;
            transform.forward = moveDir;
        }
    }

    protected virtual void FinishMovement()
    {
        transform.position = targetPosition;
        isMoving = false;
        nextMoveTime = Time.time + timeBetweenSteps;

        // Double check we are still registered at our stop point
        Vector3 currentTile = new Vector3(Mathf.Round(transform.position.x), transform.position.y, Mathf.Round(transform.position.z));
        if (!OccupiedTiles.Contains(currentTile)) OccupiedTiles.Add(currentTile);
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
}