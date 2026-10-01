using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates a persistent top-left back arrow that reopens the model upload page.
/// It uses its own overlay canvas so gameplay panels cannot cover it.
/// </summary>
public sealed class BackToUploadButton : MonoBehaviour
{
    private const string CanvasName = "BackNavigationCanvas";
    private const float ButtonSize = 76f;
    private const float SafeAreaMargin = 22f;

    private ModelUploadUI uploadUI;
    private RectTransform buttonRect;
    private Button button;
    private CanvasGroup canvasGroup;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    public static BackToUploadButton Instance { get; private set; }
    public bool IsVisible { get; private set; }

    public static BackToUploadButton Create(ModelUploadUI uploadUI)
    {
        GameObject existingCanvas = GameObject.Find(CanvasName);
        if (existingCanvas != null)
        {
            BackToUploadButton existing = existingCanvas.GetComponentInChildren<BackToUploadButton>(true);
            if (existing != null)
            {
                existing.Initialize(uploadUI);
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
            "BackToUploadButton",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(CanvasGroup),
            typeof(BackToUploadButton));
        buttonObject.layer = uiLayer;
        buttonObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(ButtonSize, ButtonSize);

        Image background = buttonObject.GetComponent<Image>();
        background.color = new Color(0.055f, 0.075f, 0.10f, 0.88f);
        background.raycastTarget = true;

        Button uiButton = buttonObject.GetComponent<Button>();
        uiButton.targetGraphic = background;
        uiButton.navigation = new Navigation { mode = Navigation.Mode.None };

        ColorBlock colors = uiButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.88f, 0.94f, 1f, 1f);
        colors.pressedColor = new Color(0.65f, 0.82f, 1f, 1f);
        colors.selectedColor = colors.highlightedColor;
        uiButton.colors = colors;

        CreateArrowGraphic(buttonObject.transform, uiLayer);

        BackToUploadButton controller = buttonObject.GetComponent<BackToUploadButton>();
        controller.buttonRect = rect;
        controller.button = uiButton;
        controller.canvasGroup = buttonObject.GetComponent<CanvasGroup>();
        controller.Initialize(uploadUI);
        controller.SetVisible(false);
        return controller;
    }

    private static void CreateArrowGraphic(Transform parent, int uiLayer)
    {
        CreateArrowPiece(parent, uiLayer, "Shaft", new Vector2(34f, 6f), new Vector2(5f, 0f), 0f);
        CreateArrowPiece(parent, uiLayer, "UpperHead", new Vector2(22f, 6f), new Vector2(-8f, 7f), 45f);
        CreateArrowPiece(parent, uiLayer, "LowerHead", new Vector2(22f, 6f), new Vector2(-8f, -7f), -45f);
    }

    private static void CreateArrowPiece(
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

    private void Initialize(ModelUploadUI targetUploadUI)
    {
        Instance = this;
        uploadUI = targetUploadUI;

        if (buttonRect == null)
            buttonRect = GetComponent<RectTransform>();
        if (button == null)
            button = GetComponent<Button>();
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        button.onClick.RemoveListener(OpenUploadPage);
        button.onClick.AddListener(OpenUploadPage);

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
        Vector2 safeTopLeftAnchor = new Vector2(
            safeArea.xMin / Screen.width,
            safeArea.yMax / Screen.height);

        buttonRect.anchorMin = safeTopLeftAnchor;
        buttonRect.anchorMax = safeTopLeftAnchor;
        buttonRect.pivot = new Vector2(0f, 1f);
        buttonRect.anchoredPosition = new Vector2(SafeAreaMargin, -SafeAreaMargin);
        buttonRect.sizeDelta = new Vector2(ButtonSize, ButtonSize);
        buttonRect.localScale = Vector3.one;
        buttonRect.localRotation = Quaternion.identity;

        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }

    private void OpenUploadPage()
    {
        if (uploadUI == null)
            uploadUI = FindAnyObjectByType<ModelUploadUI>();

        if (uploadUI != null)
            uploadUI.ShowUploadPage();
        else
            Debug.LogError("[BackToUploadButton] ModelUploadUI was not found");
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OpenUploadPage);

        if (Instance == this)
            Instance = null;
    }
}
