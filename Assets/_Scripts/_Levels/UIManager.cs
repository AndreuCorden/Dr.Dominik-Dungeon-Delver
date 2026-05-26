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
        // Simple scene-local instance assignment
        Instance = this;
    }

    void OnEnable()
    {
        // Connect to our PlayerController data channels
        PlayerController.OnHealthChanged += UpdateHealth;
        PlayerController.OnCoinsChanged += UpdateCoins;
    }

    void OnDisable()
    {
        PlayerController.OnHealthChanged -= UpdateHealth;
        PlayerController.OnCoinsChanged -= UpdateCoins;
    }

    public void OnClickExitToMainMenu()
    {
        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.ReturnToMainMenu();
        }
        else
        {
            // Fallback just in case you are testing the gameplay scene by itself without booting from the menu
            Debug.LogWarning("NavigationManager instance not found. Falling back to direct scene load.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }

    // Directly accessible by LevelHandler to display the actual generation index!
    public void UpdateRoom(int currentRoomIndex)
    {
        if (roomText != null) 
            roomText.text = $"Room: {currentRoomIndex + 1}";
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