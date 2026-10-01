using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor.Build;
#endif

/// <summary>
/// Unity Editor build automation and settings manager for Flutter integration.
/// Provides a custom Editor Window and automates setting up players and exporting libraries.
/// </summary>
public class BuildScripts : AssetPostprocessor
{
    // Preference Keys for persistent configuration
    private const string PrefFlutterRootPath = "FlutterExport_FlutterRootPath";
    private const string PrefAutoBuildOnSave = "FlutterExport_AutoBuildOnSave";
    private const string PrefExportAndroid = "FlutterExport_ExportAndroid";
    private const string PrefExportIOS = "FlutterExport_ExportIOS";

    /// <summary>
    /// Gets or sets the absolute path to the Flutter project root.
    /// </summary>
    public static string FlutterRootPath
    {
        get => EditorPrefs.GetString(PrefFlutterRootPath, string.Empty);
        set => EditorPrefs.SetString(PrefFlutterRootPath, value);
    }

    /// <summary>
    /// Gets or sets whether to auto-build for Flutter whenever a scene is saved.
    /// </summary>
    public static bool AutoBuildOnSave
    {
        get => EditorPrefs.GetBool(PrefAutoBuildOnSave, false);
        set => EditorPrefs.SetBool(PrefAutoBuildOnSave, value);
    }

    /// <summary>
    /// Gets or sets whether Android is included in the build.
    /// </summary>
    public static bool ExportAndroidEnabled
    {
        get => EditorPrefs.GetBool(PrefExportAndroid, true);
        set => EditorPrefs.SetBool(PrefExportAndroid, value);
    }

    /// <summary>
    /// Gets or sets whether iOS is included in the build.
    /// </summary>
    public static bool ExportIOSEnabled
    {
        get => EditorPrefs.GetBool(PrefExportIOS, true);
        set => EditorPrefs.SetBool(PrefExportIOS, value);
    }

    /// <summary>
    /// Calculated path for Android export.
    /// </summary>
    public static string AndroidExportPath
    {
        get
        {
            string root = FlutterRootPath;
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
            {
                return Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/Android/unityLibrary"));
            }
            return Path.GetFullPath(Path.Combine(root, "android/unityLibrary"));
        }
    }

    /// <summary>
    /// Calculated path for iOS export.
    /// </summary>
    public static string IOSExportPath
    {
        get
        {
            string root = FlutterRootPath;
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
            {
                return Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/iOS/UnityLibrary"));
            }
            return Path.GetFullPath(Path.Combine(root, "ios/UnityLibrary"));
        }
    }

    /// <summary>
    /// Called automatically by Unity after any asset is imported, deleted, or moved.
    /// Used here to detect scene saves and trigger automatic Flutter builds.
    /// </summary>
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (!AutoBuildOnSave) return;

        foreach (string asset in importedAssets)
        {
            if (asset.EndsWith(".unity"))
            {
                Debug.Log($"[FlutterExport] Scene saved: {asset}. Triggering automatic Flutter build...");
                BuildForFlutter();
                break;
            }
        }
    }

    /// <summary>
    /// Main entry point to build active export targets.
    /// </summary>
    [MenuItem("Flutter/Build for Flutter")]
    public static void BuildForFlutter()
    {
        ConfigureBuildTargets();

        bool anyBuilt = false;

        if (ExportAndroidEnabled)
        {
            BuildAndroidLibrary();
            anyBuilt = true;
        }

        if (ExportIOSEnabled)
        {
            BuildIOSLibrary();
            anyBuilt = true;
        }

        if (!anyBuilt)
        {
            Debug.LogWarning("[FlutterExport] No platforms selected for export. Enable them in Flutter -> Settings & Export.");
        }
    }

    /// <summary>
    /// Builds the Android library and formats it for Flutter consumption.
    /// </summary>
    [MenuItem("Flutter/Build Android Library Only")]
    public static void BuildAndroidLibrary()
    {
        Debug.Log($"[FlutterExport] Starting Android library build...");
        ConfigureAndroidSettings();

        string exportDir = AndroidExportPath;

        // Clean up existing directory to ensure a fresh export
        if (Directory.Exists(exportDir))
        {
            Debug.Log($"[FlutterExport] Cleaning up existing Android export directory: {exportDir}");
            Directory.Delete(exportDir, true);
        }

        string exportParent = Path.GetDirectoryName(exportDir);
if (!string.IsNullOrEmpty(exportParent) && !Directory.Exists(exportParent))
        {
            Directory.CreateDirectory(exportParent);
        }

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = GetEnabledScenes(),
            locationPathName = exportDir,
            target = BuildTarget.Android,
            options = BuildOptions.AcceptExternalModificationsToPlayer
        };

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[FlutterExport] Android library built successfully to: {exportDir}");
            Debug.Log($"[FlutterExport] Build Size: {report.summary.totalSize / (1024.0 * 1024.0):F2} MB");
        }
        else
        {
            Debug.LogError($"[FlutterExport] Android library build failed with result: {report.summary.result}");
        }
    }

    /// <summary>
    /// Builds the iOS library and formats it for Flutter consumption.
    /// </summary>
    [MenuItem("Flutter/Build iOS Library Only")]
    public static void BuildIOSLibrary()
    {
        Debug.Log($"[FlutterExport] Starting iOS library build...");
        ConfigureIOSSettings();

        string exportDir = IOSExportPath;
        
        // Use FileUtil for more reliable deletion in Unity Editor
        if (Directory.Exists(exportDir))
        {
            Debug.Log($"[FlutterExport] Cleaning up existing iOS export directory: {exportDir}");
            FileUtil.DeleteFileOrDirectory(exportDir);
        }

        string exportParent = Path.GetDirectoryName(exportDir);
if (!string.IsNullOrEmpty(exportParent) && !Directory.Exists(exportParent))
        {
            Directory.CreateDirectory(exportParent);
        }

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = GetEnabledScenes(),
            locationPathName = exportDir,
            target = BuildTarget.iOS,
            options = BuildOptions.AcceptExternalModificationsToPlayer
        };

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[FlutterExport] iOS library built successfully to: {exportDir}");
            Debug.Log($"[FlutterExport] Build Size: {report.summary.totalSize / (1024.0 * 1024.0):F2} MB");
        }
        else
        {
            Debug.LogError($"[FlutterExport] iOS library build failed with result: {report.summary.result}");
        }
    }

    /// <summary>
    /// Automatically configures global build settings to ensure compatibility with Flutter.
    /// </summary>
    public static void ConfigureBuildTargets()
    {
        if (ExportAndroidEnabled) ConfigureAndroidSettings();
        if (ExportIOSEnabled) ConfigureIOSSettings();
    }

    /// <summary>
    /// Configures Android settings specifically for flutter_unity_widget.
    /// </summary>
    public static void ConfigureAndroidSettings()
    {
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25; 
PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        
        // ARM64 is essential for modern Flutter apps
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
        
        // Ensure Unity doesn't try to manage the activity lifecycle in a way that conflicts with Flutter
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.Generic;
        
        Debug.Log("[FlutterExport] Android settings automatically optimized for Unity 6 + flutter_embed_unity.");
    }

    /// <summary>
    /// Configures iOS settings specifically for flutter_embed_unity.
    /// </summary>
    public static void ConfigureIOSSettings()
    {
        PlayerSettings.iOS.targetOSVersionString = "13.0"; // Higher base for better compatibility
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
        PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
        
        // Export as framework is handled by the "AcceptExternalModificationsToPlayer" option in the build
        
        Debug.Log("[FlutterExport] iOS settings automatically optimized for flutter_embed_unity.");
    }

    /// <summary>
    /// Gets all enabled scenes in the Build Settings.
    /// </summary>
    private static string[] GetEnabledScenes()
    {
        List<string> scenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                scenes.Add(scene.path);
            }
        }

        if (scenes.Count == 0)
        {
            string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            if (!string.IsNullOrEmpty(activeScene))
            {
                scenes.Add(activeScene);
                Debug.LogWarning($"[FlutterExport] No scenes are enabled in Build Settings. Using active scene: {activeScene}");
            }
        }

        return scenes.ToArray();
    }
}

/// <summary>
/// Custom Editor Window to manage Flutter Export path, settings, and triggers.
/// </summary>
public class FlutterExportWindow : EditorWindow
{
    private string tempPath = string.Empty;

    [MenuItem("Flutter/Settings & Export Manager")]
    public static void ShowWindow()
    {
        var window = GetWindow<FlutterExportWindow>("Flutter Export Manager");
        window.minSize = new Vector2(450, 420);
        window.Show();
    }

    private void OnEnable()
    {
        tempPath = BuildScripts.FlutterRootPath;
    }

    private void OnGUI()
    {
        // Title banner
        GUILayout.Space(10);
        EditorGUILayout.BeginVertical("box");
        var titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("Flutter Integration & Export", titleStyle, GUILayout.Height(25));
        EditorGUILayout.LabelField("Easily export Unity as a library for flutter_unity_widget", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.EndVertical();

        GUILayout.Space(15);

        // Flutter project path selection
        EditorGUILayout.LabelField("Flutter Project Root Directory", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        tempPath = EditorGUILayout.TextField(tempPath);
        if (GUILayout.Button("Browse...", GUILayout.Width(80)))
        {
            string chosenPath = EditorUtility.OpenFolderPanel("Select Flutter Project Root", tempPath, "");
            if (!string.IsNullOrEmpty(chosenPath))
            {
                tempPath = chosenPath;
                BuildScripts.FlutterRootPath = tempPath;
            }
        }
        EditorGUILayout.EndHorizontal();

        // Warning/Verification if path is invalid or valid
        if (string.IsNullOrEmpty(tempPath))
        {
            EditorGUILayout.HelpBox("Select your Flutter project folder to build directly into the Flutter app structure.", MessageType.Info);
        }
        else if (!Directory.Exists(tempPath))
        {
            EditorGUILayout.HelpBox("Selected path does not exist. Please browse a valid directory.", MessageType.Error);
        }
        else
        {
            bool hasAndroid = Directory.Exists(Path.Combine(tempPath, "android"));
            bool hasIOS = Directory.Exists(Path.Combine(tempPath, "ios"));

            if (hasAndroid && hasIOS)
            {
                EditorGUILayout.HelpBox("Valid Flutter project detected! Targets will export straight to android/unityLibrary and ios/UnityLibrary.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Selected folder exists, but does not appear to be a standard Flutter project (missing 'android' or 'ios' folders). Custom build folders will be generated under that path.", MessageType.Warning);
            }
        }

        GUILayout.Space(15);

        // Settings Section
        EditorGUILayout.LabelField("Settings & Automation", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        bool autoBuild = EditorGUILayout.ToggleLeft("Auto-Build on Scene Save (When finishing work)", BuildScripts.AutoBuildOnSave);
        if (autoBuild != BuildScripts.AutoBuildOnSave)
        {
            BuildScripts.AutoBuildOnSave = autoBuild;
        }
        if (autoBuild)
        {
            EditorGUILayout.HelpBox("Warning: Exporting the whole player library can freeze Unity for a couple of minutes when saving scenes. Perfect for final handoffs, but you might want to turn it off during rapid active coding/editing.", MessageType.None);
        }

        GUILayout.Space(5);
        
        bool expAndroid = EditorGUILayout.ToggleLeft("Include Android Export Target", BuildScripts.ExportAndroidEnabled);
        if (expAndroid != BuildScripts.ExportAndroidEnabled)
        {
            BuildScripts.ExportAndroidEnabled = expAndroid;
        }

        bool expIOS = EditorGUILayout.ToggleLeft("Include iOS Export Target", BuildScripts.ExportIOSEnabled);
        if (expIOS != BuildScripts.ExportIOSEnabled)
        {
            BuildScripts.ExportIOSEnabled = expIOS;
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space(15);

        // Display current target directories
        EditorGUILayout.LabelField("Active Output Paths:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Android: {BuildScripts.AndroidExportPath}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"iOS: {BuildScripts.IOSExportPath}", EditorStyles.miniLabel);

        GUILayout.FlexibleSpace();

        // Control Buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Configure Player Settings", GUILayout.Height(30)))
        {
            BuildScripts.ConfigureBuildTargets();
            EditorUtility.DisplayDialog("Settings Configured", "Android and iOS player settings successfully optimized for Flutter Integration!", "OK");
        }
        if (GUILayout.Button("Export for Flutter Now", GUILayout.Height(30)))
        {
            BuildScripts.BuildForFlutter();
            EditorUtility.DisplayDialog("Export Complete", "Export completed. Check the console for full build logs and statistics.", "OK");
        }
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(10);
    }
}
