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

        // Raised when what the player is looking at changes: the interactable
        // he could use right now, or null when there is nothing to use. Only
        // sent when the answer actually changes, so a listener can simply
        // show or hide itself on it.
        public static Action<IInteractable> OnInteractableChanged;

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
    }
}