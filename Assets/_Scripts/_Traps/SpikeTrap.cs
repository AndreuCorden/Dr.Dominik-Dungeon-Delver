using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class SpikeTrap : MonoBehaviour
{
    // Drag ALL 5 spike meshes into this array in the Inspector
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
    public AudioClip warningSound;  // Plays when entering Stage 2
    public AudioClip springSound;   // Plays when entering Stage 3
    public AudioClip retractSound;  // Plays when resetting back to Stage 1

    private TrapDamage damageScript;
    private AudioSource audioSource;
    
    // Arrays to hold the explicit 3-stage coordinate maps
    private Vector3[] stage1Positions;
    private Vector3[] stage2Positions;
    private Vector3[] stage3Positions;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (spikes.Length > 0)
            damageScript = spikes[0].GetComponent<TrapDamage>();

        // Initialize our coordinate maps
        stage1Positions = new Vector3[spikes.Length];
        stage2Positions = new Vector3[spikes.Length];
        stage3Positions = new Vector3[spikes.Length];

        // Bake the exact mathematical stage positions based on their initial layout position
        for (int i = 0; i < spikes.Length; i++)
        {
            Vector3 basePos = spikes[i].transform.position;
            
            stage1Positions[i] = basePos; // Hidden state (0)
            stage2Positions[i] = basePos + new Vector3(0, stage2WarningHeight, 0); // Only tops showing
            stage3Positions[i] = basePos + new Vector3(0, stage3MaxHeight, 0); // Full extension
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
            
            // Slide back down to Stage 1 positions
            if (retractSound != null) audioSource.PlayOneShot(retractSound);
            yield return StartCoroutine(MoveSpikesToStage(stage1Positions, retractSpeed));
            
            yield return new WaitForSeconds(stage1IdleTime);

            // ==========================================
            // STAGE 2: TELEGRAPH / ONLY TOPS IN VIEW
            // ==========================================
            if (warningSound != null) audioSource.PlayOneShot(warningSound);
            
            // Pop up slightly to Stage 2 positions
            yield return StartCoroutine(MoveSpikesToStage(stage2Positions, 0.15f)); 
            
            yield return new WaitForSeconds(stage2WarningTime);

            // ==========================================
            // STAGE 3: FULL EXTENSION & LETHAL
            // ==========================================
            if (springSound != null) audioSource.PlayOneShot(springSound);
            
            // Rapid snap up to Stage 3 positions
            yield return StartCoroutine(MoveSpikesToStage(stage3Positions, springSpeed));
            
            // Deal damage ONLY while completely extended at Stage 3
            if (damageScript != null) damageScript.enabled = true;
            
            yield return new WaitForSeconds(stage3ActiveTime);
        }
    }

    // Coroutine to handle the movement interpolation to a specific stage array map
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
            t = Mathf.SmoothStep(0, 1, t); // Keeps the transitions smooth, not jarringly linear

            for (int i = 0; i < spikes.Length; i++)
            {
                spikes[i].transform.position = Vector3.Lerp(startPositions[i], targetStagePositions[i], t);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Hard lock alignment checkpoint to prevent rounding errors
        for (int i = 0; i < spikes.Length; i++)
        {
            spikes[i].transform.position = targetStagePositions[i];
        }
    }
}