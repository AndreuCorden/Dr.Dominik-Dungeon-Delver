using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI roomText;
    [SerializeField] private Image[] heartImages;
    [SerializeField] private Sprite[] heartBreakSprites;
    [SerializeField] private float heartFrameDuration = 0.05f;

    private int previousHealth = -1;
    private Coroutine[] heartAnimCoroutines = new Coroutine[3];

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        int health = PlayerController.Instance != null ? PlayerController.Instance.health : 3;
        previousHealth = health;
        RefreshHeartsInstant(health);
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

    public void OnClickExitToMainMenu()
    {
        if (NavigationManager.Instance != null)
        {
            NavigationManager.Instance.ReturnToMainMenu();
        }
        else
        {
            Debug.LogWarning("NavigationManager instance not found. Falling back to direct scene load.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
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
