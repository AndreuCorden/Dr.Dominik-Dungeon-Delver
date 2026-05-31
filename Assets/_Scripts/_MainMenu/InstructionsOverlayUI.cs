using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class InstructionsOverlayUI : MonoBehaviour
{
    private const string BackgroundName = "InstructionsBackground";

    [SerializeField] private Texture2D instructionsTexture;
    [SerializeField] private Button closeButton;

    void Awake()
    {
        ConfigureCanvas();
        EnsureBackground();
        ConfigureCloseButton();
    }

    void OnEnable()
    {
        EnsureBackground();
        ConfigureCloseButton();
    }

    void ConfigureCanvas()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }
    }

    void EnsureBackground()
    {
        Transform existing = transform.Find(BackgroundName);
        if (existing != null)
            return;

        if (instructionsTexture == null)
        {
            Debug.LogWarning("Instructions texture is not assigned on InstructionsOverlayUI.");
            return;
        }

        GameObject background = new GameObject(
            BackgroundName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage));

        background.transform.SetParent(transform, false);
        background.transform.SetAsFirstSibling();

        RectTransform rect = background.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        RawImage rawImage = background.GetComponent<RawImage>();
        rawImage.texture = instructionsTexture;
        rawImage.raycastTarget = false;
    }

    void ConfigureCloseButton()
    {
        if (closeButton == null)
            closeButton = GetComponentInChildren<Button>(true);

        if (closeButton == null)
            return;

        // Replace the entire event so inspector-wired calls (e.g. ReturnToMainMenu) are cleared.
        closeButton.onClick = new Button.ButtonClickedEvent();
        closeButton.onClick.AddListener(() =>
        {
            if (NavigationManager.Instance != null)
                NavigationManager.Instance.CloseInstructions();
        });
    }
}
