using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Android file picker using Android's system document picker.
/// Pure C# implementation: no Java source files or custom activity override.
/// </summary>
public class AndroidFilePicker : MonoBehaviour
{
    private static AndroidFilePicker instance;
    private Action<string> onFileSelectedCallback;
    private SimpleFileBrowser fileBrowser;

    public string LastError { get; private set; }

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject currentActivity;
    private AndroidJavaObject activityResultLauncher;
    private PickerResultCallback pickerResultCallback;
    private readonly Queue<Action> mainThreadActions = new Queue<Action>();
    private readonly object mainThreadLock = new object();
#endif
    
    public static AndroidFilePicker Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("AndroidFilePicker");
                instance = go.AddComponent<AndroidFilePicker>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }
    
    void Awake()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Initialized lazily from Android's UI thread when Browse is tapped.
#else
        // Find or create file browser
        fileBrowser = FindObjectOfType<SimpleFileBrowser>();
        
        if (fileBrowser == null)
        {
            Debug.LogWarning("[AndroidFilePicker] SimpleFileBrowser not found in scene. Creating runtime browser...");
            CreateRuntimeBrowser();
        }
#endif
    }

    void Update()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Action action = null;
        lock (mainThreadLock)
        {
            if (mainThreadActions.Count > 0)
                action = mainThreadActions.Dequeue();
        }

        action?.Invoke();
#endif
    }
    
    /// <summary>
    /// Open file picker
    /// </summary>
    public void PickFile(Action<string> onFileSelected)
    {
        onFileSelectedCallback = onFileSelected;
        LastError = null;
        
#if UNITY_ANDROID && !UNITY_EDITOR
        if (OpenSystemDocumentPicker())
            return;

        Debug.LogWarning("[AndroidFilePicker] Android system picker is unavailable. Falling back to legacy browser.");
#endif
        
        // Open legacy in-app file browser on non-Android platforms, or as a last-resort fallback.
        if (fileBrowser != null)
        {
            fileBrowser.OpenBrowser(OnFileBrowserResult);
        }
        else
        {
            string foundFile = FindGLBInCommonLocations();
            onFileSelectedCallback?.Invoke(foundFile);
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private bool TryInitializeSystemPicker()
    {
        if (activityResultLauncher != null)
            return true;

        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            }

            if (currentActivity == null)
            {
                Debug.LogError("[AndroidFilePicker] Unity currentActivity is null");
                return false;
            }

            AndroidJavaObject registry = currentActivity.Call<AndroidJavaObject>("getActivityResultRegistry");
            if (registry == null)
            {
                Debug.LogError("[AndroidFilePicker] Current activity does not expose ActivityResultRegistry");
                return false;
            }

            var contract = new AndroidJavaObject("androidx.activity.result.contract.ActivityResultContracts$OpenDocument");
            pickerResultCallback = new PickerResultCallback(this);
            string key = $"unity_glb_picker_{GetInstanceID()}";

            activityResultLauncher = registry.Call<AndroidJavaObject>(
                "register",
                key,
                contract,
                pickerResultCallback);

            Debug.Log("[AndroidFilePicker] Android system document picker initialized");
            return activityResultLauncher != null;
        }
        catch (Exception e)
        {
            Debug.LogError($"[AndroidFilePicker] Failed to initialize Android system picker: {e}");
            activityResultLauncher = null;
            return false;
        }
    }

    private bool OpenSystemDocumentPicker()
    {
        try
        {
            EnsureCurrentActivity();

            currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                try
                {
                    if (!TryInitializeSystemPicker())
                    {
                        EnqueueOnMainThread(() => onFileSelectedCallback?.Invoke(null));
                        return;
                    }

                    string[] mimeTypes =
                    {
                        "model/gltf-binary",
                        "application/octet-stream",
                        "*/*"
                    };

                    activityResultLauncher.Call("launch", new object[] { mimeTypes });
                }
                catch (Exception e)
                {
                    Debug.LogError($"[AndroidFilePicker] Failed to launch Android system picker: {e}");
                    EnqueueOnMainThread(() => onFileSelectedCallback?.Invoke(null));
                }
            }));

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[AndroidFilePicker] Could not open Android system picker: {e}");
            return false;
        }
    }

    private void EnsureCurrentActivity()
    {
        if (currentActivity != null)
            return;

        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        }

        if (currentActivity == null)
            throw new InvalidOperationException("Unity currentActivity is null");
    }

    private void HandleSystemPickerResult(AndroidJavaObject uri)
    {
        EnqueueOnMainThread(() =>
        {
            if (uri == null)
            {
                Debug.Log("[AndroidFilePicker] Android picker cancelled");
                LastError = null;
                onFileSelectedCallback?.Invoke(null);
                return;
            }

            try
            {
                string cachedPath = CopyUriToCache(uri);
                onFileSelectedCallback?.Invoke(cachedPath);
            }
            catch (Exception e)
            {
                LastError = e.Message;
                Debug.LogError($"[AndroidFilePicker] Failed to copy selected file: {e}");
                onFileSelectedCallback?.Invoke(null);
            }
        });
    }

    private string CopyUriToCache(AndroidJavaObject uri)
    {
        string fileName = GetDisplayName(uri);
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "SelectedModel.glb";

        if (!fileName.EndsWith(".glb", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Selected file must be a .glb model.");

        string destinationPath = Path.Combine(Application.temporaryCachePath, SanitizeFileName(fileName));
        long totalBytes;

        try
        {
            // Do the stream copy in Java. Passing byte[] to InputStream.read through
            // AndroidJavaObject creates a JNI array copy whose changed contents are not
            // copied back to the managed buffer, which previously produced an all-zero
            // cache file and made every valid GLB fail its magic-byte check.
            using (var fileHelper = new AndroidJavaClass("com.mobileapp.filepicker.GlbFileHelper"))
            {
                string uriString = uri.Call<string>("toString");
                totalBytes = fileHelper.CallStatic<long>(
                    "copyUriToFile",
                    currentActivity,
                    uriString,
                    destinationPath);
            }
        }
        catch
        {
            // Never leave a partial model in the cache after a provider or storage error.
            try
            {
                if (File.Exists(destinationPath))
                    File.Delete(destinationPath);
            }
            catch (Exception cleanupError)
            {
                Debug.LogWarning($"[AndroidFilePicker] Could not delete partial cache file: {cleanupError.Message}");
            }

            throw;
        }

        ValidateGlbFile(destinationPath, totalBytes);

        Debug.Log($"[AndroidFilePicker] Copied selected model to: {destinationPath} ({totalBytes} bytes)");
        return destinationPath;
    }

    private static void ValidateGlbFile(string filePath, long copiedBytes)
    {
        if (copiedBytes < 20)
            throw new InvalidOperationException("Selected .glb file is empty or incomplete.");

        byte[] header = new byte[12];
        using (FileStream stream = File.OpenRead(filePath))
        {
            int read = stream.Read(header, 0, header.Length);
            if (read < header.Length)
                throw new InvalidOperationException("Selected .glb file header is incomplete.");
        }

        bool hasGlbMagic = header[0] == (byte)'g' &&
                           header[1] == (byte)'l' &&
                           header[2] == (byte)'T' &&
                           header[3] == (byte)'F';

        if (!hasGlbMagic)
            throw new InvalidOperationException("Selected file has .glb extension but is not a valid binary GLB file.");

        uint version = BitConverter.ToUInt32(header, 4);
        if (version != 2)
            throw new InvalidOperationException($"Unsupported GLB version {version}. Please export the model as GLB 2.0.");

        uint declaredLength = BitConverter.ToUInt32(header, 8);
        if (declaredLength != copiedBytes)
        {
            Debug.LogWarning($"[AndroidFilePicker] GLB declared length ({declaredLength}) differs from copied bytes ({copiedBytes}).");
        }
    }

    private string GetDisplayName(AndroidJavaObject uri)
    {
        AndroidJavaObject cursor = null;
        try
        {
            using (AndroidJavaObject resolver = currentActivity.Call<AndroidJavaObject>("getContentResolver"))
            {
                cursor = resolver.Call<AndroidJavaObject>("query", uri, null, null, null, null);
            }

            if (cursor != null && cursor.Call<bool>("moveToFirst"))
            {
                int nameIndex = cursor.Call<int>("getColumnIndex", "_display_name");
                if (nameIndex >= 0)
                    return cursor.Call<string>("getString", nameIndex);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AndroidFilePicker] Could not read selected file display name: {e.Message}");
        }
        finally
        {
            cursor?.Call("close");
            cursor?.Dispose();
        }

        try
        {
            string lastPathSegment = uri.Call<string>("getLastPathSegment");
            return Path.GetFileName(lastPathSegment);
        }
        catch
        {
            return null;
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            fileName = fileName.Replace(invalid, '_');

        return fileName;
    }

    private void EnqueueOnMainThread(Action action)
    {
        lock (mainThreadLock)
        {
            mainThreadActions.Enqueue(action);
        }
    }

    private class PickerResultCallback : AndroidJavaProxy
    {
        private readonly AndroidFilePicker owner;

        public PickerResultCallback(AndroidFilePicker owner)
            : base("androidx.activity.result.ActivityResultCallback")
        {
            this.owner = owner;
        }

        public void onActivityResult(AndroidJavaObject result)
        {
            owner.HandleSystemPickerResult(result);
        }
    }
#endif

    /// <summary>
    /// Handle file browser result
    /// </summary>
    void OnFileBrowserResult(string filePath)
    {
        onFileSelectedCallback?.Invoke(filePath);
    }
    
    /// <summary>
    /// Search for .glb files in common download locations (fallback)
    /// </summary>
    private string FindGLBInCommonLocations()
    {
        string[] commonPaths = new string[]
        {
            "/sdcard/Download",
            "/storage/emulated/0/Download",
            "/sdcard/Downloads",
            "/storage/emulated/0/Downloads",
            "/storage/emulated/0/Documents",
            "/sdcard/Documents"
        };
        
        foreach (string basePath in commonPaths)
        {
            if (Directory.Exists(basePath))
            {
                try
                {
                    string[] glbFiles = Directory.GetFiles(basePath, "*.glb", SearchOption.TopDirectoryOnly);
                    if (glbFiles.Length > 0)
                    {
                        Debug.Log($"[AndroidFilePicker] Found .glb file: {glbFiles[0]}");
                        return glbFiles[0];
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[AndroidFilePicker] Could not read {basePath}: {e.Message}");
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Create runtime file browser UI
    /// </summary>
    void CreateRuntimeBrowser()
    {
        // Create canvas if needed
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("FileBrowserCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        
        // Create browser UI
        GameObject browserObj = new GameObject("SimpleFileBrowser");
        browserObj.transform.SetParent(canvas.transform, false);
        
        fileBrowser = browserObj.AddComponent<SimpleFileBrowser>();
        
        // Create browser panel
        GameObject panel = new GameObject("BrowserPanel");
        panel.transform.SetParent(browserObj.transform, false);
        
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        UnityEngine.UI.Image panelImage = panel.AddComponent<UnityEngine.UI.Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        
        fileBrowser.browserPanel = panel;
        
        // Create scroll view for file list
        GameObject scrollView = new GameObject("ScrollView");
        scrollView.transform.SetParent(panel.transform, false);
        
        RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.05f, 0.15f);
        scrollRect.anchorMax = new Vector2(0.95f, 0.85f);
        scrollRect.offsetMin = Vector2.zero;
        scrollRect.offsetMax = Vector2.zero;
        
        UnityEngine.UI.ScrollRect scroll = scrollView.AddComponent<UnityEngine.UI.ScrollRect>();
        
        // Create viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform, false);
        
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        
        viewport.AddComponent<UnityEngine.UI.Image>();
        viewport.AddComponent<UnityEngine.UI.Mask>();
        
        // Create content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 500);
        
        UnityEngine.UI.VerticalLayoutGroup layout = content.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        
        UnityEngine.UI.ContentSizeFitter fitter = content.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        
        scroll.content = contentRect;
        scroll.viewport = viewportRect;
        
        fileBrowser.fileListContent = content.transform;
        
        Debug.Log("[AndroidFilePicker] Runtime file browser created");
    }
}
