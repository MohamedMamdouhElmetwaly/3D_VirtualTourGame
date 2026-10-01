using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shows a comprehensive guide for the app features (Top View and Virtual Tour)
/// Can be toggled on/off with a button
/// </summary>
public class FeatureGuidePanel : MonoBehaviour
{
    [Header("UI References")]
    public GameObject guidePanel;
    public Button closeButton;
    public Button guideToggleButton; // Optional: button to show/hide guide
    
    [Header("Guide Content")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI contentText;
    
    [Header("Settings")]
    public bool showOnStart = false;
    [Tooltip("Auto-hide guide after this many seconds (0 = manual)")]
    public float autoHideDelay = 10f;
    
    private float hideTimer = 0f;
    private bool isGuideVisible = false;
    
    // Guide content text
    private readonly string[] guideTitles = new string[]
    {
        "TOP VIEW - Floor Plan Overview",
        "VIRTUAL TOUR - First-Person Experience",
        "Controls Guide"
    };
    
    private readonly string[] guideContents = new string[]
    {
        "TOP VIEW shows a bird's-eye view of the entire space.\n\n" +
        "• Use Joystick (left side) to pan the view\n" +
        "• Drag right side to rotate the view\n" +
        "• Two-finger pinch to zoom in/out\n" +
        "• Perfect for understanding the overall layout",
        
        "VIRTUAL TOUR is a first-person walkthrough experience.\n\n" +
        "• Use Joystick (left side) to move forward/back/strafe\n" +
        "• Drag right side to look around\n" +
        "• Two-finger pinch to zoom in/out\n" +
        "• Explore the space as if you're inside it",
        
        "CONTROLS:\n" +
        "• Left Side (40%): Move / Look up/down\n" +
        "• Right Side (60%): Rotate / Look around\n" +
        "• Two Fingers: Pinch to zoom\n" +
        "• TOP VIEW & VIRTUAL TOUR: Switch between modes\n" +
        "• EXIT: Leave the app safely"
    };
    
    void Start()
    {
        if (guidePanel == null)
        {
            Debug.LogWarning("[FeatureGuidePanel] Guide panel not assigned");
            return;
        }
        
        // Setup close button
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HideGuide);
        }
        
        // Setup toggle button
        if (guideToggleButton != null)
        {
            guideToggleButton.onClick.AddListener(ToggleGuide);
        }
        
        // Initialize visibility
        if (showOnStart)
        {
            ShowGuide();
        }
        else
        {
            guidePanel.SetActive(false);
        }
    }
    
    void Update()
    {
        // Handle auto-hide timer
        if (autoHideDelay > 0 && isGuideVisible && hideTimer > 0)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0)
            {
                HideGuide();
            }
        }
    }
    
    public void ShowGuide(int guideIndex = 0)
    {
        if (guidePanel == null)
            return;
        
        // Ensure index is valid
        guideIndex = Mathf.Clamp(guideIndex, 0, guideTitles.Length - 1);
        
        // Update content
        if (titleText != null)
            titleText.text = guideTitles[guideIndex];
        if (contentText != null)
            contentText.text = guideContents[guideIndex];
        
        // Show panel
        guidePanel.SetActive(true);
        isGuideVisible = true;
        
        // Start hide timer
        if (autoHideDelay > 0)
        {
            hideTimer = autoHideDelay;
        }
        
        Debug.Log($"[FeatureGuidePanel] Showing guide: {guideTitles[guideIndex]}");
    }
    
    public void HideGuide()
    {
        if (guidePanel != null)
        {
            guidePanel.SetActive(false);
        }
        isGuideVisible = false;
        hideTimer = 0;
        
        Debug.Log("[FeatureGuidePanel] Guide hidden");
    }
    
    public void ToggleGuide()
    {
        if (isGuideVisible)
        {
            HideGuide();
        }
        else
        {
            ShowGuide(0); // Show first guide (Top View)
        }
    }
    
    public void ShowNextGuide()
    {
        // Cycle through guides
        int currentIndex = 0;
        if (titleText != null)
        {
            for (int i = 0; i < guideTitles.Length; i++)
            {
                if (titleText.text == guideTitles[i])
                {
                    currentIndex = (i + 1) % guideTitles.Length;
                    break;
                }
            }
        }
        
        ShowGuide(currentIndex);
    }
}
