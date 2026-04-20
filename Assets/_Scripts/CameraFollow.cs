using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;    // Drag your Player here
    public Vector3 offset;      // The distance between camera and player

    void Start()
    {
        // If you don't set an offset in the inspector, it calculates current distance
        if (target != null && offset == Vector3.zero)
        {
            offset = transform.position - target.position;
        }
    }

    void LateUpdate() // LateUpdate is better for cameras to prevent jittering
    {
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }
}