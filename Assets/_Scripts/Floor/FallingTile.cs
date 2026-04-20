using UnityEngine;
using System.Collections;

public class FallingTile : MonoBehaviour
{
    public void StartFalling(float delay)
    {
        StartCoroutine(FallSequence(delay));
    }

    IEnumerator FallSequence(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Optional: Change color to warn the player
        GetComponent<Renderer>().material.color = Color.red;
        yield return new WaitForSeconds(0.5f);

        // Disable collider so the player's Raycast sees a "hole"
        GetComponent<Collider>().enabled = false;

        // Simple falling physics
        float timer = 0;
        while (timer < 2f)
        {
            transform.Translate(Vector3.down * Time.deltaTime * 5f);
            timer += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}