using UnityEngine;
using System.Collections;

public class Coin : MonoBehaviour
{
    [Header("Glow Settings")]
    [SerializeField] private float glowRange = 3f;
    [SerializeField] private float glowIntensity = 1.2f;
    [SerializeField] private Color glowColor = new Color(1f, 0.75f, 0.2f); // Golden glow for the bag

    [Header("Collection Animation")]
    public float collectJumpHeight = 1.0f; // Exactly 1 unit arch up
    public float animDuration = 0.4f; 

    [Header("Audio")]
    public AudioClip collectSound;   
    [SerializeField] [Range(0f, 1f)] private float volume = 0.8f;

    private bool isFalling = false;
    private bool isCollected = false; 
    private Collider coinCollider;
    private Transform playerTransform;

    void Start()
    {
        coinCollider = GetComponent<Collider>();

        // Dynamic Runtime Light: Gives the bag its glow without asset file corruption loops
        GameObject glowObj = new GameObject("Bag_Glow_Light");
        glowObj.transform.SetParent(transform);
        glowObj.transform.localPosition = new Vector3(0f, 0.2f, 0f); 

        Light bagLight = glowObj.AddComponent<Light>();
        bagLight.type = LightType.Point;
        bagLight.color = glowColor;
        bagLight.range = glowRange;
        bagLight.intensity = glowIntensity;
        bagLight.shadows = LightShadows.None; 
    }

    void Update()
    {
        if (isCollected) return;

        // NOTE: Idle spinning logic completely removed! The bag stays stationary.

        // Keep your fallback check for void drops
        if (!isFalling)
        {
            if (!Physics.Raycast(transform.position, Vector3.down, 1.1f))
            {
                isFalling = true;
            }
            // Removed old hovering Mathf.Sin code so it sits naturally flat on the ground grid
        }

        if (isFalling)
        {
            transform.Translate(Vector3.down * Time.deltaTime * 10f, Space.World);

            if (transform.position.y < -10f)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isCollected)
        {
            isCollected = true;
            playerTransform = other.transform; // Save reference to the player to track them down

            if (coinCollider != null) coinCollider.enabled = false;

            PlayerController.Instance.AddCoin(1);

            transform.SetParent(null);
            StartCoroutine(AnimateCollectSequence());
        }
    }

    IEnumerator AnimateCollectSequence()
    {
        // Play only the crisp collection audio
        if (AudioManager.Instance != null && collectSound != null)
        {
            AudioManager.Instance.PlaySFX(collectSound, transform.position, volume);
        }

        Vector3 startPos = transform.position;
        Vector3 originalScale = transform.localScale;

        float elapsed = 0;
        while (elapsed < animDuration)
        {
            float t = elapsed / animDuration;
            
            // 1. Follow the player's position laterally as they move
            Vector3 currentPlayerPos = playerTransform != null ? playerTransform.position : startPos;
            Vector3 linearProgress = Vector3.Lerp(startPos, currentPlayerPos, t);

            // 2. Generate a clean mathematical arc that peaks exactly 1 unit up
            float parabolicArcY = 4f * collectJumpHeight * t * (1f - t);

            // 3. Combine linear movement with vertical leap
            transform.position = new Vector3(linearProgress.x, linearProgress.y + parabolicArcY, linearProgress.z);

            // Smoothly shrink into the player's center over time
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // NOTE: Old explosion SFX, explosion Prefabs, and scattering loop completely removed.
        Destroy(gameObject);
    }
}