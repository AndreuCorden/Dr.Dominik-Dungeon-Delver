using UnityEngine;

public class Patroller : BaseEnemy {
    private Vector3 moveDir = Vector3.forward;
    protected override void DetermineNextStep() {
        if (!TryMove(moveDir)) moveDir = -moveDir; // Bounce off walls/enemies
    }
}