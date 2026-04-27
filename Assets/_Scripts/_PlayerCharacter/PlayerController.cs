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
    private Vector3 visualRootInitialLocalPosition;
    private float movementVisualYOffset;
    private Animator characterAnimator;

    [Header("Emotes")]
    [SerializeField] private AnimationClip[] emoteClips;
    [SerializeField] private float emoteBlendDuration = 0.08f;
    private PlayableGraph emoteGraph;
    private bool isEmotePlaying;
    private float emoteTimer;
    private float currentEmoteDuration;

    [Header("Status Effects")]
    public float currentMoveMultiplier = 1.0f;

    [Header("Attack Settings")]
    public float attackRange = 1.1f;

    [Header("VFX")]
    public GameObject shockwavePrefab;

    [Header("Layers")]
    public LayerMask floorLayer;
    public LayerMask enemyLayer;

    private Vector3 targetPosition;
    private Vector3 moveStartPosition;
    private bool isMoving = false;
    private bool isStepMoving = false;

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

        targetPosition = RoundGridPosition(transform.position);
        moveStartPosition = targetPosition;
        transform.position = targetPosition;

        BaseEnemy.OccupiedTiles.Add(targetPosition);
    }

    void LateUpdate()
    {
        if (visualRoot != null)
            visualRoot.localPosition = visualRootInitialLocalPosition + new Vector3(0f, visualYOffset + movementVisualYOffset, 0f);
    }

    void Update()
    {
        UpdateEmoteInputAndPlayback();

        Keyboard kb = Keyboard.current;
        if (!isMoving && kb != null)
        {
            Vector3 direction = Vector3.zero;

            if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) direction = Vector3.forward;
            else if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) direction = Vector3.back;
            else if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) direction = Vector3.left;
            else if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) direction = Vector3.right;
            else if (kb.spaceKey.wasPressedThisFrame) PerformSpaceAttack();

            if (direction != Vector3.zero && IsDestinationSafe(direction))
                Move(direction);
        }

        if (!isMoving)
            CheckForVoid();

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * currentMoveMultiplier * Time.deltaTime);

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

    void UpdateEmoteInputAndPlayback()
    {
        if (isEmotePlaying)
        {
            emoteTimer += Time.deltaTime;
            if (emoteTimer >= currentEmoteDuration)
                StopCurrentEmote();
            return;
        }

        if (isMoving || Keyboard.current == null || !Keyboard.current.gKey.wasPressedThisFrame)
            return;

        TryPlayRandomEmote();
    }

    void TryPlayRandomEmote()
    {
        if (characterAnimator == null || emoteClips == null || emoteClips.Length == 0)
            return;

        int index = UnityEngine.Random.Range(0, emoteClips.Length);
        AnimationClip clip = emoteClips[index];
        if (clip == null)
            return;

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
        float totalDistance = Vector3.Distance(moveStartPosition, targetPosition);
        if (totalDistance <= Mathf.Epsilon)
        {
            movementVisualYOffset = 0f;
            return;
        }

        float remainingDistance = Vector3.Distance(transform.position, targetPosition);
        float progress = Mathf.Clamp01(1f - (remainingDistance / totalDistance));
        movementVisualYOffset = Mathf.Sin(progress * Mathf.PI) * jumpHeight;
    }

    bool IsDestinationSafe(Vector3 direction)
    {
        Vector3 dest = RoundGridPosition(targetPosition + direction);
        Ray ray = new Ray(new Vector3(dest.x, 2.0f, dest.z), Vector3.down);

        if (!Physics.Raycast(ray, out _, 1.5f, floorLayer))
            return false;

        if (BaseEnemy.OccupiedTiles.Contains(dest))
            return false;

        if (Physics.CheckSphere(dest, 0.3f, enemyLayer))
            return false;

        return true;
    }

    void Move(Vector3 direction)
    {
        StopCurrentEmote();

        BaseEnemy.OccupiedTiles.Remove(RoundGridPosition(targetPosition));

        moveStartPosition = targetPosition;
        targetPosition = RoundGridPosition(targetPosition + direction);
        BaseEnemy.OccupiedTiles.Add(targetPosition);

        isMoving = true;
        isStepMoving = true;
        transform.forward = direction;
    }

    void CheckForVoid()
    {
        Ray ray = new Ray(transform.position, Vector3.down);
        if (!Physics.Raycast(ray, out _, 1.1f, floorLayer))
            StartCoroutine(HandleFallingDeath());
    }

    System.Collections.IEnumerator HandleFallingDeath()
    {
        StopCurrentEmote();
        isMoving = true;
        isStepMoving = false;
        movementVisualYOffset = 0f;

        float fallTimer = 0f;
        while (fallTimer < 1.0f)
        {
            transform.Translate(Vector3.down * Time.deltaTime * 10f);
            transform.Rotate(Vector3.up * Time.deltaTime * 500f);
            fallTimer += Time.deltaTime;
            yield return null;
        }

        isMoving = false;
        TakeDamage(true);
    }

    public void TakeDamage(bool isFall = false)
    {
        ChangeHealth(-1);

        if (health <= 0)
        {
            ChangeHealth(3);
            AddCoin(-coins);
            UnityEngine.SceneManagement.SceneManager.LoadScene(1);
            return;
        }

        if (isFall)
        {
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            UnityEngine.SceneManagement.SceneManager.LoadScene(currentSceneName);
        }
    }

    void PerformSpaceAttack()
    {
        StartCoroutine(VisualFlash());

        if (shockwavePrefab != null)
        {
            GameObject visual = Instantiate(shockwavePrefab, transform.position, Quaternion.identity);
            visual.transform.localScale = Vector3.zero;
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

    System.Collections.IEnumerator VisualFlash()
    {
        Renderer renderer = visualRoot != null ? visualRoot.GetComponentInChildren<Renderer>() : GetComponentInChildren<Renderer>();
        if (renderer == null)
            yield break;

        Color oldColor = renderer.material.color;
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            renderer.material.color = Color.Lerp(Color.cyan, oldColor, elapsed / duration);
            yield return null;
        }

        renderer.material.color = oldColor;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    void ResetSpeedIfNoSlime()
    {
        if (currentMoveMultiplier >= 1.0f)
            return;

        Collider[] hitColliders = Physics.OverlapBox(transform.position, new Vector3(0.4f, 0.1f, 0.4f));
        bool foundSlime = false;

        foreach (Collider col in hitColliders)
        {
            if (!col.CompareTag("Slime"))
                continue;

            foundSlime = true;
            break;
        }

        if (!foundSlime)
            currentMoveMultiplier = 1.0f;
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
        StopAllCoroutines();

        BaseEnemy.OccupiedTiles.Remove(RoundGridPosition(targetPosition));

        Vector3 snappedSpawn = RoundGridPosition(newSpawnPos);
        transform.position = snappedSpawn;
        targetPosition = snappedSpawn;
        moveStartPosition = snappedSpawn;
        transform.rotation = Quaternion.identity;

        isMoving = false;
        isStepMoving = false;
        movementVisualYOffset = 0f;
        currentMoveMultiplier = 1.0f;

        BaseEnemy.OccupiedTiles.Add(targetPosition);

        Renderer renderer = visualRoot != null ? visualRoot.GetComponentInChildren<Renderer>() : GetComponentInChildren<Renderer>();
        if (renderer != null)
            renderer.material.color = Color.white;
    }

    Vector3 RoundGridPosition(Vector3 pos)
    {
        return new Vector3(Mathf.Round(pos.x), pos.y, Mathf.Round(pos.z));
    }

    void OnDisable()
    {
        StopCurrentEmote();
    }
}
