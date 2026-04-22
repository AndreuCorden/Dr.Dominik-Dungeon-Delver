using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance;
    public static event Action<int> OnHealthChanged;
    public static event Action<int> OnCoinsChanged;
    public int health = 3;
    public int coins = 0;
    public float moveSpeed = 5f;

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
    private bool isMoving = false;

    private LevelHandler levelHandler;

    void Start()
    {
        targetPosition = new Vector3(Mathf.Round(transform.position.x), transform.position.y, Mathf.Round(transform.position.z));
        transform.position = targetPosition;
        levelHandler = GameObject.FindFirstObjectByType<LevelHandler>();
        EnemyFollower.OccupiedTiles.Add(targetPosition);
    }

    void Awake()
    {
        // --- SINGLETON PATTERN ---
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep this object alive!
        }
        else
        {
            Destroy(gameObject); // Kill the duplicate that just spawned
            return;
        }
    }

    void Update()
    {
        if (!isMoving)
        {
            Vector3 direction = Vector3.zero;

            if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame) direction = Vector3.forward;
            else if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame) direction = Vector3.back;
            else if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame) direction = Vector3.left;
            else if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame) direction = Vector3.right;
            else if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                PerformSpaceAttack();
            }

            if (direction != Vector3.zero)
            {
                // REMOVED: CheckForEnemyInTile (No more bumping to kill)
                if (IsDestinationSafe(direction))
                {
                    Move(direction);
                }
            }
        }

        if (!isMoving) { CheckForVoid(); }

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * currentMoveMultiplier * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            transform.position = targetPosition;
            isMoving = false;

            // Check for slime once we land on a new tile
            ResetSpeedIfNoSlime();
        }
    }

    bool IsDestinationSafe(Vector3 direction)
    {
        Vector3 dest = targetPosition + direction;
        // Round to match the HashSet format exactly
        dest = new Vector3(Mathf.Round(dest.x), dest.y, Mathf.Round(dest.z));

        // 1. Check for Floor
        Ray ray = new Ray(new Vector3(dest.x, 2.0f, dest.z), Vector3.down);
        if (!Physics.Raycast(ray, out RaycastHit hit, 1.5f, floorLayer)) return false;

        // 2. NEW: Check the Global Occupancy List
        // This prevents you from walking into a tile an enemy is currently moving toward
        if (EnemyFollower.OccupiedTiles.Contains(dest))
        {
            Debug.Log("Tile is claimed by an enemy!");
            return false;
        }

        // 3. Physical Check (Backup)
        if (Physics.CheckSphere(dest, 0.3f, enemyLayer)) return false;

        return true;
    }

    void Move(Vector3 direction)
    {
        // Remove old position from global occupancy
        EnemyFollower.OccupiedTiles.Remove(targetPosition);

        targetPosition = targetPosition + direction;

        // Claim the new position
        EnemyFollower.OccupiedTiles.Add(targetPosition);

        isMoving = true;
        transform.forward = direction;
    }

    void CheckForVoid()
    {
        // Cast a ray straight down from the player's center
        // We check slightly further than the floor height (1.1f)
        Ray ray = new Ray(transform.position, Vector3.down);

        if (!Physics.Raycast(ray, out RaycastHit hit, 1.1f, floorLayer))
        {
            // NO FLOOR FOUND! 
            StartCoroutine(HandleFallingDeath());
        }
    }

    System.Collections.IEnumerator HandleFallingDeath()
    {
        if (isMoving && transform.position.y < 0) { /* already falling */ }
        isMoving = true;

        float fallTimer = 0;
        while (fallTimer < 1.0f) // Shortened to 1 second for snappier feel
        {
            transform.Translate(Vector3.down * Time.deltaTime * 10f);
            transform.Rotate(Vector3.up * Time.deltaTime * 500f);
            fallTimer += Time.deltaTime;
            yield return null;
        }

        // CRITICAL: Set isMoving to false before reloading so the new scene 
        // doesn't think we are still in the middle of a move.
        isMoving = false;

        TakeDamage(true);

        // After TakeDamage(true) loads a scene, this instance persists, 
        // so we MUST stop this coroutine from continuing.
        yield break;
    }

    public void TakeDamage(bool isFall = false)
    {
        ChangeHealth(-1);

        if (health <= 0)
        {
            Debug.Log("Game Over!");
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
        Debug.Log("Performing Area Attack!");

        // 1. Flash the player Cyan
        StartCoroutine(VisualFlash());

        // 2. Spawn and grow the shockwave
        if (shockwavePrefab != null)
        {
            // Spawn at player feet, starting at scale 0
            GameObject visual = Instantiate(shockwavePrefab, transform.position, Quaternion.identity);
            visual.transform.localScale = Vector3.zero;

            Destroy(visual, 0.25f); // Destroy shortly after it expands
        }

        // 3. Damage Logic
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Enemy"))
            {
                if (hitCollider.TryGetComponent<BaseEnemy>(out var enemy))
                {
                    enemy.Die();
                }
                else
                {
                    Destroy(hitCollider.gameObject); // Fallback
                }
            }
        }
    }

    System.Collections.IEnumerator VisualFlash()
    {
        Renderer r = GetComponent<Renderer>();
        if (r != null)
        {
            Color oldColor = r.material.color;
            float elapsed = 0f;
            float duration = 0.5f; // Total flash time

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                // Gradually shift from Cyan back to the original color
                r.material.color = Color.Lerp(Color.cyan, oldColor, elapsed / duration);
                yield return null;
            }
            r.material.color = oldColor;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    void ResetSpeedIfNoSlime()
    {
        // We only need to check this if we are currently slowed
        if (currentMoveMultiplier < 1.0f)
        {
            // Use a box that covers the floor tile area
            Collider[] hitColliders = Physics.OverlapBox(transform.position, new Vector3(0.4f, 0.1f, 0.4f));
            bool foundSlime = false;

            foreach (var col in hitColliders)
            {
                if (col.CompareTag("Slime"))
                {
                    foundSlime = true;
                    break;
                }
            }

            if (!foundSlime)
            {
                currentMoveMultiplier = 1.0f;
            }
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
        // 1. Force stop the falling coroutine or any flashes
        StopAllCoroutines();

        // 2. Clear old occupancy before moving
        // We use targetPosition because that's what was "claimed" last
        BaseEnemy.OccupiedTiles.Remove(targetPosition);

        // 3. Snap to the new position
        transform.position = newSpawnPos;
        targetPosition = newSpawnPos;
        transform.rotation = Quaternion.identity;

        // 4. Reset movement flags
        isMoving = false;
        currentMoveMultiplier = 1.0f;

        // 5. Claim the new starting tile
        BaseEnemy.OccupiedTiles.Add(targetPosition);

        Renderer r = GetComponent<Renderer>();
        if (r != null)
        {
            // Change "Color.white" to whatever your player's default color is
            r.material.color = Color.white;
        }
    }
}