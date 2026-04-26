using UnityEngine;
using System.Collections;

public class SpikeTrap : MonoBehaviour
{
    // Drag ALL 5 spike meshes into this array in the Inspector
    public GameObject[] spikes; 
    
    public float idleTime = 2.0f;
    public float activeTime = 1.5f;
    public Vector3 moveOffset = new Vector3(0, 1f, 0);

    private TrapDamage damageScript;

    void Start()
    {
        // Find the damage script on the first spike automatically
        if (spikes.Length > 0)
            damageScript = spikes[0].GetComponent<TrapDamage>();

        StartCoroutine(TrapCycle());
    }

    IEnumerator TrapCycle()
    {
        while (true)
        {
            // 1. RETRACTED (SAFE)
            if (damageScript != null) damageScript.enabled = false; 
            yield return StartCoroutine(MoveSpikes(-moveOffset, 0.3f));
            yield return new WaitForSeconds(idleTime);

            // 2. EXTENDING (DANGER)
            yield return StartCoroutine(MoveSpikes(moveOffset, 0.1f)); 
            // Turn on damage ONLY when they are fully up
            if (damageScript != null) damageScript.enabled = true; 
            
            yield return new WaitForSeconds(activeTime);
        }
    }

    IEnumerator MoveSpikes(Vector3 offset, float time)
    {
        Vector3[] startPos = new Vector3[spikes.Length];
        Vector3[] targetPos = new Vector3[spikes.Length];

        for (int i = 0; i < spikes.Length; i++)
        {
            startPos[i] = spikes[i].transform.position;
            targetPos[i] = startPos[i] + offset;
        }

        float elapsed = 0;
        while (elapsed < time)
        {
            float t = elapsed / time;
            for (int i = 0; i < spikes.Length; i++)
            {
                spikes[i].transform.position = Vector3.Lerp(startPos[i], targetPos[i], t);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < spikes.Length; i++)
            spikes[i].transform.position = targetPos[i];
    }
}