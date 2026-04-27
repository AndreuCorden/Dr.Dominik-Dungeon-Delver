using UnityEngine;
using System.Collections;

public class ArrowTrap : MonoBehaviour
{
    public GameObject arrowPrefab;
    public Transform[] spawnPoints;
    public float arrowSpeed = 10f;

    public void FireArrows()
    {
        bool enabled = true;
        foreach (Transform sp in spawnPoints)
        {
            GameObject arrow = Instantiate(arrowPrefab, sp.position, sp.rotation);
            Rigidbody rb = arrow.GetComponent<Rigidbody>();
            arrow.GetComponent<Arrow>().canDamage = enabled;
            enabled = false;
            if (rb != null)
            {
                rb.linearVelocity = sp.forward * arrowSpeed;
            }
        }
    }
}