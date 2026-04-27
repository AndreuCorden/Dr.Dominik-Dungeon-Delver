using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class BaseEnemy : MonoBehaviour
{
    [Header("Base Settings")]
    public float moveSpeed = 5f;
    public float timeBetweenSteps = 1.0f;
    public LayerMask floorLayer;
    public LayerMask blockingLayers;

    protected Vector3 targetPosition;
    protected bool isMoving = false;
    protected bool isFalling = false;
    protected float nextMoveTime;
    protected Transform player;

    public static HashSet<Vector3> OccupiedTiles = new HashSet<Vector3>();

    protected Vector3 GetRoundedPos(Vector3 pos)
    {
        // Preserve original Y height, round X and Z for the grid
        return new Vector3(Mathf.Round(pos.x), pos.y, Mathf.Round(pos.z));
    }

    protected virtual void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        transform.position = GetRoundedPos(transform.position);
        targetPosition = transform.position;

        Vector3 currentTile = GetRoundedPos(transform.position);
        if (!OccupiedTiles.Contains(currentTile)) OccupiedTiles.Add(currentTile);
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
            CheckForVoid();
            if (Time.time >= nextMoveTime) DetermineNextStep();
        }
    }

    protected abstract void DetermineNextStep();

    protected virtual void FinishMovement()
    {
        transform.position = targetPosition;
        isMoving = false;
        nextMoveTime = Time.time + timeBetweenSteps;

        Vector3 currentTile = GetRoundedPos(transform.position);
        if (!OccupiedTiles.Contains(currentTile)) OccupiedTiles.Add(currentTile);
    }

    protected bool TryMove(Vector3 direction)
    {
        Vector3 potentialDest = GetRoundedPos(transform.position + direction);

        bool hasFloor = Physics.Raycast(potentialDest + Vector3.up, Vector3.down, 2f, floorLayer);
        bool isClaimed = OccupiedTiles.Contains(potentialDest);
        bool isPhysicallyBlocked = Physics.CheckSphere(potentialDest + (Vector3.up * 0.5f), 0.3f, blockingLayers);

        bool isPlayerInWay = false;
        if (player != null)
        {
            Vector3 roundedPlayerPos = GetRoundedPos(player.position);
            if (Mathf.Abs(potentialDest.x - roundedPlayerPos.x) < 0.1f &&
                Mathf.Abs(potentialDest.z - roundedPlayerPos.z) < 0.1f)
            {
                isPlayerInWay = true;
            }
        }

        if (hasFloor && !isClaimed && !isPhysicallyBlocked && !isPlayerInWay)
        {
            OccupiedTiles.Remove(GetRoundedPos(transform.position));
            OccupiedTiles.Add(potentialDest);

            targetPosition = potentialDest;
            isMoving = true;
            transform.forward = direction;
            return true;
        }
        return false;
    }

    protected IEnumerator AttackLunge(Vector3 dir)
    {
        Vector3 originalPos = transform.position;
        Vector3 lungePos = originalPos + (dir * 0.3f);
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 15f;
            transform.position = Vector3.Lerp(originalPos, lungePos, Mathf.PingPong(t, 1));
            yield return null;
        }
        transform.position = originalPos;
    }

    protected void CheckForVoid()
    {
        if (!Physics.Raycast(transform.position + Vector3.up, Vector3.down, 2f, floorLayer)) isFalling = true;
    }

    protected void HandleFalling()
    {
        transform.Translate(Vector3.down * Time.deltaTime * 10f, Space.World);
        transform.Rotate(Vector3.up * Time.deltaTime * 200f);
        if (transform.position.y < -10f) Die();
    }

    public void Die()
{
    // 1. Clear Grid logic
    Vector3 gridPos = GetRoundedPos(transform.position);
    OccupiedTiles.Remove(gridPos);

    // 2. Destroy
    // The PressurePlate.Update() will see this object is null and reset itself.
    Destroy(gameObject);
}

    protected virtual void OnDestroy()
    {
        OccupiedTiles.Remove(GetRoundedPos(transform.position));
        OccupiedTiles.Remove(GetRoundedPos(targetPosition));
    }
}