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
        // Connect to our PlayerController data channels
        PlayerController.OnHealthChanged += UpdateHealth;
        PlayerController.OnCoinsChanged += UpdateCoins;
    }

    void OnDisable()
    {
        PlayerController.OnHealthChanged -= UpdateHealth;
        PlayerController.OnCoinsChanged -= UpdateCoins;
    }

    // NEW: Directly accessible by LevelHandler to display the actual generation index!
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