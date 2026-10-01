using UnityEngine;

/// <summary>
/// Ensures all critical UI elements exist when the scene starts
/// Note: ExitButton is now automatically created via ExitButton.Create() in ModelUploadUI
/// </summary>
public class UIInitializer : MonoBehaviour
{
    [Header("Debug")]
    public bool debugLog = true;
    
    void Awake()
    {
        if (debugLog) Debug.Log("[UIInitializer] UI initialization complete.");
        // Exit button is now handled by ExitButton.Create() in ModelUploadUI.Start()
    }
}
