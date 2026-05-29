using UnityEngine;

public class PixelFire : MonoBehaviour
{
    public float speed = 10f;
    public float lifetime = 0.6f;
    public float maxScale = 1.0f;

    [Header("Light Settings")]
    [SerializeField] private float lightRange = 4f;        // How far the fireball illuminates
    [SerializeField] private float maxLightIntensity = 2f; // Peak brightness at birth
    private Light projectileLight;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip crackleSound;
    [SerializeField] [Range(0f, 1f)] private float volume = 0.2f; 

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

        // ==========================================
        // DYNAMIC RUNTIME LIGHT GENERATION
        // ==========================================
        // Create a pristine child object so URP doesn't glitch on dependencies
        GameObject lightObj = new GameObject("Fireball_Light");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = Vector3.zero;

        projectileLight = lightObj.AddComponent<Light>();
        projectileLight.type = LightType.Point;
        projectileLight.range = lightRange;
        projectileLight.intensity = maxLightIntensity;
        
        // Disable shadows on projectiles for a massive performance boost during heavy bursts
        projectileLight.shadows = LightShadows.None; 
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

        // ==========================================
        // DYNAMIC LIGHT UPDATES
        // ==========================================
        if (projectileLight != null)
        {
            // 1. Force the light to match the changing color of the mesh perfectly
            projectileLight.color = baseColor;

            // 2. Fade out the light's intensity linearly as the projectile burns out
            projectileLight.intensity = Mathf.Lerp(maxLightIntensity, 0f, lifePercentage);
        }

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