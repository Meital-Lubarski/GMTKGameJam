using TMPro;
using UnityEngine;

namespace Ghost
{
    /// <summary>
    /// The stun icon and its countdown, hanging over the ghost.
    /// They are only there while she is frozen, and only while the flashlight
    /// is on her. The clock keeps running in the dark, so looking back at her
    /// finds it where it would have been.
    /// </summary>
    public class GhostStunIndicator : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The ghost this belongs to. Left empty she is found in the parents.")]
        [SerializeField] private EnemyAi ghost;

        [Tooltip("The countdown text. Left empty it is found in the children.")]
        [SerializeField] private TMP_Text timerText;

        [Tooltip(
            "Everything the indicator draws: the icon and the countdown. " +
            "Left empty every renderer in the children is used."
        )]
        [SerializeField] private Renderer[] indicatorRenderers;

        [Header("Countdown")]
        [Tooltip("Decimal places on the countdown. 0 counts in whole seconds.")]
        [SerializeField, Range(0, 2)] private int decimalPlaces;

        private void Awake()
        {
            if (ghost == null)
                ghost = GetComponentInParent<EnemyAi>(true);

            if (timerText == null)
                timerText = GetComponentInChildren<TMP_Text>(true);

            if (indicatorRenderers == null || indicatorRenderers.Length == 0)
                indicatorRenderers = GetComponentsInChildren<Renderer>(true);

            if (ghost == null)
            {
                Debug.LogError(
                    "GhostStunIndicator found no EnemyAi on itself or its parents.",
                    this
                );
            }
            else if (ghost.gameObject == gameObject)
            {
                /*
                 * The ghost leaves everything under this component alone so the
                 * indicator can rule its own renderers. Sitting on her would
                 * hand her whole body over as well, and she would never be
                 * drawn again.
                 */
                Debug.LogError(
                    "GhostStunIndicator is on the ghost herself. It belongs on the " +
                    "child holding the stun icon and the countdown.",
                    this
                );
            }

            // There is nothing to show until the first stun lands.
            SetVisible(false);
        }

        /*
         * LateUpdate, because the ghost counts the stun down in her own Update:
         * reading it afterwards shows this frame's value rather than the last.
         */
        private void LateUpdate()
        {
            if (ghost == null) return;

            bool isStunned = ghost.IsStunned;

            /*
             * The clock is read straight off the ghost instead of being counted
             * again here, so the number on screen is the stun rather than a
             * copy of it that could drift. It is kept up to date even while
             * hidden, so it is already right the moment the beam finds her.
             */
            if (isStunned) UpdateTimerText(ghost.StunTimeRemaining);

            // Only while she is frozen, and only while she is being looked at.
            SetVisible(isStunned && ghost.IsIlluminated);
        }

        private void UpdateTimerText(float secondsRemaining)
        {
            if (timerText == null) return;

            /*
             * Whole seconds are rounded up, so a stun with any time left on it
             * never reads as zero: the number runs out at the same moment she
             * starts moving again.
             */
            float displayedSeconds =
                decimalPlaces == 0
                    ? Mathf.Ceil(secondsRemaining)
                    : secondsRemaining;

            timerText.text = displayedSeconds.ToString("F" + decimalPlaces);
        }

        /// <summary>
        /// Switches the renderers rather than the objects, so the icon's own
        /// animation keeps playing while it is out of sight and is never caught
        /// halfway through on the way back in. This is how the ghost herself is
        /// hidden, and the two have to behave the same.
        /// </summary>
        private void SetVisible(bool isVisible)
        {
            if (indicatorRenderers == null) return;

            foreach (Renderer indicatorRenderer in indicatorRenderers)
            {
                if (indicatorRenderer != null)
                    indicatorRenderer.enabled = isVisible;
            }
        }
    }
}
