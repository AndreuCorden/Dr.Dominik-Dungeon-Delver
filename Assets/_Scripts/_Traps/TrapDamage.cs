using UnityEngine;

public class TrapDamage : MonoBehaviour 
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // We use our clean event-based system!
            PlayerController.Instance.TakeDamage(false); 
        }
    }
}