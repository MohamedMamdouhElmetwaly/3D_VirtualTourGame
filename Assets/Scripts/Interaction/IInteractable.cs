using UnityEngine;

/// <summary>
/// Implemented by anything the user can tap/click in the apartment scene
/// (doors, light switches, walls, furniture, ...).
///
/// The <see cref="SelectionManager"/> raycasts from the camera and calls
/// <see cref="OnInteract"/> on the first IInteractable it finds when walking
/// up the hierarchy from the collider that was hit. This keeps the system
/// polymorphic and trivially extendable: add a new behaviour, implement this
/// interface, and it "just works" with the existing SelectionManager.
/// </summary>
public interface IInteractable
{
    void OnInteract(SelectionContext context);
}

/// <summary>
/// Lightweight, immutable data handed to an interactable when it is tapped.
/// Extend this struct later (e.g. add tap count, double-tap flag, etc.)
/// without ever having to change the IInteractable signature.
/// </summary>
public readonly struct SelectionContext
{
    public readonly SelectionManager Manager;
    public readonly Camera Camera;
    public readonly RaycastHit Hit;
    public readonly Vector2 ScreenPosition;

    public SelectionContext(SelectionManager manager, Camera camera, RaycastHit hit, Vector2 screenPosition)
    {
        Manager = manager;
        Camera = camera;
        Hit = hit;
        ScreenPosition = screenPosition;
    }
}
