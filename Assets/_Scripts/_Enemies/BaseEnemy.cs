using UnityEngine;
using System.Collections;
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

    [Header("Animation")]
    [SerializeField] private Animator animatorOverride;
    [SerializeField] private float deathDestroyDelay = 0.8f;

    protected Vector3 targetPosition;
    protected bool isMoving = false;
    protected bool isFalling = false;
    protected float nextMoveTime;
    protected Transform player;
    private bool isDying = false;

    private readonly List<AnimatorBinding> animatorBindings = new List<AnimatorBinding>();
    private static readonly int IsMovingId = Animator.StringToHash("IsMoving");
    private static readonly int AttackId = Animator.StringToHash("Attack");
    private static readonly int DieId = Animator.StringToHash("Die");
    private static readonly int FallId = Animator.StringToHash("Fall");

    private struct AnimatorBinding
    {
        public Animator Animator;
        public bool HasMove;
        public bool HasAttack;
        public bool HasDie;
        public bool HasFall;
    }

    protected virtual void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;

        // Initial Grid Placement
        targetPosition = RoundToGrid(transform.position);
        transform.position = targetPosition;
        OccupiedTiles.Add(GetGridKey(transform.position));

        CacheAnimators();
        SetMoving(false);
    }

    protected virtual void Update()
    {
        if (isDying) return;
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
            TriggerFall();
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
        TriggerAttack();
        
        if (AudioManager.Instance != null && attackSFX != null)
        {
            AudioManager.Instance.PlaySFX(attackSFX, transform.position, sfxVolume);
        }
        // Trigger Damage to Player and visual lunge here
        if (player.TryGetComponent<PlayerController>(out var pc)) pc.TakeDamage(false, transform.position);
        nextMoveTime = Time.time + timeBetweenSteps;
    }

    protected virtual void StartMovement()
    {
        isMoving = true;
        SetMoving(true);

        if (AudioManager.Instance != null && moveSFX != null)
        {
            AudioManager.Instance.PlaySFX(moveSFX, transform.position, sfxVolume);
        }
    }

    protected virtual void FinishMovement()
    {
        transform.position = targetPosition;
        isMoving = false;
        SetMoving(false);
        nextMoveTime = Time.time + timeBetweenSteps;
    }

    protected Vector2 GetGridKey(Vector3 pos) => new Vector2(Mathf.Round(pos.x), Mathf.Round(pos.z));
    protected Vector3 RoundToGrid(Vector3 pos) => new Vector3(Mathf.Round(pos.x), transform.position.y, Mathf.Round(pos.z));

    public virtual void Die()
    {
        if (isDying) return;
        isDying = true;
        bool wasMoving = isMoving;
        isMoving = false;
        SetMoving(false);
        TriggerDie();

        // --- ADDED: PLAY DEATH SFX BEFORE DESTRUCTION ---
        if (AudioManager.Instance != null && dieSFX != null)
        {
            AudioManager.Instance.PlaySFX(dieSFX, transform.position, sfxVolume);
        }

        OccupiedTiles.Remove(GetGridKey(transform.position));
        if (wasMoving) OccupiedTiles.Remove(GetGridKey(targetPosition));
        DisableColliders();

        if (HasAnyDieParameter() && deathDestroyDelay > 0f)
        {
            StartCoroutine(DestroyAfterDelay());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        OccupiedTiles.Remove(GetGridKey(transform.position));
        if (isMoving) OccupiedTiles.Remove(GetGridKey(targetPosition));
    }

    private void CacheAnimators()
    {
        animatorBindings.Clear();

        Animator[] animators = animatorOverride != null
            ? new[] { animatorOverride }
            : GetComponentsInChildren<Animator>(true);

        foreach (var anim in animators)
        {
            if (anim == null) continue;

            var binding = new AnimatorBinding { Animator = anim };
            foreach (var param in anim.parameters)
            {
                switch (param.name)
                {
                    case "IsMoving":
                        binding.HasMove = true;
                        break;
                    case "Attack":
                        binding.HasAttack = true;
                        break;
                    case "Die":
                        binding.HasDie = true;
                        break;
                    case "Fall":
                        binding.HasFall = true;
                        break;
                }
            }

            animatorBindings.Add(binding);
        }
    }

    private void SetMoving(bool moving)
    {
        foreach (var binding in animatorBindings)
        {
            if (binding.HasMove) binding.Animator.SetBool(IsMovingId, moving);
        }
    }

    private void TriggerAttack()
    {
        foreach (var binding in animatorBindings)
        {
            if (binding.HasAttack) binding.Animator.SetTrigger(AttackId);
        }
    }

    private void TriggerDie()
    {
        foreach (var binding in animatorBindings)
        {
            if (binding.HasDie) binding.Animator.SetTrigger(DieId);
        }
    }

    private void TriggerFall()
    {
        foreach (var binding in animatorBindings)
        {
            if (binding.HasFall) binding.Animator.SetTrigger(FallId);
        }
    }

    private bool HasAnyDieParameter()
    {
        foreach (var binding in animatorBindings)
        {
            if (binding.HasDie) return true;
        }
        return false;
    }

    private void DisableColliders()
    {
        foreach (var col in GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(deathDestroyDelay);
        Destroy(gameObject);
    }
}