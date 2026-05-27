using UnityEngine;

public class MimicEnemy : EnemyFollower 
{
    [Header("Mimic Wake Settings")]
    public float wakeRange = 2f;

    [Header("Visual Models")]
    public GameObject chestModel; // Assign in Inspector
    public GameObject mimicModel; // Assign in Inspector

    [Header("Mimic Audio")]
    [SerializeField] private AudioClip transformSFX;

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

                // --- ADDED: PLAY TRANSFORMATION SOUND ---
                if (AudioManager.Instance != null && transformSFX != null)
                {
                    // Using sfxVolume from the BaseEnemy class so it matches your settings
                    AudioManager.Instance.PlaySFX(transformSFX, transform.position, sfxVolume);
                }
            } 
            return;
        }

        base.DetermineNextStep(); // Acts as a Follower once awake
    }
}