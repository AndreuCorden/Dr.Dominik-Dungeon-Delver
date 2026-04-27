using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI roomText;
    [SerializeField] private Image[] heartImages;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    void OnEnable()
    {
        PlayerController.OnHealthChanged += UpdateHealth;
        PlayerController.OnCoinsChanged += UpdateCoins;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        PlayerController.OnHealthChanged -= UpdateHealth;
        PlayerController.OnCoinsChanged -= UpdateCoins;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (roomText != null) roomText.text = $"Room: {scene.buildIndex}";
        if (PlayerController.Instance != null) UpdateHealth(PlayerController.Instance.health);
    }

    public void UpdateCoins(int amount) => coinText.text = $"Coins: {amount}";

    public void UpdateHealth(int health)
    {
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] != null)
                heartImages[i].color = (i < health) ? Color.red : Color.black;
        }
    }
}