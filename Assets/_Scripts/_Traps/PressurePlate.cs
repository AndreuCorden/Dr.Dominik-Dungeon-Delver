using UnityEngine;
using System.Collections.Generic;

public class PressurePlate : MonoBehaviour
{
    public ArrowTrap wallTrap;
    
    [Header("Visuals")]
    public Transform movingPart; 
    private bool isPressed = false;

    [Header("Audio Settings")]
    public AudioClip pressSound;   // Heavy stone click / mechanical snap
    public AudioClip releaseSound; // Soft reset sound
    [SerializeField] [Range(0f, 1f)] private float volume = 0.8f;

    private List<Collider> occupants = new List<Collider>();

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
        
        // --- ROUTED TO GLOBAL SFX SYSTEM ---
        if (AudioManager.Instance != null && pressSound != null) 
        {
            AudioManager.Instance.PlaySFX(pressSound, transform.position, volume);
        }

        if (wallTrap != null) wallTrap.FireArrows();

        if (movingPart != null)
            movingPart.localPosition = new Vector3(0, -0.03f, 0);
        else
            transform.localPosition -= new Vector3(0, 0.03f, 0); 
    }

    private void Release()
    {
        isPressed = false;

        // --- ROUTED TO GLOBAL SFX SYSTEM ---
        if (AudioManager.Instance != null && releaseSound != null) 
        {
            AudioManager.Instance.PlaySFX(releaseSound, transform.position, volume);
        }

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