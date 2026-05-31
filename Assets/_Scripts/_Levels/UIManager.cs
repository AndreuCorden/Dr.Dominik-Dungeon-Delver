using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public static bool IsPaused { get; private set; }

    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI roomText;
    [SerializeField] private Image[] heartImages;
    [SerializeField] private Sprite[] heartBreakSprites;
    [SerializeField] private float heartFrameDuration = 0.05f;

    [Header("Pause Menu")]
    [SerializeField] private Transform pauseUiParent;
    [SerializeField] private GameObject pauseMenuPrefab;
    [SerializeField] private GameObject pauseMenuPanel;

    private int previousHealth = -1;
    private Coroutine[] heartAnimCoroutines = new Coroutine[3];

    void Awake()
    {
        Instance = this;
        EnsurePauseUiExists();
    }

    void Start()
    {
        int health = PlayerController.Instance != null ? PlayerController.Instance.health : 3;
        previousHealth = health;
        RefreshHeartsInstant(health);
        HidePauseUi();
        ApplyPauseMenuLabels();
        WirePauseControls();
    }

    void OnEnable()
    {
        PlayerController.OnHealthChanged += UpdateHealth;
        PlayerController.OnCoinsChanged += UpdateCoins;
    }

    void OnDisable()
    {
        PlayerController.OnHealthChanged -= UpdateHealth;
        PlayerController.OnCoinsChanged -= UpdateCoins;
    }

    void OnDestroy()
    {
        IsPaused = false;
        if (Instance == this)
            Instance = null;
    }

    public void OnClickPause()
    {
        if (IsPaused)
            return;

        LevelHandler handler = FindFirstObjectByType<LevelHandler>();
        if (handler != null && handler.IsTransitioning)
            return;

        IsPaused = true;
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);

        Time.timeScale = 0f;
    }

    public void OnClickResume()
    {
        if (!IsPaused)
            return;

        if (NavigationManager.Instance != null)
            NavigationManager.Instance.ClearReturnToPauseMenuAfterInstructions();

        HidePauseUi();
        Time.timeScale = 1f;
        IsPaused = false;
    }

    public void OnClickRestartLevel()
    {
        OnClickResume();

        LevelHandler handler = FindFirstObjectByType<LevelHandler>();
        if (handler != null && !handler.IsTransitioning)
        {
            handler.StartExitTransition(handler.levelIndex);
            return;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnClickOpenInstructions()
    {
        if (!IsPaused)
            return;

        if (NavigationManager.Instance != null)
            NavigationManager.Instance.OpenInstructionsFromPauseMenu();
    }

    public void HidePauseMenuPanel()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
    }

    public void ShowPauseMenuPanel()
    {
        if (!IsPaused)
            return;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
    }

    public void OnClickExitToMainMenu()
    {
        if (NavigationManager.Instance != null)
            NavigationManager.Instance.ClearReturnToPauseMenuAfterInstructions();

        HidePauseUi();
        IsPaused = false;

        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.ReturnToMainMenu();
        }
        else
        {
            Debug.LogWarning("NavigationManager instance not found. Falling back to direct scene load.");
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
    }

    public void UpdateRoom(int currentRoomIndex)
    {
        if (roomText != null)
            roomText.text = $"{currentRoomIndex + 1}";
    }

    public void UpdateCoins(int amount) => coinText.text = amount.ToString();

    public void UpdateHealth(int health)
    {
        if (heartBreakSprites == null || heartBreakSprites.Length == 0)
        {
            UpdateHealthLegacy(health);
            return;
        }

        if (previousHealth < 0)
        {
            previousHealth = health;
            RefreshHeartsInstant(health);
            return;
        }

        if (health < previousHealth)
        {
            for (int i = health; i < previousHealth && i < heartImages.Length; i++)
                StartHeartBreakAnimation(i);
        }
        else if (health > previousHealth)
        {
            CancelAllHeartAnimations();
            RefreshHeartsInstant(health);
        }

        previousHealth = health;
    }

    void EnsurePauseUiExists()
    {
        if (pauseUiParent == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
                pauseUiParent = canvas.transform;
        }

        if (pauseMenuPanel == null && pauseMenuPrefab != null && pauseUiParent != null)
        {
            pauseMenuPanel = Instantiate(pauseMenuPrefab, pauseUiParent);
            pauseMenuPanel.name = "PauseMenu";
        }
    }

    void HidePauseUi()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
    }

    void ApplyPauseMenuLabels()
    {
        if (pauseMenuPanel == null)
            return;

        SetButtonLabel(pauseMenuPanel.transform, "Resume", "CONTINUAR");
        SetButtonLabel(pauseMenuPanel.transform, "Restart", "REINICIAR");
        SetButtonLabel(pauseMenuPanel.transform, "Settings", "INSTRUCCIONS");
        SetButtonLabel(pauseMenuPanel.transform, "Exit", "SORTIR");

        TextMeshProUGUI title = pauseMenuPanel.transform.Find("MainMenuTitle")?.GetComponent<TextMeshProUGUI>();
        if (title != null)
            title.text = "MENU DE PAUSA";
    }

    void WirePauseControls()
    {
        if (pauseMenuPanel == null)
            return;

        WireButton(pauseMenuPanel.transform, "Resume", OnClickResume);
        WireButton(pauseMenuPanel.transform, "Restart", OnClickRestartLevel);
        WireButton(pauseMenuPanel.transform, "Settings", OnClickOpenInstructions);
        WireButton(pauseMenuPanel.transform, "Exit", OnClickExitToMainMenu);
    }

    void SetButtonLabel(Transform parent, string childName, string text)
    {
        Transform child = parent.Find(childName);
        if (child == null)
            return;

        TextMeshProUGUI label = child.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.text = text;
    }

    void WireButton(Transform parent, string childName, UnityEngine.Events.UnityAction action)
    {
        if (parent == null)
            return;

        Button button = parent.Find(childName)?.GetComponent<Button>();
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    void UpdateHealthLegacy(int health)
    {
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] != null)
                heartImages[i].color = (i < health) ? Color.red : Color.black;
        }
    }

    void RefreshHeartsInstant(int health)
    {
        if (heartBreakSprites == null || heartBreakSprites.Length == 0)
            return;

        int fullFrame = 0;
        int emptyFrame = heartBreakSprites.Length - 1;

        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null)
                continue;

            heartImages[i].color = Color.white;
            heartImages[i].sprite = i < health ? heartBreakSprites[fullFrame] : heartBreakSprites[emptyFrame];
        }
    }

    void StartHeartBreakAnimation(int index)
    {
        if (index < 0 || index >= heartImages.Length)
            return;

        if (heartAnimCoroutines[index] != null)
        {
            StopCoroutine(heartAnimCoroutines[index]);
            heartAnimCoroutines[index] = null;
        }

        heartAnimCoroutines[index] = StartCoroutine(PlayHeartBreakAnimation(index));
    }

    void CancelAllHeartAnimations()
    {
        for (int i = 0; i < heartAnimCoroutines.Length; i++)
        {
            if (heartAnimCoroutines[i] == null)
                continue;

            StopCoroutine(heartAnimCoroutines[i]);
            heartAnimCoroutines[i] = null;
        }
    }

    IEnumerator PlayHeartBreakAnimation(int index)
    {
        Image img = heartImages[index];
        if (img == null || heartBreakSprites == null || heartBreakSprites.Length == 0)
            yield break;

        img.color = Color.white;
        int lastFrame = heartBreakSprites.Length - 1;

        for (int frame = 0; frame <= lastFrame; frame++)
        {
            img.sprite = heartBreakSprites[frame];
            if (frame < lastFrame)
                yield return new WaitForSeconds(heartFrameDuration);
        }

        heartAnimCoroutines[index] = null;
    }
}
