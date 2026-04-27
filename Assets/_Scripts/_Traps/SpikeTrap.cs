using UnityEngine;
using System.Collections;

public class SpikeTrap : MonoBehaviour
{
    public GameObject spikeVisual; // The actual spikes that move up/down
    public float activeTime = 2f;
    public float idleTime = 2f;

    void Start()
    {
        StartCoroutine(SpikeCycle());
    }

    IEnumerator SpikeCycle()
    {
        while (true)
        {
            // Lower Spikes
            spikeVisual.SetActive(false);
            yield return new WaitForSeconds(idleTime);

            // Raise Spikes
            spikeVisual.SetActive(true);
            // Optional: Play a "Clink!" sound here
            yield return new WaitForSeconds(activeTime);
        }
    }
}
