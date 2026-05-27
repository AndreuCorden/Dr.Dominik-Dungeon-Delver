using UnityEngine;
using System.Collections;

public class GargoyleController : MonoBehaviour
{
    public GameObject pixelFirePrefab; 
    public GameObject damageStick;
    private TrapDamage damageScript;
    public Transform shootPoint;

    [Header("Fire Sequence")]
    public int cubesPerBurst = 20;
    public float burstDuration = 0.5f;
    public float coneAngle = 15f; 

    [Header("Cycle & Movement")]
    public float timeBetweenActions = 2.0f;
    // --- NEW: ADJUSTABLE ROTATION DURATION ---
    [Tooltip("How long it takes (in seconds) to complete the 90-degree turn.")]
    public float turnDuration = 1.2f; 

    [Header("Audio Settings")]
    public AudioClip turnSound;       // Heavy stone grinding sound
    public AudioClip fireBreathSound; // Continuous roaring fire sound
    [SerializeField] [Range(0f, 1f)] private float volume = 0.7f;

    void Start()
    {
        damageScript = damageStick.GetComponent<TrapDamage>();

        if (damageScript != null) 
        {
            damageScript.enabled = false;
        }

        StartCoroutine(GargoyleRoutine());
    }

    IEnumerator GargoyleRoutine()
    {
        while (true)
        {
            // ==========================================
            // PHASE 1: ROTATE 90 DEGREES
            // ==========================================
            if (AudioManager.Instance != null && turnSound != null) 
            {
                AudioManager.Instance.PlaySFX(turnSound, transform.position, volume);
            }

            Quaternion endRotation = transform.rotation * Quaternion.Euler(0, 90, 0);
            float rotElapsed = 0;
            
            // Replaced the hardcoded 0.4f value with our new adjustable variable
            while (rotElapsed < turnDuration)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, endRotation, rotElapsed / turnDuration);
                rotElapsed += Time.deltaTime;
                yield return null;
            }
            transform.rotation = endRotation;

            yield return new WaitForSeconds(timeBetweenActions);

            // ==========================================
            // PHASE 2: FIRE BREATHING SEQUENCE
            // ==========================================
            if (AudioManager.Instance != null && fireBreathSound != null) 
            {
                AudioManager.Instance.PlaySFX(fireBreathSound, transform.position, volume);
            }

            float elapsed = 0;
            float spawnRate = burstDuration / cubesPerBurst;
            
            if (damageScript != null) damageScript.enabled = true; 

            while (elapsed < burstDuration)
            {
                SpawnFirePixel();
                elapsed += spawnRate;
                yield return new WaitForSeconds(spawnRate);
            }
            
            if (damageScript != null) damageScript.enabled = false;

            yield return new WaitForSeconds(timeBetweenActions);
        }
    }

    void SpawnFirePixel()
    {
        Quaternion randomRot = shootPoint.rotation * Quaternion.Euler(
            Random.Range(-coneAngle, coneAngle),
            Random.Range(-coneAngle, coneAngle),
            0
        );

        Instantiate(pixelFirePrefab, shootPoint.position, randomRot);
    }
}