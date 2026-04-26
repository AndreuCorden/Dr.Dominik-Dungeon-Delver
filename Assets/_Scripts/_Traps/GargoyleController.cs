using UnityEngine;
using System.Collections;

public class GargoyleController : MonoBehaviour
{
    public GameObject pixelFirePrefab; // A small 0.2 scale cube with PixelFire script
    public GameObject damageStick;
    private TrapDamage damageScript;
    public Transform shootPoint;

    [Header("Fire Sequence")]
    public int cubesPerBurst = 20;
    public float burstDuration = 0.5f;
    public float coneAngle = 15f; // How wide the cone is

    [Header("Cycle")]
    public float timeBetweenActions = 2.0f;

    void Start()
    {
        StartCoroutine(GargoyleRoutine());
        damageScript = damageStick.GetComponent<TrapDamage>();
    }

    IEnumerator GargoyleRoutine()
    {
        while (true)
        {
            // ROTATE 90
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

            // FIRE BREATHING SEQUENCE
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
        // Create a random rotation within the cone angle
        Quaternion randomRot = shootPoint.rotation * Quaternion.Euler(
            Random.Range(-coneAngle, coneAngle),
            Random.Range(-coneAngle, coneAngle),
            0
        );

        Instantiate(pixelFirePrefab, shootPoint.position, randomRot);
    }
}