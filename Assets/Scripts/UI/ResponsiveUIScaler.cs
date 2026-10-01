using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Makes UI responsive to any device screen size and aspect ratio
/// Automatically adjusts UI elements to fit device specifications
/// </summary>
public class ResponsiveUIScaler : MonoBehaviour
{
    [Header("Canvas References")]
    public Canvas mainCanvas;
    
    [Header("Scaling Settings")]
    [Tooltip("Reference resolution width")]
    public float referenceWidth = 1080f;
    
    [Tooltip("Reference resolution height")]
    public float referenceHeight = 1920f;
    
    [Tooltip("Scale UI buttons/elements based on screen size")]
    public bool autoScaleUI = true;
    
    [Tooltip("Adjust font sizes for readability")]
    public bool autoScaleFonts = true;
    
    [Header("Anchor Presets")]
    [Tooltip("Apply safe area padding to UI elements")]
    public bool applySafeAreaPadding = true;
    
    private Vector2 lastScreenSize = Vector2.zero;
    private CanvasScaler canvasScaler;
    
    void Start()
    {
        if (mainCanvas == null)
        {
            mainCanvas = GetComponent<Canvas>();
        }
        
        if (mainCanvas == null)
        {
            mainCanvas = FindObjectOfType<Canvas>();
        }
        
        // Setup canvas scaler if available
        if (mainCanvas != null)
        {
            canvasScaler = mainCanvas.GetComponent<CanvasScaler>();
            
            if (canvasScaler == null)
            {
                canvasScaler = mainCanvas.gameObject.AddComponent<CanvasScaler>();
            }
            
            ConfigureCanvasScaler();
        }
        
        ApplyResponsiveScaling();
    }
    
    void Update()
    {
        // Detect screen rotation or size change
        if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
        {
            lastScreenSize = new Vector2(Screen.width, Screen.height);
            ApplyResponsiveScaling();
            Debug.Log($"[ResponsiveUIScaler] Screen resized to {Screen.width}x{Screen.height}");
        }
    }
    
    void ConfigureCanvasScaler()
    {
        if (canvasScaler == null)
            return;
        
        // Use Scale With Screen Size for responsive UI
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(referenceWidth, referenceHeight);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        
        // Use height matching for better tablet support
        canvasScaler.matchWidthOrHeight = 0.5f; // Match based on screen aspect ratio
        
        Debug.Log($"[ResponsiveUIScaler] Canvas scaler configured - Reference: {referenceWidth}x{referenceHeight}");
    }
    
    void ApplyResponsiveScaling()
    {
        // Get current screen aspect ratio
        float screenAspect = (float)Screen.width / Screen.height;
        float referenceAspect = referenceWidth / referenceHeight;
        
        // Calculate scale factor
        float scaleFactor = Screen.width / referenceWidth;
        if (screenAspect > referenceAspect)
        {
            scaleFactor = Screen.height / referenceHeight;
        }
        
        Debug.Log($"[ResponsiveUIScaler] Screen aspect: {screenAspect:F2}, Scale factor: {scaleFactor:F2}");
        
        // Scale UI elements
        if (autoScaleUI)
        {
            ScaleUIElements(scaleFactor);
        }
        
        // Scale fonts
        if (autoScaleFonts)
        {
            ScaleFonts(scaleFactor);
        }
        
        // Apply safe area
        if (applySafeAreaPadding)
        {
            ApplySafeArea();
        }
    }
    
    void ScaleUIElements(float scaleFactor)
    {
        // Scale all buttons in the scene
        Button[] buttons = FindObjectsOfType<Button>();
        foreach (Button btn in buttons)
        {
            RectTransform rect = btn.GetComponent<RectTransform>();
            if (rect != null && rect.sizeDelta.magnitude > 0)
            {
                // Adjust button size based on scale
                float minScale = Mathf.Max(0.8f, scaleFactor);
                float maxScale = Mathf.Min(1.5f, scaleFactor * 1.2f);
                
                rect.localScale = Vector3.one * Mathf.Clamp(scaleFactor, minScale, maxScale);
            }
        }
    }
    
    void ScaleFonts(float scaleFactor)
    {
        // Scale text sizes
        TextMesh[] textMeshes = FindObjectsOfType<TextMesh>();
        foreach (TextMesh tm in textMeshes)
        {
            tm.characterSize = Mathf.Max(0.1f, tm.characterSize * scaleFactor);
        }
        
        // Scale TextMeshPro
        TMPro.TextMeshProUGUI[] textMeshPros = FindObjectsOfType<TMPro.TextMeshProUGUI>();
        foreach (TMPro.TextMeshProUGUI tmp in textMeshPros)
        {
            float baseFontSize = 36f; // Default TMPro font size
            tmp.fontSize = Mathf.Max(18, baseFontSize * scaleFactor);
        }
    }
    
    void ApplySafeArea()
    {
        // Get safe area from device
        Rect safeArea = Screen.safeArea;
        
        if (mainCanvas != null && mainCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            RectTransform canvasRect = mainCanvas.GetComponent<RectTransform>();
            
            // Convert safe area to canvas coordinates
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;
            
            // Apply padding to main canvas
            canvasRect.anchorMin = anchorMin;
            canvasRect.anchorMax = anchorMax;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;
        }
    }
    
    public void SetReferenceResolution(float width, float height)
    {
        referenceWidth = width;
        referenceHeight = height;
        
        if (canvasScaler != null)
        {
            canvasScaler.referenceResolution = new Vector2(width, height);
        }
        
        ApplyResponsiveScaling();
    }
    
    public void ForceUpdateScaling()
    {
        lastScreenSize = new Vector2(-1, -1); // Force update
        ApplyResponsiveScaling();
    }
}
