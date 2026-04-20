using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI Elements")]
    public TextMeshProUGUI coinText;
    public TextMeshProUGUI roomText;
    public Image[] heartImages;
    public Color heartEmptyColor = Color.black;

    private int coins = 0;
    private int currentHealth = 3;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        coinText.text = $"Coins: {coins}";
        UpdateHealth(PlayerController.health);
    }

    public void UpdateCoins(int amount)
    {
        coins += amount;
        coinText.text = $"Coins: {coins}";
    }

    public void UpdateHealth(int health)
    {
        currentHealth = health;
        
        // Loop through hearts and "dim" the ones we lost
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (i < currentHealth)
                heartImages[i].color = Color.red;
            else
                heartImages[i].color = heartEmptyColor; // Makes lost hearts look empty
        }
    }

    public void UpdateRoom(int roomNumber)
    {
        roomText.text = $"Room: {roomNumber}";
    }
}