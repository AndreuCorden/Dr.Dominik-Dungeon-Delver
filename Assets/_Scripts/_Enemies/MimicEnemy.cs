using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MimicEnemy : EnemyFollower 
{
    [Header("Mimic Wake Settings")]
    public float wakeRange = 2f;
    public float wakeDelayDuration = 0.8f; // How long it pauses in seconds after revealing itself

    [Header("Visual Models")]
    public GameObject chestModel; // Assign in Inspector
    public GameObject mimicModel; // Assign in Inspector

    [Header("Mimic Audio")]
    [SerializeField] private AudioClip transformSFX;

    [Header("Procedural Death Settings")]
    [SerializeField] private float fallAngle = 80f;       
    [SerializeField] private float proceduralDuration = 0.6f; 

    private bool isAwake = false;

    protected override void DetermineNextStep() 
    {
        if (!isAwake) 
        {
            if (Vector3.Distance(transform.position, player.position) <= wakeRange)
            {
                isAwake = true;
                
                // Swap models
                if (chestModel != null) chestModel.SetActive(false);
                if (mimicModel != null) mimicModel.SetActive(true);  

                // --- PLAY TRANSFORMATION SOUND ---
                if (AudioManager.Instance != null && transformSFX != null)
                {
                    AudioManager.Instance.PlaySFX(transformSFX, transform.position, sfxVolume);
                }

                // ==========================================
                // FIX: ADD WAKE DELAY
                // ==========================================
                // Pin the next move time into the future so it roars/transforms 
                // but doesn't instantly take its turn to move or hit the player!
                nextMoveTime = Time.time + wakeDelayDuration;
            } 
            return;
        }

        // Only move or attack if our custom wake delay has expired
        if (Time.time >= nextMoveTime)
        {
            base.DetermineNextStep(); // Acts as a Follower once awake
        }
    }

    // ==========================================
    // CUSTOM OVERRIDDEN PROCEDURAL DEATH SEQUENCE
    // ==========================================
    public override void Die()
    {
        if (AudioManager.Instance != null && dieSFX != null)
        {
            AudioManager.Instance.PlaySFX(dieSFX, transform.position, sfxVolume);
        }

        OccupiedTiles.Remove(GetGridKey(transform.position));
        if (isMoving) OccupiedTiles.Remove(GetGridKey(targetPosition));

        foreach (var col in GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        this.enabled = false;
        StartCoroutine(AnimateMimicDeathRoutine());
    }

    private IEnumerator AnimateMimicDeathRoutine()
    {
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = startRotation * Quaternion.Euler(fallAngle, 0f, 0f);

        List<Material> activeMaterials = new List<Material>();
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            if (renderer.gameObject.activeInHierarchy)
            {
                activeMaterials.AddRange(renderer.materials);
            }
        }

        float elapsed = 0f;
        while (elapsed < proceduralDuration)
        {
            float t = elapsed / proceduralDuration;
            float smoothFall = t * t;
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, smoothFall);

            foreach (var mat in activeMaterials)
            {
                if (mat == null) continue;

                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = Mathf.Lerp(1f, 0f, t);
                    mat.color = c;
                }
                if (mat.HasProperty("_BaseColor"))
                {
                    Color bc = mat.GetColor("_BaseColor");
                    bc.a = Mathf.Lerp(1f, 0f, t);
                    mat.SetColor("_BaseColor", bc);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}