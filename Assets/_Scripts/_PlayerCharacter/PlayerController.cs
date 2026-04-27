using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class PlayerController : MonoBehaviour
{
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
    public LayerMask floorLayer; // Assign the "Floor" layer in the inspector
    private Vector3 targetPosition;
    private Vector3 moveStartPosition;
    private bool isMoving = false;
    private bool isStepMoving = false;

    public static int health = 3;

    private LevelHandler levelHandler;

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

        targetPosition = new Vector3(Mathf.Round(transform.position.x), transform.position.y, Mathf.Round(transform.position.z));
        moveStartPosition = targetPosition;
        transform.position = targetPosition;
        levelHandler = Object.FindFirstObjectByType<LevelHandler>();
    }

    void LateUpdate()
    {
        if (visualRoot != null)
            visualRoot.localPosition = visualRootInitialLocalPosition + new Vector3(0f, visualYOffset + movementVisualYOffset, 0f);
    }

    void Update()
    {
        UpdateEmoteInputAndPlayback();

        if (!isMoving)
        {
            Vector3 direction = Vector3.zero;

            if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame) direction = Vector3.forward;
            else if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame) direction = Vector3.back;
            else if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame) direction = Vector3.left;
            else if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame) direction = Vector3.right;

            if (direction != Vector3.zero)
            {
                // ONLY move if the destination is safe
                if (IsDestinationSafe(direction))
                {
                    Move(direction);
                }
            }
        }

        if (!isMoving)
        {
            CheckForVoid();
        }

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

        int index = Random.Range(0, emoteClips.Length);
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
        // Calculate where the player WANTS to go
        Vector3 dest = targetPosition + direction;

        // We cast a ray from the destination, pointing downwards
        // Origin is slightly above the floor (y=1), pointing down
        Ray ray = new Ray(new Vector3(dest.x, 2.0f, dest.z), Vector3.down);

        // If the ray hits something on the "Floor" layer within 1.5 units
        if (Physics.Raycast(ray, out RaycastHit hit, 1.5f, floorLayer))
        {
            // Destination is safe!
            return true;
        }

        if (!isMoving && !Physics.Raycast(transform.position, Vector3.down, 1.1f, floorLayer))
        {
            // Trigger Lose Heart / Game Over logic here
            Debug.Log("Player fell into the void!");
        }

        // Nothing was hit (the floor is gone or it's the edge of the world)
        Debug.Log("Path blocked: No floor at " + dest);
        return false;
    }

    void Move(Vector3 direction)
    {
        StopCurrentEmote();

        moveStartPosition = targetPosition;
        targetPosition = targetPosition + direction; // Move relative to current target
        isMoving = true;
        isStepMoving = true;
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
        StopCurrentEmote();

        isMoving = true;
        isStepMoving = false;
        movementVisualYOffset = 0f;

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
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
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

    void OnDisable()
    {
        StopCurrentEmote();
    }
}
