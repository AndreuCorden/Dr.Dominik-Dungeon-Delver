using UnityEngine;
using System.Collections;

public class ArrowTrap : MonoBehaviour
{
    public GameObject arrowPrefab;
    public Transform[] spawnPoints;
    public float arrowSpeed = 10f;

    [Header("Audio Settings")]
    public AudioClip fireSound; // Sharp arrow release "thwip" or crossbow snap
    [SerializeField] [Range(0f, 1f)] private float volume = 0.8f;

    public void FireArrows()
    {
        // --- ROUTED TO GLOBAL SFX SYSTEM ---
        if (AudioManager.Instance != null && fireSound != null) 
        {
            AudioManager.Instance.PlaySFX(fireSound, transform.position, volume);
        }

        bool enabled = true;
        foreach (Transform sp in spawnPoints)
        {
            GameObject arrow = Instantiate(arrowPrefab, sp.position, sp.rotation);
            Rigidbody rb = arrow.GetComponent<Rigidbody>();
            arrow.GetComponent<Arrow>().canDamage = enabled;
            enabled = false;
            if (rb != null)
            {
                rb.linearVelocity = sp.forward * arrowSpeed;
            }
        }
    }
}