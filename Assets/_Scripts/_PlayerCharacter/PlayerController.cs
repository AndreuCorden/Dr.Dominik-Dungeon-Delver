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
    [SerializeField] private GameObject slashVfxPrefab;
    [SerializeField] private Transform slashSpawnPoint;
    [SerializeField] private Vector3 slashRotationOffset;
    [SerializeField] private Vector3 slashPositionOffset;
    [SerializeField] private float slashLifetime = 0.5f;

    [Header("Layers")]
    public LayerMask floorLayer;

    [Header("Audio Configurations")]
    [SerializeField] private AudioClip moveSFX;
    [SerializeField] private AudioClip attackSFX;
    [SerializeField] private AudioClip dieSFX;
    [SerializeField] [Range(0f, 1f)] private float sfxVolume = 0.8f;

    private Vector3 targetPosition;
    private Vector3 moveStartPosition;
    private float moveTimer;
    private float moveDuration;
    private bool isMoving = false;
    private bool isStepMoving = false;
    private bool isFalling = false;

    void Awake()
    {
        Instance = this;
    }

    public void SpawnSlashVFX()
    {
        if (slashVfxPrefab == null)
            return;

        if (slashSpawnPoint == null)
        {
            Debug.LogWarning("Slash Spawn Point no asignado");
            return;
        }

        Vector3 finalPosition =
            slashSpawnPoint.position +
            slashSpawnPoint.TransformDirection(slashPositionOffset);

        Vector3 flatForward = Vector3.ProjectOnPlane(slashSpawnPoint.forward, Vector3.up);
        if (flatForward.sqrMagnitude < 0.0001f)
            flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (flatForward.sqrMagnitude < 0.0001f)
            flatForward = Vector3.forward;

        Quaternion finalRotation =
            Quaternion.LookRotation(flatForward.normalized, Vector3.up) *
            Quaternion.Euler(0f, slashRotationOffset.y, 0f);

        GameObject slash = Instantiate(
            slashVfxPrefab,
            finalPosition,
            finalRotation
        );

        Destroy(slash, slashLifetime);
    }

    void ResolveSlashSpawnPoint()
    {
        if (slashSpawnPoint != null && slashSpawnPoint != transform && slashSpawnPoint.name != "SlashSpawnPoint")
            return;

        Transform searchRoot = visualRoot != null ? visualRoot : transform;
        Transform resolved = FindPreferredSlashAnchor(searchRoot);

        if (resolved == null && searchRoot != transform)
            resolved = FindPreferredSlashAnchor(transform);

        if (resolved != null)
            slashSpawnPoint = resolved;
    }

    Transform FindPreferredSlashAnchor(Transform root)
    {
        if (root == null)
            return null;

        string[] preferredNames = { "Sword", "Wrist.R", "Wrist.L", "UpperArm.R", "UpperArm.L" };
        foreach (string preferredName in preferredNames)
        {
            Transform found = FindDeepChild(root, preferredName);
            if (found != null)
                return found;
        }

        return null;
    }

    Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform found = FindDeepChild(child, childName);
            if (found != null)
                return found;
        }

        return null;
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

        if (animator == null || animator.gameObject == gameObject)
        {
            Animator resolvedAnimator = ResolveAnimator();
            if (resolvedAnimator != null)
                animator = resolvedAnimator;
        }

        ResolveSlashSpawnPoint();

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

    Animator ResolveAnimator()
    {
        if (visualRoot != null)
        {
            Animator visualAnimator = visualRoot.GetComponentInChildren<Animator>(true);
            if (visualAnimator != null)
                return visualAnimator;
        }

        Animator[] animators = GetComponentsInChildren<Animator>(true);
        foreach (var candidate in animators)
        {
            if (candidate != null && candidate.gameObject != gameObject)
                return candidate;
        }
        return animators.Length > 0 ? animators[0] : null;
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

        // --- PLAY PLAYER MOVE SFX ---
        if (AudioManager.Instance != null && moveSFX != null)
        {
            AudioManager.Instance.PlaySFX(moveSFX, transform.position, sfxVolume);
        }

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
            transform.Translate(Vector3.down * Time.deltaTime * 10f, Space.World);
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, fallTimer);
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

        // --- GAME OVER: PLAYER DIED ---
        if (health <= 0)
        {
            // --- PLAY PLAYER DEATH SFX ---
            if (AudioManager.Instance != null && dieSFX != null)
            {
                AudioManager.Instance.PlaySFX(dieSFX, transform.position, sfxVolume);
            }

            isMoving = false;
            isStepMoving = false;
            isFalling = false;

            ChangeHealth(3);
            AddCoin(-coins);

            if (NavigationManager.Instance != null)
            {
                NavigationManager.Instance.ReturnToMainMenu();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            }
            return;
        }

        // --- NON-LETHAL FALL: RESTART CURRENT LEVEL ---
        if (isFall)
        {
            isMoving = false;
            isStepMoving = false;
            isFalling = false;

            LevelHandler handler = FindFirstObjectByType<LevelHandler>();
            if (handler != null)
            {
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

        if (AudioManager.Instance != null && attackSFX != null)
            AudioManager.Instance.PlaySFX(attackSFX, transform.position, sfxVolume);

        Vector3 attackPos = GetAttackTilePosition();

        // Buscar solo en la casilla de enfrente
        Collider[] hitColliders = Physics.OverlapBox(
            attackPos,
            new Vector3(0.35f, 0.5f, 0.35f)
        );

        foreach (Collider hitCollider in hitColliders)
        {
            if (!hitCollider.CompareTag("Enemy"))
                continue;

            if (hitCollider.TryGetComponent<BaseEnemy>(out var enemy))
                enemy.Die(transform.position);
            else
                Destroy(hitCollider.gameObject);
        }
    }

    Vector3 GetAttackTilePosition()
    {
        Vector3 dir = transform.forward;
        dir.y = 0f;
        dir = dir.normalized;

        // Una casilla delante
        Vector3 tile = transform.position + dir;
        return RoundGridPosition(tile);
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
        health += amount;
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