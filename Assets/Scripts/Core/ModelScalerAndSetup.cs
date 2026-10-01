using UnityEngine;

/// <summary>
/// Automatically scales the camera to fit any model, calculates a suitable virtual tour
/// starting position, and configures lights with colliders and interactive controllers.
/// </summary>
[DefaultExecutionOrder(-20)]
public class ModelScalerAndSetup : MonoBehaviour
{
    [Header("Model Root")]
    [Tooltip("The root of the loaded model. If left empty, it will automatically find the best match.")]
    [SerializeField] private GameObject modelRoot;

    [Header("Camera Reference")]
    [Tooltip("The camera controller to configure. If left empty, it will find it automatically.")]
    [SerializeField] private CameraController cameraController;

    private void Awake()
    {
        if (cameraController == null)
            cameraController = Object.FindAnyObjectByType<CameraController>();

        if (modelRoot == null)
            FindModelRoot();

        bool useDynamic = true;
        if (cameraController != null && cameraController.activePreset != null)
        {
            useDynamic = cameraController.activePreset.useDynamicModelFitting;
        }

        if (useDynamic)
        {
            if (modelRoot != null)
            {
                SetupModelAndCamera();
            }
            else
            {
                Debug.LogWarning("[ModelScalerAndSetup] No model root found in the scene to configure.");
            }
        }
        else
        {
            Debug.Log("[ModelScalerAndSetup] Dynamic model fitting is disabled by the active preset. Camera settings from preset will be used directly.");
            if (cameraController != null && cameraController.activePreset != null)
            {
                cameraController.ApplyPreset(cameraController.activePreset);
            }
        }
    }

    public void FindModelRoot()
    {
        // Try to find the root GameObject by known names or tags
        modelRoot = GameObject.Find("ApartmentModel");
        if (modelRoot != null) return;

        // Fallback: Search for any root GameObject with "Model", "Building", "House", "Apartment" in the name
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in roots)
        {
            string nameLower = root.name.ToLower();
            if (nameLower.Contains("model") || nameLower.Contains("building") || 
                nameLower.Contains("house") || nameLower.Contains("apartment"))
            {
                modelRoot = root;
                return;
            }
        }

        // Ultimate fallback: Use the first root that isn't a known system object
        foreach (var root in roots)
        {
            string name = root.name;
            if (name != "Main Camera" && name != "Directional Light" && name != "Global Volume" &&
                name != "MainCanvas" && name != "Canvas" && name != "EventSystem" &&
                name != "SelectionManager" && name != "GameManagerObject" && name != "Floor" &&
                name != "InteractionUICanvas")
            {
                modelRoot = root;
                return;
            }
        }
    }

    public void SetupModelAndCamera()
    {
        Debug.Log($"[ModelScalerAndSetup] Configuring for model: '{modelRoot.name}'");

        // 1. Calculate Bounds
        var renderers = modelRoot.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning("[ModelScalerAndSetup] Model has no renderers, cannot calculate bounds.");
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float boundsMagnitude = bounds.size.magnitude;
        float scaleMultiplier = (boundsMagnitude > 1000f) ? 100f : 1f;
        Debug.Log($"[ModelScalerAndSetup] Bounds: Center={bounds.center}, Size={bounds.size}, Scale multiplier={scaleMultiplier}");

        // 2. Configure Camera Top View Position & Orthographic Size
        float sizeBasedOnZ = bounds.size.z / 2f;
        // Assume standard aspect ratio fallback for portrait view bounds calculations
        float aspect = 1080f / 1920f;
        if (Camera.main != null)
        {
            aspect = (float)Screen.width / Screen.height;
        }
        float sizeBasedOnX = (bounds.size.x / 2f) / aspect;
        
        float topViewPadding = 1.15f;
        if (cameraController != null && cameraController.activePreset != null)
        {
            topViewPadding = cameraController.activePreset.topViewPadding;
        }
        float optimalOrthographicSize = Mathf.Max(sizeBasedOnZ, sizeBasedOnX) * topViewPadding;

        // 3. Calculate Virtual Tour Start Position
        Vector3 tourStartPos = Vector3.zero;
        Vector3 tourStartRot = Vector3.zero;
        CalculateVirtualTourStart(bounds, scaleMultiplier, out tourStartPos, out tourStartRot);

        // 4. Apply to CameraController
        if (cameraController != null)
        {
            cameraController.UpdateForDynamicModel(bounds, tourStartPos, tourStartRot, optimalOrthographicSize);
            Debug.Log($"[ModelScalerAndSetup] Camera scaled successfully. Orthographic Size: {optimalOrthographicSize}, Virtual Tour Start: {tourStartPos}");
        }
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

    public void CalculateVirtualTourStart(Bounds bounds, float scaleMultiplier, out Vector3 startPos, out Vector3 startRot)
    {
        // Get values from active preset if available
        float tourEyeHeight = 1.25f;
        if (cameraController != null && cameraController.activePreset != null)
        {
            tourEyeHeight = cameraController.activePreset.virtualTourEyeHeight;
        }

        // We want the virtual tour to always start INSIDE the model (in a spacious, central area).
        // Using the horizontal center of the bounding box is the most reliable way to achieve this.
        startPos = bounds.center;
        startPos.y = bounds.min.y + (tourEyeHeight * scaleMultiplier);
        
        // Face forward/inward
        startRot = new Vector3(0f, 180f, 0f);
        
        Debug.Log($"[CalculateVirtualTourStart] Configured starting position inside the model: {startPos}");
    }

}
