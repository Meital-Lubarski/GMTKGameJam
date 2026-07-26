using UnityEngine;

public class BillboardController : MonoBehaviour
{
    // The transform of the 2D character we want to rotate
    [SerializeField] private Transform characterTransform;
    
    // Cached camera transform for maximum performance
    private Transform _mainCameraTransform;

    private void Start()
    {
        // Fallback in case the serialized field wasn't assigned in the inspector
        if (characterTransform == null)
        {
            characterTransform = transform;
        }

        TryCacheCamera();
    }

    /// <summary>
    /// Finds the main camera and holds on to it. False while there is still no
    /// camera to find.
    ///
    /// Retried rather than settled once at startup. Whether the camera exists
    /// by the time this runs depends on the order objects are started in, and
    /// that order is not promised to be the same from one platform to the
    /// next. Giving up on the first miss left the sprite turned whichever way
    /// something else had last pointed it, for the whole of the run.
    /// </summary>
    private bool TryCacheCamera()
    {
        if (_mainCameraTransform != null) return true;

        Camera mainCamera = Camera.main;

        if (mainCamera == null) return false;

        _mainCameraTransform = mainCamera.transform;

        return true;
    }

    private void LateUpdate()
    {
        // No camera yet, so there is nothing to turn towards. Checked again
        // every frame until one turns up.
        if (!TryCacheCamera()) return;

        /*
         * A sprite is drawn on its own XY plane and reads correctly when its
         * forward runs the same way the camera looks, not back towards it.
         * Turning it the other way shows its back, and because the material
         * draws both sides it does not disappear: it comes back mirrored,
         * which only becomes obvious once there is text on it.
         */
        Vector3 directionToFace = _mainCameraTransform.forward;

        // Cylindrical billboarding: Ignore vertical tilt
        directionToFace.y = 0f;

        // Looking straight down or up leaves no direction to turn towards.
        if (directionToFace.sqrMagnitude < 0.0001f) return;

        characterTransform.rotation = Quaternion.LookRotation(directionToFace);
    }
}