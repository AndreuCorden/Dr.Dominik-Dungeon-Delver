using UnityEngine;
using System.Collections;

public class DoorOpener : MonoBehaviour
{
    // Drag the 'NGF_Env_Door_12' object into this slot in the Inspector
    public Transform doorHinge; 
    public float openAngle = 90f;
    public float openSpeed = 2f;
    private bool isOpen = false;

    [Header("Door Audio Configurations")]
    [SerializeField] private AudioClip doorOpenSFX;
    
    [Tooltip("Adjusts the relative volume balance for this specific sound effect clip.")]
    [SerializeField] [Range(0f, 1f)] private float volumeOffset = 1f;

    public void OpenDoor()
    {
        if (!isOpen)
        {
            isOpen = true;

            // ==========================================
            // STANDARDIZED MASTER AUDIO SYSTEM INTEGRATION
            // ==========================================
            if (AudioManager.Instance != null && doorOpenSFX != null)
            {
                // Plays the sound right at the door's current position in the world.
                // The master volume slider will automatically scale this volumeOffset!
                AudioManager.Instance.PlaySFX(doorOpenSFX, transform.position, volumeOffset);
            }

            StartCoroutine(AnimateOpen());
        }
    }

    IEnumerator AnimateOpen()
    {
        Quaternion targetRotation = doorHinge.localRotation * Quaternion.Euler(0, openAngle, 0);
        while (Quaternion.Angle(doorHinge.localRotation, targetRotation) > 0.1f)
        {
            doorHinge.localRotation = Quaternion.Slerp(doorHinge.localRotation, targetRotation, Time.deltaTime * openSpeed);
            yield return null;
        }
        
        // Snap explicitly to final target to finish the movement smoothly
        doorHinge.localRotation = targetRotation; 
    }
}