using UnityEngine;
using UnityEngine.InputSystem;

public class FlashlightController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BatteryManager batteryManager;
    [SerializeField] private Light flashlight;
    [SerializeField] private Transform flashlightOrigin;
    [SerializeField] private Transform ghost;

    [Header("Ghost Distance")]
    [Tooltip("At or below this distance, the flashlight uses strong mode.")]
    [SerializeField, Min(0f)] private float ghostCloseDistance = 8f;

    [Header("Light Intensity")]
    [SerializeField, Min(0f)] private float weakLightIntensity = 2f;
    [SerializeField, Min(0f)] private float strongLightIntensity = 8f;

    [Header("Battery Drain Per Second")]
    [SerializeField, Min(0f)] private float weakDrainPerSecond = 0.5f;
    [SerializeField, Min(0f)] private float strongDrainPerSecond = 2f;

    [Header("Ghost Detection")]
    [Tooltip("Put the Ghost layer here.")]
    [SerializeField] private LayerMask ghostLayer;

    [Tooltip("Maximum distance at which the flashlight can reveal the ghost.")]
    [SerializeField, Min(0f)] private float detectionDistance = 30f;

    [Tooltip("Makes detection wider than a single thin ray.")]
    [SerializeField, Min(0f)] private float detectionRadius = 0.4f;

    [Header("Stun Input - New Input System")]
    [SerializeField] private InputActionReference stunAction;

    private IFlashlightTarget currentlyIlluminatedTarget;
    private bool ghostIsClose;

    private void Awake()
    {
        if (flashlightOrigin == null)
        {
            flashlightOrigin = transform;
        }
    }

    private void OnEnable()
    {
        if (stunAction != null)
        {
            stunAction.action.performed += OnStunPerformed;
            stunAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (stunAction != null)
        {
            stunAction.action.performed -= OnStunPerformed;
            stunAction.action.Disable();
        }

        ClearIlluminatedTarget();
    }

    private void Update()
    {
        if (batteryManager == null || flashlight == null)
        {
            return;
        }

        if (batteryManager.IsEmpty)
        {
            flashlight.enabled = false;
            ClearIlluminatedTarget();
            return;
        }

        flashlight.enabled = true;

        UpdateGhostDistanceState();
        UpdateFlashlightIntensity();
        DrainBattery();
        DetectGhostInFlashlight();
    }

    private void UpdateGhostDistanceState()
    {
        if (ghost == null)
        {
            ghostIsClose = false;
            return;
        }

        float distanceToGhost = Vector3.Distance(
            flashlightOrigin.position,
            ghost.position
        );

        ghostIsClose = distanceToGhost <= ghostCloseDistance;
    }

    private void UpdateFlashlightIntensity()
    {
        flashlight.intensity = ghostIsClose
            ? strongLightIntensity
            : weakLightIntensity;
    }

    private void DrainBattery()
    {
        float drainRate = ghostIsClose
            ? strongDrainPerSecond
            : weakDrainPerSecond;

        batteryManager.Drain(drainRate * Time.deltaTime);
    }

    private void DetectGhostInFlashlight()
    {
        Ray ray = new Ray(
            flashlightOrigin.position,
            flashlightOrigin.forward
        );

        bool hitGhost = Physics.SphereCast(
            ray,
            detectionRadius,
            out RaycastHit hit,
            detectionDistance,
            ghostLayer,
            QueryTriggerInteraction.Collide
        );

        if (!hitGhost)
        {
            ClearIlluminatedTarget();
            return;
        }

        IFlashlightTarget target = FindFlashlightTarget(hit.collider);

        if (target == null)
        {
            ClearIlluminatedTarget();
            return;
        }

        if (currentlyIlluminatedTarget == target)
        {
            return;
        }

        ClearIlluminatedTarget();

        currentlyIlluminatedTarget = target;
        currentlyIlluminatedTarget.SetIlluminated(true);
    }

    private IFlashlightTarget FindFlashlightTarget(Collider hitCollider)
    {
        MonoBehaviour[] behaviours =
            hitCollider.GetComponentsInParent<MonoBehaviour>();

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IFlashlightTarget target)
            {
                return target;
            }
        }

        return null;
    }

    private void ClearIlluminatedTarget()
    {
        if (currentlyIlluminatedTarget == null)
        {
            return;
        }

        currentlyIlluminatedTarget.SetIlluminated(false);
        currentlyIlluminatedTarget = null;
    }

    private void OnStunPerformed(InputAction.CallbackContext context)
    {
        if (batteryManager == null || batteryManager.IsEmpty)
        {
            return;
        }

        if (currentlyIlluminatedTarget == null)
        {
            return;
        }

        float stunDuration = batteryManager.GetStunDuration();

        if (stunDuration <= 0f)
        {
            return;
        }

        currentlyIlluminatedTarget.Stun(stunDuration);
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin = flashlightOrigin != null
            ? flashlightOrigin
            : transform;

        Gizmos.DrawWireSphere(
            origin.position + origin.forward * detectionDistance,
            detectionRadius
        );
    }
}