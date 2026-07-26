using System;

namespace General
{
    public static class EventManager
    { 
        //Event Template
        //public static Action<bool> EventTemplate;
        public static Action<float> OnChargeChanged;
        public static Action<int> OnBarsChanged;
        public static Action OnBatteryEmpty;

        // Raised when the game is paused (true) and again when it is let go
        // (false). The clock is stopped either way, so this is for whatever
        // cannot be stopped by the clock alone: anything driven by raw input,
        // and any part of the HUD that does not belong over the menu.
        public static Action<bool> OnGamePaused;

        // Raised when what the player is looking at changes: the interactable
        // he could use right now, or null when there is nothing to use. Only
        // sent when the answer actually changes, so a listener can simply
        // show or hide itself on it.
        public static Action<IInteractable> OnInteractableChanged;

        // Raised when the kill animation the catch started has played out.
        // The catch itself is over long before this: it is what lets the
        // Game Over screen wait for the ghost to finish with him rather than
        // landing on top of her while she is still doing it.
        public static Action OnCaughtAnimationFinished;

        // Raised once when the ghost keeps the player inside its catch radius
        // long enough. The player loses (handled by the listener + ending scene).
        public static Action OnPlayerCaught;

        // Raised when the flashlight beam stuns the ghost. The float is how
        // long the stun lasts, so a listener can match an animation or a sound
        // to its length. OnGhostStunEnded follows once the ghost recovers.
        public static Action<float> OnGhostStunned;
        public static Action OnGhostStunEnded;

        // Raised when the ghost reaches the player and starts closing in on
        // him. The float is how long she needs to catch him, so a listener can
        // match an animation to that window. OnGhostApproachEnded follows if
        // the player escapes the radius or stuns her in time; if he does
        // neither, OnPlayerCaught follows instead.
        public static Action<float> OnGhostApproachStarted;
        public static Action OnGhostApproachEnded;

        /// <summary>
        /// Forgets every listener. These events are static, so they outlive the
        /// scene the listeners were in: anything that somehow failed to let go
        /// on its way out would still be called in the next run, on an object
        /// that is no longer there. Called when a scene is left behind for
        /// good, so a new run really does start from nothing.
        /// </summary>
        public static void ClearAllListeners()
        {
            OnChargeChanged = null;
            OnBarsChanged = null;
            OnBatteryEmpty = null;

            OnGamePaused = null;
            OnInteractableChanged = null;

            OnPlayerCaught = null;
            OnCaughtAnimationFinished = null;

            OnGhostStunned = null;
            OnGhostStunEnded = null;

            OnGhostApproachStarted = null;
            OnGhostApproachEnded = null;
        }
    }
}