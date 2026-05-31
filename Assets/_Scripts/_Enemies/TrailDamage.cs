using UnityEngine;

public class TrailDamage : MonoBehaviour
{
    public float slowAmount = 0.5f;
    public float lifetime = 5.0f; // Trail disappears after 3 seconds
    
    public LayerMask floorLayer;
    private bool isFalling = false;

    void Start() => Destroy(gameObject, lifetime);

    void Update()
    {
        if (isFalling)
        {
            // Fall into the abyss
            transform.Translate(Vector3.down * Time.deltaTime * 10f);
            return;
        }

        // Check if the floor is still there
        // Raycast from slightly above the slime
        if (!Physics.Raycast(transform.position + Vector3.up, Vector3.down, 2f, floorLayer))
        {
            isFalling = true;
            // Stop the slime from being destroyed while falling so it can vanish off-screen
            CancelInvoke(); 
            Destroy(gameObject, 2f); 
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null) pc.currentMoveMultiplier = slowAmount;
        }
    }
}