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

    [Header("Fire Breath Settings")]
    public GameObject pixelFirePrefab;
    [HideInInspector] public Transform shootPoint; 

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

    // Helper lists to track the 6 coordinates (3x2 footprint) this Boss occupies
    private List<Vector2> currentOccupiedKeys = new List<Vector2>();
    private List<Vector2> targetOccupiedKeys = new List<Vector2>();

    private bool isAttacking = false;
    private Vector3 moveStartPosition;
    private float moveTimer;

    protected override void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        targetPosition = RoundToGrid(transform.position);
        transform.position = targetPosition;

        if (transform.forward == Vector3.zero) transform.forward = Vector3.forward;

        // Populate the starting 3x2 grid footprint
        UpdateOccupiedTilesMap(transform.position, transform.forward, currentOccupiedKeys);

        // Register all 6 core blocks into the global system
        foreach (Vector2 key in currentOccupiedKeys)
        {
            if (!OccupiedTiles.Contains(key))
                OccupiedTiles.Add(key);
        }

        nextMoveTime = Time.time;

        InitializeAnimations();
        CacheDamageFlashTargets();
    }

    protected override void PerformAttack(Vector3 dir)
    {
        if (isDying) return;

        transform.forward = dir;
        isMoving = false;
        SetMoving(false);
        TriggerAttack();

        if (AudioManager.Instance != null && attackSFX != null)
            AudioManager.Instance.PlaySFX(attackSFX, transform.position, sfxVolume);

        if (player.TryGetComponent<PlayerController>(out var pc))
            pc.TakeDamage(false, transform.position);

        StartCoroutine(ExecuteFireBreathSequence());

        nextMoveTime = Time.time + attackAnimDuration;
    }

    private IEnumerator ExecuteFireBreathSequence()
    {
        isAttacking = true; 
        
        float elapsed = 0;
        float spawnRate = burstDuration / Mathf.Max(cubesPerBurst, 1);

        while (elapsed < burstDuration)
        {
            SpawnFirePixel();
            elapsed += spawnRate;
            yield return new WaitForSeconds(spawnRate);
        }

        float remainingTime = attackAnimDuration - burstDuration;
        if (remainingTime > 0f)
        {
            yield return new WaitForSeconds(remainingTime);
        }

        isAttacking = false; 
    }

    private void SpawnFirePixel()
    {
        Transform originNode = (shootPoint != null) ? shootPoint : transform;

        // Fire cone vector calculations
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

    // --- UPGRADED: 3x2 FOOTPRINT TRYMOVE LOGIC ---
    protected new bool TryMove(Vector3 direction)
    {
        if (isAttacking || isDying) return false;

        // Range Check: Attack if the player is standing directly within the 3x2 footprint path ahead
        Vector3 attackCheckPos = RoundToGrid(transform.position + (direction * 2f));
        UpdateOccupiedTilesMap(attackCheckPos, direction, targetOccupiedKeys);
        Vector2 playerKey = GetGridKey(player.position);

        if (targetOccupiedKeys.Contains(playerKey))
        {
            PerformAttack(direction);
            return true;
        }

        Vector3 centerDest3D = RoundToGrid(transform.position + direction);

        // Recalculate target tiles for environmental/obstacle validation checks
        UpdateOccupiedTilesMap(centerDest3D, direction, targetOccupiedKeys);

        // Multi-Tile Validation Loop
        foreach (Vector2 targetKey in targetOccupiedKeys)
        {
            Vector3 worldCheckPos = new Vector3(targetKey.x, centerDest3D.y, targetKey.y);

            // 1. Structural Floor Validation
            bool segmentHasFloor = Physics.Raycast(worldCheckPos + Vector3.up, Vector3.down, 2f, floorLayer);
            if (!segmentHasFloor) return false; 

            // 2. Occupied Block checking (Disregard tiles we currently already own)
            if (OccupiedTiles.Contains(targetKey) && !currentOccupiedKeys.Contains(targetKey))
            {
                return false; 
            }
        }

        // SWAP OCCUPATION MAPS: Clear old footprint coordinates out, register new ones
        foreach (Vector2 oldKey in currentOccupiedKeys) OccupiedTiles.Remove(oldKey);
        foreach (Vector2 newKey in targetOccupiedKeys) OccupiedTiles.Add(newKey);

        List<Vector2> temp = currentOccupiedKeys;
        currentOccupiedKeys = targetOccupiedKeys;
        targetOccupiedKeys = temp;

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
            
            if (!isFalling && !isAttacking && Time.time >= nextMoveTime)
                DetermineNextStep();
        }
    }

    protected override void FinishMovement()
    {
        transform.position = targetPosition;
        isMoving = false;
        moveTimer = 0f;
        SetMoving(false);
        nextMoveTime = Time.time + timeBetweenSteps;

        // Re-align precision profiles
        UpdateOccupiedTilesMap(transform.position, transform.forward, currentOccupiedKeys);
    }

    protected override void DetermineNextStep()
    {
        if (isAttacking) return;

        Vector3 diff = player.position - transform.position;
        Vector3 primary = Mathf.Abs(diff.x) > Mathf.Abs(diff.z) ?
            new Vector3(Mathf.Sign(diff.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(diff.z));

        if (!TryMove(primary))
        {
            Vector3 secondary = (primary.x != 0) ? new Vector3(0, 0, Mathf.Sign(diff.z)) : new Vector3(Mathf.Sign(diff.x), 0, 0);
            TryMove(secondary);
        }
    }

    protected override void CheckForVoid()
    {
        if (!Physics.Raycast(transform.position + Vector3.up, Vector3.down, 2f, floorLayer))
        {
            isFalling = true;
            ClearEntireFootprint();
        }
    }

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

        // If dead, wipe keys instantly so entities don't get trapped by an ongoing corpse animation sequence
        ClearEntireFootprint();
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
            boxOriginalColor(i);
    }

    private void boxOriginalColor(int i) => eyeLightOriginalColors[i] = eyeLights[i].color;

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
        // Guard checking prevents clearing a spot that a brand-new spawn just claimed!
        if (!isDying)
        {
            ClearEntireFootprint();
        }
    }

    // =========================================================
    // DYNAMIC 3x2 BOUNDING BOX FOOTPRINT GENERATOR
    // =========================================================
    private void UpdateOccupiedTilesMap(Vector3 centerPos, Vector3 forwardDir, List<Vector2> listToFill)
    {
        listToFill.Clear();

        // Round directions to absolute clean grid cardinals
        Vector3 fwd = new Vector3(Mathf.Round(forwardDir.x), 0f, Mathf.Round(forwardDir.z)).normalized;
        if (fwd.sqrMagnitude < 0.1f) fwd = Vector3.forward;

        // Calculate a perpendicular right vector relative to our forward look vector
        Vector3 side = new Vector3(-fwd.z, 0f, fwd.x); 

        // Let centerPos be the front-row center tile. 
        // The front row is 3 tiles wide (Left, Center, Right)
        Vector3 frontCenter = centerPos;
        Vector3 frontLeft = centerPos - side;
        Vector3 frontRight = centerPos + side;

        // The back row sits exactly 1 step backwards behind the front row (-fwd)
        Vector3 backCenter = frontCenter - fwd;
        Vector3 backLeft = frontLeft - fwd;
        Vector3 backRight = frontRight - fwd;

        // Register all 6 tile vectors securely into the collection output
        listToFill.Add(GetGridKey(frontCenter));
        listToFill.Add(GetGridKey(frontLeft));
        listToFill.Add(GetGridKey(frontRight));
        listToFill.Add(GetGridKey(backCenter));
        listToFill.Add(GetGridKey(backLeft));
        listToFill.Add(GetGridKey(backRight));
    }

    private void ClearEntireFootprint()
    {
        foreach (Vector2 key in currentOccupiedKeys) OccupiedTiles.Remove(key);
        foreach (Vector2 key in targetOccupiedKeys) OccupiedTiles.Remove(key);
    }
}