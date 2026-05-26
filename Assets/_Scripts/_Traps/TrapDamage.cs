using UnityEngine;

public class TrapDamage : MonoBehaviour
{
    public float damageCooldown = 2.0f;
    private float lastDamageTime;

    private void OnTriggerEnter(Collider other)
    {
        // MANUAL CHECK: OnTriggerEnter fires even if script is disabled!
        if (!enabled) return;

        HandleDamage(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!enabled) return;

        HandleDamage(other);
    }

    private void HandleDamage(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Only damage if enough time has passed (prevents instant death)
            if (Time.time >= lastDamageTime + damageCooldown)
            {
                PlayerController.Instance.TakeDamage(false, transform.position);
                lastDamageTime = Time.time;
            }
        }
        else if (other.CompareTag("Enemy"))
        {
            // Damage enemies immediately without cooldown (they can be sacrificed!)
            if (other.TryGetComponent<BaseEnemy>(out var enemy))
            {
                enemy.Die();
            }
        }
    }
}