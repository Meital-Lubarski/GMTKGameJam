using UnityEngine;
using UnityEngine.InputSystem;

namespace General
{
    public class CheatCodes : MonoBehaviour
    {
        private InputSystem_Actions _inputActions;
        private void OnEnable()
        {
            _inputActions = new InputSystem_Actions();
            _inputActions.Enable();
        
            _inputActions.CheatCode.ExitGame.performed += ExitGame;
            _inputActions.CheatCode.RestartGame.performed += RestartGame;
        }
        private void OnDisable()
        {
            _inputActions.CheatCode.ExitGame.performed -= ExitGame;
            _inputActions.CheatCode.RestartGame.performed -= RestartGame;
        
            _inputActions.Disable();
        }
        private void ExitGame(InputAction.CallbackContext context)
        {
        
        }

        private void RestartGame(InputAction.CallbackContext context)
        {
        
        }
    
    }
}
