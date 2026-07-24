using UnityEngine;
using UnityEngine.InputSystem;

public class CreditsController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MenuUIManager menuUIManager;

    [Header("Input")]
    [SerializeField] private InputActionReference backAction;

    private void OnEnable()
    {
        if (backAction == null)
        {
            Debug.LogError(
                "CreditsController has no Back Action assigned.",
                this
            );

            return;
        }

        backAction.action.performed += OnBackPressed;
        backAction.action.Enable();
    }

    private void OnDisable()
    {
        if (backAction == null)
        {
            return;
        }

        backAction.action.performed -= OnBackPressed;
        backAction.action.Disable();
    }

    private void OnBackPressed(
        InputAction.CallbackContext context
    )
    {
        if (menuUIManager == null)
        {
            Debug.LogError(
                "CreditsController has no MenuUIManager assigned.",
                this
            );

            return;
        }

        menuUIManager.ShowMainMenu();
    }
}