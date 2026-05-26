using UnityEngine;

public class Arrow : MonoBehaviour
{
    public float lifetime = 3f;
    public bool canDamage = true;

    void Start() => Destroy(gameObject, lifetime);

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