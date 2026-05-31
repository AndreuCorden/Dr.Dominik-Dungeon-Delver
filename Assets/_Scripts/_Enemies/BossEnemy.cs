using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class BossEnemy : EnemyFollower
{
    [Header("Boss Health Settings")]
    public int health = 3;
    [SerializeField][Range(0f, 1f)] private float volume = 0.9f;

    [Header("Boss Death")]
    [SerializeField] private float bossDeathDuration = 3f;

    [Header("Boss Attack")]
    [SerializeField] private float attackAnimDuration = 2.467f;

    // =========================================================
    // UPDATED: FIRE BREATH ATTACK CONFIGURATIONS (NO DAMAGE STICK)
    // =========================================================
    [Header("Fire Breath Settings")]
    public GameObject pixelFirePrefab;
    [HideInInspector] public Transform shootPoint; // Automatically assigned by the level generator code

    public int cubesPerBurst = 20;
    public float burstDuration = 0.5f;
    public float coneAngle = 15f;

    [Header("Boss Damage Feedback")]
    [SerializeField] private float damageFlashDuration = 0.4f;
    [SerializeField] private int damageFlashPulses = 3;
    [SerializeField] private Color damageFlashColor = new Color(1f, 0.12f, 0.12f, 1f);

    private struct MaterialColorSnapshot
    {
        public Material Material;
        public Color OriginalColor;
        public Color OriginalBaseColor;
        public bool HasColor;
        public bool HasBaseColor;
    }

    private MaterialColorSnapshot[] materialSnapshots;
    private Light[] eyeLights;
    private Color[] eyeLightOriginalColors;
    private Coroutine damageFlashRoutine;

    // A helper list to track all 3 coordinates this Boss currently spans
    private List<Vector2> currentOccupiedKeys = new List<Vector2>();
    private List<Vector2> targetOccupiedKeys = new List<Vector2>();

    // NEW STATE TRACKER: Prevents the movement AI loops from running mid-animation
    private bool isAttacking = false;

    protected override void Start()
    {
        // Find player and round center position using BaseEnemy initialization rules
        player = GameObject.FindGameObjectWithTag("Player").transform;
        targetPosition = RoundToGrid(transform.position);
        transform.position = targetPosition;

        // Force a starting forward direction so our side-tile offsets calculate correctly
        if (transform.forward == Vector3.zero) transform.forward = Vector3.forward;

        // Explicitly map all 3 tiles we are starting on
        UpdateOccupiedTilesMap(transform.position, transform.forward, currentOccupiedKeys);

        // Register all 3 blocks into the global system
        foreach (Vector2 key in currentOccupiedKeys)
        {
            OccupiedTiles.Add(key);
        }

        nextMoveTime = Time.time;

        InitializeAnimations();
        CacheDamageFlashTargets();
    }

    // =========================================================
    // MODIFIED: ATTACK TRIGGERS VISUAL FIRE BURST AND DIRECT PLAYER DAMAGE
    // =========================================================
    protected override void PerformAttack(Vector3 dir)
    {
        if (isDying) return;

        transform.forward = dir;
        isMoving = false;
        SetMoving(false);
        TriggerAttack();

        // Standardized project master volume calculation logic
        if (AudioManager.Instance != null && attackSFX != null)
            AudioManager.Instance.PlaySFX(attackSFX, transform.position, sfxVolume);

        // Standard Proximity Damage: Hurts player instantly if they are within the attack zone
        if (player.TryGetComponent<PlayerController>(out var pc))
            pc.TakeDamage(false, transform.position);

        // Run fire breath particle stream burst purely for visual effect/juice!
        StartCoroutine(ExecuteFireBreathSequence());

        nextMoveTime = Time.time + attackAnimDuration;
    }

    // =========================================================
    // VISUAL FIRE STREAM COROUTINE GENERATOR LOOP
    // =========================================================
    private IEnumerator ExecuteFireBreathSequence()
    {
        isAttacking = true; // Lock out movement steps!
        
        float elapsed = 0;
        float spawnRate = burstDuration / Mathf.Max(cubesPerBurst, 1);

        while (elapsed < burstDuration)
        {
            SpawnFirePixel();
            elapsed += spawnRate;
            yield return new WaitForSeconds(spawnRate);
        }

        // Wait out the rest of the physical animation length before unlocking AI tracking
        float remainingTime = attackAnimDuration - burstDuration;
        if (remainingTime > 0f)
        {
            yield return new WaitForSeconds(remainingTime);
        }

        isAttacking = false; // Safely unlock movement now that full animation has played out!
    }

    private void SpawnFirePixel()
    {
        // Use either the programmatically generated snout location or fall back to center body
        Transform originNode = (shootPoint != null) ? shootPoint : transform;

        Quaternion randomRot = originNode.rotation * Quaternion.Euler(
            Random.Range(-coneAngle, coneAngle),
            Random.Range(-coneAngle, coneAngle),
            0
        );

        if (pixelFirePrefab != null)
        {
            Instantiate(pixelFirePrefab, originNode.position, randomRot);
        }
    }

    protected override void StartMovement()
    {
        moveStartPosition = transform.position;
        isMoving = true;
        SetMoving(true);
    }

    private Vector3 moveStartPosition;

    // --- OVERRIDDEN MULTI-TILE VALIDATION AND MOVEMENT ---

    protected new bool TryMove(Vector3 direction)
    {
        // Safety lock: if animating an attack, do not attempt to process steps or shift forward vectors
        if (isAttacking) return false;

        // --- FIX: ATTACK RANGE CHECK BEFORE MOVEMENT CALCULATIONS ---
        // Calculate the grid tile that is exactly 2 blocks away from our *current* center
        Vector3 attackCheckPos = RoundToGrid(transform.position + (direction * 2f));
        Vector2 attackCheckKey = GetGridKey(attackCheckPos);
        Vector2 playerKey = GetGridKey(player.position);

        // If the player is standing on the center line 2 blocks ahead, attack!
        if (playerKey == attackCheckKey)
        {
            PerformAttack(direction);
            return true;
        }

        // Also check if the player is standing 2 blocks ahead but diagonally touching our wings
        Vector3 rightOffset = Vector3.Cross(Vector3.up, direction).normalized;
        Vector2 attackLeftWingKey = GetGridKey(attackCheckPos - rightOffset);
        Vector2 attackRightWingKey = GetGridKey(attackCheckPos + rightOffset);

        if (playerKey == attackLeftWingKey || playerKey == attackRightWingKey)
        {
            PerformAttack(direction);
            return true;
        }
        // -------------------------------------------------------------

        Vector3 centerDest3D = RoundToGrid(transform.position + direction);

        // Calculate what our 3-tile footprint will look like at the destination
        UpdateOccupiedTilesMap(centerDest3D, direction, targetOccupiedKeys);

        // Multi-Tile Validation Check Loop
        foreach (Vector2 targetKey in targetOccupiedKeys)
        {
            Vector3 worldCheckPos = new Vector3(targetKey.x, centerDest3D.y, targetKey.y);

            // Does a physical floor tile exist under this specific footprint segment?
            bool segmentHasFloor = Physics.Raycast(worldCheckPos + Vector3.up, Vector3.down, 2f, floorLayer);
            if (!segmentHasFloor) return false; // Entire move fails if any part hangs over a void

            // Is this tile blocked by another enemy? 
            if (OccupiedTiles.Contains(targetKey) && !currentOccupiedKeys.Contains(targetKey))
            {
                return false; // Path blocked by another entity
            }
        }

        // Movement Execution: Clear old footprint coordinates, write new ones
        foreach (Vector2 oldKey in currentOccupiedKeys) OccupiedTiles.Remove(oldKey);
        foreach (Vector2 newKey in targetOccupiedKeys) OccupiedTiles.Add(newKey);

        // Swap local trackers
        List<Vector2> temp = currentOccupiedKeys;
        currentOccupiedKeys = targetOccupiedKeys;
        targetOccupiedKeys = temp;

        // --- PLAY BOSS WALK SFX ---
        if (AudioManager.Instance != null && moveSFX != null)
        {
            AudioManager.Instance.PlaySFX(moveSFX, transform.position, volume);
        }

        targetPosition = centerDest3D;
        transform.forward = direction;
        StartMovement();
        return true;
    }

    protected override void Update()
    {
        if (isDying) return;
        if (isFalling) { HandleFalling(); return; }

        if (isMoving)
        {
            float stepDuration = 1f / Mathf.Max(moveSpeed, 0.01f);
            moveTimer += Time.deltaTime;
            float t = stepDuration <= Mathf.Epsilon ? 1f : Mathf.Clamp01(moveTimer / stepDuration);
            transform.position = Vector3.Lerp(moveStartPosition, targetPosition, t);

            if (t >= 1f)
                FinishMovement();
        }
        else
        {
            transform.position = targetPosition;
            CheckForVoid();
            
            // MODIFIED: Added !isAttacking check to prevent calculation attempts mid-animation
            if (!isFalling && !isAttacking && Time.time >= nextMoveTime)
                DetermineNextStep();
        }
    }

    private float moveTimer;

    protected override void FinishMovement()
    {
        transform.position = targetPosition;
        isMoving = false;
        moveTimer = 0f;
        SetMoving(false);
        nextMoveTime = Time.time + timeBetweenSteps;

        // Double-check alignment precision for all 3 tracked sub-tiles
        UpdateOccupiedTilesMap(transform.position, transform.forward, currentOccupiedKeys);
    }

    protected override void DetermineNextStep()
    {
        // Absolute check backup
        if (isAttacking) return;

        Vector3 diff = player.position - transform.position;
        Vector3 primary = Mathf.Abs(diff.x) > Mathf.Abs(diff.z) ?
            new Vector3(Mathf.Sign(diff.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(diff.z));

        // Call our localized multi-tile TryMove instead of base.TryMove
        if (!TryMove(primary))
        {
            Vector3 secondary = (primary.x != 0) ? new Vector3(0, 0, Mathf.Sign(diff.z)) : new Vector3(Mathf.Sign(diff.x), 0, 0);
            TryMove(secondary);
        }
    }

    // --- OVERRIDDEN VOID DETECTION ---

    protected override void CheckForVoid()
    {
        if (!Physics.Raycast(transform.position + Vector3.up, Vector3.down, 2f, floorLayer))
        {
            isFalling = true;
            ClearEntireFootprint();
        }
    }

    // --- DAMAGE AND LIFECYCLE MANAGEMENT ---

    public override void Die(Vector3? deathSourcePosition = null)
    {
        if (isDying) return;

        health -= 1;
        Debug.Log($"Boss took damage! Health remaining: {health}");

        if (health > 0 && !isFalling)
        {
            FaceDeathSource(deathSourcePosition);
            PlayDamageFlash();
            return;
        }

        StartCoroutine(BossDeathSequence(deathSourcePosition));
    }

    private void CacheDamageFlashTargets()
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        var snapshots = new List<MaterialColorSnapshot>();

        foreach (var renderer in renderers)
        {
            if (renderer is MeshRenderer meshRenderer && !meshRenderer.enabled)
                continue;

            foreach (var material in renderer.materials)
            {
                if (material == null) continue;

                var snapshot = new MaterialColorSnapshot { Material = material };
                if (material.HasProperty("_Color"))
                {
                    snapshot.HasColor = true;
                    snapshot.OriginalColor = material.color;
                }
                if (material.HasProperty("_BaseColor"))
                {
                    snapshot.HasBaseColor = true;
                    snapshot.OriginalBaseColor = material.GetColor("_BaseColor");
                }
                snapshots.Add(snapshot);
            }
        }

        materialSnapshots = snapshots.ToArray();
        eyeLights = GetComponentsInChildren<Light>(true);
        eyeLightOriginalColors = new Color[eyeLights.Length];
        for (int i = 0; i < eyeLights.Length; i++)
            eyeLightOriginalColors[i] = eyeLights[i].color;
    }

    private void PlayDamageFlash()
    {
        if (materialSnapshots == null || materialSnapshots.Length == 0)
            CacheDamageFlashTargets();

        if (damageFlashRoutine != null)
            StopCoroutine(damageFlashRoutine);

        damageFlashRoutine = StartCoroutine(DamageFlashRoutine());
    }

    private IEnumerator DamageFlashRoutine()
    {
        float pulseDuration = damageFlashDuration / Mathf.Max(damageFlashPulses, 1);

        for (int pulse = 0; pulse < damageFlashPulses; pulse++)
        {
            ApplyDamageFlashColor(damageFlashColor);
            yield return new WaitForSeconds(pulseDuration * 0.45f);
            RestoreDamageFlashColors();
            yield return new WaitForSeconds(pulseDuration * 0.55f);
        }

        damageFlashRoutine = null;
    }

    private void ApplyDamageFlashColor(Color flashColor)
    {
        foreach (var snapshot in materialSnapshots)
        {
            if (snapshot.Material == null) continue;

            if (snapshot.HasColor)
                snapshot.Material.color = flashColor;
            if (snapshot.HasBaseColor)
                snapshot.Material.SetColor("_BaseColor", flashColor);
        }

        for (int i = 0; i < eyeLights.Length; i++)
        {
            if (eyeLights[i] != null)
                eyeLights[i].color = flashColor;
        }
    }

    private void RestoreDamageFlashColors()
    {
        foreach (var snapshot in materialSnapshots)
        {
            if (snapshot.Material == null) continue;

            if (snapshot.HasColor)
                snapshot.Material.color = snapshot.OriginalColor;
            if (snapshot.HasBaseColor)
                snapshot.Material.SetColor("_BaseColor", snapshot.OriginalBaseColor);
        }

        for (int i = 0; i < eyeLights.Length; i++)
        {
            if (eyeLights[i] != null)
                eyeLights[i].color = eyeLightOriginalColors[i];
        }
    }

    private IEnumerator BossDeathSequence(Vector3? deathSourcePosition)
    {
        if (damageFlashRoutine != null)
        {
            StopCoroutine(damageFlashRoutine);
            damageFlashRoutine = null;
        }
        RestoreDamageFlashColors();

        isDying = true;
        isMoving = false;
        SetMoving(false);
        ClearEntireFootprint();
        FaceDeathSource(deathSourcePosition);
        TriggerDie();
        PlayDeathSfx();
        DisableColliders();

        float deathDuration = bossDeathDuration;
        if (animatorOverride != null && animatorOverride.runtimeAnimatorController != null)
        {
            yield return null;
            AnimatorStateInfo state = animatorOverride.GetCurrentAnimatorStateInfo(0);
            if (state.IsName("Die") && state.length > 0f)
                deathDuration = state.length;
        }

        Light[] lights = GetComponentsInChildren<Light>();
        float[] lightStart = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++)
            lightStart[i] = lights[i].intensity;

        float elapsed = 0f;
        while (elapsed < deathDuration)
        {
            float t = Mathf.Clamp01(elapsed / deathDuration);
            for (int i = 0; i < lights.Length; i++)
                lights[i].intensity = Mathf.Lerp(lightStart[i], 0f, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (NavigationManager.Instance != null)
            NavigationManager.Instance.OpenCreditsScene();
        else
            SceneManager.LoadScene("Credits");

        Destroy(gameObject);
    }

    protected override void OnDestroy()
    {
        ClearEntireFootprint();
    }

    // --- FOOTPRINT CALCULATOR UTILITIES ---

    private void UpdateOccupiedTilesMap(Vector3 centerPos, Vector3 forwardDir, List<Vector2> listToFill)
    {
        listToFill.Clear();
        Vector3 rightOffset = Vector3.Cross(Vector3.up, forwardDir).normalized;

        Vector3 centerTile = centerPos;
        Vector3 leftTile = centerPos - rightOffset;
        Vector3 rightTile = centerPos + rightOffset;

        listToFill.Add(GetGridKey(centerTile));
        listToFill.Add(GetGridKey(leftTile));
        listToFill.Add(GetGridKey(rightTile));
    }

    private void ClearEntireFootprint()
    {
        foreach (Vector2 key in currentOccupiedKeys) OccupiedTiles.Remove(key);
        foreach (Vector2 key in targetOccupiedKeys) OccupiedTiles.Remove(key);
    }
}