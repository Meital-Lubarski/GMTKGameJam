using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform interactionOrigin;

    [Header("Interaction")]
    [SerializeField, Min(0f)] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("New Input System")]
    [SerializeField] private InputActionReference interactAction;

    private void Awake()
    {
        if (interactionOrigin == null)
        {
            Camera mainCamera = Camera.main;

            if (mainCamera != null)
            {
                interactionOrigin = mainCamera.transform;
            }
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
        Debug.Log("E pressed - trying to interact.");

        if (interactionOrigin == null)
        {
            Debug.LogError("Interaction Origin is null.");
            return;
        }

        Ray ray = new Ray(
            interactionOrigin.position,
            interactionOrigin.forward
        );

        Debug.DrawRay(
            interactionOrigin.position,
            interactionOrigin.forward * interactionDistance,
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
            Debug.Log("Raycast did not hit an interactable object.");
            return;
        }

        Debug.Log("Raycast hit: " + hit.collider.name);

        IInteractable interactable = FindInteractable(hit.collider);

        if (interactable == null)
        {
            Debug.LogError(
                "The hit object does not contain an IInteractable component."
            );
            return;
        }

        Debug.Log("Calling Interact.");
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
        if (interactionOrigin == null)
        {
            return;
        }

        Gizmos.DrawLine(
            interactionOrigin.position,
            interactionOrigin.position +
            interactionOrigin.forward * interactionDistance
        );
    }
}