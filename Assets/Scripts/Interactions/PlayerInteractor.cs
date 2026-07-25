using General;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Looks through the crosshair for whatever the player could use, and uses it
/// when the interact key is pressed.
///
/// The look-up runs on its own slow clock rather than every frame: a single
/// ray a few times a second is enough for a prompt on screen, and the result
/// is kept, so pressing the key costs nothing extra. Whenever the answer
/// changes it is announced once through <see cref="EventManager.OnInteractableChanged"/>,
/// so the UI only ever reacts to real changes.
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [Header("References")]
    [Tooltip("First-person camera. If left empty, Camera.main is used automatically.")]
    [SerializeField] private Camera playerCamera;

    [Header("Interaction")]
    [SerializeField, Min(0f)] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactableLayer;

    [Tooltip(
        "Seconds between two looks through the crosshair. 0 looks every " +
        "frame. Small values feel instant and still cost almost nothing."
    )]
    [SerializeField, Min(0f)] private float scanInterval = 0.1f;

    [Header("New Input System")]
    [SerializeField] private InputActionReference interactAction;

    /// <summary>
    /// What the player could use right now, or null when there is nothing.
    /// Already filtered by <see cref="IInteractable.CanInteract"/>, so a
    /// listener can show its prompt on anything that is not null.
    /// </summary>
    public IInteractable CurrentInteractable { get; private set; }

    // Center of the viewport = where the crosshair sits on screen.
    private static readonly Vector3 ScreenCenter = new Vector3(0.5f, 0.5f, 0f);

    private float nextScanTime;

    /*
     * The last collider the ray found and the interactable behind it. Looking
     * at the same object twice in a row is the normal case, so remembering the
     * pair keeps the component search out of almost every scan.
     */
    private Collider lastHitCollider;
    private IInteractable lastHitInteractable;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        nextScanTime = 0f;

        if (interactAction == null)
        {
            return;
        }

        interactAction.action.performed += OnInteractPerformed;
        interactAction.action.Enable();
    }

    private void OnDisable()
    {
        // Nothing can be used while this is off, so the prompt goes with it.
        SetCurrentInteractable(null);

        if (interactAction == null)
        {
            return;
        }

        interactAction.action.performed -= OnInteractPerformed;
        interactAction.action.Disable();
    }

    private void Update()
    {
        if (Time.time < nextScanTime)
        {
            return;
        }

        nextScanTime = Time.time + scanInterval;

        Scan();
    }

    /// <summary>
    /// Looks through the crosshair once and keeps what it finds.
    /// </summary>
    private void Scan()
    {
        IInteractable found = FindInteractableInView();

        /*
         * An object can stop being usable while it is still being looked at -
         * a battery that is picked up, or one that would top up nothing. It is
         * dropped here, so the prompt never promises a key press that does
         * nothing.
         */
        if (found != null && !found.CanInteract)
        {
            found = null;
        }

        SetCurrentInteractable(found);
    }

    private IInteractable FindInteractableInView()
    {
        if (playerCamera == null)
        {
            Debug.LogError(
                "PlayerInteractor has no camera assigned and no Camera.main was found.",
                this
            );

            enabled = false;
            return null;
        }

        // Ray straight through the crosshair (center of the screen).
        Ray ray = playerCamera.ViewportPointToRay(ScreenCenter);

        bool hitSomething = Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionDistance,
            interactableLayer,
            QueryTriggerInteraction.Collide
        );

        if (!hitSomething)
        {
            ForgetLastHit();
            return null;
        }

        if (hit.collider == lastHitCollider)
        {
            return lastHitInteractable;
        }

        lastHitCollider = hit.collider;

        /*
         * Searched on the parents as well, so the collider is free to sit on a
         * child of the object that owns the script.
         */
        lastHitInteractable =
            hit.collider.GetComponentInParent<IInteractable>();

        return lastHitInteractable;
    }

    private void SetCurrentInteractable(IInteractable interactable)
    {
        if (ReferenceEquals(CurrentInteractable, interactable))
        {
            return;
        }

        CurrentInteractable = interactable;

        EventManager.OnInteractableChanged?.Invoke(interactable);
    }

    private void ForgetLastHit()
    {
        lastHitCollider = null;
        lastHitInteractable = null;
    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        /*
         * A fresh look before acting, so the press always lands on what is on
         * screen at this exact moment rather than on the last scan, which can
         * be a fraction of a second old.
         */
        Scan();

        if (CurrentInteractable == null)
        {
            return;
        }

        CurrentInteractable.Interact();

        /*
         * Using something usually spends it, and the scan clock is reset so the
         * prompt answers the press right away instead of on the next tick.
         */
        nextScanTime = Time.time + scanInterval;

        Scan();
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
