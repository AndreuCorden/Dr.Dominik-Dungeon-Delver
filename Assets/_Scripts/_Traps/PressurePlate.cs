using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class PressurePlate : MonoBehaviour
{
    public ArrowTrap wallTrap;
    
    [Header("Visuals")]
    public Transform movingPart; 
    private bool isPressed = false;

    [Header("Audio Settings")]
    public AudioClip pressSound;   // Heavy stone click / mechanical snap
    public AudioClip releaseSound; // Optional: soft reset sound

    private AudioSource audioSource;
    private List<Collider> occupants = new List<Collider>();

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        bool changed = false;
        for (int i = occupants.Count - 1; i >= 0; i--)
        {
            if (occupants[i] == null || !occupants[i].gameObject.activeInHierarchy)
            {
                occupants.RemoveAt(i);
                changed = true;
            }
        }

        if (changed && isPressed && occupants.Count == 0)
        {
            Release();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Enemy"))
        {
            if (!occupants.Contains(other)) occupants.Add(other);

            if (!isPressed) Press();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (occupants.Contains(other)) occupants.Remove(other);

        if (isPressed && occupants.Count == 0) Release();
    }

    private void Press()
    {
        isPressed = true;
        
        // --- AUDIO TRIGGER ---
        if (pressSound != null) audioSource.PlayOneShot(pressSound);

        if (wallTrap != null) wallTrap.FireArrows();

        if (movingPart != null)
            movingPart.localPosition = new Vector3(0, -0.03f, 0);
        else
            transform.localPosition -= new Vector3(0, 0.03f, 0); 
    }

    private void Release()
    {
        isPressed = false;

        // --- AUDIO TRIGGER ---
        if (releaseSound != null) audioSource.PlayOneShot(releaseSound);

        if (movingPart != null)
            movingPart.localPosition = Vector3.zero;
        else
            transform.localPosition += new Vector3(0, 0.02f, 0); 
    }

    public void ResetPlate(Collider col)
    {
        if (occupants.Contains(col)) occupants.Remove(col);
        if (isPressed && occupants.Count == 0) Release();
    }
}