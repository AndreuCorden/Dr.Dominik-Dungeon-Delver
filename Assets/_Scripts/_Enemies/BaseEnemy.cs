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

    [Header("Audio Configurations")]
    [SerializeField] protected AudioClip moveSFX;
    [SerializeField] protected AudioClip attackSFX;
    // --- NEW: DEATH SFX FIELD ---
    [SerializeField] protected AudioClip dieSFX; 
    [SerializeField] [Range(0f, 1f)] protected float sfxVolume = 0.8f;

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
        if (transform.position.y < -10f) Die(); // Direct redirection to clean up data properly on drop fall out
    }

    protected bool TryMove(Vector3 direction)
    {
        Vector3 dest3D = RoundToGrid(transform.position + direction);

        if (GetGridKey(player.position) == GetGridKey(dest3D))
        {
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
            transform.forward = direction;
            StartMovement();
            return true;
        }
        return false;
    }

    protected void PerformAttack(Vector3 dir)
    {
        transform.forward = dir;
        
        if (AudioManager.Instance != null && attackSFX != null)
        {
            AudioManager.Instance.PlaySFX(attackSFX, transform.position, sfxVolume);
        }

        if (player.TryGetComponent<PlayerController>(out var pc)) pc.TakeDamage();
        nextMoveTime = Time.time + timeBetweenSteps;
    }

    protected virtual void StartMovement()
    {
        isMoving = true;

        if (AudioManager.Instance != null && moveSFX != null)
        {
            AudioManager.Instance.PlaySFX(moveSFX, transform.position, sfxVolume);
        }
    }

    protected virtual void FinishMovement()
    {
        transform.position = targetPosition;
        isMoving = false;
        nextMoveTime = Time.time + timeBetweenSteps;
    }

    protected Vector2 GetGridKey(Vector3 pos) => new Vector2(Mathf.Round(pos.x), Mathf.Round(pos.z));
    protected Vector3 RoundToGrid(Vector3 pos) => new Vector3(Mathf.Round(pos.x), transform.position.y, Mathf.Round(pos.z));

    public virtual void Die()
    {
        // --- ADDED: PLAY DEATH SFX BEFORE DESTRUCTION ---
        if (AudioManager.Instance != null && dieSFX != null)
        {
            AudioManager.Instance.PlaySFX(dieSFX, transform.position, sfxVolume);
        }

        OccupiedTiles.Remove(GetGridKey(transform.position));
        if (isMoving) OccupiedTiles.Remove(GetGridKey(targetPosition));
        Destroy(gameObject);
    }

    protected virtual void OnDestroy()
    {
        OccupiedTiles.Remove(GetGridKey(transform.position));
        if (isMoving) OccupiedTiles.Remove(GetGridKey(targetPosition));
    }
}