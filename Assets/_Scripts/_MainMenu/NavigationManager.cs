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

    [Header("UI Persistent Overlays")]
    [SerializeField] private GameObject instructionsCanvas;

    private bool returnToPauseMenuAfterInstructions;

    public bool AreInstructionsOpen =>
        instructionsCanvas != null && instructionsCanvas.activeSelf;

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

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        HidePersistentOverlays();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == mainMenuSceneName)
            HidePersistentOverlays();
    }

    void HidePersistentOverlays()
    {
        if (instructionsCanvas != null)
            instructionsCanvas.SetActive(false);

        returnToPauseMenuAfterInstructions = false;
        Time.timeScale = 1f;
    }

    public void ClearReturnToPauseMenuAfterInstructions()
    {
        returnToPauseMenuAfterInstructions = false;
    }

    public void OpenInstructionsFromPauseMenu()
    {
        OpenInstructions(openedFromPauseMenu: true);
    }

    public void OpenInstructions(bool openedFromPauseMenu = false)
    {
        if (instructionsCanvas == null)
        {
            Debug.LogWarning("Instructions Canvas reference is missing on NavigationManager!");
            return;
        }

        if (AreInstructionsOpen)
            return;

        bool openedWhilePaused = openedFromPauseMenu ||
            (UIManager.Instance != null && UIManager.IsPaused);

        returnToPauseMenuAfterInstructions = openedWhilePaused;

        if (openedWhilePaused && UIManager.Instance != null)
            UIManager.Instance.HidePauseMenuPanel();

        instructionsCanvas.SetActive(true);
        Time.timeScale = 0f;
    }

    public void CloseInstructions()
    {
        if (instructionsCanvas == null)
        {
            Debug.LogWarning("Instructions Canvas reference is missing on NavigationManager!");
            return;
        }

        if (!AreInstructionsOpen)
            return;

        instructionsCanvas.SetActive(false);

        if (returnToPauseMenuAfterInstructions &&
            UIManager.Instance != null &&
            UIManager.IsPaused)
        {
            returnToPauseMenuAfterInstructions = false;
            UIManager.Instance.ShowPauseMenuPanel();
            Time.timeScale = 0f;
            return;
        }

        returnToPauseMenuAfterInstructions = false;
        Time.timeScale = 1f;
    }

    public void ToggleInstructions()
    {
        if (AreInstructionsOpen)
            CloseInstructions();
        else
            OpenInstructions(UIManager.Instance != null && UIManager.IsPaused);
    }

    void Update()
    {
        HandleKeyboardShortcuts();
    }

    public void StartGame()
    {
        HidePersistentOverlays();
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void ReturnToMainMenu()
    {
        HidePersistentOverlays();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OpenCreditsScene()
    {
        HidePersistentOverlays();
        SceneManager.LoadScene(creditsSceneName);
    }

    private void HandleKeyboardShortcuts()
    {
        if (Keyboard.current == null) return;

        string currentSceneName = SceneManager.GetActiveScene().name;

        if (Keyboard.current.iKey.wasPressedThisFrame)
            ToggleInstructions();

        if (instructionsCanvas != null && instructionsCanvas.activeSelf) return;

        if (UIManager.Instance != null && UIManager.IsPaused) return;

        if (Keyboard.current.mKey.wasPressedThisFrame && currentSceneName != mainMenuSceneName)
            ReturnToMainMenu();

        if (Keyboard.current.cKey.wasPressedThisFrame && currentSceneName != creditsSceneName)
            OpenCreditsScene();

        if (Keyboard.current.jKey.wasPressedThisFrame)
            StartGame();

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
