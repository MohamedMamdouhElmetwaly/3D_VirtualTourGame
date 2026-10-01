using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Tool to save current camera settings and apply them to new models.
/// This ensures all models use the SAME camera configuration.
/// </summary>
public class CameraSettingsSaver : EditorWindow
{
    private static readonly string SETTINGS_PATH = "Assets/Resources/SavedCameraSettings.asset";
    
    [MenuItem("Tools/Camera Settings Manager")]
    public static void ShowWindow()
    {
        GetWindow<CameraSettingsSaver>("Camera Settings");
    }

    private void OnGUI()
    {
        GUILayout.Label("Camera Settings Manager", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Check if camera exists in scene
        Camera mainCam = Camera.main;
        CameraController camController = mainCam != null ? mainCam.GetComponent<CameraController>() : null;

        if (mainCam == null)
        {
            EditorGUILayout.HelpBox("Main Camera not found in scene!", MessageType.Error);
            return;
        }

        if (camController == null)
        {
            EditorGUILayout.HelpBox("CameraController component not found on Main Camera!", MessageType.Error);
            return;
        }

        // Show current camera settings
        EditorGUILayout.HelpBox("Current Camera Configuration (from Inspector):", MessageType.Info);
        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(true);
        
        EditorGUILayout.LabelField("Movement Settings:", EditorStyles.boldLabel);
        EditorGUILayout.FloatField("Movement Speed", camController.movementSpeed);
        EditorGUILayout.FloatField("Look Sensitivity", camController.lookSensitivity);
        EditorGUILayout.FloatField("Zoom Speed", camController.zoomSpeed);
        EditorGUILayout.FloatField("Smooth Speed", camController.smoothSpeed);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Top View Settings:", EditorStyles.boldLabel);
        EditorGUILayout.Vector3Field("Position", camController.topViewPosition);
        EditorGUILayout.Vector3Field("Rotation", camController.topViewRotation);
        EditorGUILayout.FloatField("Orthographic Size", camController.topViewOrthographicSize);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Virtual Tour Settings:", EditorStyles.boldLabel);
        EditorGUILayout.Vector3Field("Start Position", camController.virtualTourStartPosition);
        EditorGUILayout.Vector3Field("Start Rotation", camController.virtualTourStartRotation);
        EditorGUILayout.FloatField("Field of View", camController.virtualTourFOV);
        
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.Space();

        // Check if settings are saved
        var savedSettings = LoadSavedSettings();
        if (savedSettings != null)
        {
            EditorGUILayout.HelpBox($"✓ Saved settings found: {SETTINGS_PATH}", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("No saved settings found. Save current settings first!", MessageType.Warning);
        }
        EditorGUILayout.Space();

        // Save button
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("💾 Save Current Camera Settings", GUILayout.Height(40)))
        {
            SaveCurrentSettings(camController);
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space();

        // Apply button
        EditorGUI.BeginDisabledGroup(savedSettings == null);
        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("📋 Apply Saved Settings to Current Camera", GUILayout.Height(40)))
        {
            ApplySavedSettings(camController);
        }
        GUI.backgroundColor = Color.white;
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.Space();

        // Show saved settings
        if (savedSettings != null)
        {
            if (GUILayout.Button("🔍 Show Saved Settings Details"))
            {
                ShowSavedSettingsDialog(savedSettings);
            }
        }
    }

    private void SaveCurrentSettings(CameraController camController)
    {
        // Ensure Resources folder exists
        if (!Directory.Exists("Assets/Resources"))
        {
            Directory.CreateDirectory("Assets/Resources");
            AssetDatabase.Refresh();
        }

        // Create or load existing settings
        SavedCameraSettings settings = LoadSavedSettings();
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<SavedCameraSettings>();
            AssetDatabase.CreateAsset(settings, SETTINGS_PATH);
        }

        // Save all settings
        settings.movementSpeed = camController.movementSpeed;
        settings.lookSensitivity = camController.lookSensitivity;
        settings.zoomSpeed = camController.zoomSpeed;
        settings.smoothSpeed = camController.smoothSpeed;
        settings.scrollSensitivity = camController.scrollSensitivity;

        settings.topViewPosition = camController.topViewPosition;
        settings.topViewRotation = camController.topViewRotation;
        settings.topViewOrthographicSize = camController.topViewOrthographicSize;

        settings.virtualTourStartPosition = camController.virtualTourStartPosition;
        settings.virtualTourStartRotation = camController.virtualTourStartRotation;
        settings.virtualTourFOV = camController.virtualTourFOV;

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        Debug.Log($"[CameraSettingsSaver] ✓ Camera settings saved to: {SETTINGS_PATH}");
        EditorUtility.DisplayDialog(
            "Settings Saved!",
            "Camera settings have been saved successfully!\n\n" +
            $"Saved to: {SETTINGS_PATH}\n\n" +
            "These settings will be applied to all new models automatically.",
            "OK");
    }

    private void ApplySavedSettings(CameraController camController)
    {
        var settings = LoadSavedSettings();
        if (settings == null)
        {
            EditorUtility.DisplayDialog("Error", "No saved settings found!", "OK");
            return;
        }

        Undo.RecordObject(camController, "Apply Saved Camera Settings");

        camController.movementSpeed = settings.movementSpeed;
        camController.lookSensitivity = settings.lookSensitivity;
        camController.zoomSpeed = settings.zoomSpeed;
        camController.smoothSpeed = settings.smoothSpeed;
        camController.scrollSensitivity = settings.scrollSensitivity;

        camController.topViewPosition = settings.topViewPosition;
        camController.topViewRotation = settings.topViewRotation;
        camController.topViewOrthographicSize = settings.topViewOrthographicSize;

        camController.virtualTourStartPosition = settings.virtualTourStartPosition;
        camController.virtualTourStartRotation = settings.virtualTourStartRotation;
        camController.virtualTourFOV = settings.virtualTourFOV;

        EditorUtility.SetDirty(camController);

        Debug.Log("[CameraSettingsSaver] ✓ Saved settings applied to current camera");
        EditorUtility.DisplayDialog("Settings Applied!", "Saved camera settings have been applied to the current camera.", "OK");
    }

    public static SavedCameraSettings LoadSavedSettings()
    {
        return AssetDatabase.LoadAssetAtPath<SavedCameraSettings>(SETTINGS_PATH);
    }

    private void ShowSavedSettingsDialog(SavedCameraSettings settings)
    {
        string info = "SAVED CAMERA SETTINGS\n\n";
        info += "Movement Settings:\n";
        info += $"  Speed: {settings.movementSpeed}\n";
        info += $"  Look Sensitivity: {settings.lookSensitivity}\n";
        info += $"  Zoom Speed: {settings.zoomSpeed}\n";
        info += $"  Smooth Speed: {settings.smoothSpeed}\n\n";
        
        info += "Top View:\n";
        info += $"  Position: {settings.topViewPosition}\n";
        info += $"  Rotation: {settings.topViewRotation}\n";
        info += $"  Size: {settings.topViewOrthographicSize}\n\n";
        
        info += "Virtual Tour:\n";
        info += $"  Position: {settings.virtualTourStartPosition}\n";
        info += $"  Rotation: {settings.virtualTourStartRotation}\n";
        info += $"  FOV: {settings.virtualTourFOV}\n";

        Debug.Log($"[Saved Settings]\n{info}");
        EditorUtility.DisplayDialog("Saved Camera Settings", info, "OK");
    }
}

/// <summary>
/// ScriptableObject to store camera settings persistently.
/// </summary>
[System.Serializable]
public class SavedCameraSettings : ScriptableObject
{
    [Header("Movement Settings")]
    public float movementSpeed = 1000f;
    public float lookSensitivity = 8f;
    public float zoomSpeed = 100f;
    public float smoothSpeed = 10f;
    public float scrollSensitivity = 2f;

    [Header("Top View Settings")]
    public Vector3 topViewPosition = new Vector3(-20, 11991.48f, -1523);
    public Vector3 topViewRotation = new Vector3(90, 0, 0);
    public float topViewOrthographicSize = 6997.486f;

    [Header("Virtual Tour Settings")]
    public Vector3 virtualTourStartPosition = new Vector3(-64.42599f, 1689.906f, -1695.566f);
    public Vector3 virtualTourStartRotation = new Vector3(5.192f, -179.347f, -0.001f);
    public float virtualTourFOV = 60f;
}
