using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    public void SpawnSlashVFX()
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SpawnSlashVFX();
        }
    }
}
