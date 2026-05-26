using UnityEngine;
using System.Collections.Generic;

public abstract class BaseEnemy : MonoBehaviour
{
    // The shared map for ALL enemies and the player
    public static HashSet<Vector2> OccupiedTiles = new HashSet<Vector2>();

    [Header("Base Settings")]
    public float moveSpeed = 5f;
    public float timeBetweenSteps = 1f;
    public LayerMask floorLayer;
    public LayerMask blockingLayers;

    protected Vector3 targetPosition;
    protected bool isMoving = false;
    protected bool isFalling = false;
    protected float nextMoveTime;
    protected Transform player;

    protected virtual void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;

        // Initial Grid Placement
        targetPosition = RoundToGrid(transform.position);
        transform.position = targetPosition;
        OccupiedTiles.Add(GetGridKey(transform.position));
    }

    protected virtual void Update()
    {
        if (isFalling) { HandleFalling(); return; }

        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f) FinishMovement();
        }
        else
        {
            CheckForVoid(); // Always check if floor exists beneath feet
            if (!isFalling && Time.time >= nextMoveTime) DetermineNextStep();
        }
    }

    protected abstract void DetermineNextStep();

    // --- SHARED LOGIC ---

    protected void CheckForVoid()
    {
        // If no floor is hit by a raycast downward, trigger falling
        if (!Physics.Raycast(transform.position + Vector3.up, Vector3.down, 2f, floorLayer))
        {
            isFalling = true;
            OccupiedTiles.Remove(GetGridKey(transform.position));
        }
    }

    protected void HandleFalling()
    {
        transform.Translate(Vector3.down * Time.deltaTime * 10f, Space.World);
        transform.Rotate(Vector3.up * Time.deltaTime * 200f);
        if (transform.position.y < -10f) Destroy(gameObject);
    }

    protected bool TryMove(Vector3 direction)
    {
        Vector3 dest3D = RoundToGrid(transform.position + direction);

        // Check if player is standing exactly where we want to go (Attack Range)
        if (GetGridKey(player.position) == GetGridKey(dest3D))
        {
            StartMovement();
            PerformAttack(direction);
            return true;
        }

        bool hasFloor = Physics.Raycast(dest3D + Vector3.up, Vector3.down, 2f, floorLayer);
        bool isOccupied = OccupiedTiles.Contains(GetGridKey(dest3D));

        if (hasFloor && !isOccupied)
        {
            OccupiedTiles.Remove(GetGridKey(transform.position));
            OccupiedTiles.Add(GetGridKey(dest3D));
            targetPosition = dest3D;
            isMoving = true;
            transform.forward = direction;
            StartMovement();
            return true;
        }
        return false;
    }

    protected void PerformAttack(Vector3 dir)
    {
        transform.forward = dir;
        // Trigger Damage to Player and visual lunge here
        if (player.TryGetComponent<PlayerController>(out var pc)) pc.TakeDamage(false, transform.position);
        nextMoveTime = Time.time + timeBetweenSteps;
    }

    protected virtual void StartMovement()
    {
        isMoving = true;
    }

    protected virtual void FinishMovement()
    {
        transform.position = targetPosition;
        isMoving = false;
        nextMoveTime = Time.time + timeBetweenSteps;
    }

    // Utility
    protected Vector2 GetGridKey(Vector3 pos) => new Vector2(Mathf.Round(pos.x), Mathf.Round(pos.z));
    protected Vector3 RoundToGrid(Vector3 pos) => new Vector3(Mathf.Round(pos.x), transform.position.y, Mathf.Round(pos.z));

    public virtual void Die()
    {
        // 1. Remove the current position
        OccupiedTiles.Remove(GetGridKey(transform.position));

        // 2. IMPORTANT: Remove the target position if we were moving toward it
        if (isMoving)
        {
            OccupiedTiles.Remove(GetGridKey(targetPosition));
        }

        Destroy(gameObject);
    }

    protected virtual void OnDestroy()
    {
        // Final safety check to ensure this enemy NEVER leaves a ghost tile
        OccupiedTiles.Remove(GetGridKey(transform.position));
        if (isMoving) OccupiedTiles.Remove(GetGridKey(targetPosition));
    }
}