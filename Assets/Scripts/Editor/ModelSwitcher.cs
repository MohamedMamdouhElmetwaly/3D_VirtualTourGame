using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

/// <summary>
/// Unity Editor tool to easily switch between different apartment models in the scene.
/// Accessible via Tools menu.
/// </summary>
public class ModelSwitcher : EditorWindow
{
    private static string[] modelPaths = new string[]
    {
        "Assets/Model/twokinds_modern_trio_apartment.glb",
        "Assets/Model/studio_apartment_vray_baked_textures_included.glb",
        "Assets/Model/styl_3.glb"
    };

    private static string[] modelNames = new string[]
    {
        "Twokinds Modern Trio Apartment",
        "Studio Apartment (Vray)",
        "Styl 3"
    };

    private int selectedModelIndex = 0;
    private Vector2 scrollPosition;

    [MenuItem("Tools/Model Switcher")]
    public static void ShowWindow()
    {
        GetWindow<ModelSwitcher>("Model Switcher");
    }

    private void OnGUI()
    {
        GUILayout.Label("Apartment Model Switcher", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox("This tool will replace the current model in the scene with your selected model. The ModelScalerAndSetup script will automatically configure it.", MessageType.Info);
        EditorGUILayout.Space();

        // Model selection
        selectedModelIndex = EditorGUILayout.Popup("Select Model:", selectedModelIndex, modelNames);
        EditorGUILayout.Space();

        // Show current model info
        GameObject currentModel = FindCurrentModel();
        if (currentModel != null)
        {
            EditorGUILayout.LabelField("Current Model:", currentModel.name, EditorStyles.helpBox);
        }
        else
        {
            EditorGUILayout.LabelField("Current Model:", "None found", EditorStyles.helpBox);
        }
        EditorGUILayout.Space();

        // Switch button
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Switch to Selected Model", GUILayout.Height(40)))
        {
            SwitchModel(modelPaths[selectedModelIndex]);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        // Quick access buttons
        GUILayout.Label("Quick Access:", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        for (int i = 0; i < modelNames.Length; i++)
        {
            if (GUILayout.Button(modelNames[i], GUILayout.Height(30)))
            {
                SwitchModel(modelPaths[i]);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private GameObject FindCurrentModel()
    {
        // Look for common model names
        var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        
        foreach (var root in rootObjects)
        {
            string nameLower = root.name.ToLower();
            if (nameLower.Contains("apartment") || nameLower.Contains("model") || 
                nameLower.Contains("building") || nameLower.Contains("house") ||
                nameLower.Contains("twokinds") || nameLower.Contains("studio") ||
                nameLower.Contains("styl"))
            {
                return root;
            }
        }

        return null;
    }

    private void SwitchModel(string modelPath)
    {
        if (!EditorUtility.DisplayDialog("Confirm Model Switch", 
            $"This will remove the current model and load:\n\n{System.IO.Path.GetFileNameWithoutExtension(modelPath)}\n\nContinue?", 
            "Yes", "Cancel"))
        {
            return;
        }

        // Remove current model
        GameObject currentModel = FindCurrentModel();
        if (currentModel != null)
        {
            Debug.Log($"[ModelSwitcher] Removing current model: {currentModel.name}");
            DestroyImmediate(currentModel);
        }

        // Load new model
        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (modelPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", $"Could not load model at:\n{modelPath}", "OK");
            return;
        }

        // Instantiate in scene
        GameObject newModel = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
        newModel.name = System.IO.Path.GetFileNameWithoutExtension(modelPath);
        
        // Position at origin
        newModel.transform.position = Vector3.zero;
        newModel.transform.rotation = Quaternion.identity;
        newModel.transform.localScale = Vector3.one;

        // Select the new model
        Selection.activeGameObject = newModel;

        // Find and trigger ModelScalerAndSetup if it exists
        var scaler = FindAnyObjectByType<ModelScalerAndSetup>();
        if (scaler != null)
        {
            scaler.FindModelRoot();
            scaler.SetupModelAndCamera();
            Debug.Log($"[ModelSwitcher] Successfully switched to: {newModel.name} and configured it.");
        }
        else
        {
            Debug.LogWarning("[ModelSwitcher] ModelScalerAndSetup not found in scene. Model added but not configured.");
        }

        // Mark scene as dirty
        EditorUtility.SetDirty(newModel);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Success", $"Model switched to:\n{newModel.name}\n\nDon't forget to save the scene!", "OK");
    }

    [MenuItem("Tools/Load Twokinds Apartment (Quick)")]
    public static void QuickLoadTwokinds()
    {
        LoadModelQuick(modelPaths[0], modelNames[0]);
    }

    [MenuItem("Tools/Load Studio Apartment (Quick)")]
    public static void QuickLoadStudio()
    {
        LoadModelQuick(modelPaths[1], modelNames[1]);
    }

    private static void LoadModelQuick(string modelPath, string modelName)
    {
        // Remove current model
        var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in rootObjects)
        {
            string nameLower = root.name.ToLower();
            if (nameLower.Contains("apartment") || nameLower.Contains("twokinds") || 
                nameLower.Contains("studio") || nameLower.Contains("styl"))
            {
                DestroyImmediate(root);
                break;
            }
        }

        // Load new model
        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (modelPrefab != null)
        {
            GameObject newModel = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
            newModel.name = System.IO.Path.GetFileNameWithoutExtension(modelPath);
            newModel.transform.position = Vector3.zero;
            newModel.transform.rotation = Quaternion.identity;
            newModel.transform.localScale = Vector3.one;

            // Trigger setup
            var scaler = FindAnyObjectByType<ModelScalerAndSetup>();
            if (scaler != null)
            {
                scaler.FindModelRoot();
                scaler.SetupModelAndCamera();
            }

            Selection.activeGameObject = newModel;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            
            Debug.Log($"[ModelSwitcher] Loaded: {modelName}");
        }
        else
        {
            Debug.LogError($"[ModelSwitcher] Could not load model at: {modelPath}");
        }
    }
}
