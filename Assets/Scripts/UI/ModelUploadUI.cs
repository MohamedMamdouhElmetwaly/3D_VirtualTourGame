using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using System.IO;
using TMPro;

/// <summary>
/// UI for uploading 3D model files before the game starts
/// Supports .glb (recommended) and .obj formats only
/// Note: .gltf files require external .bin files and are not supported for direct upload
/// Shows a file picker and loads the selected model into the scene
/// </summary>
public class ModelUploadUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject uploadPanel;
    public Button assignButton;
    public TMP_InputField filePathInput;
    public TextMeshProUGUI statusText;
    public Button browseButton;
    public Button loadFromURLButton;
    public TMP_InputField urlInput;
    
    [Header("References")]
    public RuntimeModelLoader modelLoader;
    public GameManager gameManager;
    
    private string selectedFilePath = "";
    private bool isLoadingFromURL = false;
    
    // WebGL file upload support
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void UploadFile(string gameObjectName, string methodName);
#endif

    void Start()
    {
        // Show upload panel at start
        if (uploadPanel != null)
            uploadPanel.SetActive(true);

        // Persistent navigation stays available above both the tour and upload UI.
        BackToUploadButton.Create(this).SetVisible(false);
        
        // Create persistent exit button in top-right corner (hidden until model loads)
        ExitButton.Create().SetVisible(false);

        // Setup button listeners
        if (assignButton != null)
        {
            assignButton.onClick.AddListener(OnAssignClicked);
            assignButton.interactable = false; // Disabled until file is selected
        }
        
        if (browseButton != null)
        {
            browseButton.onClick.AddListener(OnBrowseClicked);
        }
        
        if (loadFromURLButton != null)
        {
            loadFromURLButton.onClick.AddListener(OnLoadFromURLClicked);
        }
        
        if (filePathInput != null)
        {
            filePathInput.onEndEdit.AddListener(OnFilePathChanged);
        }
        
        UpdateStatusText("Please select a 3D model file (.glb) to begin", Color.white);
    }

    /// <summary>
    /// Reopen the upload page while keeping the current model, camera and game alive.
    /// </summary>
    public void ShowUploadPage()
    {
        if (uploadPanel == null)
        {
            Debug.LogError("[ModelUploadUI] Upload panel is not assigned");
            return;
        }

        uploadPanel.SetActive(true);
        uploadPanel.transform.SetAsLastSibling();

        bool modelIsLoading = modelLoader != null && modelLoader.IsLoading;

        if (browseButton != null)
            browseButton.interactable = !modelIsLoading;
        if (loadFromURLButton != null)
            loadFromURLButton.interactable = !modelIsLoading;
        if (filePathInput != null)
            filePathInput.interactable = !modelIsLoading;
        if (urlInput != null)
            urlInput.interactable = !modelIsLoading;

        if (modelIsLoading)
        {
            if (assignButton != null)
                assignButton.interactable = false;

            UpdateStatusText("The selected model is still loading...", Color.yellow);
            return;
        }

        bool canAssignSelectedFile = !string.IsNullOrWhiteSpace(selectedFilePath) &&
                                     Path.GetExtension(selectedFilePath).Equals(
                                         ".glb",
                                         System.StringComparison.OrdinalIgnoreCase);

#if !UNITY_WEBGL
        canAssignSelectedFile = canAssignSelectedFile && File.Exists(selectedFilePath);
#endif

        if (assignButton != null)
            assignButton.interactable = canAssignSelectedFile;

        UpdateStatusText(
            canAssignSelectedFile
                ? $"Ready: {Path.GetFileName(selectedFilePath)}"
                : "Current tour is still running. Select another .glb model.",
            canAssignSelectedFile ? Color.green : Color.white);

        Debug.Log("[ModelUploadUI] Upload page reopened without stopping the game");
    }
    
    /// <summary>
    /// Called when Browse button is clicked
    /// </summary>
    void OnBrowseClicked()
    {
        isLoadingFromURL = false;
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL: Use JavaScript file picker
        UploadFile(gameObject.name, "OnFileUploaded");
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        // Windows: Use native file dialog
        OpenWindowsFileDialog();
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        // Mac: Use native file dialog
        OpenMacFileDialog();
#elif UNITY_ANDROID
        // Android: Use native file picker
        OpenAndroidFilePicker();
#elif UNITY_IOS
        // iOS: Use native file picker
        OpenIOSFilePicker();
#else
        UpdateStatusText("File picker not supported on this platform", Color.red);
#endif
    }
    
    /// <summary>
    /// Called when Load from URL button is clicked
    /// </summary>
    void OnLoadFromURLClicked()
    {
        isLoadingFromURL = true;
        
        string url = urlInput != null ? urlInput.text.Trim() : "";
        
        if (string.IsNullOrEmpty(url))
        {
            UpdateStatusText("Error: Please enter a URL", Color.red);
            return;
        }
        
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            UpdateStatusText("Error: URL must start with http:// or https://", Color.red);
            return;
        }
        
        UpdateStatusText("Downloading model from URL...", Color.yellow);
        
        // Disable UI during loading
        if (assignButton != null)
            assignButton.interactable = false;
        if (browseButton != null)
            browseButton.interactable = false;
        if (loadFromURLButton != null)
            loadFromURLButton.interactable = false;
        if (urlInput != null)
            urlInput.interactable = false;
        
        // Load the model from URL
        if (modelLoader != null)
        {
            StartCoroutine(LoadModelFromURL(url));
        }
        else
        {
            Debug.LogError("[ModelUploadUI] RuntimeModelLoader not assigned!");
            UpdateStatusText("Error: Model loader not configured", Color.red);
        }
    }
    
    /// <summary>
    /// Download and load model from URL
    /// </summary>
    System.Collections.IEnumerator LoadModelFromURL(string url)
    {
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();
            
            if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ModelUploadUI] Failed to download from URL: {www.error}");
                UpdateStatusText($"Error: {www.error}", Color.red);
                
                // Re-enable UI
                if (browseButton != null)
                    browseButton.interactable = true;
                if (loadFromURLButton != null)
                    loadFromURLButton.interactable = true;
                if (urlInput != null)
                    urlInput.interactable = true;
                yield break;
            }
            
            byte[] modelData = www.downloadHandler.data;
            string fileName = Path.GetFileName(url);
            if (string.IsNullOrEmpty(fileName))
                fileName = "DownloadedModel";
            
            UpdateStatusText("Loading downloaded model...", Color.yellow);
            
            // Load model from bytes
            if (modelLoader != null)
            {
                modelLoader.LoadModelFromBytes(modelData, fileName);
                
                // Hide upload panel and start the game
                StartCoroutine(WaitForModelLoadAndStartGame());
            }
        }
    }
    
    /// <summary>
    /// Called when file path is manually entered
    /// </summary>
    void OnFilePathChanged(string path)
    {
        isLoadingFromURL = false;
        ValidateFilePath(path);
    }
    
    /// <summary>
    /// Validate the file path and enable/disable assign button
    /// </summary>
    void ValidateFilePath(string path)
    {
        selectedFilePath = path.Trim();
        
        if (string.IsNullOrEmpty(selectedFilePath))
        {
            UpdateStatusText("Please select a 3D model file", Color.white);
            if (assignButton != null)
                assignButton.interactable = false;
            return;
        }
        
        // Check file extension - support GLB only
        string extension = Path.GetExtension(selectedFilePath).ToLower();
        if (extension != ".glb")
        {
            UpdateStatusText("Error: File must be .glb format", Color.red);
            if (assignButton != null)
                assignButton.interactable = false;
            return;
        }
        
        // Check if file exists (for standalone platforms)
#if !UNITY_WEBGL
        if (!File.Exists(selectedFilePath))
        {
            UpdateStatusText("Error: File not found", Color.red);
            if (assignButton != null)
                assignButton.interactable = false;
            return;
        }
#endif
        
        // Valid file
        UpdateStatusText($"Ready: {Path.GetFileName(selectedFilePath)}", Color.green);
        if (assignButton != null)
            assignButton.interactable = true;
    }
    
    /// <summary>
    /// Called when Assign button is clicked
    /// </summary>
    void OnAssignClicked()
    {
        if (string.IsNullOrEmpty(selectedFilePath))
        {
            UpdateStatusText("Error: No file selected", Color.red);
            return;
        }
        
        UpdateStatusText("Loading model...", Color.yellow);
        
        // Disable UI during loading
        if (assignButton != null)
            assignButton.interactable = false;
        if (browseButton != null)
            browseButton.interactable = false;
        if (filePathInput != null)
            filePathInput.interactable = false;
        
        // Load the model
        if (modelLoader != null)
        {
            modelLoader.LoadModelFromFile(selectedFilePath);
            
            // Hide upload panel and start the game
            StartCoroutine(WaitForModelLoadAndStartGame());
        }
        else
        {
            Debug.LogError("[ModelUploadUI] RuntimeModelLoader not assigned!");
            UpdateStatusText("Error: Model loader not configured", Color.red);
        }
    }
    
    /// <summary>
    /// Wait for model to load, then hide upload panel and start the game
    /// </summary>
    System.Collections.IEnumerator WaitForModelLoadAndStartGame()
    {
        // Hide upload panel immediately so we can show the loading screen
        if (uploadPanel != null)
            uploadPanel.SetActive(false);

        BackToUploadButton.Instance?.SetVisible(false);
        ExitButton.Instance?.SetVisible(false);
            
        // Start the game with loading screen
        if (gameManager != null)
        {
            gameManager.StartGameAfterUpload();
        }
        
        // Wait a frame to let the loader start
        yield return null;
        
        // Wait for model to finish loading in background
        while (modelLoader != null && modelLoader.IsLoading && !modelLoader.LoadFailed)
        {
            yield return new WaitForSeconds(0.1f);
        }

        if (modelLoader != null && modelLoader.LoadFailed)
        {
            if (uploadPanel != null)
                uploadPanel.SetActive(true);

            UpdateStatusText(modelLoader.LastLoadError, Color.red);

            if (assignButton != null)
                assignButton.interactable = true;
            if (browseButton != null)
                browseButton.interactable = true;
            if (filePathInput != null)
                filePathInput.interactable = true;
            yield break;
        }

        BackToUploadButton.Instance?.SetVisible(true);
        ExitButton.Instance?.SetVisible(true);
        
        Debug.Log("[ModelUploadUI] Model loaded, game already starting via GameManager");
    }
    
    /// <summary>
    /// Update status text with color
    /// </summary>
    void UpdateStatusText(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }
        Debug.Log($"[ModelUploadUI] {message}");
    }
    
    // Platform-specific file dialog implementations
    
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    void OpenWindowsFileDialog()
    {
#if UNITY_EDITOR
        string filePath = UnityEditor.EditorUtility.OpenFilePanelWithFilters("Select a 3D Model File", "", 
            new string[] { "3D Models", "glb,gltf,fbx,obj", "All Files", "*" });
        if (!string.IsNullOrEmpty(filePath))
        {
            if (filePathInput != null)
                filePathInput.text = filePath;
            ValidateFilePath(filePath);
        }
#else
        UpdateStatusText("Info: Please enter file path manually in standalone", Color.yellow);
#endif
    }
#endif
    
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
    void OpenMacFileDialog()
    {
        // Use native Mac file dialog via AppleScript
        string[] paths = System.IO.Directory.GetFiles(Application.dataPath, "*.glb", SearchOption.AllDirectories);
        if (paths.Length > 0)
        {
            selectedFilePath = paths[0];
            if (filePathInput != null)
                filePathInput.text = selectedFilePath;
            ValidateFilePath(selectedFilePath);
        }
        else
        {
            UpdateStatusText("Info: Please enter file path manually", Color.yellow);
        }
    }
#endif
    
#if UNITY_ANDROID
    void OpenAndroidFilePicker()
    {
        // Android: Open file browser UI
        UpdateStatusText("Opening file browser...", Color.yellow);
        
        AndroidFilePicker.Instance.PickFile((filePath) =>
        {
            if (string.IsNullOrEmpty(filePath))
            {
                string pickerError = AndroidFilePicker.Instance != null ? AndroidFilePicker.Instance.LastError : null;
                UpdateStatusText(
                    string.IsNullOrEmpty(pickerError)
                        ? "No file selected. You can also use 'Load from URL'"
                        : pickerError,
                    string.IsNullOrEmpty(pickerError) ? Color.yellow : Color.red);
                return;
            }
            
            // File was selected
            if (filePathInput != null)
            {
                filePathInput.text = filePath;
            }
            
            UpdateStatusText($"Selected: {Path.GetFileName(filePath)}", Color.green);
            ValidateFilePath(filePath);
        });
    }
#endif
    
#if UNITY_IOS
    void OpenIOSFilePicker()
    {
        // iOS: Use native file picker via iOS Gallery
        try
        {
            UpdateStatusText("Opening file browser...", Color.yellow);
            
            // Use iOS native file picker
            // This is simplified - you may need to use a plugin for full iOS file browser support
            // For now, we'll guide the user to use URL or manual entry
            UpdateStatusText("Please select .glb file using file app or enter URL", Color.yellow);
            
            Debug.Log("[ModelUploadUI] iOS file picker - please select file manually");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ModelUploadUI] Error with iOS file picker: {ex.Message}");
            UpdateStatusText("Please enter file path or URL manually", Color.yellow);
        }
    }
#endif
    
#if UNITY_WEBGL && !UNITY_EDITOR
    /// <summary>
    /// Called from JavaScript when a file is uploaded (WebGL only)
    /// </summary>
    public void OnFileUploaded(string payload)
    {
        try
        {
            string fileName = "UploadedModel.glb";
            string base64Data = payload;

            int separatorIndex = payload.IndexOf('|');
            if (separatorIndex > 0)
            {
                fileName = payload.Substring(0, separatorIndex);
                base64Data = payload.Substring(separatorIndex + 1);
            }

            byte[] fileData = System.Convert.FromBase64String(base64Data);
            
            // Load model from bytes
            if (modelLoader != null)
            {
                modelLoader.LoadModelFromBytes(fileData, fileName);
                
                // Hide upload panel and start game
                StartCoroutine(WaitForModelLoadAndStartGame());
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ModelUploadUI] Error loading uploaded file: {e.Message}");
            UpdateStatusText("Error: Failed to load file", Color.red);
        }
    }
#endif
}
