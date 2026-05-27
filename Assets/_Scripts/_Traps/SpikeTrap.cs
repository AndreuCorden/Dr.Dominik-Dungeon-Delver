using UnityEngine;
using System.Collections;

public class SpikeTrap : MonoBehaviour
{
    public GameObject[] spikes; 
    
    [Header("Stage Duration Settings")]
    public float stage1IdleTime = 2.0f;    // Time spent hidden
    public float stage2WarningTime = 0.6f;  // Time spent showing just the tips
    public float stage3ActiveTime = 1.5f;   // Time spent fully extended/lethal

    [Header("Stage Travel Heights")]
    public float stage2WarningHeight = 1f; // How high the tips peek out
    public float stage3MaxHeight = 2f;     // Total height when fully extended

    [Header("Movement Speeds")]
    public float springSpeed = 0.05f;       // Lower is faster (Stage 2 -> Stage 3 snap)
    public float retractSpeed = 0.4f;       // Time it takes to slide back into the floor

    [Header("Audio Settings")]
    public AudioClip warningSound;  // Plays when entering Stage 2 (Gears tightening)
    public AudioClip springSound;   // Plays when entering Stage 3 (Quick burst + Woosh)
    public AudioClip retractSound;  // Plays when resetting back to Stage 1 (Gears retracting)
    [SerializeField] [Range(0f, 1f)] private float volume = 0.8f;

    private TrapDamage damageScript;
    
    private Vector3[] stage1Positions;
    private Vector3[] stage2Positions;
    private Vector3[] stage3Positions;

    void Start()
    {
        if (spikes.Length > 0)
            damageScript = spikes[0].GetComponent<TrapDamage>();

        stage1Positions = new Vector3[spikes.Length];
        stage2Positions = new Vector3[spikes.Length];
        stage3Positions = new Vector3[spikes.Length];

        for (int i = 0; i < spikes.Length; i++)
        {
            Vector3 basePos = spikes[i].transform.position;
            
            stage1Positions[i] = basePos;
            stage2Positions[i] = basePos + new Vector3(0, stage2WarningHeight, 0);
            stage3Positions[i] = basePos + new Vector3(0, stage3MaxHeight, 0);
        }

        StartCoroutine(TrapCycle());
    }

    IEnumerator TrapCycle()
    {
        while (true)
        {
            // ==========================================
            // STAGE 1: HIDDEN & SAFE
            // ==========================================
            if (damageScript != null) damageScript.enabled = false;
            
            if (AudioManager.Instance != null && retractSound != null) 
            {
                AudioManager.Instance.PlaySFX(retractSound, transform.position, volume);
            }
            yield return StartCoroutine(MoveSpikesToStage(stage1Positions, retractSpeed));
            
            yield return new WaitForSeconds(stage1IdleTime);

            // ==========================================
            // STAGE 2: TELEGRAPH / ONLY TOPS IN VIEW
            // ==========================================
            if (AudioManager.Instance != null && warningSound != null) 
            {
                AudioManager.Instance.PlaySFX(warningSound, transform.position, volume);
            }
            yield return StartCoroutine(MoveSpikesToStage(stage2Positions, 0.15f)); 
            
            yield return new WaitForSeconds(stage2WarningTime);

            // ==========================================
            // STAGE 3: FULL EXTENSION & LETHAL
            // ==========================================
            if (AudioManager.Instance != null && springSound != null) 
            {
                AudioManager.Instance.PlaySFX(springSound, transform.position, volume);
            }
            yield return StartCoroutine(MoveSpikesToStage(stage3Positions, springSpeed));
            
            if (damageScript != null) damageScript.enabled = true;
            
            yield return new WaitForSeconds(stage3ActiveTime);
        }
    }

    IEnumerator MoveSpikesToStage(Vector3[] targetStagePositions, float duration)
    {
        Vector3[] startPositions = new Vector3[spikes.Length];
        for (int i = 0; i < spikes.Length; i++)
        {
            startPositions[i] = spikes[i].transform.position;
        }

        float elapsed = 0;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0, 1, t);

            for (int i = 0; i < spikes.Length; i++)
            {
                spikes[i].transform.position = Vector3.Lerp(startPositions[i], targetStagePositions[i], t);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < spikes.Length; i++)
        {
            spikes[i].transform.position = targetStagePositions[i];
        }
    }
}