using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor tool to automatically create the Model Upload UI
/// </summary>
public class ModelUploadUICreator : EditorWindow
{
    [MenuItem("Tools/Create Model Upload UI")]
    public static void ShowWindow()
    {
        GetWindow<ModelUploadUICreator>("Model Upload UI Creator");
    }

    private Canvas targetCanvas;
    private GameObject uploadPanelPrefab;

    void OnGUI()
    {
        GUILayout.Label("Model Upload UI Creator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "This tool will automatically create the Model Upload UI in your Canvas.\n\n" +
            "It will create:\n" +
            "• Upload Panel\n" +
            "• Title Text\n" +
            "• File Path Input Field\n" +
            "• Browse Button\n" +
            "• Assign Button\n" +
            "• Status Text\n" +
            "• ModelUploadManager GameObject with script",
            MessageType.Info
        );

        GUILayout.Space(10);

        targetCanvas = (Canvas)EditorGUILayout.ObjectField("Target Canvas", targetCanvas, typeof(Canvas), true);

        GUILayout.Space(10);

        EditorGUI.BeginDisabledGroup(targetCanvas == null);
        
        if (GUILayout.Button("Create Upload UI", GUILayout.Height(40)))
        {
            CreateUploadUI();
        }
        
        EditorGUI.EndDisabledGroup();

        if (targetCanvas == null)
        {
            EditorGUILayout.HelpBox("Please assign a Canvas to create the UI in.", MessageType.Warning);
        }
    }

    void CreateUploadUI()
    {
        if (targetCanvas == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign a Canvas first!", "OK");
            return;
        }

        // Create Upload Panel
        GameObject uploadPanel = CreateUploadPanel(targetCanvas.transform);
        
        // Create UI elements
        GameObject titleText = CreateTitleText(uploadPanel.transform);
        GameObject filePathInput = CreateFilePathInput(uploadPanel.transform);
        GameObject browseButton = CreateBrowseButton(uploadPanel.transform);
        GameObject assignButton = CreateAssignButton(uploadPanel.transform);
        GameObject statusText = CreateStatusText(uploadPanel.transform);
        
        // Create ModelUploadManager
        GameObject manager = new GameObject("ModelUploadManager");
        ModelUploadUI uploadUI = manager.AddComponent<ModelUploadUI>();
        
        // Assign references
        uploadUI.uploadPanel = uploadPanel;
        uploadUI.assignButton = assignButton.GetComponent<Button>();
        uploadUI.filePathInput = filePathInput.GetComponent<TMP_InputField>();
        uploadUI.statusText = statusText.GetComponent<TextMeshProUGUI>();
        uploadUI.browseButton = browseButton.GetComponent<Button>();
        
        // Try to find and assign RuntimeModelLoader and GameManager
        RuntimeModelLoader loader = FindAnyObjectByType<RuntimeModelLoader>();
        if (loader != null)
            uploadUI.modelLoader = loader;
            
        GameManager gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager != null)
            uploadUI.gameManager = gameManager;
        
        // Mark objects dirty for saving
        EditorUtility.SetDirty(uploadPanel);
        EditorUtility.SetDirty(manager);
        
        // Select the manager in hierarchy
        Selection.activeGameObject = manager;
        
        EditorUtility.DisplayDialog(
            "Success!",
            "Model Upload UI created successfully!\n\n" +
            "Next steps:\n" +
            "1. Check ModelUploadManager references\n" +
            "2. Assign RuntimeModelLoader if not auto-found\n" +
            "3. Assign GameManager if not auto-found\n" +
            "4. Test in Play mode!",
            "OK"
        );
        
        Debug.Log("[ModelUploadUICreator] ✓ Upload UI created successfully!");
    }

    GameObject CreateUploadPanel(Transform parent)
    {
        GameObject panel = new GameObject("UploadPanel");
        panel.transform.SetParent(parent, false);
        
        // Add Image component for background
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.8f); // Semi-transparent black
        
        // Set to full screen
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        
        return panel;
    }

    GameObject CreateTitleText(Transform parent)
    {
        GameObject title = new GameObject("TitleText");
        title.transform.SetParent(parent, false);
        
        TextMeshProUGUI text = title.AddComponent<TextMeshProUGUI>();
        text.text = "Upload 3D Model";
        text.fontSize = 48;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        
        RectTransform rt = title.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(600, 100);
        rt.anchoredPosition = new Vector2(0, -150);
        
        return title;
    }

    GameObject CreateFilePathInput(Transform parent)
    {
        // Create from TextMeshPro prefab
        GameObject inputField = new GameObject("FilePathInput");
        inputField.transform.SetParent(parent, false);
        
        // Add Image background
        Image bg = inputField.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.1f);
        
        TMP_InputField input = inputField.AddComponent<TMP_InputField>();
        
        // Create Text Area
        GameObject textArea = new GameObject("Text Area");
        textArea.transform.SetParent(inputField.transform, false);
        RectTransform textAreaRT = textArea.AddComponent<RectTransform>();
        textAreaRT.anchorMin = Vector2.zero;
        textAreaRT.anchorMax = Vector2.one;
        textAreaRT.sizeDelta = Vector2.zero;
        textAreaRT.offsetMin = new Vector2(10, 6);
        textAreaRT.offsetMax = new Vector2(-10, -7);
        textArea.AddComponent<RectMask2D>();
        
        // Create Placeholder
        GameObject placeholder = new GameObject("Placeholder");
        placeholder.transform.SetParent(textArea.transform, false);
        TextMeshProUGUI placeholderText = placeholder.AddComponent<TextMeshProUGUI>();
        placeholderText.text = "Enter file path or use Browse button...";
        placeholderText.fontSize = 20;
        placeholderText.color = new Color(1f, 1f, 1f, 0.5f);
        placeholderText.fontStyle = FontStyles.Italic;
        RectTransform placeholderRT = placeholder.GetComponent<RectTransform>();
        placeholderRT.anchorMin = Vector2.zero;
        placeholderRT.anchorMax = Vector2.one;
        placeholderRT.sizeDelta = Vector2.zero;
        
        // Create Text
        GameObject text = new GameObject("Text");
        text.transform.SetParent(textArea.transform, false);
        TextMeshProUGUI textComponent = text.AddComponent<TextMeshProUGUI>();
        textComponent.fontSize = 20;
        textComponent.color = Color.white;
        RectTransform textRT = text.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;
        
        // Configure input field
        input.textViewport = textAreaRT;
        input.textComponent = textComponent;
        input.placeholder = placeholderText;
        
        // Position
        RectTransform rt = inputField.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(700, 60);
        rt.anchoredPosition = new Vector2(0, 50);
        
        return inputField;
    }

    GameObject CreateBrowseButton(Transform parent)
    {
        return CreateButton(parent, "BrowseButton", "Browse...", 
                          new Vector2(200, 60), new Vector2(0, -50),
                          new Color(0.2f, 0.6f, 1f));
    }

    GameObject CreateAssignButton(Transform parent)
    {
        return CreateButton(parent, "AssignButton", "Assign", 
                          new Vector2(300, 80), new Vector2(0, -150),
                          new Color(0.2f, 0.8f, 0.2f));
    }

    GameObject CreateButton(Transform parent, string name, string text, Vector2 size, Vector2 position, Color color)
    {
        GameObject button = new GameObject(name);
        button.transform.SetParent(parent, false);
        
        // Add Image
        Image image = button.AddComponent<Image>();
        image.color = color;
        
        // Add Button
        Button btn = button.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.2f;
        colors.pressedColor = color * 0.8f;
        btn.colors = colors;
        
        // Create Text child
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(button.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        
        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;
        
        // Position button
        RectTransform rt = button.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
        
        return button;
    }

    GameObject CreateStatusText(Transform parent)
    {
        GameObject status = new GameObject("StatusText");
        status.transform.SetParent(parent, false);
        
        TextMeshProUGUI text = status.AddComponent<TextMeshProUGUI>();
        text.text = "Please select a .glb file to begin";
        text.fontSize = 20;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        
        RectTransform rt = status.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(700, 50);
        rt.anchoredPosition = new Vector2(0, 100);
        
        return status;
    }
}
