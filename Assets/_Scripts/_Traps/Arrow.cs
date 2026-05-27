using UnityEngine;

public class Arrow : MonoBehaviour
{
    public float lifetime = 3f;
    public bool canDamage = true;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip swishSound;
    [SerializeField] [Range(0f, 1f)] private float volume = 0.6f;

    void Start()
    {
        // Play the swish sound in 3D space at the arrow's starting point
        if (AudioManager.Instance != null && swishSound != null)
        {
            AudioManager.Instance.PlaySFX(swishSound, transform.position, volume);
        }

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if(canDamage)
            {
                PlayerController.Instance.TakeDamage(false, transform.position);
            }
            Destroy(gameObject);
        }
        else if(other.CompareTag("Enemy"))
        {
            if (other.TryGetComponent<BaseEnemy>(out var enemy))
            {
                enemy.Die();
            }
            Destroy(gameObject);
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Trap"))
        {
            Destroy(gameObject); // Hit a wall or gargoyle
        }
    }
}