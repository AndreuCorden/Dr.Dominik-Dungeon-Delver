using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditsScreenUI : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    void Start()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCreditsMusic();
        }
    }

    public void ExitToMainMenu()
    {
        // If the persistent manager is alive, use it
        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.ReturnToMainMenu();
        }
        else
        {
            // Fallback for direct scene testing in the editor
            Debug.Log("NavigationManager not found. Falling back to direct scene load.");
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}