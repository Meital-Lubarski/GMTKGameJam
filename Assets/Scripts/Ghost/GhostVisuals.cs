using System.Collections;
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

        [Header("Kill Animation")]
        [Tooltip(
            "The Tag given to the kill state in the Animator. It is how the " +
            "end of the animation is recognised. Leave it empty, or leave the " +
            "state untagged, and the wait falls back to Max Seconds."
        )]
        [SerializeField] private string killStateTag = "Kill";

        [Tooltip(
            "How long to wait for the kill animation at the very most. It is " +
            "what answers a state that never arrives or never ends, so the " +
            "Game Over screen cannot be left waiting forever."
        )]
        [SerializeField, Min(0f)] private float killAnimationMaxSeconds = 5f;

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
            if (animator == null)
            {
                // Nothing to watch, so nothing is kept waiting on it.
                EventManager.OnCaughtAnimationFinished?.Invoke();
                return;
            }

            animator.SetTrigger(Kill);

            StartCoroutine(AnnounceWhenKillAnimationEnds());
        }

        /// <summary>
        /// Follows the kill animation from the trigger to its last frame and
        /// says so once. The ghost is the only one who knows when she is done
        /// with him, so she is the one who tells everybody.
        ///
        /// Counted in real seconds throughout: whoever is waiting for this is
        /// about to stop the clock, and a wait measured on a stopped clock
        /// never ends.
        /// </summary>
        private IEnumerator AnnounceWhenKillAnimationEnds()
        {
            float giveUpTime = Time.unscaledTime + killAnimationMaxSeconds;

            bool watchesState = !string.IsNullOrEmpty(killStateTag);

            if (watchesState)
            {
                // The trigger only asks for the state; the transition into it
                // takes a moment to arrive.
                while (
                    Time.unscaledTime < giveUpTime &&
                    !IsPlayingKillState()
                )
                {
                    yield return null;
                }

                // Then it plays, once, to the end.
                while (
                    Time.unscaledTime < giveUpTime &&
                    IsPlayingKillState() &&
                    animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f
                )
                {
                    yield return null;
                }
            }
            else
            {
                while (Time.unscaledTime < giveUpTime)
                {
                    yield return null;
                }
            }

            EventManager.OnCaughtAnimationFinished?.Invoke();
        }

        private bool IsPlayingKillState()
        {
            if (animator == null)
            {
                return false;
            }

            return animator
                .GetCurrentAnimatorStateInfo(0)
                .IsTag(killStateTag);
        }
    }
}
