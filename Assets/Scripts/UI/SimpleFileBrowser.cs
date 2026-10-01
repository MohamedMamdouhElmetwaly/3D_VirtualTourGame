using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Simple file browser UI for Android - allows users to browse and select .glb files
/// Pure C# implementation, no Java plugins needed
/// </summary>
public class SimpleFileBrowser : MonoBehaviour
{
    [Header("UI References")]
    public GameObject browserPanel;
    public Transform fileListContent;
    public GameObject fileItemPrefab;
    public TextMeshProUGUI currentPathText;
    public Button backButton;
    public Button cancelButton;
    public Button selectButton;
    
    [Header("Settings")]
    public string[] allowedExtensions = { ".glb", ".gltf" };
    
    private string currentPath;
    private string selectedFilePath;
    private System.Action<string> onFileSelectedCallback;
    
    void Awake()
    {
        if (browserPanel != null)
            browserPanel.SetActive(false);
        
        if (backButton != null)
            backButton.onClick.AddListener(NavigateUp);
        
        if (cancelButton != null)
            cancelButton.onClick.AddListener(Cancel);
        
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(ConfirmSelection);
            selectButton.interactable = false;
        }
    }
    
    /// <summary>
    /// Open file browser
    /// </summary>
    public void OpenBrowser(System.Action<string> callback)
    {
        onFileSelectedCallback = callback;
        selectedFilePath = null;
        
        if (selectButton != null)
            selectButton.interactable = false;
        
#if UNITY_ANDROID && !UNITY_EDITOR
        // Request storage permission first
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.ExternalStorageRead))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.ExternalStorageRead);
        }
        
        // Start at common storage root
        string startPath = "/storage/emulated/0";
        if (!Directory.Exists(startPath))
            startPath = "/sdcard";
        
        NavigateToPath(startPath);
#else
        // For editor testing
        NavigateToPath(Application.dataPath);
#endif
        
        if (browserPanel != null)
            browserPanel.SetActive(true);
    }
    
    /// <summary>
    /// Navigate to specific path
    /// </summary>
    void NavigateToPath(string path)
    {
        if (!Directory.Exists(path))
        {
            Debug.LogWarning($"[FileBrowser] Path does not exist: {path}");
            return;
        }
        
        currentPath = path;
        
        if (currentPathText != null)
            currentPathText.text = currentPath;
        
        // Clear existing items
        foreach (Transform child in fileListContent)
        {
            Destroy(child.gameObject);
        }
        
        // Get directories and files
        List<string> items = new List<string>();
        
        try
        {
            // Add directories first
            string[] directories = Directory.GetDirectories(currentPath);
            items.AddRange(directories);
            
            // Add files with allowed extensions
            string[] files = Directory.GetFiles(currentPath);
            foreach (string file in files)
            {
                string extension = Path.GetExtension(file).ToLower();
                if (allowedExtensions.Contains(extension))
                {
                    items.Add(file);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[FileBrowser] Error reading directory: {e.Message}");
        }
        
        // Create UI items
        foreach (string item in items)
        {
            CreateFileItem(item);
        }
        
        // Update back button
        if (backButton != null)
        {
            bool canGoBack = currentPath != "/" && !string.IsNullOrEmpty(Path.GetDirectoryName(currentPath));
            backButton.interactable = canGoBack;
        }
    }
    
    /// <summary>
    /// Create UI item for file/folder
    /// </summary>
    void CreateFileItem(string fullPath)
    {
        GameObject item;
        
        if (fileItemPrefab != null)
        {
            item = Instantiate(fileItemPrefab, fileListContent);
        }
        else
        {
            // Create simple item if no prefab
            item = new GameObject("FileItem");
            item.transform.SetParent(fileListContent, false);
            
            // Add button
            Button button = item.AddComponent<Button>();
            
            // Add text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(item.transform, false);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.fontSize = 18;
            text.color = Color.white;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);
        }
        
        // Get display name
        string displayName = Path.GetFileName(fullPath);
        bool isDirectory = Directory.Exists(fullPath);
        
        if (isDirectory)
            displayName = "📁 " + displayName;
        else
            displayName = "📄 " + displayName;
        
        // Set text
        TextMeshProUGUI itemText = item.GetComponentInChildren<TextMeshProUGUI>();
        if (itemText != null)
            itemText.text = displayName;
        
        // Set button action
        Button itemButton = item.GetComponent<Button>();
        if (itemButton != null)
        {
            itemButton.onClick.AddListener(() => OnItemClicked(fullPath, isDirectory));
        }
        
        // Set RectTransform
        RectTransform rect = item.GetComponent<RectTransform>();
        if (rect == null)
            rect = item.AddComponent<RectTransform>();
        
        rect.sizeDelta = new Vector2(0, 50);
    }
    
    /// <summary>
    /// Handle item click
    /// </summary>
    void OnItemClicked(string path, bool isDirectory)
    {
        if (isDirectory)
        {
            // Navigate into directory
            NavigateToPath(path);
        }
        else
        {
            // Select file
            selectedFilePath = path;
            
            if (selectButton != null)
                selectButton.interactable = true;
            
            Debug.Log($"[FileBrowser] File selected: {path}");
        }
    }
    
    /// <summary>
    /// Navigate up one level
    /// </summary>
    void NavigateUp()
    {
        string parentPath = Path.GetDirectoryName(currentPath);
        
        if (!string.IsNullOrEmpty(parentPath))
        {
            NavigateToPath(parentPath);
        }
    }
    
    /// <summary>
    /// Confirm file selection
    /// </summary>
    void ConfirmSelection()
    {
        if (browserPanel != null)
            browserPanel.SetActive(false);
        
        onFileSelectedCallback?.Invoke(selectedFilePath);
    }
    
    /// <summary>
    /// Cancel file selection
    /// </summary>
    void Cancel()
    {
        if (browserPanel != null)
            browserPanel.SetActive(false);
        
        onFileSelectedCallback?.Invoke(null);
    }
}
