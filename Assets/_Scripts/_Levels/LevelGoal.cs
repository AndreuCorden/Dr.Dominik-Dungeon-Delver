using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelGoal : MonoBehaviour
{
    [Header("Progression")]
    public string nextLevelName; // Manually set this in the Inspector for each level
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Optional: Reset health to 3 when winning a level
            // PlayerController.health = 3; 

            Debug.Log("Level Complete! Moving to " + nextLevelName);
            SceneManager.LoadScene(nextLevelName);
        }
    }
}