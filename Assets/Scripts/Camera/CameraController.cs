using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
        }
    }

    [Header("Camera Settings")]
    public float movementSpeed = 150f; // Joystick movement speed (increased for faster movement)
    public float lookSensitivity = 0.1f; // Mouse look sensitivity (decreased for slower look)
    public float zoomSpeed = 150f; // INCREASED for faster zoom response
    public float smoothSpeed = 12f; // INCREASED for smoother, more responsive camera
    public float scrollSensitivity = 3f; // INCREASED for faster scroll
    public float touchDragSensitivity = 0.1f; // INCREASED from 0.05 for more responsive dragging
    public float touchLookSensitivity = 0.003f; // Touch look sensitivity (decreased for slower look)

    [Header("Two-Finger Zoom")]
    [Tooltip("Pinch speed shared by Virtual Tour and Top View.")]
    [Range(0.5f, 6f)] public float pinchZoomSensitivity = 3.6f;
    [Tooltip("Higher values follow the fingers faster; lower values feel softer.")]
    [Range(5f, 30f)] public float pinchZoomSmoothing = 18f;
    
    [Header("Input Settings")]
    public bool enableMouseInput = false; // SET TO FALSE FOR TOUCH TESTING
    
    [Header("Virtual Joystick")]
    public VirtualJoystick movementJoystick;
    
    [Header("Virtual Tour Settings")]
    public Vector3 virtualTourStartPosition = new Vector3(-64.42599f, 1689.906f, -1695.566f);
    public Vector3 virtualTourStartRotation = new Vector3(5.192f, -179.347f, -0.001f);
    public float virtualTourFOV = 60f;
    
    [Header("Preset Settings")]
    public CameraAppSettingsPreset activePreset;
    
    [Header("Top View Settings")]
    public Vector3 topViewPosition = new Vector3(-20, 11991.48f, -1523);
    public Vector3 topViewRotation = new Vector3(90, 0, 0);
    public float topViewOrthographicSize = 6997.486f;
    
    private Camera cam;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float targetFOV;
    private float targetOrthographicSize;
    
    private Vector2 moveInput;
    private Vector2 lookDelta;
    private float zoomInput;
    private float pinchZoomInput;
    private float smoothedPinchZoomInput;
    
    private bool isVirtualTourMode = false; // Start in Top View mode
    private bool isMoving = false;
    private bool isLooking = false;
    
    private Vector2 lastMousePosition;
    private float rotationX = 0f;
    private float rotationY = 0f;
    
    public bool isDynamicModelConfigured = false;
    public float dynamicScaleRatio = 1f; // Cache ratio to scale movement/zoom speeds dynamically
    public float scaleMultiplier = 1f; // Scaler ratio multiplier (1 for real-life scale, 100 for old huge model)

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
            cam = Camera.main;

        if (activePreset != null)
        {
            ApplyPreset(activePreset);
        }
            
        // Start in Top View mode
        cam.orthographic = true;
        cam.orthographicSize = topViewOrthographicSize;
        targetOrthographicSize = topViewOrthographicSize;
        targetPosition = topViewPosition;
        targetRotation = Quaternion.Euler(topViewRotation);
        transform.position = targetPosition;
        transform.rotation = targetRotation;
        
        rotationX = topViewRotation.x;
        rotationY = topViewRotation.y;
        
        targetFOV = cam.fieldOfView;
        
        Debug.Log($"[CameraStart] Initialized in Top View mode");
        Debug.Log($"[CameraStart] Position: {transform.position}, Rotation: {transform.rotation.eulerAngles}");
        Debug.Log($"[CameraStart] rotationX: {rotationX}, rotationY: {rotationY}");
        Debug.Log($"[CameraStart] topViewPosition: {topViewPosition}");
    }
    
    public void ApplyPreset(CameraAppSettingsPreset preset)
    {
        if (preset == null) return;
        
        activePreset = preset;
        
        lookSensitivity = preset.lookSensitivity;
        smoothSpeed = preset.smoothSpeed;
        
        if (isDynamicModelConfigured && preset.useDynamicModelFitting)
        {
            movementSpeed = preset.movementSpeed * dynamicScaleRatio;
            zoomSpeed = preset.zoomSpeed * dynamicScaleRatio;
        }
        else
        {
            movementSpeed = preset.movementSpeed;
            zoomSpeed = preset.zoomSpeed;
        }
        
        if (!isDynamicModelConfigured || !preset.useDynamicModelFitting)
        {
            topViewPosition = preset.topViewPosition;
            topViewRotation = preset.topViewRotation;
            topViewOrthographicSize = preset.topViewOrthographicSize;
            
            virtualTourStartPosition = preset.virtualTourStartPosition;
            virtualTourStartRotation = preset.virtualTourStartRotation;
            virtualTourFOV = preset.virtualTourFOV;
        }

        // Apply to camera state
        if (isVirtualTourMode)
        {
            targetFOV = virtualTourFOV;
        }
        else
        {
            targetOrthographicSize = topViewOrthographicSize;
        }
        
        // GameManager will handle UI toggles via its own ApplyPreset or by observing activePreset
    }

    [ContextMenu("Save Current to Preset")]
    public void SaveToPreset()
    {
        if (activePreset == null)
        {
            Debug.LogError("No active preset assigned to save to!");
            return;
        }
        
        activePreset.movementSpeed = movementSpeed;
        activePreset.lookSensitivity = lookSensitivity;
        activePreset.zoomSpeed = zoomSpeed;
        activePreset.smoothSpeed = smoothSpeed;
        
        activePreset.topViewPosition = topViewPosition;
        activePreset.topViewRotation = topViewRotation;
        activePreset.topViewOrthographicSize = topViewOrthographicSize;
        
        activePreset.virtualTourStartPosition = virtualTourStartPosition;
        activePreset.virtualTourStartRotation = virtualTourStartRotation;
        activePreset.virtualTourFOV = virtualTourFOV;
        
        if (isVirtualTourMode)
        {
            activePreset.virtualTourFOV = targetFOV;
            virtualTourFOV = targetFOV; // Sync local field too
        }

        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(activePreset);
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log($"Saved current settings to preset: {activePreset.name}");
        #endif
    }
    
    void Update()
    {
        // Debug heartbeat to confirm script is running
        if (Time.frameCount % 120 == 0)
        {
            Debug.Log($"[CameraHeartbeat] Script is running on {gameObject.name}. Active Mode: {(isVirtualTourMode ? "VirtualTour" : "TopView")}");
        }
        
        HandleInput();
        UpdateCameraTransform();
    }
    
    void HandleInput()
    {
        // Reset zoom input each frame before accumulating
        zoomInput = 0f;
        pinchZoomInput = 0f;

        // Mouse input for editor testing
        if (enableMouseInput)
        {
            HandleMouseInput();
        }
        
        // Touch input for mobile
        HandleTouchInput();
        
        // Virtual joystick input
        HandleJoystickInput();
        
        // New Input System scroll wheel zoom
        if (Mouse.current != null)
        {
            Vector2 scrollDelta = Mouse.current.scroll.ReadValue();
            if (Mathf.Abs(scrollDelta.y) > 0.01f)
            {
                float normalizedScroll = scrollDelta.y / 120f; // Normalize
                zoomInput += normalizedScroll * 20f; // Increased sensitivity
                Debug.Log($"[ZoomInput] NEW Input scroll: {scrollDelta.y}, normalized: {normalizedScroll}, zoomInput: {zoomInput}");
            }
        }

        if (Mathf.Abs(zoomInput) > 0.0001f)
        {
            Debug.Log($"[ZoomInput] === TOTAL ZOOM: {zoomInput} ===");
        }
        else
        {
            // Debug every 2 seconds if no zoom detected
            if (Time.frameCount % 120 == 0)
            {
                Debug.Log("[ZoomInput] No scroll detected. Try scrolling in Game View!");
            }
        }
    }
    
    void HandleMouseInput()
    {
        if (Mouse.current == null)
        {
            return;
        }

        // Left click + drag for movement
        if (Mouse.current.leftButton.isPressed)
        {
            if (!isMoving)
            {
                isMoving = true;
                lastMousePosition = Mouse.current.position.ReadValue();
            }
            
            Vector2 currentMousePos = Mouse.current.position.ReadValue();
            Vector2 mouseDelta = currentMousePos - lastMousePosition;
            
            if (isVirtualTourMode)
            {
                // Left side: vertical movement only
                float speed = 2.5f * scaleMultiplier;
                if (activePreset != null)
                {
                    speed = (activePreset.movementSpeed / 1000f) * 2.5f * scaleMultiplier;
                }

                float dragSensitivity = 0.05f;
                Vector3 moveDir = Vector3.up * mouseDelta.y * dragSensitivity * Time.deltaTime * speed;
                targetPosition += moveDir;
            }
            else
            {
                // Top View: left side vertical drag pans forward/back only
                float worldUnitsPerPixel = (2.0f * cam.orthographicSize) / Screen.height;
                Vector3 worldForward = Quaternion.Euler(0, rotationY, 0) * Vector3.forward;

                Vector3 moveDir = -worldForward * mouseDelta.y * worldUnitsPerPixel;
                moveDir.y = 0;

                targetPosition += moveDir;
            }
            
            lastMousePosition = currentMousePos;
        }
        else
        {
            isMoving = false;
        }
        
        // Right click + drag for look around - ENABLED IN BOTH MODES
        if (Mouse.current.rightButton.isPressed)
        {
            if (!isLooking)
            {
                isLooking = true;
                lastMousePosition = Mouse.current.position.ReadValue();
                Debug.Log("Right-click look started");
            }
            
            Vector2 currentMousePos = Mouse.current.position.ReadValue();
            Vector2 mouseDelta = currentMousePos - lastMousePosition;
            
            if (mouseDelta.magnitude > 0.1f)
            {
                // Look around in both modes
                rotationY += mouseDelta.x * lookSensitivity * 0.02f; // DECREASED from 0.05f for slower look
                rotationX -= mouseDelta.y * lookSensitivity * 0.02f; // DECREASED from 0.05f for slower look
                rotationX = Mathf.Clamp(rotationX, -90f, 90f);
                
                targetRotation = Quaternion.Euler(rotationX, rotationY, 0);
                Debug.Log($"Look - Rot: ({rotationX:F1}, {rotationY:F1})");
            }
            
            lastMousePosition = currentMousePos;
        }
        else
        {
            if (isLooking)
            {
                Debug.Log("Right-click released");
            }
            isLooking = false;
        }
    }
    
    void HandleTouchInput()
    {
        if (Touchscreen.current == null) return;
        
        var touches = Touchscreen.current.touches;

        int firstFreeTouch = -1;
        int secondFreeTouch = -1;
        bool joystickTouchActive = false;

        // The joystick owns one finger. Keep that finger out of the camera
        // gesture recognizer so a second finger can look while moving.
        for (int i = 0; i < touches.Count; i++)
        {
            if (!touches[i].isInProgress)
                continue;

            int touchId = touches[i].touchId.ReadValue();
            Vector2 touchPosition = touches[i].position.ReadValue();
            bool isJoystickTouch = movementJoystick != null &&
                (movementJoystick.IsPointerActive(touchId) ||
                 (movementJoystick.IsDragging && IsTouchOnJoystick(touchPosition)));

            if (isJoystickTouch)
            {
                joystickTouchActive = true;
                continue;
            }

            if (firstFreeTouch < 0)
                firstFreeTouch = i;
            else if (secondFreeTouch < 0)
                secondFreeTouch = i;
        }

        // Joystick movement is processed separately in HandleJoystickInput.
        // A simultaneous free finger is dedicated to camera look.
        if (joystickTouchActive)
        {
            if (firstFreeTouch >= 0)
            {
                ApplyTouchLook(touches[firstFreeTouch].delta.ReadValue());
            }
            return;
        }

        // Two free fingers retain the existing pinch-to-zoom gesture.
        if (firstFreeTouch >= 0 && secondFreeTouch >= 0)
        {
            var touch0 = touches[firstFreeTouch];
            var touch1 = touches[secondFreeTouch];

            Vector2 touch0Pos = touch0.position.ReadValue();
            Vector2 touch1Pos = touch1.position.ReadValue();
            Vector2 touch0Delta = touch0.delta.ReadValue();
            Vector2 touch1Delta = touch1.delta.ReadValue();

            Vector2 touch0Prev = touch0Pos - touch0Delta;
            Vector2 touch1Prev = touch1Pos - touch1Delta;

            float previousDistance = (touch0Prev - touch1Prev).magnitude;
            float currentDistance = (touch0Pos - touch1Pos).magnitude;

            // Normalize the gesture so its speed is consistent on phones and
            // tablets instead of depending on raw screen pixels.
            float referencePixels = Mathf.Max(240f, Mathf.Min(Screen.width, Screen.height));
            float normalizedDelta = (currentDistance - previousDistance) / referencePixels;
            pinchZoomInput += normalizedDelta * pinchZoomSensitivity;
            return;
        }

        if (firstFreeTouch < 0)
            return;

        // One free finger: left side moves vertically, right side looks.
        var touch = touches[firstFreeTouch];
        Vector2 touchPos = touch.position.ReadValue();
        Vector2 touchDelta = touch.delta.ReadValue();
        float leftZoneEnd = Screen.width * 0.4f;

        if (touchPos.x < leftZoneEnd)
        {
            if (isVirtualTourMode)
            {
                float speed = 2.5f * scaleMultiplier;
                if (activePreset != null)
                {
                    speed = (activePreset.movementSpeed / 1000f) * 2.5f * scaleMultiplier;
                }

                Vector3 moveDir = Vector3.up * touchDelta.y * touchDragSensitivity * Time.deltaTime * speed;
                targetPosition += moveDir;
            }
            else
            {
                float worldUnitsPerPixel = (2.0f * cam.orthographicSize) / Screen.height;
                Vector3 worldForward = Quaternion.Euler(0, rotationY, 0) * Vector3.forward;

                Vector3 moveDir = -worldForward * touchDelta.y * worldUnitsPerPixel;
                moveDir.y = 0;

                targetPosition += moveDir;
            }
        }
        else
        {
            ApplyTouchLook(touchDelta);
        }
    }

    private void ApplyTouchLook(Vector2 touchDelta)
    {
        rotationY += touchDelta.x * lookSensitivity * touchLookSensitivity;
        rotationX -= touchDelta.y * lookSensitivity * touchLookSensitivity;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);
        targetRotation = Quaternion.Euler(rotationX, rotationY, 0f);
    }
    
    bool IsTouchOnJoystick(Vector2 touchPosition)
    {
        if (movementJoystick == null || movementJoystick.joystickBackground == null)
            return false;
        
        RectTransform joystickRect = movementJoystick.joystickBackground;
        return RectTransformUtility.RectangleContainsScreenPoint(joystickRect, touchPosition, null);
    }
    
    void HandleJoystickInput()
    {
        if (movementJoystick == null) return;
        
        Vector2 joystickInput = movementJoystick.GetInputVector();
        
        if (joystickInput.magnitude > 0.1f)
        {
            Debug.Log($"[Joystick] Input: ({joystickInput.x:F2}, {joystickInput.y:F2}), Mode: {(isVirtualTourMode ? "VirtualTour" : "TopView")}");
            
            if (isVirtualTourMode)
            {
                // Joystick: horizontal movement only (strafe + forward/back, no vertical)
                float speed = 1.5f * scaleMultiplier;
                if (activePreset != null)
                {
                    speed = (activePreset.movementSpeed / 1000f) * 1.5f * scaleMultiplier;
                }

                Vector3 flatRight = transform.right;
                flatRight.y = 0f;
                flatRight.Normalize();

                Vector3 flatForward = transform.forward;
                flatForward.y = 0f;
                flatForward.Normalize();

                Vector3 moveDir = flatRight * joystickInput.x * Time.deltaTime * speed;
                moveDir += flatForward * joystickInput.y * Time.deltaTime * speed;
                targetPosition += moveDir;

                Debug.Log($"[Joystick] VirtualTour moveDir: {moveDir}, newTarget: {targetPosition}");
            }
            else
            {
                // Top View: Pan proportionally to screen size/orthographic size
                float panSpeed = topViewOrthographicSize * 0.5f; 
                
                Vector3 worldForward = Quaternion.Euler(0, rotationY, 0) * Vector3.forward;
                Vector3 worldRight = Quaternion.Euler(0, rotationY, 0) * Vector3.right;
                
                Vector3 moveDir = worldRight * joystickInput.x * Time.deltaTime * panSpeed;
                moveDir += worldForward * joystickInput.y * Time.deltaTime * panSpeed;
                moveDir.y = 0;
                
                targetPosition += moveDir;
            }
        }
    }
    
    void UpdateCameraTransform()
    {
        ApplySmoothPinchZoom();

        // Handle zoom - HIGH SPEED
        if (Mathf.Abs(zoomInput) > 0.0001f)
        {
            Debug.Log($"[Zoom] Processing zoomInput: {zoomInput}, Mode: {(isVirtualTourMode ? "VirtualTour" : "TopView")}");
            
            if (isVirtualTourMode)
            {
                // Virtual tour: adjust field of view (MUCH FASTER zoom in virtual tour)
                float oldFOV = targetFOV;
                targetFOV -= zoomInput * zoomSpeed * 1.5f;
                targetFOV = Mathf.Clamp(targetFOV, 20f, 90f);
                Debug.Log($"[Zoom] Virtual Tour FOV: {oldFOV:F1} → {targetFOV:F1}, Current: {cam.fieldOfView:F1}");
            }
            else
            {
                // Top view: adjust orthographic size for zoom
                if (cam.orthographic)
                {
                    float oldSize = targetOrthographicSize;
                    // Scale zoom based on current size to make it feel consistent across zoom levels
                    float sizeFactor = targetOrthographicSize / topViewOrthographicSize;
                    float zoomAmount = zoomInput * zoomSpeed * 1f * Mathf.Max(sizeFactor, 0.2f); // MUCH SLOWER zoom in top view
                    
                    targetOrthographicSize -= zoomAmount;
                    
                    float minSize = topViewOrthographicSize * 0.1f;
                    float maxSize = topViewOrthographicSize * 2.5f;
                    targetOrthographicSize = Mathf.Clamp(targetOrthographicSize, minSize, maxSize);
                    
                    Debug.Log($"[Zoom] Top View Size: {oldSize:F1} → {targetOrthographicSize:F1}, Current: {cam.orthographicSize:F1}, ZoomAmount: {zoomAmount:F1}");
                }
            }
        }
        
        // Detect large position changes
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        
        // Smooth camera transitions
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
        
        if (isVirtualTourMode)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * smoothSpeed);
        }
        else if (cam.orthographic)
        {
            // Always ensure targetOrthographicSize is initialized
            if (targetOrthographicSize <= 0) targetOrthographicSize = cam.orthographicSize;
            
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetOrthographicSize, Time.deltaTime * smoothSpeed);
        }
    }

    private void ApplySmoothPinchZoom()
    {
        float blend = 1f - Mathf.Exp(-pinchZoomSmoothing * Time.unscaledDeltaTime);
        smoothedPinchZoomInput = Mathf.Lerp(smoothedPinchZoomInput, pinchZoomInput, blend);

        if (Mathf.Abs(pinchZoomInput) < 0.0001f &&
            Mathf.Abs(smoothedPinchZoomInput) < 0.0001f)
        {
            smoothedPinchZoomInput = 0f;
            return;
        }

        // Multiplicative zoom gives both modes the same proportional movement.
        // Spreading fingers zooms in; bringing them together zooms out.
        float zoomFactor = Mathf.Exp(-smoothedPinchZoomInput);

        if (isVirtualTourMode)
        {
            targetFOV = Mathf.Clamp(targetFOV * zoomFactor, 20f, 90f);
        }
        else if (cam != null && cam.orthographic)
        {
            float minSize = topViewOrthographicSize * 0.1f;
            float maxSize = topViewOrthographicSize * 2.5f;
            targetOrthographicSize = Mathf.Clamp(
                targetOrthographicSize * zoomFactor,
                minSize,
                maxSize);
        }
    }

    public void SwitchToVirtualTour()
    {
        isVirtualTourMode = true;
        smoothedPinchZoomInput = 0f;
        
        if (cam == null)
            cam = GetComponent<Camera>();
        
        if (cam != null)
        {
            cam.orthographic = false;
            targetFOV = virtualTourFOV;
            cam.fieldOfView = virtualTourFOV;
        }
        
        targetPosition = virtualTourStartPosition;
        rotationX = virtualTourStartRotation.x;
        rotationY = virtualTourStartRotation.y;
        targetRotation = Quaternion.Euler(virtualTourStartRotation);
    }

    public void SwitchToTopView()
    {
        isVirtualTourMode = false;
        smoothedPinchZoomInput = 0f;
        
        if (cam == null)
            cam = GetComponent<Camera>();
        
        if (cam != null)
        {
            cam.orthographic = true;
            targetOrthographicSize = topViewOrthographicSize;
            cam.orthographicSize = topViewOrthographicSize;
        }
        
        targetPosition = topViewPosition;
        rotationX = topViewRotation.x;
        rotationY = topViewRotation.y;
        targetRotation = Quaternion.Euler(topViewRotation);
    }

    public void ApplyViewImmediately()
    {
        transform.position = targetPosition;
        transform.rotation = targetRotation;

        if (cam == null)
            cam = GetComponent<Camera>();

        if (cam == null)
            return;

        if (isVirtualTourMode)
        {
            cam.fieldOfView = targetFOV;
        }
        else if (cam.orthographic)
        {
            if (targetOrthographicSize <= 0f)
                targetOrthographicSize = topViewOrthographicSize;
            cam.orthographicSize = targetOrthographicSize;
        }
    }

    public void UpdateForDynamicModel(Bounds bounds, Vector3 tourStartPos, Vector3 tourStartRot, float topViewSize)
    {
        isDynamicModelConfigured = true;
        
        float boundsMagnitude = bounds.size.magnitude;
        scaleMultiplier = (boundsMagnitude > 1000f) ? 100f : 1f;
        
        // Calculate the scale ratio of this model compared to the preset's native size
        float baseTopViewSize = 7000f; // Native/reference top-view size of the preset
        if (activePreset != null)
        {
            baseTopViewSize = activePreset.topViewOrthographicSize;
        }
        
        // Avoid division by zero
        if (baseTopViewSize <= 0.1f) baseTopViewSize = 7000f;
        
        dynamicScaleRatio = topViewSize / baseTopViewSize;
        
        // Scale speeds proportionally so movement/zoom feels identical on small and large models
        if (activePreset != null && activePreset.useDynamicModelFitting)
        {
            movementSpeed = activePreset.movementSpeed * dynamicScaleRatio;
            zoomSpeed = activePreset.zoomSpeed * dynamicScaleRatio;
        }
        else if (activePreset != null)
        {
            movementSpeed = activePreset.movementSpeed;
            zoomSpeed = activePreset.zoomSpeed;
        }

        // Ensure speed bounds are sane
        movementSpeed = Mathf.Max(movementSpeed, 0.5f);
        zoomSpeed = Mathf.Max(zoomSpeed, 0.05f);
        
        float heightFactor = 3f;
        Vector3 dynamicTopRotation = new Vector3(90, 0, 0);
        if (activePreset != null)
        {
            heightFactor = activePreset.topViewHeightFactor;
            dynamicTopRotation = activePreset.topViewRotationDynamic;
        }

        float heightOffset = Mathf.Max(bounds.size.y * heightFactor, 10f); // Scale with model height dynamically
        topViewPosition = new Vector3(bounds.center.x, bounds.max.y + heightOffset, bounds.center.z);
        topViewRotation = dynamicTopRotation;
        topViewOrthographicSize = topViewSize;
        virtualTourStartPosition = tourStartPos;
        virtualTourStartRotation = tourStartRot;

        // If in top view mode (default at start), immediately apply these as active targets
        if (!isVirtualTourMode)
        {
            targetPosition = topViewPosition;
            targetOrthographicSize = topViewOrthographicSize;
            if (cam == null)
                cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = topViewOrthographicSize;
            }
            transform.position = topViewPosition;
            transform.rotation = Quaternion.Euler(topViewRotation);
            
            rotationX = topViewRotation.x;
            rotationY = topViewRotation.y;
            targetRotation = Quaternion.Euler(topViewRotation);
        }
    }
    
    // TEMPORARY: Public methods for testing zoom with buttons
    public void TestZoomIn()
    {
        // Directly modify the camera for immediate zoom
        if (isVirtualTourMode)
        {
            targetFOV -= 5f;
            targetFOV = Mathf.Clamp(targetFOV, 20f, 90f);
            Debug.Log($"[TEST] Zoom IN - FOV: {targetFOV}");
        }
        else
        {
            if (cam.orthographic)
            {
                targetOrthographicSize -= topViewOrthographicSize * 0.05f;
                float minSize = topViewOrthographicSize * 0.1f;
                float maxSize = topViewOrthographicSize * 2.5f;
                targetOrthographicSize = Mathf.Clamp(targetOrthographicSize, minSize, maxSize);
                Debug.Log($"[TEST] Zoom IN - Orthographic Size: {targetOrthographicSize}");
            }
        }
    }
    
    public void TestZoomOut()
    {
        if (isVirtualTourMode)
        {
            targetFOV += 5f;
            targetFOV = Mathf.Clamp(targetFOV, 20f, 90f);
            Debug.Log($"[TEST] Zoom OUT - FOV: {targetFOV}");
        }
        else
        {
            if (cam.orthographic)
            {
                targetOrthographicSize += topViewOrthographicSize * 0.05f;
                float minSize = topViewOrthographicSize * 0.1f;
                float maxSize = topViewOrthographicSize * 2.5f;
                targetOrthographicSize = Mathf.Clamp(targetOrthographicSize, minSize, maxSize);
                Debug.Log($"[TEST] Zoom OUT - Orthographic Size: {targetOrthographicSize}");
            }
        }
    }
}
