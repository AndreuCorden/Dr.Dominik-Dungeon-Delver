using UnityEngine;

public class EnemyAnimationEvents : MonoBehaviour
{
    public void SpawnSlashVFX()
    {
        BaseEnemy enemy = GetComponentInParent<BaseEnemy>();
        if (enemy != null)
            enemy.SpawnSlashVFX();
    }
}
