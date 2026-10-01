using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GLTFast;
using GLTFast.Loading;

/// <summary>
/// Runtime model loader that loads user-uploaded 3D model files at runtime
/// Supports .glb (recommended) and .obj formats only
/// Note: .gltf JSON files require external .bin files and are not supported; use .glb instead
/// Applies all camera settings and interactions
/// </summary>
public class RuntimeModelLoader : MonoBehaviour
{
    public static RuntimeModelLoader Instance { get; private set; }

    [Header("References")]
    public CameraController cameraController;
    public GameObject loadingPanel;
    public UnityEngine.UI.Slider loadingProgressBar;
    public UnityEngine.UI.Text loadingText;

    private GameObject currentModel;
    private string currentModelPath;
    private bool isLoading;
    private bool loadFailed;
    private string lastLoadError;

    public bool IsLoading => isLoading;
    public bool LoadFailed => loadFailed;
    public string LastLoadError => lastLoadError;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    /// <summary>
    /// Load a 3D model file (GLB or OBJ) from the given file path
    /// </summary>
    public void LoadModelFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"[RuntimeModelLoader] File not found: {filePath}");
            return;
        }

        string extension = Path.GetExtension(filePath).ToLower();
        if (!IsSupportedModelExtension(extension))
        {
            Debug.LogError($"[RuntimeModelLoader] Unsupported file format: {extension}. Use .glb or .obj");
            return;
        }

        currentModelPath = filePath;
        loadFailed = false;
        lastLoadError = null;
        StartCoroutine(LoadModelCoroutine(filePath));
    }

    /// <summary>
    /// Load a 3D model from byte array (for WebGL file upload or API download)
    /// </summary>
    public void LoadModelFromBytes(byte[] modelData, string fileName = "UploadedModel")
    {
        if (modelData == null || modelData.Length == 0)
        {
            Debug.LogError("[RuntimeModelLoader] No data provided");
            return;
        }

        loadFailed = false;
        lastLoadError = null;
        StartCoroutine(LoadModelFromBytesCoroutine(modelData, fileName));
    }

    private static bool IsSupportedModelExtension(string extension)
    {
        return extension == ".glb";
    }

    private IEnumerator LoadModelCoroutine(string filePath)
    {
        isLoading = true;
        loadFailed = false;
        lastLoadError = null;

        UpdateLoadingUI(0f, "Loading model file...");

        string extension = Path.GetExtension(filePath).ToLower();

        if (extension == ".glb")
        {
            yield return LoadGltfFromFile(filePath);
            isLoading = false;
            yield break;
        }

        FailLoad($"Unsupported format: {extension}. Only .glb is supported.");
        yield return new WaitForSeconds(2f);
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
        isLoading = false;
    }

    private IEnumerator LoadObjFromFile(string filePath)
    {
        UpdateLoadingUI(0.2f, "Parsing OBJ data...");
        RemoveCurrentModel();

        GameObject model = null;
        string error = null;
        try
        {
            model = RuntimeObjLoader.LoadFromFile(filePath);
        }
        catch (System.Exception e)
        {
            error = e.Message;
        }

        if (error != null || model == null)
        {
            FailLoad($"Failed to load OBJ: {error ?? "Invalid file"}");
            yield return new WaitForSeconds(2f);
            if (loadingPanel != null)
                loadingPanel.SetActive(false);
            yield break;
        }

        currentModel = model;
        yield return FinalizeLoadedModel(Path.GetFileNameWithoutExtension(filePath));
    }

    private IEnumerator LoadModelFromBytesCoroutine(byte[] modelData, string modelName)
    {
        isLoading = true;
        loadFailed = false;
        lastLoadError = null;

        UpdateLoadingUI(0.1f, "Analyzing model format...");

        // Detect format from file extension
        string extension = Path.GetExtension(modelName).ToLower();
        
        if (extension == ".glb" || string.IsNullOrEmpty(extension))
        {
            yield return LoadGltfBinaryFromBytes(modelData, modelName);
        }
        else
        {
            FailLoad($"Unsupported format: {extension}. Only .glb is supported.");
            UpdateLoadingUI(0f, $"Error: Unsupported format {extension}");
            yield return new WaitForSeconds(2f);
            if (loadingPanel != null)
                loadingPanel.SetActive(false);
        }

        isLoading = false;
    }

    private IEnumerator LoadObjFromBytes(byte[] objData, string modelName)
    {
        UpdateLoadingUI(0.2f, "Parsing OBJ data...");
        RemoveCurrentModel();

        GameObject model = null;
        string error = null;
        try
        {
            string objText = System.Text.Encoding.UTF8.GetString(objData);
            model = RuntimeObjLoader.LoadFromText(objText, Path.GetFileNameWithoutExtension(modelName));
        }
        catch (System.Exception e)
        {
            error = e.Message;
        }

        if (error != null || model == null)
        {
            FailLoad($"Failed to load OBJ: {error ?? "Invalid file"}");
            yield return new WaitForSeconds(2f);
            if (loadingPanel != null)
                loadingPanel.SetActive(false);
            yield break;
        }

        currentModel = model;
        yield return FinalizeLoadedModel(Path.GetFileNameWithoutExtension(modelName));
    }
    
    private IEnumerator LoadGltfFromFile(string filePath)
    {
        string fileName = Path.GetFileName(filePath);
        UpdateLoadingUI(0.2f, "Reading GLB data...");

        byte[] glbData = null;
        string readError = null;

        try
        {
            glbData = File.ReadAllBytes(filePath);
            ValidateGlbBytes(glbData);
        }
        catch (System.Exception e)
        {
            readError = e.Message;
        }

        if (!string.IsNullOrEmpty(readError))
        {
            FailLoad($"Failed to load .glb: {readError}");
            UpdateLoadingUI(0f, "Error: Invalid GLB file");
            yield return new WaitForSeconds(2f);
            if (loadingPanel != null)
                loadingPanel.SetActive(false);
            yield break;
        }

        yield return LoadGltfBinaryFromBytes(glbData, fileName);
    }

    private IEnumerator LoadGltfBinaryFromBytes(byte[] glbData, string modelName)
    {
        UpdateLoadingUI(0.2f, "Parsing GLB data...");
        RemoveCurrentModel();

        string validationError = null;
        try
        {
            ValidateGlbBytes(glbData);
        }
        catch (System.Exception e)
        {
            validationError = e.Message;
        }

        if (!string.IsNullOrEmpty(validationError))
        {
            FailLoad($"Failed to load GLB: {validationError}");
            UpdateLoadingUI(0f, "Error: Invalid GLB file");
            yield return new WaitForSeconds(2f);
            if (loadingPanel != null)
                loadingPanel.SetActive(false);
            yield break;
        }

        var gltf = CreateGltfImport();
        var loadTask = gltf.LoadGltfBinary(glbData, importSettings: CreateImportSettings());
        while (!loadTask.IsCompleted) yield return null;

        if (!TryGetTaskResult(loadTask, out bool success, out string loadError) || !success)
        {
            FailLoad($"Failed to load GLB: {loadError ?? "Invalid file"}");
            UpdateLoadingUI(0f, "Error: Failed to load GLB");
            yield return new WaitForSeconds(2f);
            if (loadingPanel != null)
                loadingPanel.SetActive(false);
            yield break;
        }

        yield return InstantiateLoadedGltf(gltf, Path.GetFileNameWithoutExtension(modelName));
    }

    private static void ValidateGlbBytes(byte[] glbData)
    {
        if (glbData == null || glbData.Length < 20)
            throw new System.InvalidOperationException("File is empty or incomplete.");

        bool hasGlbMagic = glbData[0] == (byte)'g' &&
                           glbData[1] == (byte)'l' &&
                           glbData[2] == (byte)'T' &&
                           glbData[3] == (byte)'F';

        if (!hasGlbMagic)
            throw new System.InvalidOperationException("File has .glb extension but is not a binary GLB.");

        uint version = System.BitConverter.ToUInt32(glbData, 4);
        if (version != 2)
            throw new System.InvalidOperationException($"Unsupported GLB version {version}. Please export as GLB 2.0.");

        uint declaredLength = System.BitConverter.ToUInt32(glbData, 8);
        if (declaredLength != glbData.Length)
        {
            Debug.LogWarning($"[RuntimeModelLoader] GLB declared length ({declaredLength}) differs from file bytes ({glbData.Length}).");
        }
    }

    private IEnumerator LoadGltfJsonFromBytes(byte[] jsonData, string modelName)
    {
        UpdateLoadingUI(0.2f, "Parsing GLTF data...");
        RemoveCurrentModel();

        string json = System.Text.Encoding.UTF8.GetString(jsonData);
        var gltf = CreateGltfImport();
        var loadTask = gltf.LoadGltfJson(
            json,
            new System.Uri("file:///"),
            CreateImportSettings());
        while (!loadTask.IsCompleted) yield return null;

        if (!TryGetTaskResult(loadTask, out bool success, out string loadError) || !success)
        {
            FailLoad($"Failed to load GLTF: {loadError ?? "Invalid file"}");
            UpdateLoadingUI(0f, "Error: Failed to load GLTF");
            yield return new WaitForSeconds(2f);
            if (loadingPanel != null)
                loadingPanel.SetActive(false);
            yield break;
        }

        yield return InstantiateLoadedGltf(gltf, Path.GetFileNameWithoutExtension(modelName));
    }

    private IEnumerator InstantiateLoadedGltf(GltfImport gltf, string modelName)
    {
        UpdateLoadingUI(0.4f, "Instantiating model...");

        currentModel = new GameObject(modelName);

        var instTask = gltf.InstantiateMainSceneAsync(currentModel.transform);
        while (!instTask.IsCompleted) yield return null;

        if (!TryGetTaskResult(instTask, out bool success, out string instError) || !success)
        {
            Destroy(currentModel);
            currentModel = null;
            FailLoad($"Failed to instantiate: {instError ?? "Unknown error"}");
            UpdateLoadingUI(0f, "Error: Failed to create model");
            yield return new WaitForSeconds(2f);
            if (loadingPanel != null)
                loadingPanel.SetActive(false);
            yield break;
        }

        yield return FinalizeLoadedModel(modelName);
    }

    private IEnumerator FinalizeLoadedModel(string modelName)
    {
        currentModel.transform.position = Vector3.zero;
        currentModel.transform.rotation = Quaternion.identity;
        currentModel.transform.localScale = Vector3.one;

        Debug.Log($"[RuntimeModelLoader] Model instantiated: {modelName}");

        UpdateLoadingUI(0.5f, "Fixing materials...");
        yield return null;

        FixMaterialsForURP();

        UpdateLoadingUI(0.6f, "Configuring camera...");
        yield return null;

        ConfigureCameraForModel();

        UpdateLoadingUI(0.9f, "Finalizing...");
        yield return new WaitForSeconds(0.2f);

        UpdateLoadingUI(1f, "Model loaded successfully!");
        yield return new WaitForSeconds(0.5f);

        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        Debug.Log("[RuntimeModelLoader] Model loading complete!");
    }

    private GltfImport CreateGltfImport()
    {
        // glTFast automatically selects its URP material generator from the
        // active render pipeline. Its shaders are retained for Android in
        // ProjectSettings/GraphicsSettings.asset.
        return new GltfImport();
    }

    private static ImportSettings CreateImportSettings()
    {
        return new ImportSettings
        {
            GenerateMipMaps = true,
            AnisotropicFilterLevel = 3,
            NodeNameMethod = NameImportMethod.OriginalUnique
        };
    }

    private void RemoveCurrentModel()
    {
        if (currentModel == null)
            return;

        Debug.Log("[RuntimeModelLoader] Removing old model");
        Destroy(currentModel);
        currentModel = null;
    }

    private void FailLoad(string message)
    {
        loadFailed = true;
        lastLoadError = message;
        Debug.LogError($"[RuntimeModelLoader] {message}");
        UpdateLoadingUI(0f, message);
    }

    private static bool TryGetTaskResult(System.Threading.Tasks.Task<bool> task, out bool result, out string error)
    {
        result = false;
        error = null;
        try
        {
            result = task.Result;
            return true;
        }
        catch (System.Exception e)
        {
            error = e.Message;
            return false;
        }
    }

    private void FixMaterialsForURP()
    {
        if (currentModel == null)
            return;

        Debug.Log("[RuntimeModelLoader] Preserving original GLB materials - NO modifications applied");

        // Count materials for logging only
        Renderer[] renderers = currentModel.GetComponentsInChildren<Renderer>();
        int totalMaterials = 0;
        int texturedMaterials = 0;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            Material[] materials = renderer.materials;
            foreach (Material mat in materials)
            {
                if (mat != null)
                {
                    totalMaterials++;
                    if (mat.mainTexture != null)
                        texturedMaterials++;
                    
                    // Log material info but DO NOT modify anything
                    Debug.Log($"[RuntimeModelLoader] Material: {mat.name}, Shader: {mat.shader?.name ?? "null"}, " +
                             $"Texture: {(mat.mainTexture != null ? mat.mainTexture.name : "None")}");
                }
            }
        }

        Debug.Log($"[RuntimeModelLoader] Total materials: {totalMaterials}, With textures: {texturedMaterials}");
        Debug.Log($"[RuntimeModelLoader] All original materials preserved from GLB file");
    }

    private void ConfigureCameraForModel()
    {
        if (currentModel == null || cameraController == null)
            return;

        // Calculate model bounds
        Renderer[] renderers = currentModel.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning("[RuntimeModelLoader] No renderers found in model");
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Debug.Log($"[RuntimeModelLoader] Model bounds: {bounds.center}, Size: {bounds.size}");

        // Determine scale multiplier
        float boundsMagnitude = bounds.size.magnitude;
        float scaleMultiplier = (boundsMagnitude > 1000f) ? 100f : 1f;

        // Calculate top view settings
        float sizeBasedOnZ = bounds.size.z / 2f;
        float aspect = (float)Screen.width / Screen.height;
        float sizeBasedOnX = (bounds.size.x / 2f) / aspect;
        float topViewOrthographicSize = Mathf.Max(sizeBasedOnZ, sizeBasedOnX) * 1.15f;

        // Top view position
        float heightOffset = Mathf.Max(bounds.size.y * 3f, 10f);
        Vector3 topViewPos = new Vector3(bounds.center.x, bounds.max.y + heightOffset, bounds.center.z);
        Vector3 topViewRot = new Vector3(90, 0, 0);

        // Virtual tour start position (inside the model)
        Vector3 tourStartPos = bounds.center;
        tourStartPos.y = bounds.min.y + (1.25f * scaleMultiplier);
        Vector3 tourStartRot = new Vector3(0f, 180f, 0f);

        // Calculate movement speeds based on model size
        float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        float movementSpeed = maxSize * 10f;
        float zoomSpeed = maxSize;

        // Apply to camera controller
        cameraController.topViewPosition = topViewPos;
        cameraController.topViewRotation = topViewRot;
        cameraController.topViewOrthographicSize = topViewOrthographicSize;
        
        cameraController.virtualTourStartPosition = tourStartPos;
        cameraController.virtualTourStartRotation = tourStartRot;
        cameraController.virtualTourFOV = 60f;
        
        cameraController.movementSpeed = movementSpeed;
        cameraController.zoomSpeed = zoomSpeed;
        cameraController.scaleMultiplier = scaleMultiplier;

        // Start in top view mode and snap camera to the model immediately
        cameraController.SwitchToTopView();
        cameraController.ApplyViewImmediately();

        Debug.Log($"[RuntimeModelLoader] Camera configured for model");
    }


    private Bounds CalculateLocalBounds(Transform parent)
    {
        Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);
        bool first = true;
        var renderers = parent.GetComponentsInChildren<Renderer>(true);

        foreach (var r in renderers)
        {
            Bounds worldB = r.bounds;
            Vector3[] corners = new Vector3[8] {
                new Vector3(worldB.min.x, worldB.min.y, worldB.min.z),
                new Vector3(worldB.min.x, worldB.min.y, worldB.max.z),
                new Vector3(worldB.min.x, worldB.max.y, worldB.min.z),
                new Vector3(worldB.min.x, worldB.max.y, worldB.max.z),
                new Vector3(worldB.max.x, worldB.min.y, worldB.min.z),
                new Vector3(worldB.max.x, worldB.min.y, worldB.max.z),
                new Vector3(worldB.max.x, worldB.max.y, worldB.min.z),
                new Vector3(worldB.max.x, worldB.max.y, worldB.max.z)
            };

            foreach (var corner in corners)
            {
                Vector3 localCorner = parent.InverseTransformPoint(corner);
                if (first)
                {
                    localBounds = new Bounds(localCorner, Vector3.zero);
                    first = false;
                }
                else
                {
                    localBounds.Encapsulate(localCorner);
                }
            }
        }

        return localBounds;
    }

    private void UpdateLoadingUI(float progress, string message)
    {
        if (loadingProgressBar != null)
            loadingProgressBar.value = progress;

        if (loadingText != null)
            loadingText.text = message;

        Debug.Log($"[RuntimeModelLoader] {(progress * 100):F0}% - {message}");
    }

    public GameObject GetCurrentModel()
    {
        return currentModel;
    }

    public string GetCurrentModelPath()
    {
        return currentModelPath;
    }
}
