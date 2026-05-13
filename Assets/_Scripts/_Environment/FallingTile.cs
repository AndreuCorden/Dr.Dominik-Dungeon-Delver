using UnityEngine;
using System.Collections;

public class FallingTile : MonoBehaviour
{
    private bool isFallingStarted = false;

    public void StartFalling(float delay)
    {
        if (!isFallingStarted && gameObject.activeInHierarchy)
        {
            isFallingStarted = true;
            StartCoroutine(FallSequence(delay));
        }
    }

    IEnumerator FallSequence(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Only floors should change color and disable their collider
        if (gameObject.CompareTag("Floor"))
        {
            // Defensive check for Renderer (checks children too)
            Renderer rend = GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                rend.material.color = Color.red;
            }

            yield return new WaitForSeconds(0.5f);

            // Defensive check for Collider
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }
        }
        else
        {
            // If it's not a floor (Trap, Gargoyle, etc.), just wait the same 
            // warning duration so everything starts moving at the same time.
            yield return new WaitForSeconds(0.5f);
        }

        // Standard falling physics for everyone
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