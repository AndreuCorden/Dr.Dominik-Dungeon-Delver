using UnityEngine;

public class PixelFire : MonoBehaviour
{
    public float speed = 10f;
    public float lifetime = 0.6f;
    public float maxScale = 1.0f; // Renamed to accurately reflect the target size

    private MeshRenderer meshRenderer;
    private float startTime;

    // Gradient: White -> Yellow -> Orange -> Red -> Fade Out
    private Color[] fireColors = {
        Color.white,
        Color.yellow,
        new Color(1, 0.5f, 0), // Orange
        Color.red 
        // Removed Black so it fades cleanly out of Red instead of turning muddy black first
    };

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        startTime = Time.time;

        // Start completely small (scale 0)
        transform.localScale = Vector3.zero;
        Destroy(gameObject, lifetime);

        speed *= Random.Range(0.8f, 1.2f);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        float lifePercentage = (Time.time - startTime) / lifetime;
        lifePercentage = Mathf.Clamp01(lifePercentage);

        // 1. EVALUATE COLOR
        Color baseColor = EvaluateGradient(lifePercentage);

        // --- EXPONENTIAL FADE CHANGER ---
        // Squaring (or cubing) the inverted percentage keeps the alpha high 
        // for longer, before plunging sharply downward to zero at the end.
        float exponentialAlpha = Mathf.Pow(1f - lifePercentage, 0.2f); // Try 2f or 3f for sharper drops

        baseColor.a = exponentialAlpha;
        meshRenderer.material.color = baseColor;

        // 2. GROW FROM SMALL TO BIG
        float scale = Mathf.Lerp(0f, maxScale, lifePercentage);
        transform.localScale = Vector3.one * scale;
    }

    Color EvaluateGradient(float t)
    {
        t = Mathf.Clamp01(t);
        float floatIndex = t * (fireColors.Length - 1);
        int index = Mathf.FloorToInt(floatIndex);
        int nextIndex = Mathf.Min(index + 1, fireColors.Length - 1);
        float localT = floatIndex - index;

        return Color.Lerp(fireColors[index], fireColors[nextIndex], localT);
    }
}