using UnityEngine;

public class BillboardController : MonoBehaviour
{
    // The transform of the 2D character we want to rotate
    [SerializeField] private Transform characterTransform;
    
    // Cached camera transform for maximum performance
    private Transform _mainCameraTransform;

    private void Start()
    {
        // Cache the Main Camera to avoid expensive Camera.main calls in LateUpdate
        if (Camera.main != null)
        {
            _mainCameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogWarning("BillboardSprite: Main Camera not found in the scene.");
        }

        // Fallback in case the serialized field wasn't assigned in the inspector
        if (characterTransform == null)
        {
            characterTransform = transform;
        }
    }

    private void LateUpdate()
    {
        // Safety check to ensure we have a camera reference
        if (_mainCameraTransform == null) return;

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