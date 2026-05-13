using UnityEngine;
using System.Collections;

public class DoorOpener : MonoBehaviour
{
    // Drag the 'NGF_Env_Door_12' object into this slot in the Inspector
    public Transform doorHinge; 
    public float openAngle = 90f;
    public float openSpeed = 2f;
    private bool isOpen = false;

    public void OpenDoor()
    {
        if (!isOpen)
        {
            isOpen = true;
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
    }
}