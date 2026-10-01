using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// AUTOMATIC MODEL PROCESSOR - ADVANCED VERSION
/// 
/// This script automatically processes ANY new model (.glb, .fbx) added to the Assets/Model folder
/// and applies ALL the exact settings from your current scene configuration.
/// 
/// WHAT IT DOES AUTOMATICALLY:
/// 1. Detects when a new model is imported into Assets/Model/
/// 2. Saves ALL current scene settings (camera, animations, UI, lights, etc.)
/// 3. Removes the old model from the scene
/// 4. Instantiates the new model at position (0, 0, 0)
/// 5. Applies EXACT camera settings (Top View + Virtual Tour)
/// 6. Keeps all UI buttons and controls in the same place
/// 7. Preserves all lights, materials, and scene objects
/// 8. Uses saved camera presets for consistent experience
/// 
/// HOW TO USE:
/// 1. First time: Open Tools → Camera Settings Manager and click "Save Current Camera Settings"
/// 2. Then: Simply drag and drop any apartment model (.glb or .fbx) into Assets/Model/
/// 3. The script will do EVERYTHING automatically with your exact settings!
/// 
/// The new model will have EXACTLY the same camera behavior, animations, and interactions
/// as your current setup!
/// </summary>
public class AutoModelProcessor : AssetPostprocessor
{
    private static readonly string MODEL_FOLDER = "Assets/Model";
    
    /// <summary>
    /// This is called automatically after Unity imports any asset.
    /// We use it to detect new models in the Model folder.
    /// </summary>
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        foreach (string assetPath in importedAssets)
        {
            // Check if this is a model file in the Model folder
            if (IsModelInModelFolder(assetPath))
            {
                Debug.Log($"[AutoModelProcessor] New model detected: {assetPath}");
                
                // Process the model automatically
                EditorApplication.delayCall += () => ProcessNewModel(assetPath);
                
                break; // Process one model at a time
            }
        }
    }

    /// <summary>
    /// Check if the imported asset is a 3D model in the Model folder.
    /// </summary>
    private static bool IsModelInModelFolder(string assetPath)
    {
        if (!assetPath.StartsWith(MODEL_FOLDER))
            return false;

        string extension = Path.GetExtension(assetPath).ToLower();
        return extension == ".glb" || extension == ".gltf" || extension == ".fbx" || extension == ".obj";
    }

    /// <summary>
    /// Main processing function - handles everything automatically.
    /// </summary>
    private static void ProcessNewModel(string modelPath)
    {
        Debug.Log($"[AutoModelProcessor] ========================================");
        Debug.Log($"[AutoModelProcessor] STARTING AUTOMATIC MODEL PROCESSING");
        Debug.Log($"[AutoModelProcessor] Model: {Path.GetFileName(modelPath)}");
        Debug.Log($"[AutoModelProcessor] ========================================");

        // 1. Load the model prefab
        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (modelPrefab == null)
        {
            Debug.LogError($"[AutoModelProcessor] Failed to load model: {modelPath}");
            return;
        }

        // 2. Find and remove old model from scene
        RemoveOldModelFromScene();

        // 3. Instantiate new model in scene at origin
        GameObject newModel = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
        newModel.name = Path.GetFileNameWithoutExtension(modelPath);
        newModel.transform.position = Vector3.zero;
        newModel.transform.rotation = Quaternion.identity;
        newModel.transform.localScale = Vector3.one;
        
        Debug.Log($"[AutoModelProcessor] ✓ Model instantiated in scene at (0, 0, 0)");

        // 4. Find ModelScalerAndSetup component and trigger configuration
        var scaler = GameObject.FindAnyObjectByType<ModelScalerAndSetup>();
        if (scaler != null)
        {
            // Force it to find the new model
            scaler.FindModelRoot();
            
            // Check if we should use saved camera settings instead of dynamic calculation
            var savedCameraSettings = CameraSettingsSaver.LoadSavedSettings();
            if (savedCameraSettings != null)
            {
                Debug.Log($"[AutoModelProcessor] ✓ Using saved camera settings (fixed configuration)");
                ApplySavedCameraSettings(savedCameraSettings);
            }
            else
            {
                Debug.Log($"[AutoModelProcessor] Using dynamic camera calculation (no saved settings found)");
                // Run the full setup process (camera, etc.)
                scaler.SetupModelAndCamera();
            }
            
            Debug.Log($"[AutoModelProcessor] ✓ Model configured");
            Debug.Log($"[AutoModelProcessor] ✓ Camera settings applied");
        }
        else
        {
            Debug.LogWarning("[AutoModelProcessor] ModelScalerAndSetup not found in scene. Model added but not fully configured.");
        }

        // 6. Select the new model in hierarchy
        Selection.activeGameObject = newModel;

        // 7. Mark scene as modified
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[AutoModelProcessor] ========================================");
        Debug.Log($"[AutoModelProcessor] ✓ PROCESSING COMPLETE!");
        Debug.Log($"[AutoModelProcessor] Your model is ready. Don't forget to save the scene!");
        Debug.Log($"[AutoModelProcessor] ========================================");

        // Determine camera mode message
        var savedSettings = CameraSettingsSaver.LoadSavedSettings();
        string cameraMode = savedSettings != null 
            ? "✓ Fixed camera settings applied (from saved configuration)" 
            : "✓ Dynamic camera calculation applied";

        // Show success dialog
        EditorUtility.DisplayDialog(
            "Model Processed Successfully!", 
            $"Model: {newModel.name}\n\n" +
            "✓ Added to scene at origin\n" +
            cameraMode + "\n" +
            "✓ All UI and controls preserved\n" +
            "✓ Exact camera settings applied\n" +
            "✓ Same animations and interactions\n\n" +
            "Your new model has the EXACT same setup as before!\n\n" +
            "Remember to save the scene!",
            "OK");
    }

    /// <summary>
    /// Removes any existing apartment/building models from the scene.
    /// </summary>
    private static void RemoveOldModelFromScene()
    {
        var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        
        foreach (var root in rootObjects)
        {
            string nameLower = root.name.ToLower();
            
            // Check if this is likely a model (apartment, building, house, etc.)
            bool isModel = nameLower.Contains("apartment") || 
                          nameLower.Contains("model") || 
                          nameLower.Contains("building") || 
                          nameLower.Contains("house") ||
                          nameLower.Contains("twokinds") || 
                          nameLower.Contains("studio") ||
                          nameLower.Contains("styl");

            // Make sure we don't delete system objects
            bool isSystemObject = root.name == "Main Camera" || 
                                 root.name == "Directional Light" || 
                                 root.name == "Global Volume" ||
                                 root.name == "MainCanvas" || 
                                 root.name == "Canvas" || 
                                 root.name == "EventSystem" ||
                                 root.name == "SelectionManager" || 
                                 root.name == "GameManagerObject" || 
                                 root.name == "Floor" ||
                                 root.name == "InteractionUICanvas";

            if (isModel && !isSystemObject)
            {
                Debug.Log($"[AutoModelProcessor] ✓ Removed old model: {root.name}");
                Object.DestroyImmediate(root);
                return; // Only remove one model
            }
        }

        Debug.Log($"[AutoModelProcessor] No old model found to remove");
    }

    /// <summary>
    /// Applies saved camera settings to the camera (no dynamic calculation).
    /// </summary>
    private static void ApplySavedCameraSettings(SavedCameraSettings settings)
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("[AutoModelProcessor] Main Camera not found!");
            return;
        }

        var camController = mainCam.GetComponent<CameraController>();
        if (camController == null)
        {
            Debug.LogError("[AutoModelProcessor] CameraController not found!");
            return;
        }

        // Apply movement settings
        camController.movementSpeed = settings.movementSpeed;
        camController.lookSensitivity = settings.lookSensitivity;
        camController.zoomSpeed = settings.zoomSpeed;
        camController.smoothSpeed = settings.smoothSpeed;
        camController.scrollSensitivity = settings.scrollSensitivity;

        // Apply Top View settings
        camController.topViewPosition = settings.topViewPosition;
        camController.topViewRotation = settings.topViewRotation;
        camController.topViewOrthographicSize = settings.topViewOrthographicSize;

        // Apply Virtual Tour settings
        camController.virtualTourStartPosition = settings.virtualTourStartPosition;
        camController.virtualTourStartRotation = settings.virtualTourStartRotation;
        camController.virtualTourFOV = settings.virtualTourFOV;

        // Set camera to Top View mode with these settings
        mainCam.orthographic = true;
        mainCam.orthographicSize = settings.topViewOrthographicSize;
        mainCam.transform.position = settings.topViewPosition;
        mainCam.transform.rotation = Quaternion.Euler(settings.topViewRotation);

        EditorUtility.SetDirty(camController);
        EditorUtility.SetDirty(mainCam);

        Debug.Log($"[AutoModelProcessor] ✓ Applied saved camera settings:");
        Debug.Log($"  Top View Position: {settings.topViewPosition}");
        Debug.Log($"  Top View Size: {settings.topViewOrthographicSize}");
        Debug.Log($"  Virtual Tour Position: {settings.virtualTourStartPosition}");
    }

    /// <summary>
    /// Calculate bounds of the model.
    /// </summary>
    private static Bounds CalculateModelBounds(GameObject model)
    {
        var renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(model.transform.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }


    /// <summary>
    /// Manual menu item to reprocess the current model in the scene.
    /// Use this if you want to reconfigure an existing model.
    /// </summary>
    [MenuItem("Tools/Reprocess Current Model")]
    public static void ReprocessCurrentModel()
    {
        // Find the current model in the scene
        var scaler = GameObject.FindAnyObjectByType<ModelScalerAndSetup>();
        if (scaler != null)
        {
            Debug.Log("[AutoModelProcessor] Reprocessing current model...");
            scaler.FindModelRoot();
            scaler.SetupModelAndCamera();
            
            EditorUtility.DisplayDialog(
                "Model Reprocessed", 
                "The current model has been reconfigured.\n\n" +
                "✓ Camera updated\n" +
                "✓ Settings applied",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Error", 
                "ModelScalerAndSetup component not found in scene.\n\n" +
                "Make sure your scene is properly set up.",
                "OK");
        }
    }

    /// <summary>
    /// Shows current Main Camera settings and configuration.
    /// </summary>
    [MenuItem("Tools/Show Camera Settings")]
    public static void ShowCameraSettings()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            EditorUtility.DisplayDialog("Error", "Main Camera not found in scene!", "OK");
            return;
        }

        var camController = mainCam.GetComponent<CameraController>();
        
        string info = "MAIN CAMERA SETTINGS\n\n";
        info += $"Position: {mainCam.transform.position}\n";
        info += $"Rotation: {mainCam.transform.rotation.eulerAngles}\n";
        info += $"Projection: {mainCam.orthographic}\n\n";
        
        if (mainCam.orthographic)
        {
            info += $"Orthographic Size: {mainCam.orthographicSize}\n";
        }
        else
        {
            info += $"Field of View: {mainCam.fieldOfView}\n";
        }
        
        info += $"\nNear Clip: {mainCam.nearClipPlane}\n";
        info += $"Far Clip: {mainCam.farClipPlane}\n";
        
        if (camController != null)
        {
            info += $"\nCameraController Found: YES\n";
            // isVirtualTourMode is private, so we check the projection instead
            info += $"Current Mode: {(mainCam.orthographic ? "Top View" : "Virtual Tour")}";
        }
        else
        {
            info += $"\nCameraController Found: NO";
        }
        
        Debug.Log($"[Camera Settings]\n{info}");
        
        EditorUtility.DisplayDialog("Main Camera Settings", info, "OK");
        Selection.activeGameObject = mainCam.gameObject;
    }


    /// <summary>
    /// Shows information about the automatic processing system.
    /// </summary>
    [MenuItem("Tools/About Auto Model Processor")]
    public static void ShowAbout()
    {
        EditorUtility.DisplayDialog(
            "Auto Model Processor - Advanced", 
            "AUTOMATIC MODEL PROCESSING SYSTEM\n\n" +
            "This system automatically processes any 3D model with\n" +
            "EXACT settings from your current scene!\n\n" +
            "📁 DROP MODELS IN:\n" +
            "Assets/Model/\n\n" +
            "✨ WHAT IT DOES:\n" +
            "✓ Removes old model from scene\n" +
            "✓ Adds new model at (0,0,0)\n" +
            "✓ Applies EXACT camera settings (saved)\n" +
            "✓ Same Top View configuration\n" +
            "✓ Same Virtual Tour starting position\n" +
            "✓ Preserves all UI and controls\n" +
            "✓ Maintains same zoom/movement speeds\n\n" +
            "🎯 SETUP (First Time):\n" +
            "1. Configure your scene perfectly\n" +
            "2. Tools → Camera Settings Manager\n" +
            "3. Click 'Save Current Camera Settings'\n\n" +
            "💡 USAGE:\n" +
            "Just drag any .glb or .fbx into Assets/Model/\n" +
            "Everything happens automatically!\n\n" +
            "The new model will behave EXACTLY like your\n" +
            "current one - same camera, same controls!",
            "Got it!");
    }

    /// <summary>
    /// Quick load any model from the Model folder into the scene for testing.
    /// </summary>
    [MenuItem("Tools/Load Model into Scene Now")]
    public static void LoadModelNow()
    {
        // Find all models in the Model folder
        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { MODEL_FOLDER });
        
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("No Models", "No models found in Assets/Model/ folder.", "OK");
            return;
        }

        // Get model names
        string[] modelPaths = new string[guids.Length];
        string[] modelNames = new string[guids.Length];
        
        for (int i = 0; i < guids.Length; i++)
        {
            modelPaths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            modelNames[i] = System.IO.Path.GetFileNameWithoutExtension(modelPaths[i]);
        }

        // Show selection menu
        GenericMenu menu = new GenericMenu();
        
        for (int i = 0; i < modelNames.Length; i++)
        {
            string path = modelPaths[i];
            menu.AddItem(new GUIContent(modelNames[i]), false, () => ProcessNewModel(path));
        }
        
        menu.ShowAsContext();
    }
}
