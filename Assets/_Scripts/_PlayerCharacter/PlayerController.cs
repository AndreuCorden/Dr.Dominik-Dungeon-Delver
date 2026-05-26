using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance;
    public static event Action<int> OnHealthChanged;
    public static event Action<int> OnCoinsChanged;

    public int health = 3;
    public int coins = 0;
    public float moveSpeed = 5f;

    [Header("Visual")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float visualYOffset = 0f;
    [SerializeField] private float jumpHeight = 0.35f;
    [SerializeField] private float turnSpeed = 720f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    private Vector3 visualRootInitialLocalPosition;
    private float movementVisualYOffset;
    private Quaternion targetRotation;

    [Header("Status Effects")]
    public float currentMoveMultiplier = 1.0f;

    [Header("Attack Settings")]
    public float attackRange = 1.1f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string hitTrigger = "Hit";
    [SerializeField] private string attackTrigger = "Attack";

    [Header("VFX")]
    public GameObject shockwavePrefab;

    [Header("Layers")]
    public LayerMask floorLayer;

    private Vector3 targetPosition;
    private Vector3 moveStartPosition;
    private float moveTimer;
    private float moveDuration;
    private bool isMoving = false;
    private bool isStepMoving = false;
    private bool isFalling = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        if (visualRoot == null)
        {
            visualRoot = GetFirstChildTransform(GetComponentsInChildren<Animator>(true));
            if (visualRoot == null)
                visualRoot = GetFirstChildTransform(GetComponentsInChildren<SkinnedMeshRenderer>(true));
            if (visualRoot == null)
                visualRoot = GetFirstChildTransform(GetComponentsInChildren<MeshRenderer>(true));
        }

        if (visualRoot != null)
            visualRootInitialLocalPosition = visualRoot.localPosition;

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        targetPosition = RoundGridPosition(transform.position);
        moveStartPosition = targetPosition;
        transform.position = targetPosition;
        targetRotation = transform.rotation;

        BaseEnemy.OccupiedTiles.Add(new Vector2(targetPosition.x, targetPosition.z));
    }

    void LateUpdate()
    {
        if (visualRoot != null)
            visualRoot.localPosition = visualRootInitialLocalPosition + new Vector3(0f, visualYOffset + movementVisualYOffset, 0f);
    }

    Transform GetFirstChildTransform(Component[] components)
    {
        foreach (var component in components)
        {
            if (component == null || component.transform == transform)
                continue;

            Transform candidate = component.transform;
            while (candidate.parent != null && candidate.parent != transform)
                candidate = candidate.parent;

            return candidate;
        }
        return null;
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (!isMoving && kb != null)
        {
            if (kb.spaceKey.wasPressedThisFrame)
            {
                PerformSpaceAttack();
            }
            else
            {
                Vector3 direction = GetHeldMoveDirection(kb);
                if (direction != Vector3.zero && IsDestinationSafe(direction))
                    Move(direction);
            }
        }
        if (!isMoving)
            CheckForVoid();
        UpdateMovementPosition();
        UpdateRotation();

        if (isStepMoving)
            UpdateStepJumpOffset();
        else
            movementVisualYOffset = 0f;

        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            transform.position = targetPosition;
            isMoving = false;
            isStepMoving = false;
            movementVisualYOffset = 0f;

            ResetSpeedIfNoSlime();
        }
    }

    Vector3 GetHeldMoveDirection(Keyboard kb)
    {
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) return Vector3.forward;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) return Vector3.back;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) return Vector3.left;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) return Vector3.right;
        return Vector3.zero;
    }

    void UpdateMovementPosition()
    {
        if (isFalling)
            return;

        if (!isMoving)
        {
            transform.position = targetPosition;
            return;
        }

        moveTimer += Time.deltaTime;
        float normalizedTime = moveDuration <= Mathf.Epsilon ? 1f : Mathf.Clamp01(moveTimer / moveDuration);
        float curvedTime = moveCurve != null ? moveCurve.Evaluate(normalizedTime) : normalizedTime;
        transform.position = Vector3.LerpUnclamped(moveStartPosition, targetPosition, curvedTime);
    }

    void UpdateRotation()
    {
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    void UpdateStepJumpOffset()
    {
        if (moveDuration <= Mathf.Epsilon)
        {
            movementVisualYOffset = 0f;
            return;
        }

        float progress = Mathf.Clamp01(moveTimer / moveDuration);
        float parabola = 4f * progress * (1f - progress);
        movementVisualYOffset = parabola * jumpHeight;
    }
    bool IsDestinationSafe(Vector3 direction)
    {
        Vector3 dest = RoundGridPosition(targetPosition + direction);
        Ray ray = new Ray(new Vector3(dest.x, 5.0f, dest.z), Vector3.down);

        if (!Physics.Raycast(ray, out _, 6.0f, floorLayer))
        {
            Debug.Log($"Movement Blocked: No floor detected at {dest}");
            return false;
        }
        if (BaseEnemy.OccupiedTiles.Contains(new Vector2(dest.x, dest.z)))
            return false;
        return true;
    }
    void Move(Vector3 direction)
    {
        Vector3 dest = RoundGridPosition(targetPosition + direction);

        currentMoveMultiplier = CheckForSlimeAt(dest) ? 0.5f : 1.0f;

        BaseEnemy.OccupiedTiles.Remove(new Vector2(targetPosition.x, targetPosition.z));

        moveStartPosition = targetPosition;
        targetPosition = dest;
        BaseEnemy.OccupiedTiles.Add(new Vector2(targetPosition.x, targetPosition.z));
        isMoving = true;
        isStepMoving = true;
        moveTimer = 0f;

        float effectiveSpeed = moveSpeed * currentMoveMultiplier;
        moveDuration = Vector3.Distance(moveStartPosition, targetPosition) / Mathf.Max(effectiveSpeed, 0.01f);
        targetRotation = Quaternion.LookRotation(direction, Vector3.up);
    }
    void CheckForVoid()
    {
        if (isFalling) return;
        Ray ray = new Ray(transform.position, Vector3.down);
        if (!Physics.Raycast(ray, out _, 1.1f, floorLayer))
            StartCoroutine(HandleFallingDeath());
    }

    System.Collections.IEnumerator HandleFallingDeath()
    {
        isFalling = true;
        isMoving = true;
        isStepMoving = false;
        movementVisualYOffset = 0f;
        float fallTimer = 0f;
        while (fallTimer < 1.0f)
        {
            transform.Translate(Vector3.down * Time.deltaTime * 10f);
            fallTimer += Time.deltaTime;
            yield return null;
        }

        isFalling = false;
        isMoving = false;
        TakeDamage(true);
    }

    public void TakeDamage(bool isFall = false, Vector3? damageSourcePosition = null)
    {
        if (damageSourcePosition.HasValue)
        {
            Vector3 damageDirection = damageSourcePosition.Value - transform.position;
            damageDirection.y = 0f;
            if (damageDirection.sqrMagnitude > 0.0001f)
            {
                targetRotation = Quaternion.LookRotation(damageDirection, Vector3.up);
                transform.rotation = targetRotation;
            }
        }

        ChangeHealth(-1);

        if (!isFall)
            TriggerHitAnimation();

        // 1. CRITICAL CHECK FIRST: Is the player completely dead?
        if (health <= 0)
        {
            LevelHandler handler = FindFirstObjectByType<LevelHandler>();

            isMoving = false;
            isStepMoving = false;
            isFalling = false;

            // Reset local currencies and vital configurations safely 
            ChangeHealth(3);
            AddCoin(-coins);

            if (handler != null)
            {
                // Send player all the way back to Level Index 1 for Game Over
                handler.StartExitTransition(0);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(1);
            }
            return; // Halt logic completely so fall checks don't override this!
        }

        // 2. SECONDARY CHECK: Did they just drop in a hole but still have health left?
        if (isFall)
        {
            isMoving = false;
            isStepMoving = false;
            isFalling = false;

            LevelHandler handler = FindFirstObjectByType<LevelHandler>();
            if (handler != null)
            {
                // Re-inject current levelIndex to restart smoothly inside the same scene
                handler.StartExitTransition(handler.levelIndex);
            }
            else
            {
                int currentSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
                UnityEngine.SceneManagement.SceneManager.LoadScene(currentSceneIndex);
            }
        }
    }

    void TriggerHitAnimation()
    {
        if (animator == null || string.IsNullOrWhiteSpace(hitTrigger))
            return;

        animator.SetTrigger(hitTrigger);
    }

    void TriggerAttackAnimation()
    {
        if (animator == null || string.IsNullOrWhiteSpace(attackTrigger))
            return;

        animator.SetTrigger(attackTrigger);
    }


    void PerformSpaceAttack()
    {
        TriggerAttackAnimation();
        StartCoroutine(VisualFlash());

        if (shockwavePrefab != null)
        {
            Vector3 shockwavePosition = GetShockwaveSpawnPosition();
            GameObject visual = Instantiate(shockwavePrefab, shockwavePosition, Quaternion.identity);
            visual.transform.localScale = Vector3.zero;
            ApplyShockwaveFireTint(visual);
            Destroy(visual, 0.25f);
        }

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange);
        foreach (Collider hitCollider in hitColliders)
        {
            if (!hitCollider.CompareTag("Enemy"))
                continue;

            if (hitCollider.TryGetComponent<BaseEnemy>(out var enemy))
                enemy.Die();
            else
                Destroy(hitCollider.gameObject);
        }
    }

    Vector3 GetShockwaveSpawnPosition()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 3f, floorLayer))
            return hit.point + Vector3.up * 0.03f;

        return new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);
    }

    void ApplyShockwaveFireTint(GameObject shockwaveVisual)
    {
        Renderer[] renderers = shockwaveVisual.GetComponentsInChildren<Renderer>();
        Color fireCore = new Color(1f, 0.12f, 0.02f, 1f);
        Color fireGlow = new Color(1f, 0.22f, 0.04f, 1f) * 2.4f;

        foreach (Renderer rend in renderers)
        {
            Material material = rend.material;
            if (material.HasProperty("_Color"))
                material.color = fireCore;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", fireCore);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", fireGlow);
            }
        }
    }

    System.Collections.IEnumerator VisualFlash()
    {
        Renderer renderer = GetMainVisualRenderer();
        if (renderer == null)
            yield break;
        Color oldColor = renderer.material.color;
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            renderer.material.color = Color.Lerp(new Color(1f, 0.25f, 0.08f, 1f), oldColor, elapsed / duration);
            yield return null;
        }
        renderer.material.color = oldColor;
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.25f, 0.08f, 1f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
    void ResetSpeedIfNoSlime()
    {
        if (!CheckForSlimeAt(transform.position))
        {
            currentMoveMultiplier = 1.0f;
        }
    }
    public void ChangeHealth(int amount)
    {
        //health += amount;
        OnHealthChanged?.Invoke(health);
    }

    public void AddCoin(int amount)
    {
        coins += amount;
        OnCoinsChanged?.Invoke(coins);
    }

    public void ResetState(Vector3 newSpawnPos)
    {
        StopAllCoroutines();

        BaseEnemy.OccupiedTiles.Remove(RoundGridPosition(new Vector2(targetPosition.x, targetPosition.z)));

        Vector3 snappedSpawn = RoundGridPosition(newSpawnPos);
        transform.position = snappedSpawn;
        targetPosition = snappedSpawn;
        moveStartPosition = snappedSpawn;
        moveTimer = 0f;
        moveDuration = 0f;
        transform.rotation = Quaternion.identity;
        targetRotation = transform.rotation;

        isMoving = false;
        isStepMoving = false;
        isFalling = false;
        movementVisualYOffset = 0f;
        currentMoveMultiplier = 1.0f;
        transform.localScale = Vector3.one;
        BaseEnemy.OccupiedTiles.Add(new Vector2(targetPosition.x, targetPosition.z));

        Renderer renderer = GetMainVisualRenderer();
        if (renderer != null)
            renderer.material.color = Color.white;
    }
    Renderer GetMainVisualRenderer()
    {
        return visualRoot != null ? visualRoot.GetComponentInChildren<Renderer>() : GetComponentInChildren<Renderer>();
    }
    Vector3 RoundGridPosition(Vector3 pos)
    {
        return new Vector3(Mathf.Round(pos.x), pos.y, Mathf.Round(pos.z));
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    bool CheckForSlimeAt(Vector3 position)
    {
        Collider[] hitColliders = Physics.OverlapBox(new Vector3(position.x, 0, position.z), new Vector3(0.45f, 1f, 0.45f));

        foreach (var col in hitColliders)
        {
            if (col.CompareTag("Slime"))
            {
                return true;
            }
        }
        return false;
    }
}