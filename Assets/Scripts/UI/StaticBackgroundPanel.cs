using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Makes a UI panel static and immune to touch/drag interactions
/// Useful for background elements that should not move with user input
/// </summary>
public class StaticBackgroundPanel : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Settings")]
    [Tooltip("If true, blocks raycasts from passing through to other objects")]
    public bool blockRaycasts = true;
    
    [Tooltip("If true, this panel will consume all touch events")]
    public bool consumeEvents = false;
    
    private RectTransform rectTransform;
    private Canvas canvas;
    
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        
        // Ensure we have a GraphicRaycaster parent
        if (canvas == null)
        {
            Debug.LogWarning("[StaticBackgroundPanel] Should be placed under a Canvas");
        }
        
        // Ensure we have a CanvasGroup for proper raycasting
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Configure CanvasGroup for static behavior
        canvasGroup.blocksRaycasts = blockRaycasts;
        canvasGroup.interactable = false; // Not interactable but still visible
        
        // Ensure Image component exists so raycasting works
        Image image = GetComponent<Image>();
        if (image == null)
        {
            image = gameObject.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = true;
        }
        
        Debug.Log("[StaticBackgroundPanel] Configured as static background - will not respond to touch");
    }
    
    // Implement drag handlers to consume and ignore drag events
    public void OnPointerDown(PointerEventData eventData)
    {
        if (consumeEvents)
        {
            eventData.Use();
        }
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (consumeEvents)
        {
            eventData.Use();
        }
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (consumeEvents)
        {
            eventData.Use();
        }
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (consumeEvents)
        {
            eventData.Use();
        }
    }
}
