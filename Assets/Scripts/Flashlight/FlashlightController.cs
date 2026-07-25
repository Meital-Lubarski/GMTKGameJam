using UnityEngine;
using UnityEngine.InputSystem;

public class FlashlightController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BatteryManager batteryManager;
    [SerializeField] private Light flashlight;
    [SerializeField] private Transform flashlightOrigin;
    [SerializeField] private Transform ghost;

    [Header("New Input System")]
    [Tooltip("The Flashlight action from the Player map. Toggles the light.")]
    [SerializeField] private InputActionReference flashlightAction;

    [Tooltip("Should the flashlight already be lit when the run starts?")]
    [SerializeField] private bool startTurnedOn = true;

    [Header("Ghost Distance")]
    [Tooltip("At or below this distance, the flashlight uses strong mode.")]
    [SerializeField, Min(0f)]
    private float ghostCloseDistance = 8f;

    [Header("Light Intensity")]
    [SerializeField, Min(0f)]
    private float weakLightIntensity = 2f;

    [SerializeField, Min(0f)]
    private float strongLightIntensity = 8f;

    [Header("Battery Drain Multiplier")]
    [Tooltip("1 means each bar lasts exactly its Seconds Per Bar value.")]
    [SerializeField, Min(0f)]
    private float weakDrainMultiplier = 1f;

    [Tooltip("Drain while the ghost is close. 2 empties the battery twice as fast.")]
    [SerializeField, Min(0f)]
    private float strongDrainMultiplier = 2f;

    [Header("Ghost Detection")]
    [Tooltip("Put the Ghost layer here.")]
    [SerializeField]
    private LayerMask ghostLayer;

    [Tooltip("Maximum distance at which the flashlight can reveal the ghost.")]
    [SerializeField, Min(0f)]
    private float detectionDistance = 30f;

    [Tooltip("Makes detection wider than a single thin ray.")]
    [SerializeField, Min(0f)]
    private float detectionRadius = 0.4f;

    [Header("Stun Range")]
    [Tooltip(
        "How close the ghost has to be for the beam to stun her. The beam still " +
        "reveals her as far as Detection Distance, so she can be seen coming " +
        "from across the room without being stoppable from there. Keep this " +
        "wider than her Attack Range, or the player could never break a catch."
    )]
    [SerializeField, Min(0f)]
    private float stunRange = 8f;

    private IFlashlightTarget currentlyIlluminatedTarget;
    private bool ghostIsClose;
    private bool isTurnedOn;

    /*
     * מונע קריאה ל-Stun בכל פריים.
     * הסטאן מופעל פעם אחת כשקרן הפנס מתחילה לפגוע ברוח.
     */
    private bool stunAppliedForCurrentIllumination;

    private void Awake()
    {
        if (flashlightOrigin == null)
        {
            flashlightOrigin = transform;
        }

        if (batteryManager == null)
        {
            Debug.LogError(
                "FlashlightController has no BatteryManager assigned.",
                this
            );
        }

        if (flashlight == null)
        {
            Debug.LogError(
                "FlashlightController has no Light assigned.",
                this
            );
        }
    }

    private void OnEnable()
    {
        if (flashlightAction != null)
        {
            flashlightAction.action.performed += OnFlashlightPerformed;
            flashlightAction.action.Enable();
        }
    }

    /*
     * The starting state is decided here rather than in OnEnable so the
     * battery has already been filled by its own Awake.
     */
    private void Start()
    {
        // Never start lit on a dead battery.
        SetFlashlightOn(
            startTurnedOn &&
            batteryManager != null &&
            !batteryManager.IsEmpty
        );
    }

    private void OnDisable()
    {
        if (flashlightAction != null)
        {
            flashlightAction.action.performed -= OnFlashlightPerformed;
            flashlightAction.action.Disable();
        }

        ClearIlluminatedTarget();
    }

    private void OnFlashlightPerformed(InputAction.CallbackContext context)
    {
        ToggleFlashlight();
    }

    private void ToggleFlashlight()
    {
        if (flashlight == null)
        {
            return;
        }

        // An empty battery cannot be switched back on until it is recharged.
        if (
            !isTurnedOn &&
            (batteryManager == null || batteryManager.IsEmpty)
        )
        {
            return;
        }

        SetFlashlightOn(!isTurnedOn);
    }

    private void SetFlashlightOn(bool isOn)
    {
        isTurnedOn = isOn;

        if (flashlight != null)
        {
            flashlight.enabled = isOn;
        }

        // A switched off flashlight cannot keep revealing the ghost.
        if (!isOn)
        {
            ClearIlluminatedTarget();
        }
    }

    private void Update()
    {
        if (
            batteryManager == null ||
            flashlight == null
        )
        {
            return;
        }

        // While switched off the battery is preserved and nothing is revealed.
        if (!isTurnedOn)
        {
            return;
        }

        if (batteryManager.IsEmpty)
        {
            SetFlashlightOn(false);
            return;
        }

        UpdateGhostDistanceState();
        UpdateFlashlightIntensity();
        DrainBattery();
        DetectGhostInFlashlight();
    }

    private void UpdateGhostDistanceState()
    {
        if (
            ghost == null ||
            flashlightOrigin == null
        )
        {
            ghostIsClose = false;
            return;
        }

        float distanceToGhost =
            Vector3.Distance(
                flashlightOrigin.position,
                ghost.position
            );

        ghostIsClose =
            distanceToGhost <= ghostCloseDistance;
    }

    private void UpdateFlashlightIntensity()
    {
        flashlight.intensity =
            ghostIsClose
                ? strongLightIntensity
                : weakLightIntensity;
    }

    private void DrainBattery()
    {
        float drainMultiplier =
            ghostIsClose
                ? strongDrainMultiplier
                : weakDrainMultiplier;

        batteryManager.Drain(
            drainMultiplier * Time.deltaTime
        );
    }

    private void DetectGhostInFlashlight()
    {
        if (flashlightOrigin == null)
        {
            return;
        }

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

        IFlashlightTarget target =
            FindFlashlightTarget(hit.collider);

        if (target == null)
        {
            ClearIlluminatedTarget();
            return;
        }

        /*
         * הפנס עדיין על אותה רוח.
         * לא מפעילים שוב את הסטאן ולא מאפסים את הטיימר.
         */
        if (currentlyIlluminatedTarget == target)
        {
            /*
             * The stun is not applied again, but it is retried: the ghost may
             * have been too far away to stun when the beam first found her and
             * have closed the distance since, with the beam never leaving her.
             */
            ApplyStunToCurrentTarget(hit.distance);
            return;
        }

        ClearIlluminatedTarget();

        currentlyIlluminatedTarget = target;
        stunAppliedForCurrentIllumination = false;

        /*
         * הרוח נראית כל עוד הפנס עליה.
         */
        currentlyIlluminatedTarget.SetIlluminated(true);

        /*
         * מפעילים סטאן פעם אחת בלבד.
         */
        ApplyStunToCurrentTarget(hit.distance);
    }

    private void ApplyStunToCurrentTarget(float distanceToGhost)
    {
        if (stunAppliedForCurrentIllumination)
        {
            return;
        }

        if (currentlyIlluminatedTarget == null)
        {
            return;
        }

        /*
         * Out of reach: the beam keeps showing her, but from this far away it
         * does nothing to her. The player has to let her come closer.
         */
        if (distanceToGhost > stunRange)
        {
            return;
        }

        if (
            batteryManager == null ||
            batteryManager.IsEmpty
        )
        {
            return;
        }

        float stunDuration =
            batteryManager.GetStunDuration();

        if (stunDuration <= 0f)
        {
            return;
        }

        currentlyIlluminatedTarget.Stun(
            stunDuration
        );

        stunAppliedForCurrentIllumination = true;
    }

    private IFlashlightTarget FindFlashlightTarget(
        Collider hitCollider
    )
    {
        MonoBehaviour[] behaviours =
            hitCollider.GetComponentsInParent<MonoBehaviour>();

        foreach (
            MonoBehaviour behaviour in behaviours
        )
        {
            if (
                behaviour is IFlashlightTarget target
            )
            {
                return target;
            }
        }

        return null;
    }

    private void ClearIlluminatedTarget()
    {
        if (currentlyIlluminatedTarget != null)
        {
            currentlyIlluminatedTarget.SetIlluminated(
                false
            );
        }

        currentlyIlluminatedTarget = null;
        stunAppliedForCurrentIllumination = false;
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin =
            flashlightOrigin != null
                ? flashlightOrigin
                : transform;

        Gizmos.DrawWireSphere(
            origin.position +
            origin.forward * detectionDistance,
            detectionRadius
        );

        // How close the ghost has to be before the beam can stop her.
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            origin.position,
            stunRange
        );
    }
}