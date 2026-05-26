using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
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

    [Header("Cycle")]
    public float timeBetweenActions = 2.0f;

    [Header("Audio Settings")]
    public AudioClip turnSound;       // Heavy stone grinding sound
    public AudioClip fireBreathSound; // Continuous roaring fire sound

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        damageScript = damageStick.GetComponent<TrapDamage>();

        // --- THE INITIALIZATION FIX ---
        // Explicitly force the trap to be safe on frame zero!
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
            if (turnSound != null) audioSource.PlayOneShot(turnSound);

            Quaternion endRotation = transform.rotation * Quaternion.Euler(0, 90, 0);
            float rotElapsed = 0;
            while (rotElapsed < 0.4f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, endRotation, rotElapsed / 0.4f);
                rotElapsed += Time.deltaTime;
                yield return null;
            }
            transform.rotation = endRotation;

            yield return new WaitForSeconds(timeBetweenActions);

            // ==========================================
            // PHASE 2: FIRE BREATHING SEQUENCE
            // ==========================================
            if (fireBreathSound != null) audioSource.PlayOneShot(fireBreathSound);

            float elapsed = 0;
            float spawnRate = burstDuration / cubesPerBurst;
            
            // Activate damage zone ONLY during the actual fire burst
            if (damageScript != null) damageScript.enabled = true; 

            while (elapsed < burstDuration)
            {
                SpawnFirePixel();
                elapsed += spawnRate;
                yield return new WaitForSeconds(spawnRate);
            }
            
            // Instantly make the trap safe again when fire stops
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