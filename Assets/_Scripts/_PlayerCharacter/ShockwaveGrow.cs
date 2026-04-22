using UnityEngine;

public class ShockwaveGrow : MonoBehaviour
{
    public float growSpeed = 15f;
    public float targetScale = 3.0f; // How far the "ring" expands

    private float currentScale = 0f;

    void Update()
    {
        // Increment the scale value
        currentScale = Mathf.MoveTowards(currentScale, targetScale, growSpeed * Time.deltaTime);

        // Apply it ONLY to X and Z. Keep Y very thin (0.05f) so it looks like a disc.
        transform.localScale = new Vector3(currentScale, 0.05f, currentScale);
    }
}