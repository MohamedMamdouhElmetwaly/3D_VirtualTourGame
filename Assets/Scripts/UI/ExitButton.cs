using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates a persistent top-right exit button that quits the application.
/// It uses its own overlay canvas so gameplay panels cannot cover it.
/// </summary>
public sealed class ExitButton : MonoBehaviour
{
    private const string CanvasName = "ExitButtonCanvas";
    private const float ButtonSize = 76f;
    private const float SafeAreaMargin = 22f;

    private RectTransform buttonRect;
    private Button button;
    private CanvasGroup canvasGroup;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    public static ExitButton Instance { get; private set; }
    public bool IsVisible { get; private set; }

    public static ExitButton Create()
    {
        GameObject existingCanvas = GameObject.Find(CanvasName);
        if (existingCanvas != null)
        {
            ExitButton existing = existingCanvas.GetComponentInChildren<ExitButton>(true);
            if (existing != null)
            {
                existing.Initialize();
                return existing;
            }
        }

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0)
            uiLayer = 5;

        GameObject canvasObject = new GameObject(
            CanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.layer = uiLayer;

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 30000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject buttonObject = new GameObject(
            "ExitButton",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(CanvasGroup),
            typeof(ExitButton));
        buttonObject.layer = uiLayer;
        buttonObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(ButtonSize, ButtonSize);

        Image background = buttonObject.GetComponent<Image>();
        background.color = new Color(0.85f, 0.15f, 0.15f, 0.88f); // Red background
        background.raycastTarget = true;

        Button uiButton = buttonObject.GetComponent<Button>();
        uiButton.targetGraphic = background;
        uiButton.navigation = new Navigation { mode = Navigation.Mode.None };

        ColorBlock colors = uiButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.88f, 0.88f, 1f);
        colors.pressedColor = new Color(1f, 0.65f, 0.65f, 1f);
        colors.selectedColor = colors.highlightedColor;
        uiButton.colors = colors;

        CreateExitGraphic(buttonObject.transform, uiLayer);

        ExitButton controller = buttonObject.GetComponent<ExitButton>();
        controller.buttonRect = rect;
        controller.button = uiButton;
        controller.canvasGroup = buttonObject.GetComponent<CanvasGroup>();
        controller.Initialize();
        controller.SetVisible(false); // Hidden by default, shown after loading
        return controller;
    }

    private static void CreateExitGraphic(Transform parent, int uiLayer)
    {
        // Create an "X" shape with two diagonal lines
        CreateExitPiece(parent, uiLayer, "Diagonal1", new Vector2(50f, 6f), Vector2.zero, 45f);
        CreateExitPiece(parent, uiLayer, "Diagonal2", new Vector2(50f, 6f), Vector2.zero, -45f);
    }

    private static void CreateExitPiece(
        Transform parent,
        int uiLayer,
        string name,
        Vector2 size,
        Vector2 position,
        float rotation)
    {
        GameObject piece = new GameObject(name, typeof(RectTransform), typeof(Image));
        piece.layer = uiLayer;
        piece.transform.SetParent(parent, false);

        RectTransform rect = piece.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localRotation = Quaternion.Euler(0f, 0f, rotation);

        Image image = piece.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = false;
    }

    private void Initialize()
    {
        Instance = this;

        if (buttonRect == null)
            buttonRect = GetComponent<RectTransform>();
        if (button == null)
            button = GetComponent<Button>();
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        button.onClick.RemoveListener(ExitGame);
        button.onClick.AddListener(ExitGame);

        ApplySafeAreaPosition();
    }

    public void SetVisible(bool visible)
    {
        IsVisible = visible;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;

    }

    private void Update()
    {
        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
        if (lastSafeArea != Screen.safeArea || lastScreenSize != screenSize)
            ApplySafeAreaPosition();
    }

    private void ApplySafeAreaPosition()
    {
        if (buttonRect == null || Screen.width <= 0 || Screen.height <= 0)
            return;

        Rect safeArea = Screen.safeArea;
        // Position at top-right corner
        Vector2 safeTopRightAnchor = new Vector2(
            safeArea.xMax / Screen.width,
            safeArea.yMax / Screen.height);

        buttonRect.anchorMin = safeTopRightAnchor;
        buttonRect.anchorMax = safeTopRightAnchor;
        buttonRect.pivot = new Vector2(1f, 1f); // Pivot at top-right
        buttonRect.anchoredPosition = new Vector2(-SafeAreaMargin, -SafeAreaMargin); // Negative margin for right side
        buttonRect.sizeDelta = new Vector2(ButtonSize, ButtonSize);
        buttonRect.localScale = Vector3.one;
        buttonRect.localRotation = Quaternion.identity;

        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }

    private void ExitGame()
    {
        Debug.Log("[ExitButton] Exiting the application...");
        
#if UNITY_EDITOR
        // Stop play mode in the editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Quit the application on device
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(ExitGame);

        if (Instance == this)
            Instance = null;
    }
}
