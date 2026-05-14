using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Animations;
using UnityEngine.Playables;

public abstract class BaseEnemy : MonoBehaviour
{
    // The shared map for ALL enemies and the player
    public static HashSet<Vector2> OccupiedTiles = new HashSet<Vector2>();

    [Header("Base Settings")]
    public float moveSpeed = 5f;
    public float timeBetweenSteps = 1f;
    public LayerMask floorLayer;
    public LayerMask blockingLayers;

    [Header("Visual Setup")]
    [SerializeField] private GameObject visualPrefab;
    [SerializeField] private Vector3 visualLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 visualLocalEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 visualLocalScale = Vector3.one;
    [SerializeField] private bool disablePrimitiveBody = true;
    [SerializeField] private Material visualOverrideMaterial;

    [Header("Animation Clips")]
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip moveClip;
    [SerializeField] private AnimationClip attackClip;
    [SerializeField] private AnimationClip deathClip;
    [SerializeField] private float animationBlendDuration = 0.08f;

    [Header("Movement Feel")]
    [SerializeField] private float turnSpeed = 720f;
    [SerializeField] private float turnBeforeMoveAngle = 6f;
    [SerializeField] private float stepJumpHeight = 0.35f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private Vector3 facingEulerOffset = new Vector3(0f, 180f, 0f);

    [Header("Grounding Tuning")]
    [SerializeField] private float visualGroundOffset = 0f;
    [SerializeField] private bool autoCenterVisualOnTile = false;
    [SerializeField] private bool autoLowerFloatingVisual = true;
    [SerializeField, Range(0f, 1f)] private float autoLowerStrength = 1f;
    [SerializeField] private float autoLowerTolerance = 0.01f;

    protected Vector3 targetPosition;
    protected bool isMoving = false;
    protected bool isFalling = false;
    protected float nextMoveTime;
    protected Transform player;

    private Vector3 moveStartPosition;
    private float moveTimer;
    private float moveDuration;
    private Quaternion targetRotation;
    private float movementVisualYOffset;

    private GameObject spawnedVisual;
    private Transform movementVisualRoot;
    private Vector3 movementVisualRootInitialLocalPosition;
    private Animator enemyAnimator;
    private Animation legacyAnimation;
    private PlayableGraph animationGraph;
    private AnimationClip currentClip;
    private bool isPlayingOneShot;
    private float oneShotTimer;
    private float oneShotDuration;
    private bool isDying;
    [Header("Death Animation")]
    [SerializeField] private float deathDuration = 0.45f;
    [SerializeField] private float deathSinkDistance = 0.55f;
    [SerializeField] private float deathSpinSpeed = 540f;
    [SerializeField] private float deathScaleMultiplier = 0.7f;

    protected virtual void Start()
    {
        TryResolvePlayer();

        SetupVisuals();
        NormalizeRendererMaterialsForUrp();
        CacheAnimationComponents();
        CacheMovementVisualRoot();
        LowerVisualIfFloating();

        PlayLoopAnimation(idleClip);
        // Initial Grid Placement
        targetPosition = RoundToGrid(transform.position);
        transform.position = targetPosition;
        moveStartPosition = targetPosition;
        targetRotation = transform.rotation;
        OccupiedTiles.Add(GetGridKey(transform.position));
    }

    protected virtual void LateUpdate()
    {
        if (movementVisualRoot != null)
            movementVisualRoot.localPosition = movementVisualRootInitialLocalPosition + new Vector3(0f, visualGroundOffset + movementVisualYOffset, 0f);
    }

    protected virtual void Update()
    {
        if (isDying)
            return;

        if (!TryResolvePlayer())
            return;

        if (isFalling) { HandleFalling(); return; }

        if (isMoving)
        {
            UpdateRotation();
            UpdateMovementPosition();
            UpdateStepJumpOffset();
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f || moveTimer >= moveDuration)
                FinishMovement();
        }
        else
        {
            movementVisualYOffset = 0f;
            SnapToCurrentTileCenter();
            CheckForVoid(); // Always check if floor exists beneath feet
            if (!isFalling && Time.time >= nextMoveTime) DetermineNextStep();
        }

        UpdateAnimationState();
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
        if (!TryResolvePlayer())
            return false;

        Vector3 dest3D = RoundToGrid(transform.position + direction);

        // Check if player is standing exactly where we want to go (Attack Range)
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
            moveStartPosition = transform.position;
            targetPosition = dest3D;
            targetRotation = GetFacingRotation(direction);
            moveTimer = 0f;
            moveDuration = Vector3.Distance(moveStartPosition, targetPosition) / Mathf.Max(moveSpeed, 0.01f);
            isMoving = true;
            return true;
        }
        return false;
    }

    protected void PerformAttack(Vector3 dir)
    {
        transform.rotation = GetFacingRotation(dir);
        // Trigger Damage to Player and visual lunge here
        if (player != null && player.TryGetComponent<PlayerController>(out var pc))
            pc.TakeDamage();
        else if (PlayerController.Instance != null)
            PlayerController.Instance.TakeDamage();
        nextMoveTime = Time.time + timeBetweenSteps;
    }

    private bool TryResolvePlayer()
    {
        if (player != null && player.gameObject.activeInHierarchy)
            return true;

        if (PlayerController.Instance != null)
        {
            player = PlayerController.Instance.transform;
            return true;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
            return false;

        player = playerObject.transform;
        return true;
    }

    protected IEnumerator AttackLunge(Vector3 dir)
    {
        PlayAttackAnimation();

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

    protected virtual void FinishMovement()
    {
        transform.position = targetPosition;
        moveStartPosition = targetPosition;
        moveTimer = 0f;
        moveDuration = 0f;
        movementVisualYOffset = 0f;
        isMoving = false;
        nextMoveTime = Time.time + timeBetweenSteps;
    }

    // Utility
    protected Vector2 GetGridKey(Vector3 pos) => new Vector2(Mathf.Round(pos.x), Mathf.Round(pos.z));
    protected Vector3 RoundToGrid(Vector3 pos) => new Vector3(Mathf.Round(pos.x), transform.position.y, Mathf.Round(pos.z));

    public virtual void Die()
    {
        if (isDying)
            return;

        if (this is MimicEnemy)
        {
            OccupiedTiles.Remove(GetGridKey(transform.position));
            if (isMoving) OccupiedTiles.Remove(GetGridKey(targetPosition));
            StopAllAnimationPlayback(false);
            Destroy(gameObject);
            return;
        }

        StartCoroutine(PlayDeathAndDestroy());
    }

    private IEnumerator PlayDeathAndDestroy()
    {
        isDying = true;
        OccupiedTiles.Remove(GetGridKey(transform.position));
        if (isMoving) OccupiedTiles.Remove(GetGridKey(targetPosition));

        isMoving = false;
        isFalling = false;
        moveTimer = 0f;
        moveDuration = 0f;
        movementVisualYOffset = 0f;

        Collider ownCollider = GetComponent<Collider>();
        if (ownCollider != null)
            ownCollider.enabled = false;

        Rigidbody ownRigidbody = GetComponent<Rigidbody>();
        if (ownRigidbody != null)
        {
            ownRigidbody.linearVelocity = Vector3.zero;
            ownRigidbody.angularVelocity = Vector3.zero;
            ownRigidbody.isKinematic = true;
            ownRigidbody.useGravity = false;
        }

        if (deathClip != null)
            PlayOneShotAnimation(deathClip);
        else
            StopAllAnimationPlayback(false);

        Vector3 startPosition = transform.position;
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, deathDuration);
        float targetScaleFactor = Mathf.Clamp(deathScaleMultiplier, 0.05f, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            transform.Rotate(0f, deathSpinSpeed * Time.deltaTime, 0f, Space.World);
            transform.position = startPosition + Vector3.down * (deathSinkDistance * progress);
            transform.localScale = Vector3.Lerp(startScale, startScale * targetScaleFactor, progress);
            yield return null;
        }

        Destroy(gameObject);
    }

    protected void PlayAttackAnimation()
    {
        PlayOneShotAnimation(attackClip);
    }

    private void SetupVisuals()
    {
        if (visualPrefab != null)
        {
            spawnedVisual = Instantiate(visualPrefab, transform);
            spawnedVisual.transform.localPosition = visualLocalPosition;
            spawnedVisual.transform.localRotation = Quaternion.Euler(visualLocalEulerAngles);
            spawnedVisual.transform.localScale = visualLocalScale;
        }
        else
        {
            spawnedVisual = TryCreateVisualProxyFromRootMesh();
        }

        if (spawnedVisual == null)
            return;

        if (CanApplyVisualOverrideMaterial())
        {
            Renderer[] renderers = spawnedVisual.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
                renderer.material = visualOverrideMaterial;
        }

        if (!disablePrimitiveBody)
            return;

        bool hasVisualRenderer = spawnedVisual.GetComponentInChildren<Renderer>(true) != null;
        if (!hasVisualRenderer)
            return;

        MeshRenderer primitiveRenderer = GetComponent<MeshRenderer>();
        if (primitiveRenderer != null)
            primitiveRenderer.enabled = false;

        MeshFilter primitiveFilter = GetComponent<MeshFilter>();
        if (primitiveFilter != null && visualPrefab != null)
            primitiveFilter.sharedMesh = null;
    }

    private GameObject TryCreateVisualProxyFromRootMesh()
    {
        MeshFilter primitiveFilter = GetComponent<MeshFilter>();
        MeshRenderer primitiveRenderer = GetComponent<MeshRenderer>();

        if (primitiveFilter == null || primitiveRenderer == null || primitiveFilter.sharedMesh == null)
            return null;

        GameObject visualProxy = new GameObject("VisualProxy");
        visualProxy.transform.SetParent(transform, false);
        visualProxy.transform.localPosition = visualLocalPosition;
        visualProxy.transform.localRotation = Quaternion.Euler(visualLocalEulerAngles);
        visualProxy.transform.localScale = visualLocalScale;

        MeshFilter proxyFilter = visualProxy.AddComponent<MeshFilter>();
        proxyFilter.sharedMesh = primitiveFilter.sharedMesh;

        MeshRenderer proxyRenderer = visualProxy.AddComponent<MeshRenderer>();
        proxyRenderer.sharedMaterials = primitiveRenderer.sharedMaterials;
        proxyRenderer.shadowCastingMode = primitiveRenderer.shadowCastingMode;
        proxyRenderer.receiveShadows = primitiveRenderer.receiveShadows;
        proxyRenderer.lightProbeUsage = primitiveRenderer.lightProbeUsage;
        proxyRenderer.reflectionProbeUsage = primitiveRenderer.reflectionProbeUsage;

        return visualProxy;
    }

    protected void CacheAnimationComponents()
    {
        enemyAnimator = GetComponentInChildren<Animator>(true);
        legacyAnimation = enemyAnimator == null ? GetComponentInChildren<Animation>(true) : null;
    }

    private void UpdateAnimationState()
    {
        if (spawnedVisual != null && !spawnedVisual.activeInHierarchy)
            return;

        if (isPlayingOneShot)
        {
            oneShotTimer += Time.deltaTime;
            if (oneShotTimer >= oneShotDuration)
            {
                isPlayingOneShot = false;
                oneShotTimer = 0f;
                oneShotDuration = 0f;
                currentClip = null;
                StopAllAnimationPlayback(true);
            }
            return;
        }
        // 2. IMPORTANT: Remove the target position if we were moving toward it
        if (isMoving)
        {
            OccupiedTiles.Remove(GetGridKey(targetPosition));
        }

        AnimationClip wantedClip = idleClip;
        PlayLoopAnimation(wantedClip);
    }

    private void PlayLoopAnimation(AnimationClip clip)
    {
        if (clip == null || currentClip == clip)
            return;

        if (enemyAnimator != null)
        {
            PlayAnimatorClip(clip);
        }
        else if (legacyAnimation != null)
        {
            PlayLegacyClip(clip, true);
        }

        currentClip = clip;
    }

    protected void SetSpawnedVisualActive(bool active)
    {
        if (spawnedVisual == null)
            return;

        spawnedVisual.SetActive(active);
        CacheAnimationComponents();
        CacheMovementVisualRoot();
        LowerVisualIfFloating();
    }

    private void PlayOneShotAnimation(AnimationClip clip)
    {
        if (clip == null)
            return;

        if (enemyAnimator != null)
        {
            PlayAnimatorClip(clip);
        }
        else if (legacyAnimation != null)
        {
            PlayLegacyClip(clip, false);
        }
        else
        {
            return;
        }

        isPlayingOneShot = true;
        oneShotTimer = 0f;
        oneShotDuration = Mathf.Max(clip.length, 0.01f);
        currentClip = clip;
    }

    private void PlayAnimatorClip(AnimationClip clip)
    {
        if (animationGraph.IsValid())
            animationGraph.Destroy();

        animationGraph = PlayableGraph.Create("EnemyAnimationGraph");
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(animationGraph, "EnemyAnimationOutput", enemyAnimator);
        AnimationClipPlayable playable = AnimationClipPlayable.Create(animationGraph, clip);
        playable.SetApplyFootIK(false);
        playable.SetApplyPlayableIK(false);
        output.SetSourcePlayable(playable);
        animationGraph.Play();
    }

    private void PlayLegacyClip(AnimationClip clip, bool loop)
    {
        if (legacyAnimation.GetClip(clip.name) == null)
            legacyAnimation.AddClip(clip, clip.name);

        AnimationState state = legacyAnimation[clip.name];
        if (state != null)
            state.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;

        legacyAnimation.CrossFade(clip.name, Mathf.Max(animationBlendDuration, 0f));
    }

    private void StopAllAnimationPlayback(bool rebindAnimator)
    {
        if (animationGraph.IsValid())
            animationGraph.Destroy();

        if (enemyAnimator != null && rebindAnimator)
        {
            enemyAnimator.Rebind();
            enemyAnimator.Update(Mathf.Max(animationBlendDuration, 0f));
        }

        if (legacyAnimation != null)
            legacyAnimation.Stop();
    }

    private void CacheMovementVisualRoot()
    {
        movementVisualRoot = null;

        if (spawnedVisual != null)
            movementVisualRoot = spawnedVisual.transform;
        else
            movementVisualRoot = GetComponentInChildren<SkinnedMeshRenderer>()?.transform ?? GetComponentInChildren<MeshRenderer>()?.transform;

        if (movementVisualRoot == transform)
            movementVisualRoot = null;

        if (movementVisualRoot != null)
            movementVisualRootInitialLocalPosition = movementVisualRoot.localPosition;
    }

    private void UpdateMovementPosition()
    {
        float angleToTarget = Quaternion.Angle(transform.rotation, targetRotation);
        if (angleToTarget > turnBeforeMoveAngle)
        {
            transform.position = moveStartPosition;
            return;
        }

        moveTimer += Time.deltaTime;
        float normalizedTime = moveDuration <= Mathf.Epsilon ? 1f : Mathf.Clamp01(moveTimer / moveDuration);
        float curvedTime = moveCurve != null ? moveCurve.Evaluate(normalizedTime) : normalizedTime;

        transform.position = Vector3.LerpUnclamped(moveStartPosition, targetPosition, curvedTime);
    }

    private void UpdateRotation()
    {
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private Quaternion GetFacingRotation(Vector3 direction)
    {
        if (direction.sqrMagnitude <= Mathf.Epsilon)
            return transform.rotation;

        Quaternion baseRotation = Quaternion.LookRotation(direction, Vector3.up);
        return baseRotation * Quaternion.Euler(facingEulerOffset);
    }

    private void UpdateStepJumpOffset()
    {
        if (moveDuration <= Mathf.Epsilon)
        {
            movementVisualYOffset = 0f;
            return;
        }

        float progress = Mathf.Clamp01(moveTimer / moveDuration);
        float parabola = 4f * progress * (1f - progress);
        movementVisualYOffset = parabola * stepJumpHeight;
    }

    private void SnapToCurrentTileCenter()
    {
        Vector3 snapped = RoundToGrid(targetPosition);
        targetPosition = snapped;
        transform.position = snapped;
        moveStartPosition = snapped;
    }

    private void LowerVisualIfFloating()
    {
        if (movementVisualRoot == null)
            return;

        Renderer[] renderers = movementVisualRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
            return;

        if (autoCenterVisualOnTile)
            CenterVisualOnTile(renderers);

        if (!autoLowerFloatingVisual)
            return;

        Collider bodyCollider = GetComponent<Collider>();
        if (bodyCollider == null)
            return;

        float visualMinY = float.PositiveInfinity;
        foreach (Renderer renderer in renderers)
            visualMinY = Mathf.Min(visualMinY, renderer.bounds.min.y);

        if (float.IsInfinity(visualMinY))
            return;

        float colliderMinY = bodyCollider.bounds.min.y;
        float floatingGap = visualMinY - colliderMinY;
        float correction = (floatingGap - Mathf.Max(0f, autoLowerTolerance)) * Mathf.Clamp01(autoLowerStrength);
        if (correction <= 0f)
            return;

        movementVisualRoot.localPosition -= new Vector3(0f, correction, 0f);
        movementVisualRootInitialLocalPosition = movementVisualRoot.localPosition;
    }

    private void CenterVisualOnTile(Renderer[] renderers)
    {
        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            combinedBounds.Encapsulate(renderers[i].bounds);

        Vector3 worldCenterOffset = combinedBounds.center - transform.position;
        Vector3 planarOffset = new Vector3(worldCenterOffset.x, 0f, worldCenterOffset.z);

        if (planarOffset.sqrMagnitude <= 0.0001f)
            return;

        movementVisualRoot.localPosition -= transform.InverseTransformVector(planarOffset);
        movementVisualRootInitialLocalPosition = movementVisualRoot.localPosition;
    }

    [ContextMenu("Recalculate Visual Grounding")]
    private void RecalculateVisualGrounding()
    {
        CacheMovementVisualRoot();
        LowerVisualIfFloating();
    }

    private void NormalizeRendererMaterialsForUrp()
    {
        Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLitShader == null)
            return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < materials.Length; i++)
            {
                if (!ShouldConvertToUrpLit(materials[i]))
                    continue;

                materials[i] = CreateUrpLitMaterial(materials[i], urpLitShader);
                changed = true;
            }

            if (changed)
                renderer.sharedMaterials = materials;
        }
    }

    private static bool ShouldConvertToUrpLit(Material material)
    {
        if (material == null)
            return true;

        Shader shader = material.shader;
        if (shader == null)
            return true;

        if (shader.name == "Hidden/InternalErrorShader")
            return true;

        if (shader.name == "Standard")
            return true;

        if (!shader.isSupported)
            return true;

        return !material.HasProperty("_BaseMap") && material.HasProperty("_MainTex");
    }

    private static Material CreateUrpLitMaterial(Material source, Shader urpLitShader)
    {
        Material converted = new Material(urpLitShader);

        if (source == null)
            return converted;

        converted.name = source.name;

        Texture baseTexture = GetTextureSafe(source, "_BaseMap");
        if (baseTexture == null)
            baseTexture = GetTextureSafe(source, "_MainTex");
        if (baseTexture == null)
            baseTexture = source.mainTexture;

        if (baseTexture != null)
        {
            converted.SetTexture("_BaseMap", baseTexture);
            converted.SetTexture("_MainTex", baseTexture);
        }

        Texture normalMap = GetTextureSafe(source, "_BumpMap");
        if (normalMap != null)
            converted.SetTexture("_BumpMap", normalMap);

        Texture metallicMap = GetTextureSafe(source, "_MetallicGlossMap");
        if (metallicMap != null)
            converted.SetTexture("_MetallicGlossMap", metallicMap);

        Texture occlusionMap = GetTextureSafe(source, "_OcclusionMap");
        if (occlusionMap != null)
            converted.SetTexture("_OcclusionMap", occlusionMap);

        Texture emissionMap = GetTextureSafe(source, "_EmissionMap");
        if (emissionMap != null)
            converted.SetTexture("_EmissionMap", emissionMap);

        Color baseColor = Color.white;
        if (source.HasProperty("_BaseColor"))
            baseColor = source.GetColor("_BaseColor");
        else if (source.HasProperty("_Color"))
            baseColor = source.GetColor("_Color");
        converted.SetColor("_BaseColor", baseColor);
        converted.SetColor("_Color", baseColor);

        if (source.HasProperty("_Metallic"))
            converted.SetFloat("_Metallic", source.GetFloat("_Metallic"));
        if (source.HasProperty("_Glossiness"))
            converted.SetFloat("_Smoothness", source.GetFloat("_Glossiness"));
        else if (source.HasProperty("_Smoothness"))
            converted.SetFloat("_Smoothness", source.GetFloat("_Smoothness"));
        if (source.HasProperty("_BumpScale"))
            converted.SetFloat("_BumpScale", source.GetFloat("_BumpScale"));
        if (source.HasProperty("_OcclusionStrength"))
            converted.SetFloat("_OcclusionStrength", source.GetFloat("_OcclusionStrength"));

        return converted;
    }

    private static Texture GetTextureSafe(Material source, string propertyName)
    {
        if (!source.HasProperty(propertyName))
            return null;

        return source.GetTexture(propertyName);
    }

    private bool CanApplyVisualOverrideMaterial()
    {
        if (visualOverrideMaterial == null)
            return false;

        if (visualOverrideMaterial.HasProperty("_BaseMap") && visualOverrideMaterial.GetTexture("_BaseMap") != null)
            return true;

        return visualOverrideMaterial.mainTexture != null;
    }

    protected virtual void OnDestroy()
    {
        // Final safety check to ensure this enemy NEVER leaves a ghost tile
        OccupiedTiles.Remove(GetGridKey(transform.position));
        if (isMoving) OccupiedTiles.Remove(GetGridKey(targetPosition));
        StopAllAnimationPlayback(false);
    }

    protected virtual void OnDisable()
    {
        StopAllAnimationPlayback(false);
    }
}
