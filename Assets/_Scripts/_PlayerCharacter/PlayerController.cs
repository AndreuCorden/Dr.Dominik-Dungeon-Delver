using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

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
    private Animator characterAnimator;
    private Quaternion targetRotation;

    [Header("Animation Parameters")]
    [SerializeField] private string movingBoolParameter = "IsMoving";
    [SerializeField] private string speedFloatParameter = "Speed";
    [SerializeField] private string attackTriggerParameter = "Attack";
    [SerializeField] private string hitTriggerParameter = "Hit";
    private bool hasMovingBoolParameter;
    private bool hasSpeedFloatParameter;
    private bool hasAttackTriggerParameter;
    private bool hasHitTriggerParameter;
    private int movingBoolHash;
    private int speedFloatHash;
    private int attackTriggerHash;
    private int hitTriggerHash;

    [Header("Emotes")]
    [SerializeField] private AnimationClip[] emoteClips;
    [SerializeField] private float emoteBlendDuration = 0.08f;
    [Header("Idle Variation")]
    [SerializeField] private AnimationClip[] idleVariationClips;
    [SerializeField] private Vector2 idleVariationIntervalRange = new Vector2(4f, 7f);
    private PlayableGraph emoteGraph;
    private bool isEmotePlaying;
    private float emoteTimer;
    private float currentEmoteDuration;
    private float idleVariationTimer;
    private float nextIdleVariationDelay;

    [Header("Status Effects")]
    public float currentMoveMultiplier = 1.0f;

    [Header("Attack Settings")]
    public float attackRange = 1.1f;
    [SerializeField] private bool useMovementClip = false;
    [SerializeField] private AnimationClip movementClip;
    [SerializeField] private AnimationClip attackClip;
    [SerializeField] private float movementBlendDuration = 0.08f;
    [SerializeField] private float attackBlendDuration = 0.08f;
    [Header("Damage Feedback")]
    [SerializeField] private float damageInvulnerabilityDuration = 0.7f;
    [SerializeField] private float damageFlashInterval = 0.08f;
    [SerializeField] private Color damageFlashColor = new Color(1f, 0.25f, 0.25f, 1f);
    [SerializeField] private AnimationClip hitReactionClip;
    [SerializeField] private float hitBlendDuration = 0.05f;

    [Header("VFX")]
    public GameObject shockwavePrefab;

    [Header("Layers")]
    public LayerMask floorLayer;
    public LayerMask enemyLayer;

    private Vector3 targetPosition;
    private Vector3 moveStartPosition;
    private float moveTimer;
    private float moveDuration;
    private bool isMoving = false;
    private bool isStepMoving = false;
    private bool isFalling = false; // Added to decouple death loops safely
    private PlayableGraph attackGraph;
    private PlayableGraph movementGraph;
    private bool isMovementClipPlaying;
    private bool isAttackClipPlaying;
    private float attackClipTimer;
    private float currentAttackClipDuration;
    private PlayableGraph hitGraph;
    private bool isHitClipPlaying;
    private float hitClipTimer;
    private float currentHitClipDuration;
    private float invulnerableUntilTime;
    private Coroutine damageInvulnerabilityRoutine;

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
            visualRoot = GetComponentInChildren<Animator>()?.transform;
            if (visualRoot == transform) visualRoot = null;

            if (visualRoot == null)
                visualRoot = GetComponentInChildren<SkinnedMeshRenderer>()?.transform;

            if (visualRoot == null)
                visualRoot = GetComponentInChildren<MeshRenderer>()?.transform;

            if (visualRoot == transform) visualRoot = null;
        }

        if (visualRoot != null)
            visualRootInitialLocalPosition = visualRoot.localPosition;

        characterAnimator = GetComponentInChildren<Animator>();
        if (characterAnimator == null && visualRoot != null)
            characterAnimator = visualRoot.GetComponentInParent<Animator>();

        CacheAnimatorParameters();
        ScheduleNextIdleVariation();

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

    void Update()
    {
        // CRITICAL FIX: If we are falling through the void, let the coroutine have
        // exclusive control over transform translations. Do not check movement input or calculations.
        if (isFalling)
        {
            UpdateRotation();
            return;
        }

        UpdateEmoteInputAndPlayback();
        UpdateAttackClipPlayback();
        UpdateHitClipPlayback();

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
        UpdateAnimatorValues();

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
        SyncMovementAnimationState();
    }

    void CacheAnimatorParameters()
    {
        if (characterAnimator == null)
            return;

        hasMovingBoolParameter = false;
        hasSpeedFloatParameter = false;
        hasAttackTriggerParameter = false;
        hasHitTriggerParameter = false;

        movingBoolHash = string.IsNullOrWhiteSpace(movingBoolParameter) ? 0 : Animator.StringToHash(movingBoolParameter);
        speedFloatHash = string.IsNullOrWhiteSpace(speedFloatParameter) ? 0 : Animator.StringToHash(speedFloatParameter);
        attackTriggerHash = string.IsNullOrWhiteSpace(attackTriggerParameter) ? 0 : Animator.StringToHash(attackTriggerParameter);
        hitTriggerHash = string.IsNullOrWhiteSpace(hitTriggerParameter) ? 0 : Animator.StringToHash(hitTriggerParameter);

        foreach (AnimatorControllerParameter parameter in characterAnimator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool && parameter.nameHash == movingBoolHash)
                hasMovingBoolParameter = true;
            else if (parameter.type == AnimatorControllerParameterType.Float && parameter.nameHash == speedFloatHash)
                hasSpeedFloatParameter = true;
            else if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.nameHash == attackTriggerHash)
                hasAttackTriggerParameter = true;
            else if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.nameHash == hitTriggerHash)
                hasHitTriggerParameter = true;
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

    void UpdateAnimatorValues()
    {
        if (characterAnimator == null)
            return;

        if (hasMovingBoolParameter)
            characterAnimator.SetBool(movingBoolHash, isMoving);

        if (hasSpeedFloatParameter)
            characterAnimator.SetFloat(speedFloatHash, isMoving ? currentMoveMultiplier : 0f);
    }

    void UpdateMovementPosition()
    {
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

    void UpdateEmoteInputAndPlayback()
    {
        if (isEmotePlaying)
        {
            emoteTimer += Time.deltaTime;
            if (emoteTimer >= currentEmoteDuration)
                StopCurrentEmote();
            return;
        }

        if (isMoving || Keyboard.current == null)
        {
            idleVariationTimer = 0f;
            return;
        }

        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            if (TryPlayRandomEmote())
                idleVariationTimer = 0f;
            return;
        }

        if (HasGameplayInput(Keyboard.current))
        {
            idleVariationTimer = 0f;
            return;
        }

        TryPlayIdleVariation();
    }

    bool TryPlayRandomEmote()
    {
        return TryPlayRandomClip(emoteClips);
    }

    void TryPlayIdleVariation()
    {
        if (idleVariationClips == null || idleVariationClips.Length == 0)
            return;

        idleVariationTimer += Time.deltaTime;
        if (idleVariationTimer < nextIdleVariationDelay)
            return;

        if (TryPlayRandomClip(idleVariationClips))
            ScheduleNextIdleVariation();
        else
            idleVariationTimer = 0f;
    }

    void ScheduleNextIdleVariation()
    {
        idleVariationTimer = 0f;
        float minDelay = Mathf.Max(0.5f, Mathf.Min(idleVariationIntervalRange.x, idleVariationIntervalRange.y));
        float maxDelay = Mathf.Max(minDelay, Mathf.Max(idleVariationIntervalRange.x, idleVariationIntervalRange.y));
        nextIdleVariationDelay = UnityEngine.Random.Range(minDelay, maxDelay);
    }

    bool HasGameplayInput(Keyboard kb)
    {
        return kb.wKey.wasPressedThisFrame
               || kb.aKey.wasPressedThisFrame
               || kb.sKey.wasPressedThisFrame
               || kb.dKey.wasPressedThisFrame
               || kb.upArrowKey.wasPressedThisFrame
               || kb.downArrowKey.wasPressedThisFrame
               || kb.leftArrowKey.wasPressedThisFrame
               || kb.rightArrowKey.wasPressedThisFrame
               || kb.spaceKey.wasPressedThisFrame;
    }

    bool TryPlayRandomClip(AnimationClip[] clips)
    {
        if (characterAnimator == null || clips == null || clips.Length == 0)
            return false;

        int index = UnityEngine.Random.Range(0, clips.Length);
        AnimationClip clip = clips[index];
        if (clip == null)
            return false;

        if (emoteGraph.IsValid())
            emoteGraph.Destroy();

        emoteGraph = PlayableGraph.Create("PlayerEmoteGraph");
        var output = AnimationPlayableOutput.Create(emoteGraph, "PlayerEmoteOutput", characterAnimator);
        var playable = AnimationClipPlayable.Create(emoteGraph, clip);
        playable.SetApplyFootIK(false);
        playable.SetApplyPlayableIK(false);
        output.SetSourcePlayable(playable);
        emoteGraph.Play();

        isEmotePlaying = true;
        emoteTimer = 0f;
        currentEmoteDuration = Mathf.Max(clip.length, 0.01f);
        return true;
    }

    void StopCurrentEmote()
    {
        if (!isEmotePlaying)
            return;

        isEmotePlaying = false;
        emoteTimer = 0f;
        currentEmoteDuration = 0f;

        if (emoteGraph.IsValid())
            emoteGraph.Destroy();

        if (characterAnimator != null)
        {
            characterAnimator.Rebind();
            if (emoteBlendDuration <= 0f)
                characterAnimator.Update(0f);
            else
                characterAnimator.Update(emoteBlendDuration);
        }
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
        StopCurrentEmote();
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
        isFalling = true; // Block double calculations
        StopCurrentEmote();
        isMoving = true;
        isStepMoving = false;
        movementVisualYOffset = 0f;
        float fallTimer = 0f;
        while (fallTimer < 1.0f)
        {
            // FIX: Specifying Space.World ensures downward motion ignores the rapid rotation
            transform.Translate(Vector3.down * Time.deltaTime * 10f, Space.World);
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, fallTimer);
            fallTimer += Time.deltaTime;
            yield return null;
        }

        isMoving = false;
        TakeDamage(true);
    }

    public void TakeDamage(bool isFall = false)
    {
        if (!isFall && Time.time < invulnerableUntilTime)
            return;

        StopCurrentEmote();
        PlayHitReaction();
        ChangeHealth(-1);

        if (!isFall)
            StartDamageInvulnerability();

        if (health <= 0)
        {
            LevelHandler handler = FindFirstObjectByType<LevelHandler>();

            isMoving = false;
            isStepMoving = false;
            isFalling = false;

            if (handler != null)
            {
                ChangeHealth(3);
                AddCoin(-coins);
                handler.StartExitTransition(1);
            }
            else
            {
                ChangeHealth(3);
                AddCoin(-coins);
                UnityEngine.SceneManagement.SceneManager.LoadScene(1);
            }
            return;
        }

        if (isFall)
        {
            int currentSceneIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;

            isMoving = false;
            isStepMoving = false;
            isFalling = false;

            UnityEngine.SceneManagement.SceneManager.LoadScene(currentSceneIndex);
        }
    }

    void StartDamageInvulnerability()
    {
        invulnerableUntilTime = Time.time + Mathf.Max(0f, damageInvulnerabilityDuration);
        if (damageInvulnerabilityRoutine != null)
            StopCoroutine(damageInvulnerabilityRoutine);

        damageInvulnerabilityRoutine = StartCoroutine(DamageInvulnerabilityFlash());
    }

    System.Collections.IEnumerator DamageInvulnerabilityFlash()
    {
        Renderer renderer = GetMainVisualRenderer();
        if (renderer == null)
            yield break;

        Color baseColor = renderer.material.color;
        float interval = Mathf.Max(0.03f, damageFlashInterval);
        bool useFlashColor = false;

        while (Time.time < invulnerableUntilTime)
        {
            useFlashColor = !useFlashColor;
            renderer.material.color = useFlashColor ? damageFlashColor : baseColor;
            yield return new WaitForSeconds(interval);
        }

        renderer.material.color = baseColor;
        damageInvulnerabilityRoutine = null;
    }

    void PerformSpaceAttack()
    {
        StopCurrentEmote();
        PlayAttackAnimation();
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

    void PlayAttackAnimation()
    {
        if (characterAnimator == null)
            return;

        if (hasAttackTriggerParameter)
            characterAnimator.SetTrigger(attackTriggerHash);

        if (attackClip == null)
            return;

        if (attackGraph.IsValid())
            attackGraph.Destroy();

        attackGraph = PlayableGraph.Create("PlayerAttackGraph");
        var output = AnimationPlayableOutput.Create(attackGraph, "PlayerAttackOutput", characterAnimator);
        var playable = AnimationClipPlayable.Create(attackGraph, attackClip);
        playable.SetApplyFootIK(false);
        playable.SetApplyPlayableIK(false);
        output.SetSourcePlayable(playable);
        attackGraph.Play();

        isAttackClipPlaying = true;
        attackClipTimer = 0f;
        currentAttackClipDuration = Mathf.Max(attackClip.length, 0.01f);
    }

    void SyncMovementAnimationState()
    {
        if (!useMovementClip || characterAnimator == null || movementClip == null)
        {
            StopMovementAnimation();
            return;
        }

        bool shouldPlayMovement = isMoving && !isEmotePlaying && !isAttackClipPlaying && !isHitClipPlaying;

        if (shouldPlayMovement)
        {
            if (!isMovementClipPlaying)
                PlayMovementAnimation();
            return;
        }

        StopMovementAnimation();
    }

    void PlayMovementAnimation()
    {
        if (movementGraph.IsValid())
            movementGraph.Destroy();

        movementGraph = PlayableGraph.Create("PlayerMovementGraph");
        var output = AnimationPlayableOutput.Create(movementGraph, "PlayerMovementOutput", characterAnimator);
        var playable = AnimationClipPlayable.Create(movementGraph, movementClip);
        playable.SetApplyFootIK(true);
        playable.SetApplyPlayableIK(false);
        output.SetSourcePlayable(playable);
        movementGraph.Play();

        isMovementClipPlaying = true;
    }

    void StopMovementAnimation()
    {
        if (!isMovementClipPlaying)
            return;

        isMovementClipPlaying = false;

        if (movementGraph.IsValid())
            movementGraph.Destroy();

        if (characterAnimator != null)
        {
            characterAnimator.Rebind();
            characterAnimator.Update(Mathf.Max(movementBlendDuration, 0f));
        }
    }

    void PlayHitReaction()
    {
        if (characterAnimator == null)
            return;

        if (hasHitTriggerParameter)
            characterAnimator.SetTrigger(hitTriggerHash);

        if (hitReactionClip == null)
            return;

        if (hitGraph.IsValid())
            hitGraph.Destroy();

        hitGraph = PlayableGraph.Create("PlayerHitGraph");
        var output = AnimationPlayableOutput.Create(hitGraph, "PlayerHitOutput", characterAnimator);
        var playable = AnimationClipPlayable.Create(hitGraph, hitReactionClip);
        playable.SetApplyFootIK(false);
        playable.SetApplyPlayableIK(false);
        output.SetSourcePlayable(playable);
        hitGraph.Play();

        isHitClipPlaying = true;
        hitClipTimer = 0f;
        currentHitClipDuration = Mathf.Max(hitReactionClip.length, 0.01f);
    }

    void UpdateAttackClipPlayback()
    {
        if (!isAttackClipPlaying)
            return;

        attackClipTimer += Time.deltaTime;
        if (attackClipTimer >= currentAttackClipDuration)
            StopAttackClipPlayback();
    }

    void UpdateHitClipPlayback()
    {
        if (!isHitClipPlaying)
            return;

        hitClipTimer += Time.deltaTime;
        if (hitClipTimer >= currentHitClipDuration)
            StopHitReactionPlayback();
    }

    void StopAttackClipPlayback()
    {
        if (!isAttackClipPlaying)
            return;
        isAttackClipPlaying = false;
        attackClipTimer = 0f;
        currentAttackClipDuration = 0f;
        if (attackGraph.IsValid())
            attackGraph.Destroy();

        if (characterAnimator != null)
        {
            characterAnimator.Rebind();
            characterAnimator.Update(Mathf.Max(attackBlendDuration, 0f));
        }
    }
    void StopHitReactionPlayback()
    {
        if (!isHitClipPlaying)
            return;

        isHitClipPlaying = false;
        hitClipTimer = 0f;
        currentHitClipDuration = 0f;

        if (hitGraph.IsValid())
            hitGraph.Destroy();

        if (characterAnimator != null)
        {
            characterAnimator.Rebind();
            characterAnimator.Update(Mathf.Max(hitBlendDuration, 0f));
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
        StopCurrentEmote();
        StopMovementAnimation();
        StopAttackClipPlayback();
        StopHitReactionPlayback();
        StopAllCoroutines();
        damageInvulnerabilityRoutine = null;
        invulnerableUntilTime = 0f;
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
    void OnDisable()
    {
        StopCurrentEmote();
        StopMovementAnimation();
        StopAttackClipPlayback();
        StopHitReactionPlayback();
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