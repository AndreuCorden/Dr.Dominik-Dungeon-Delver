using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
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

    public static int health = 3;

    private LevelHandler levelHandler;

    void Start()
    {
        targetPosition = new Vector3(Mathf.Round(transform.position.x), transform.position.y, Mathf.Round(transform.position.z));
        transform.position = targetPosition;
        levelHandler = Object.FindFirstObjectByType<LevelHandler>();
        EnemyFollower.OccupiedTiles.Add(targetPosition);
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
        isMoving = true;

        float fallTimer = 0;
        while (fallTimer < 1.5f)
        {
            transform.Translate(Vector3.down * Time.deltaTime * 10f);
            transform.Rotate(Vector3.up * Time.deltaTime * 500f);
            fallTimer += Time.deltaTime;
            yield return null;
        }

        // Pass 'true' because this was a fall, requiring a scene reset
        TakeDamage(true);
    }

    // Added a parameter 'isFall' to decide if we reload the scene
    public void TakeDamage(bool isFall = false)
    {
        health--;

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateHealth(health);

        if (health <= 0)
        {
            Debug.Log("Game Over!");
            health = 3;
            UnityEngine.SceneManagement.SceneManager.LoadScene(1);
        }
        else if (isFall)
        {
            // Only reload the scene if the player actually fell
            Debug.Log("Fell! Reloading scene...");
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            UnityEngine.SceneManagement.SceneManager.LoadScene(currentSceneName);
        }
        else
        {
            // Enemy hit or other damage: No reload, just a small "Ouch"
            Debug.Log("Hit by enemy! Health is now: " + health);

            // OPTIONAL: Add a small knockback or invincibility frames here
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
                Destroy(hitCollider.gameObject);
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
}