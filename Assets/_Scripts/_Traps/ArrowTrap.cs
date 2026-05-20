using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class ArrowTrap : MonoBehaviour
{
    public GameObject arrowPrefab;
    public Transform[] spawnPoints;
    public float arrowSpeed = 10f;

    [Header("Audio Settings")]
    public AudioClip fireSound; // Sharp arrow release "thwip" or crossbow snap

    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void FireArrows()
    {
        // --- AUDIO TRIGGER ---
        if (fireSound != null) audioSource.PlayOneShot(fireSound);

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