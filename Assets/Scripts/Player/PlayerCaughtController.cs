using General;
using Ghost;
using StarterAssets;
using UnityEngine;

/// <summary>
/// Takes the player's body away from him the moment the ghost catches him:
/// walking and looking around stop, and the view is turned onto the ghost so he
/// has to watch what got him.
/// </summary>
[RequireComponent(typeof(FirstPersonController))]
public class PlayerCaughtController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FirstPersonController firstPersonController;

    [Tooltip(
        "The controller's Cinemachine Camera Target: the pivot the view looks " +
        "up and down around. Left empty it is taken from the controller."
    )]
    [SerializeField] private Transform cameraPivot;

    [Tooltip(
        "What the view turns onto once the player is caught. Left empty the " +
        "ghost is found in the scene. Point it at her face rather than her feet."
    )]
    [SerializeField] private Transform ghostLookTarget;

    [Header("Caught")]
    [Tooltip("How long the view takes to swing onto the ghost. 0 snaps straight to her.")]
    [SerializeField, Min(0f)] private float lookTurnDuration = 0.25f;

    private bool _isCaught;
    private float _lookTurnProgress;

    // Where the view was standing when the catch landed, so it swings from
    // there rather than jumping.
    private Quaternion _bodyRotationWhenCaught;
    private Quaternion _pivotRotationWhenCaught;

    private void Awake()
    {
        if (firstPersonController == null)
            firstPersonController = GetComponent<FirstPersonController>();

        if (
            cameraPivot == null &&
            firstPersonController != null &&
            firstPersonController.CinemachineCameraTarget != null
        )
        {
            cameraPivot = firstPersonController.CinemachineCameraTarget.transform;
        }
    }

    private void OnEnable()
    {
        EventManager.OnPlayerCaught += HandlePlayerCaught;
    }

    private void OnDisable()
    {
        EventManager.OnPlayerCaught -= HandlePlayerCaught;
    }

    // Every run starts with the player in control of himself.
    private void Start()
    {
        SetMovementEnabled(true);
    }

    private void HandlePlayerCaught()
    {
        // The run is already over, so a second catch changes nothing.
        if (_isCaught) return;

        _isCaught = true;

        SetMovementEnabled(false);

        _bodyRotationWhenCaught = transform.rotation;

        _pivotRotationWhenCaught =
            cameraPivot != null
                ? cameraPivot.localRotation
                : Quaternion.identity;

        _lookTurnProgress = 0f;
    }

    /*
     * LateUpdate, so the aim is the last word on where the view points: the
     * controller sets the camera pivot there too, and the ghost turns to face
     * the player in her own LateUpdate.
     */
    private void LateUpdate()
    {
        if (!_isCaught) return;

        if (lookTurnDuration > 0f)
        {
            _lookTurnProgress = Mathf.Min(
                1f,
                _lookTurnProgress + Time.deltaTime / lookTurnDuration
            );
        }
        else
        {
            _lookTurnProgress = 1f;
        }

        AimAtGhost(_lookTurnProgress);
    }

    /// <summary>
    /// Points the view at the ghost, part of the way there while the turn is
    /// still running.
    /// </summary>
    private void AimAtGhost(float turnAmount)
    {
        Transform lookTarget = ResolveGhostLookTarget();

        if (lookTarget == null) return;

        Vector3 eyePosition =
            cameraPivot != null
                ? cameraPivot.position
                : transform.position;

        Vector3 toGhost = lookTarget.position - eyePosition;

        Vector3 flatToGhost = new Vector3(toGhost.x, 0f, toGhost.z);

        // Standing right on top of her leaves no direction to turn towards.
        if (flatToGhost.sqrMagnitude < 0.0001f) return;

        /*
         * The body turns left and right and the pivot looks up and down: the
         * same split the controller uses, so the view never rolls over.
         */
        Quaternion bodyRotation = Quaternion.LookRotation(flatToGhost);

        float pitch =
            -Mathf.Atan2(toGhost.y, flatToGhost.magnitude) * Mathf.Rad2Deg;

        Quaternion pivotRotation = Quaternion.Euler(pitch, 0f, 0f);

        transform.rotation = Quaternion.Slerp(
            _bodyRotationWhenCaught,
            bodyRotation,
            turnAmount
        );

        if (cameraPivot != null)
        {
            cameraPivot.localRotation = Quaternion.Slerp(
                _pivotRotationWhenCaught,
                pivotRotation,
                turnAmount
            );
        }
    }

    private Transform ResolveGhostLookTarget()
    {
        if (ghostLookTarget != null) return ghostLookTarget;

        // Not wired up in the inspector, so fall back to whichever ghost is in
        // the scene. Kept once it is found, so this only searches on the frame
        // of the catch.
        EnemyAi ghost = FindFirstObjectByType<EnemyAi>();

        if (ghost != null) ghostLookTarget = ghost.transform;

        return ghostLookTarget;
    }

    /// <summary>
    /// Switching the controller off freezes walking and looking around at once,
    /// which is exactly what leaves the view free for the catch to aim it.
    /// </summary>
    private void SetMovementEnabled(bool isEnabled)
    {
        if (firstPersonController == null) return;

        firstPersonController.enabled = isEnabled;
    }
}
