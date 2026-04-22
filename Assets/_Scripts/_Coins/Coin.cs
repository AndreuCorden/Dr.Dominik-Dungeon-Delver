using UnityEngine;

public class Coin : MonoBehaviour
{
    public float rotateSpeed = 100f;

    private bool isFalling = false;

    void Update()
    {
        // 1. Always Spin
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime, Space.World);

        // 2. Check for the void (only if we haven't started falling yet)
        if (!isFalling)
        {
            // Raycast check: Is there a floor?
            if (!Physics.Raycast(transform.position, Vector3.down, 1.1f))
            {
                isFalling = true;
            }
            else
            {
                // ONLY Bob if we are NOT falling
                float newY = 1.0f + Mathf.Sin(Time.time * 5f) * 0.1f;
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            }
        }

        // 3. Falling Logic
        if (isFalling)
        {
            // Move down without being snapped back up by the bobbing code
            transform.Translate(Vector3.down * Time.deltaTime * 10f, Space.World);

            // Destroy after falling
            if (transform.position.y < -10f)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the thing that touched us is the Player
        if (other.CompareTag("Player"))
        {
            // Access the persistent instance
            PlayerController.Instance.AddCoin(1);

            // Play a sound here later if you want!

            // Destroy the coin so it disappears
            Destroy(gameObject);
        }
    }
}