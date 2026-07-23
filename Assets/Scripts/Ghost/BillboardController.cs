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

        // Calculate the direction opposite to the camera's forward vector
        // This ensures standard Unity sprites face the camera perfectly
        Vector3 directionToFace = -_mainCameraTransform.forward;
        
        // Cylindrical billboarding: Ignore vertical tilt
        directionToFace.y = 0f;
        characterTransform.rotation = Quaternion.LookRotation(directionToFace);
    }
}