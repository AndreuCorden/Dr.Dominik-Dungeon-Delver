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

    protected virtual void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        // Reset grid on first spawn
        if (GameObject.FindObjectsByType<BaseEnemy>(FindObjectsSortMode.None).Length <= 1)
        {
            OccupiedTiles.Clear();
        }

        targetPosition = transform.position;
        // Claim starting spot
        OccupiedTiles.Add(new Vector3(Mathf.Round(transform.position.x), transform.position.y, Mathf.Round(transform.position.z)));
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

    // Every enemy must define how it chooses its next tile
    protected abstract void DetermineNextStep();

    protected virtual void FinishMovement()
    {
        transform.position = targetPosition;
        isMoving = false;
        nextMoveTime = Time.time + timeBetweenSteps;

        Vector3 currentTile = new Vector3(Mathf.Round(transform.position.x), transform.position.y, Mathf.Round(transform.position.z));
        if (!OccupiedTiles.Contains(currentTile)) OccupiedTiles.Add(currentTile);
    }

    protected bool TryMove(Vector3 direction)
    {
        Vector3 potentialDest = transform.position + direction;
        potentialDest = new Vector3(Mathf.Round(potentialDest.x), potentialDest.y, Mathf.Round(potentialDest.z));

        bool hasFloor = Physics.Raycast(potentialDest + Vector3.up, Vector3.down, 2f, floorLayer);
        bool isClaimed = OccupiedTiles.Contains(potentialDest);
        bool isPhysicallyBlocked = Physics.CheckSphere(potentialDest, 0.4f, blockingLayers);

        if (hasFloor && !isClaimed && !isPhysicallyBlocked)
        {
            OccupiedTiles.Remove(new Vector3(Mathf.Round(transform.position.x), transform.position.y, Mathf.Round(transform.position.z)));
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

    private void CheckForVoid()
    {
        if (!Physics.Raycast(transform.position + Vector3.up, Vector3.down, 2f, floorLayer)) isFalling = true;
    }

    private void HandleFalling()
    {
        transform.Translate(Vector3.down * Time.deltaTime * 10f, Space.World);
        transform.Rotate(Vector3.up * Time.deltaTime * 200f);
        if (transform.position.y < -10f) Die();
    }

    public void Die()
    {
        OccupiedTiles.Remove(new Vector3(Mathf.Round(transform.position.x), transform.position.y, Mathf.Round(transform.position.z)));
        OccupiedTiles.Remove(targetPosition);
        Destroy(gameObject);
    }

    protected virtual void OnDestroy()
    {
        // Safety cleanup: removes both the current visual position and the intended target position
        Vector3 currentPos = new Vector3(Mathf.Round(transform.position.x), transform.position.y, Mathf.Round(transform.position.z));
        OccupiedTiles.Remove(currentPos);
        OccupiedTiles.Remove(targetPosition);
    }
}