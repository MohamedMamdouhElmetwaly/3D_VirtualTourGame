using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Central, single point of input for object interaction.
///
/// Detects a "tap" (a quick press + release with little movement so it is not
/// confused with a camera drag), raycasts from the camera into the world, finds
/// the first <see cref="IInteractable"/> on (or above) the object that was hit,
/// and forwards the interaction to it.
///
/// Works with touch (mobile) and falls back to the mouse for in-Editor testing,
/// matching the direct-device input style already used by CameraController.
/// </summary>
[DefaultExecutionOrder(-50)]
public class SelectionManager : MonoBehaviour
{
    [Header("Camera")]
    [Tooltip("Camera used for raycasting. Falls back to Camera.main if left empty.")]
    [SerializeField] private Camera raycastCamera;

    [Header("Raycast")]
    [Tooltip("Only objects on these layers can be interacted with. Default = Everything.")]
    [SerializeField] private LayerMask interactableMask = ~0;
    [Tooltip("Max raycast distance. The apartment world is large-scale, so keep this high.")]
    [SerializeField] private float maxRayDistance = 100000f;

    [Header("Tap Detection")]
    [Tooltip("Max seconds between press and release to still count as a tap.")]
    [SerializeField] private float tapMaxDuration = 0.4f;
    [Tooltip("Max finger/cursor travel in pixels for a tap. Larger = treated as a drag and ignored.")]
    [SerializeField] private float tapMaxMovement = 20f;

    [Header("Debug")]
    [SerializeField] private bool logSelections = false;

    /// <summary>Raised after any interactable is successfully tapped.</summary>
    public System.Action<IInteractable, SelectionContext> OnInteractableSelected;

    private bool tracking;
    private Vector2 pressPosition;
    private double pressTime;
    private int activeTouchId = -1;

    private void Awake()
    {
        if (raycastCamera == null)
            raycastCamera = Camera.main;
    }

    private void Update()
    {
        if (!EnsureCamera())
            return;

        Vector2 position;
        bool pressedThisFrame;
        bool releasedThisFrame;

        var touch = Touchscreen.current;
        if (touch != null && (touch.primaryTouch.press.isPressed ||
                              touch.primaryTouch.press.wasReleasedThisFrame))
        {
            // Touch device (mobile) takes priority.
            position = touch.primaryTouch.position.ReadValue();
            pressedThisFrame = touch.primaryTouch.press.wasPressedThisFrame;
            releasedThisFrame = touch.primaryTouch.press.wasReleasedThisFrame;
            if (pressedThisFrame)
                activeTouchId = touch.primaryTouch.touchId.ReadValue();
        }
        else
        {
            // Mouse fallback for Editor / desktop testing.
            var mouse = Mouse.current;
            if (mouse == null)
                return;

            position = mouse.position.ReadValue();
            pressedThisFrame = mouse.leftButton.wasPressedThisFrame;
            releasedThisFrame = mouse.leftButton.wasReleasedThisFrame;
            activeTouchId = -1;
        }

        if (pressedThisFrame)
        {
            // Never start a selection that begins on top of UI (sliders, buttons...).
            if (IsPointerOverUI(activeTouchId))
            {
                Debug.Log("[SelectionManager] Press started over UI, ignoring");
                tracking = false;
                return;
            }

            tracking = true;
            pressPosition = position;
            pressTime = Time.unscaledTimeAsDouble;
            Debug.Log($"[SelectionManager] Tap started at {pressPosition}");
        }
        else if (releasedThisFrame && tracking)
        {
            tracking = false;

            double duration = Time.unscaledTimeAsDouble - pressTime;
            float movement = Vector2.Distance(position, pressPosition);

            Debug.Log($"[SelectionManager] Tap released. Duration: {duration:F2}s (max: {tapMaxDuration}s), Movement: {movement:F1}px (max: {tapMaxMovement}px)");

            if (duration <= tapMaxDuration && movement <= tapMaxMovement)
            {
                TrySelectAt(position);
            }
            else
            {
                Debug.Log($"[SelectionManager] Tap ignored - doesn't meet tap criteria");
            }
        }
    }

    private void TrySelectAt(Vector2 screenPosition)
    {
        Ray ray = raycastCamera.ScreenPointToRay(screenPosition);
        Debug.Log($"[SelectionManager] Raycast from screen position: {screenPosition}");

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, interactableMask, QueryTriggerInteraction.Ignore))
        {
            Debug.Log($"[SelectionManager] Raycast hit: {hit.collider.gameObject.name}");
            
            // Walk up the hierarchy so child colliders still resolve to the owning behaviour.
            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                Debug.Log($"[SelectionManager] ✓ Found IInteractable: {((Component)interactable).gameObject.name}");
                var ctx = new SelectionContext(this, raycastCamera, hit, screenPosition);
                interactable.OnInteract(ctx);
                OnInteractableSelected?.Invoke(interactable, ctx);

                if (logSelections)
                    Debug.Log($"[SelectionManager] Interacted with '{((Component)interactable).gameObject.name}'.",
                        (Component)interactable);
                return;
            }
            else
            {
                Debug.LogWarning($"[SelectionManager] ⚠ Hit collider '{hit.collider.gameObject.name}' but found no IInteractable component");
            }
        }
        else
        {
            Debug.Log("[SelectionManager] Raycast did not hit anything");
        }

        if (logSelections)
            Debug.Log("[SelectionManager] Tap did not hit an interactable.");
    }

    private bool EnsureCamera()
    {
        if (raycastCamera != null)
            return true;

        raycastCamera = Camera.main;
        return raycastCamera != null;
    }

    private static bool IsPointerOverUI(int touchId)
    {
        var es = EventSystem.current;
        if (es == null)
            return false;

        return touchId >= 0 ? es.IsPointerOverGameObject(touchId) : es.IsPointerOverGameObject();
    }
}
