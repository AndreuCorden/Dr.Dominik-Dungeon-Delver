using UnityEngine;
using System.Collections;

public class Coin : MonoBehaviour
{
    public float rotateSpeed = 100f;
    
    [Header("Collection Animation")]
    public float collectJumpHeight = 1.5f; 
    public float collectSpinMultiplier = 8f; 
    public float animDuration = 0.4f; 
    public GameObject collectEffectPrefab; 

    [Header("Audio")]
    public AudioClip collectSound;   
    public AudioClip explosionSound; 
    [SerializeField] [Range(0f, 1f)] private float volume = 0.8f;

    private bool isFalling = false;
    private bool isCollected = false; 
    private Collider coinCollider;

    void Start()
    {
        coinCollider = GetComponent<Collider>();
    }

    void Update()
    {
        if (isCollected) return;

        // 1. Always Spin normally
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime, Space.World);

        // 2. Check for the void
        if (!isFalling)
        {
            if (!Physics.Raycast(transform.position, Vector3.down, 1.1f))
            {
                isFalling = true;
            }
            else
            {
                float newY = 1.0f + Mathf.Sin(Time.time * 5f) * 0.1f;
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            }
        }

        // 3. Falling Logic
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
            if (coinCollider != null) coinCollider.enabled = false;

            PlayerController.Instance.AddCoin(1);

            // Clear the parent completely to neutralize weird grouping scale artifacts
            transform.SetParent(null);

            StartCoroutine(AnimateCollectSequence());
        }
    }

    IEnumerator AnimateCollectSequence()
    {
        // --- PLAY COIN COLLECT SFX (Global) ---
        if (AudioManager.Instance != null && collectSound != null)
        {
            AudioManager.Instance.PlaySFX(collectSound, transform.position, volume);
        }

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + new Vector3(0, collectJumpHeight, 0);
        Vector3 originalScale = transform.localScale;

        float elapsed = 0;
        while (elapsed < animDuration)
        {
            float t = elapsed / animDuration;
            
            // Move upward
            float smoothT = Mathf.Sin(t * Mathf.PI * 0.5f); 
            transform.position = Vector3.Lerp(startPos, targetPos, smoothT);

            // Spin perfectly along clean world axis
            transform.Rotate(Vector3.up * collectSpinMultiplier * 360f * Time.deltaTime, Space.World);

            // Scale down smoothly in the last 40% of the movement sequence
            if (t > 0.6f)
            {
                float shrinkT = (t - 0.6f) / 0.4f; 
                transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, shrinkT);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = Vector3.zero;

        // --- PLAY EXPLOSION SFX (Global) ---
        // This won't get cut off anymore since the AudioManager handles its lifetime!
        if (AudioManager.Instance != null && explosionSound != null)
        {
            AudioManager.Instance.PlaySFX(explosionSound, transform.position, volume);
        }

        if (collectEffectPrefab != null)
        {
            for (int i = 0; i < 6; i++)
            {
                Quaternion scatterRot = Quaternion.Euler(0, i * 60f, 0); 
                Instantiate(collectEffectPrefab, transform.position, scatterRot);
            }
        }

        Destroy(gameObject);
    }
}