using System;
using General;
using Unity.VisualScripting;
using UnityEngine;

namespace Ghost
{
    public class GhostVisuals : MonoBehaviour
    {
        private static readonly int ExitStun = Animator.StringToHash("ExitStun");
        private static readonly int Stun = Animator.StringToHash("Stun");
        private static readonly int Kill = Animator.StringToHash("Kill");
        [SerializeField] private Animator animator;
        private void OnEnable()
        {
            EventManager.OnPlayerCaught += EnableCaughtAnimation;
            EventManager.OnGhostStunned += EnableStun;
            EventManager.OnGhostStunEnded += DisableStun;
            EventManager.OnPlayerCaught += EnableCaughtAnimation;
        }

        private void OnDisable()
        {
            EventManager.OnPlayerCaught -= EnableCaughtAnimation;
            EventManager.OnGhostStunned -= EnableStun;
            EventManager.OnGhostStunEnded -= DisableStun;
            EventManager.OnPlayerCaught -= EnableCaughtAnimation;
        }
        
        private void EnableStun(float f)
        {
            animator.SetTrigger(Stun);
        }

        private void DisableStun()
        {
            animator.SetTrigger(ExitStun);
        }
        
        private void EnableCaughtAnimation()
        {
            animator.SetTrigger(Kill);
        }
    }
}
