using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    void Awake()
    {
        MainMenuCameraSetup.Apply();
    }

    void Start()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMainMenuMusic();
        }
    }

    public void OnClickStartGame()
    {
        if (NavigationManager.Instance != null)
            NavigationManager.Instance.StartGame();
    }

    public void OnClickOpenCredits()
    {
        if (NavigationManager.Instance != null)
            NavigationManager.Instance.OpenCreditsScene();
    }

    // New bridge method for your Main Menu Button Event Clicker
    public void OnClickToggleInstructions()
    {
        if (NavigationManager.Instance != null)
            NavigationManager.Instance.ToggleInstructions();
    }

    public void OnClickExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}