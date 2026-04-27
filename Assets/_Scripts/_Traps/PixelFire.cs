using UnityEngine;

public class PixelFire : MonoBehaviour
{
    public float speed = 6f; // Faster speed + shorter life = dense, short flame
    public float lifetime = 0.6f; // REDUCED: Fire won't travel as far
    public float initialScale = 1.0f; // REDUCED: Smaller starting blocks
    
    private MeshRenderer meshRenderer;
    private float startTime;

    // Gradient: White -> Yellow -> Orange -> Red -> Black
    private Color[] fireColors = { 
        Color.white, 
        Color.yellow, 
        new Color(1, 0.5f, 0), // Orange
        Color.red, 
        Color.black 
    };

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        startTime = Time.time;
        transform.localScale = Vector3.one * initialScale;
        Destroy(gameObject, lifetime);
        
        speed *= Random.Range(0.8f, 1.2f);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        float lifePercentage = (Time.time - startTime) / lifetime;

        // Ensure we evaluate the full array for the color
        meshRenderer.material.color = EvaluateGradient(lifePercentage);

        // Shrink from initialScale down to zero
        float scale = Mathf.Lerp(initialScale, 0, lifePercentage);
        transform.localScale = Vector3.one * scale;
    }

    Color EvaluateGradient(float t)
    {
        // Clamping T between 0 and 1 ensures it always reaches the last color (Black)
        t = Mathf.Clamp01(t);
        float floatIndex = t * (fireColors.Length - 1);
        int index = Mathf.FloorToInt(floatIndex);
        int nextIndex = Mathf.Min(index + 1, fireColors.Length - 1);
        float localT = floatIndex - index;

        return Color.Lerp(fireColors[index], fireColors[nextIndex], localT);
    }
}