using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("References")]
    [Tooltip("First-person camera. If left empty, Camera.main is used automatically.")]
    [SerializeField] private Camera playerCamera;

    [Header("Interaction")]
    [SerializeField, Min(0f)] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("New Input System")]
    [SerializeField] private InputActionReference interactAction;

    // Center of the viewport = where the crosshair sits on screen.
    private static readonly Vector3 ScreenCenter = new Vector3(0.5f, 0.5f, 0f);

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        if (interactAction == null)
        {
            return;
        }

        interactAction.action.performed += OnInteractPerformed;
        interactAction.action.Enable();
    }

    private void OnDisable()
    {
        if (interactAction == null)
        {
            return;
        }

        interactAction.action.performed -= OnInteractPerformed;
        interactAction.action.Disable();
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        TryInteract();
    }

    private void TryInteract()
    {
        if (playerCamera == null)
        {
            Debug.LogError(
                "PlayerInteractor has no camera assigned and no Camera.main was found.",
                this
            );
            return;
        }

        // Ray straight through the crosshair (center of the screen).
        Ray ray = playerCamera.ViewportPointToRay(ScreenCenter);

        Debug.DrawRay(
            ray.origin,
            ray.direction * interactionDistance,
            Color.red,
            2f
        );

        bool hitSomething = Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionDistance,
            interactableLayer,
            QueryTriggerInteraction.Collide
        );

        if (!hitSomething)
        {
            return;
        }

        IInteractable interactable = FindInteractable(hit.collider);

        if (interactable == null)
        {
            return;
        }

        interactable.Interact();
    }

    private IInteractable FindInteractable(Collider hitCollider)
    {
        MonoBehaviour[] behaviours =
            hitCollider.GetComponentsInParent<MonoBehaviour>();

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IInteractable interactable)
            {
                return interactable;
            }
        }

        return null;
    }

    private void OnDrawGizmosSelected()
    {
        Camera cam = playerCamera != null ? playerCamera : Camera.main;

        if (cam == null)
        {
            return;
        }

        Ray ray = cam.ViewportPointToRay(ScreenCenter);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            ray.origin,
            ray.origin + ray.direction * interactionDistance
        );
    }
}
