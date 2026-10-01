using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject loadingScreen;
    public GameObject mainUI;
    public Slider progressBar;
    public Text progressText;
    
    public Button virtualTourButton;
    public Button topViewButton;
    
    [Header("Camera Reference")]
    public CameraController cameraController;
    
    [Header("Joystick Reference")]
    public GameObject virtualJoystick;
    
    [Header("Zoom Buttons Reference")]
    public GameObject zoomInButton;
    public GameObject zoomOutButton;
    
    [Header("Movement Guide Reference")]
    public MovementGuideUI movementGuideUI;
    public Toggle guideToggle; // Reference to the GuideToggle button

    [Header("Touch Indicators Reference")]
    public TouchControlsUI touchControlsUI;

    [Header("Exit Button Reference")]
    public GameObject exitButton;
    
    private bool isLoadingComplete = false;
    private bool gameStarted = false;
    
    void Start()
    {
        // Initially hide everything
        if (loadingScreen != null)
            loadingScreen.SetActive(false);
            
        if (mainUI != null)
            mainUI.SetActive(false);
            
        // Explicitly hide joystick and zoom buttons during loading
        if (virtualJoystick != null)
            virtualJoystick.SetActive(false);
            
        if (zoomInButton != null)
            zoomInButton.SetActive(false);
            
        if (zoomOutButton != null)
            zoomOutButton.SetActive(false);
        
        // Hide guide toggle during loading
        if (guideToggle != null)
            guideToggle.gameObject.SetActive(false);

        if (exitButton != null)
            exitButton.SetActive(false);

        BackToUploadButton.Instance?.SetVisible(false);
        
        // Setup button listeners
        if (virtualTourButton != null)
            virtualTourButton.onClick.AddListener(OnVirtualTourClicked);
            
        if (topViewButton != null)
            topViewButton.onClick.AddListener(OnTopViewClicked);
            
        // Note: Loading sequence will be started by ModelUploadUI after model is loaded
    }
    
    /// <summary>
    /// Called by ModelUploadUI after the model has been uploaded and loaded
    /// </summary>
    public void StartGameAfterUpload()
    {
        if (gameStarted)
            return;
            
        gameStarted = true;
        
        if (loadingScreen != null)
            loadingScreen.SetActive(true);
            
        StartCoroutine(LoadingSequence());
    }
    
    IEnumerator LoadingSequence()
    {
        float progress = 0f;
        
        // Phase 1: Fake progress while model might be starting to load
        while (progress < 0.8f)
        {
            progress += Time.deltaTime * 0.4f; // Slower fake progress
            
            UpdateLoadingUI(progress);
            yield return null;
        }
        
        // Phase 2: Wait for actual model loading to complete
        float waitTime = 0f;
        const float maxWaitSeconds = 60f;

        if (RuntimeModelLoader.Instance != null)
        {
            while (RuntimeModelLoader.Instance.IsLoading)
            {
                if (RuntimeModelLoader.Instance.LoadFailed)
                {
                    Debug.LogError($"[GameManager] Model load failed: {RuntimeModelLoader.Instance.LastLoadError}");
                    break;
                }

                waitTime += Time.deltaTime;
                if (waitTime >= maxWaitSeconds)
                {
                    Debug.LogError("[GameManager] Timed out waiting for model to load");
                    break;
                }

                if (progress < 0.98f)
                {
                    progress += Time.deltaTime * 0.05f;
                    UpdateLoadingUI(progress);
                }

                yield return null;
            }
        }
        
        // Phase 3: Finalize
        progress = 1f;
        UpdateLoadingUI(progress);
        
        yield return new WaitForSeconds(0.5f);

        bool modelLoaded = RuntimeModelLoader.Instance != null &&
                           RuntimeModelLoader.Instance.GetCurrentModel() != null;

        if (!modelLoaded)
        {
            if (loadingScreen != null)
                loadingScreen.SetActive(false);

            if (progressText != null)
            {
                string error = RuntimeModelLoader.Instance != null
                    ? RuntimeModelLoader.Instance.LastLoadError
                    : "Model loader not found";
                progressText.text = string.IsNullOrEmpty(error) ? "Failed to load model" : error;
            }

            Debug.LogError("[GameManager] Could not start tour because model failed to load");
            yield break;
        }
        
        isLoadingComplete = true;
        
        if (loadingScreen != null)
            loadingScreen.SetActive(false);
            
        if (mainUI != null)
            mainUI.SetActive(true);
            
        // Show UI elements based on preset if available
        if (cameraController != null && cameraController.activePreset != null)
        {
            ApplyPreset(cameraController.activePreset);
        }
        else
        {
            // Fallback defaults if no preset
            if (virtualJoystick != null)
                virtualJoystick.SetActive(true);
                
            if (zoomInButton != null)
                zoomInButton.SetActive(true);
                
            if (zoomOutButton != null)
                zoomOutButton.SetActive(true);
        }
        
        // Show guide toggle after loading completes
        if (guideToggle != null)
            guideToggle.gameObject.SetActive(true);
            
        // Show guide automatically after loading and ensure toggle is enabled
        if (movementGuideUI != null)
        {
            movementGuideUI.SetGuideEnabled(true);
        }

        if (exitButton != null)
            exitButton.SetActive(true);

        BackToUploadButton.Instance?.SetVisible(true);
        ExitButton.Instance?.SetVisible(true);
    }

    private void UpdateLoadingUI(float progress)
    {
        progress = Mathf.Clamp01(progress);
        
        if (progressBar != null)
            progressBar.value = progress;
            
        if (progressText != null)
            progressText.text = $"Loading... {(int)(progress * 100)}%";
    }
    
    public void OnVirtualTourClicked()
    {
        if (cameraController != null)
        {
            cameraController.SwitchToVirtualTour();
            Debug.Log("Switched to Virtual Tour mode");
        }
    }
    
    public void OnTopViewClicked()
    {
        if (cameraController != null)
        {
            cameraController.SwitchToTopView();
            Debug.Log("Switched to Top View mode");
        }
    }

    public void ApplyPreset(CameraAppSettingsPreset preset)
    {
        if (preset == null) return;
        
        // 1. Tell CameraController to apply its settings
        if (cameraController != null)
        {
            cameraController.ApplyPreset(preset);
        }
        
        // 2. Handle UI toggles
        if (virtualJoystick != null)
            virtualJoystick.SetActive(preset.joystickEnabled);
            
        if (zoomInButton != null)
            zoomInButton.SetActive(preset.zoomButtonsEnabled);
            
        if (zoomOutButton != null)
            zoomOutButton.SetActive(preset.zoomButtonsEnabled);

            
        if (touchControlsUI != null)
        {
            touchControlsUI.gameObject.SetActive(preset.touchIndicatorsEnabled);
            touchControlsUI.showIndicators = preset.touchIndicatorsEnabled;
        }
            
        if (movementGuideUI != null)
            movementGuideUI.gameObject.SetActive(preset.movementGuideEnabled);
            
        Debug.Log($"Applied app configuration from preset: {preset.name}");
    }
}
