using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    public ArrowTrap wallTrap; // Assigned during Grid Generation
    private bool isPressed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!isPressed && (other.CompareTag("Player") || other.CompareTag("Enemy")))
        {
            isPressed = true;
            if (wallTrap != null) wallTrap.FireArrows();
            
            // Optional: Visual feedback
            transform.localPosition -= new Vector3(0, 0.05f, 0); 
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Enemy"))
        {
            isPressed = false;
            transform.localPosition += new Vector3(0, 0.05f, 0);
        }
    }
}
