using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Provides on-screen tooltips/guides for UI buttons
/// Shows guidance when user hovers or clicks buttons
/// </summary>
public class ButtonGuideTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Tooltip Settings")]
    [TextArea(3, 5)]
    public string tooltipText = "Button description";
    
    [Tooltip("Show tooltip on hover (enter)")]
    public bool showOnHover = true;
    
    [Tooltip("Show tooltip on click")]
    public bool showOnClick = true;
    
    [Tooltip("Duration to show tooltip (0 = permanent until hover away)")]
    public float displayDuration = 0f;
    
    [Header("Tooltip References")]
    public Canvas tooltipCanvas;
    public GameObject tooltipPanelPrefab;
    
    private GameObject tooltipInstance;
    private TextMeshProUGUI tooltipText_UI;
    private CanvasGroup tooltipCanvasGroup;
    private float hideTimer = 0f;
    
    void Start()
    {
        // Ensure we can show tooltips
        if (tooltipCanvas == null)
        {
            // Try to find the canvas in parent
            tooltipCanvas = GetComponentInParent<Canvas>();
        }
        
        if (tooltipCanvas == null)
        {
            Debug.LogWarning("[ButtonGuideTooltip] No Canvas found for tooltip");
        }
    }
    
    void Update()
    {
        // Handle auto-hide timer
        if (displayDuration > 0 && tooltipInstance != null)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0)
            {
                HideTooltip();
            }
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (showOnHover)
        {
            ShowTooltip();
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (displayDuration == 0 && showOnHover)
        {
            HideTooltip();
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (showOnClick)
        {
            ShowTooltip();
            
            if (displayDuration > 0)
            {
                hideTimer = displayDuration;
            }
        }
    }
    
    public void ShowTooltip()
    {
        if (tooltipInstance == null)
        {
            CreateTooltip();
        }
        
        if (tooltipInstance != null)
        {
            tooltipInstance.SetActive(true);
            
            if (tooltipCanvasGroup != null)
            {
                tooltipCanvasGroup.alpha = 1f;
            }
        }
    }
    
    public void HideTooltip()
    {
        if (tooltipInstance != null)
        {
            tooltipInstance.SetActive(false);
        }
    }
    
    void CreateTooltip()
    {
        if (tooltipCanvas == null)
            return;
        
        // Create a simple tooltip panel if no prefab provided
        GameObject panel = new GameObject("Tooltip");
        panel.transform.SetParent(tooltipCanvas.transform, false);
        
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(300, 100);
        
        // Position below the button
        RectTransform buttonRect = GetComponent<RectTransform>();
        if (buttonRect != null)
        {
            panelRect.anchoredPosition = new Vector2(buttonRect.anchoredPosition.x, buttonRect.anchoredPosition.y - 120);
        }
        
        // Add background image
        Image bgImage = panel.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.8f);
        
        // Add text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(panel.transform, false);
        
        tooltipText_UI = textObj.AddComponent<TextMeshProUGUI>();
        tooltipText_UI.text = tooltipText;
        tooltipText_UI.alignment = TextAlignmentOptions.Center;
        tooltipText_UI.fontSize = 20;
        tooltipText_UI.color = Color.white;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        textRect.sizeDelta = Vector2.zero;
        
        // Add canvas group for visibility control
        tooltipCanvasGroup = panel.AddComponent<CanvasGroup>();
        tooltipCanvasGroup.alpha = 1f;
        
        tooltipInstance = panel;
        tooltipInstance.SetActive(false);
    }
}
