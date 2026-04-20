using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public LayerMask floorLayer; // Assign the "Floor" layer in the inspector
    private Vector3 targetPosition;
    private bool isMoving = false;

    void Start()
    {
        targetPosition = new Vector3(Mathf.Round(transform.position.x), transform.position.y, Mathf.Round(transform.position.z));
        transform.position = targetPosition;
    }

    void Update()
    {
        if (!isMoving)
        {
            Vector3 direction = Vector3.zero;

            if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame) direction = Vector3.forward;
            else if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame) direction = Vector3.back;
            else if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame) direction = Vector3.left;
            else if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame) direction = Vector3.right;

            if (direction != Vector3.zero)
            {
                // ONLY move if the destination is safe
                if (IsDestinationSafe(direction))
                {
                    Move(direction);
                }
            }
        }

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            transform.position = targetPosition;
            isMoving = false;
        }
    }

    bool IsDestinationSafe(Vector3 direction)
    {
        // Calculate where the player WANTS to go
        Vector3 dest = targetPosition + direction;

        // We cast a ray from the destination, pointing downwards
        // Origin is slightly above the floor (y=1), pointing down
        Ray ray = new Ray(new Vector3(dest.x, 2.0f, dest.z), Vector3.down);

        // If the ray hits something on the "Floor" layer within 1.5 units
        if (Physics.Raycast(ray, out RaycastHit hit, 1.5f, floorLayer))
        {
            // Destination is safe!
            return true;
        }

        if (!isMoving && !Physics.Raycast(transform.position, Vector3.down, 1.1f, floorLayer))
        {
            // Trigger Lose Heart / Game Over logic here
            Debug.Log("Player fell into the void!");
        }   

        // Nothing was hit (the floor is gone or it's the edge of the world)
        Debug.Log("Path blocked: No floor at " + dest);
        return false;
    }

    void Move(Vector3 direction)
    {
        targetPosition = targetPosition + direction; // Move relative to current target
        isMoving = true;
        transform.forward = direction;
    }
}