using UnityEngine;

public class PixelFire : MonoBehaviour
{
    public float speed = 10f;
    public float lifetime = 0.6f;
    public float maxScale = 1.0f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip crackleSound;
    [SerializeField] [Range(0f, 1f)] private float volume = 0.2f; // Kept lower due to burst grouping densities

    private MeshRenderer meshRenderer;
    private float startTime;

    private Color[] fireColors = {
        Color.white,
        Color.yellow,
        new Color(1, 0.5f, 0),
        Color.red 
    };

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        startTime = Time.time;

        transform.localScale = Vector3.zero;
        Destroy(gameObject, lifetime);

        speed *= Random.Range(0.8f, 1.2f);

        // --- PLAY CRACKLE DYNAMICALLY ON INSTANTIATION ---
        if (AudioManager.Instance != null && crackleSound != null)
        {
            AudioManager.Instance.PlaySFX(crackleSound, transform.position, volume);
        }
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        float lifePercentage = (Time.time - startTime) / lifetime;
        lifePercentage = Mathf.Clamp01(lifePercentage);

        Color baseColor = EvaluateGradient(lifePercentage);
        float exponentialAlpha = Mathf.Pow(1f - lifePercentage, 0.2f);

        baseColor.a = exponentialAlpha;
        meshRenderer.material.color = baseColor;

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