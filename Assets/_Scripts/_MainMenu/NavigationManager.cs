using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class NavigationManager : MonoBehaviour
{
    public static NavigationManager Instance { get; private set; }

    [Header("Scene Name Settings")]
    public string mainMenuSceneName = "MainMenu";
    public string gameplaySceneName = "LevelScene_01";
    public string creditsSceneName = "Credits";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        HandleKeyboardShortcuts();
    }

    // --- GLOBAL SCENE NAVIGATION ---

    public void StartGame() => SceneManager.LoadScene(gameplaySceneName);
    public void ReturnToMainMenu() => SceneManager.LoadScene(mainMenuSceneName);
    public void OpenCreditsScene() => SceneManager.LoadScene(creditsSceneName);

    // --- GLOBAL KEYBOARD SHORTCUTS ---
    private void HandleKeyboardShortcuts()
    {
        if (Keyboard.current == null) return;

        string currentSceneName = SceneManager.GetActiveScene().name;

        if (Keyboard.current.mKey.wasPressedThisFrame && currentSceneName != mainMenuSceneName)
        {
            ReturnToMainMenu();
        }

        if (Keyboard.current.cKey.wasPressedThisFrame && currentSceneName != creditsSceneName)
        {
            OpenCreditsScene();
        }

        if (Keyboard.current.jKey.wasPressedThisFrame)
        {
            StartGame();
        }

        CheckNumberKeys();
    }

    private void CheckNumberKeys()
    {
        var kbd = Keyboard.current;
        if (kbd == null) return;

        if (kbd.digit0Key.wasPressedThisFrame || kbd.numpad0Key.wasPressedThisFrame) LoadSpecificLevel(0);
        else if (kbd.digit1Key.wasPressedThisFrame || kbd.numpad1Key.wasPressedThisFrame) LoadSpecificLevel(1);
        else if (kbd.digit2Key.wasPressedThisFrame || kbd.numpad2Key.wasPressedThisFrame) LoadSpecificLevel(2);
        else if (kbd.digit3Key.wasPressedThisFrame || kbd.numpad3Key.wasPressedThisFrame) LoadSpecificLevel(3);
        else if (kbd.digit4Key.wasPressedThisFrame || kbd.numpad4Key.wasPressedThisFrame) LoadSpecificLevel(4);
        else if (kbd.digit5Key.wasPressedThisFrame || kbd.numpad5Key.wasPressedThisFrame) LoadSpecificLevel(5);
        else if (kbd.digit6Key.wasPressedThisFrame || kbd.numpad6Key.wasPressedThisFrame) LoadSpecificLevel(6);
        else if (kbd.digit7Key.wasPressedThisFrame || kbd.numpad7Key.wasPressedThisFrame) LoadSpecificLevel(7);
        else if (kbd.digit8Key.wasPressedThisFrame || kbd.numpad8Key.wasPressedThisFrame) LoadSpecificLevel(8);
        else if (kbd.digit9Key.wasPressedThisFrame || kbd.numpad9Key.wasPressedThisFrame) LoadSpecificLevel(9);
    }

    private void LoadSpecificLevel(int levelIndex)
    {
        LevelHandler activeHandler = GameObject.FindAnyObjectByType<LevelHandler>();

        if (activeHandler != null && SceneManager.GetActiveScene().name == gameplaySceneName)
        {
            // NEW: If the level handler is currently mid-animation, reject the input entirely!
            if (activeHandler.IsTransitioning)
            {
                Debug.LogWarning("Developer Shortcut Rejected: Level is currently transitioning.");
                return;
            }

            Debug.Log($"Mid-game Developer Shortcut: Transitioning directly to Level Index {levelIndex}");
            activeHandler.StartExitTransition(levelIndex);
        }
        else
        {
            Debug.Log($"Menu Developer Shortcut: Queueing Level Index {levelIndex}");
            PlayerPrefs.SetInt("SelectedLevelIndex", levelIndex);
            PlayerPrefs.Save();
            StartGame();
        }
    }
}