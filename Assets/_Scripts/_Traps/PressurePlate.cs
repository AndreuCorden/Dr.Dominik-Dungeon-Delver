using UnityEngine;
using System.Collections.Generic;

public class PressurePlate : MonoBehaviour
{
    public ArrowTrap wallTrap;
    
    [Header("Visuals")]
    public Transform movingPart; 
    private bool isPressed = false;

    // Track objects currently on the plate
    private List<Collider> occupants = new List<Collider>();

    private void Update()
    {
        // CLEANUP: If an enemy dies while on the plate, their collider becomes null.
        // We remove any null entries from our list every frame.
        bool changed = false;
        for (int i = occupants.Count - 1; i >= 0; i--)
        {
            if (occupants[i] == null || !occupants[i].gameObject.activeInHierarchy)
            {
                occupants.RemoveAt(i);
                changed = true;
            }
        }

        // If the last person died on the plate, release it.
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
        if (wallTrap != null) wallTrap.FireArrows();

        // Move the visual part, NOT the whole object with the collider
        if (movingPart != null)
            movingPart.localPosition = new Vector3(0, -0.03f, 0);
        else
            transform.localPosition -= new Vector3(0, 0.03f, 0); // Fallback
    }

    private void Release()
    {
        isPressed = false;

        if (movingPart != null)
            movingPart.localPosition = Vector3.zero;
        else
            transform.localPosition += new Vector3(0, 0.02f, 0); // Fallback
    }

    // Keep this for manual calls if needed, but Update() now handles the logic automatically
    public void ResetPlate(Collider col)
    {
        if (occupants.Contains(col)) occupants.Remove(col);
        if (isPressed && occupants.Count == 0) Release();
    }
}