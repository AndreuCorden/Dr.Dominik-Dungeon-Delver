using UnityEngine;

public class MimicEnemy : EnemyFollower 
{
    [Header("Mimic Settings")]
    public float wakeUpDistance = 1.5f;
    public GameObject chestVisual;   
    public GameObject monsterVisual; 

    private bool isAwake = false;

    protected override void Start()
    {
        base.Start(); 
        chestVisual.SetActive(true);
        monsterVisual.SetActive(false);
        isAwake = false;
        nextMoveTime = float.MaxValue;
    }

    protected override void Update()
    {
        // Use base.Update for falling/gravity
        base.Update();

        // While asleep, actively check for the player
        if (!isAwake && !isFalling)
        {
            CheckForPlayer();
        }
    }

    void CheckForPlayer()
    {
        if (player == null) 
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
            return;
        }

        // 2D Distance check for the wake-up trigger
        float dist = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), 
                                      new Vector2(player.position.x, player.position.z));

        if (dist <= wakeUpDistance)
        {
            WakeUp();
        }
    }

    void WakeUp()
    {
        isAwake = true;
        
        // Ensure we are perfectly aligned on the grid upon awakening
        transform.position = GetRoundedPos(transform.position);
        Vector3 currentPos = GetRoundedPos(transform.position);
        if (!OccupiedTiles.Contains(currentPos)) OccupiedTiles.Add(currentPos);

        // Reset nextMoveTime so it begins attacking/moving shortly after transformation
        nextMoveTime = Time.time + 0.5f; 
        
        chestVisual.SetActive(false);
        monsterVisual.SetActive(true);
        
        Debug.Log("Mimic Awakened!");
    }

    protected override void DetermineNextStep()
    {
        // If not awake, do nothing. If awake, run the EnemyFollower attack/move logic.
        if (!isAwake) return;
        base.DetermineNextStep();
    }
}