using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    // These methods are what your UI Buttons will call in the Inspector!

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
}