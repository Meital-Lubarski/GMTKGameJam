using General;
using UnityEngine;

namespace Ghost
{
    public class GhostVisuals : MonoBehaviour
    {
        private static readonly int ExitStun = Animator.StringToHash("ExitStun");
        private static readonly int Stun = Animator.StringToHash("Stun");
        private static readonly int Kill = Animator.StringToHash("Kill");
        private static readonly int Approach = Animator.StringToHash("Approach");
        private static readonly int ExitApproach = Animator.StringToHash("ExitApproach");
        [SerializeField] private Animator animator;
        private void OnEnable()
        {
            EventManager.OnPlayerCaught += EnableCaughtAnimation;
            EventManager.OnGhostStunned += EnableStun;
            EventManager.OnGhostStunEnded += DisableStun;
            EventManager.OnGhostApproachStarted += EnableApproach;
            EventManager.OnGhostApproachEnded += DisableApproach;
        }

        private void OnDisable()
        {
            EventManager.OnPlayerCaught -= EnableCaughtAnimation;
            EventManager.OnGhostStunned -= EnableStun;
            EventManager.OnGhostStunEnded -= DisableStun;
            EventManager.OnGhostApproachStarted -= EnableApproach;
            EventManager.OnGhostApproachEnded -= DisableApproach;
        }

        private void EnableStun(float f)
        {
            animator.SetTrigger(Stun);
        }

        private void DisableStun()
        {
            animator.SetTrigger(ExitStun);
        }

        /*
         * The ghost is on the player and is closing in on him. The float is how
         * long she needs to catch him, so the animation can be paced to it.
         */
        private void EnableApproach(float catchTime)
        {
            animator.SetTrigger(Approach);
        }

        /*
         * The player got away or the flashlight stunned her, so she drops back
         * to walking.
         */
        private void DisableApproach()
        {
            animator.SetTrigger(ExitApproach);
        }

        private void EnableCaughtAnimation()
        {
            animator.SetTrigger(Kill);
        }
    }
}
