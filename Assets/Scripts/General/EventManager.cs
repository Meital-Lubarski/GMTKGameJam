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

        // Raised once when the ghost keeps the player inside its catch radius
        // long enough. The player loses (handled by the listener + ending scene).
        public static Action OnPlayerCaught;

        // Raised when the flashlight beam stuns the ghost. The float is how
        // long the stun lasts, so a listener can match an animation or a sound
        // to its length. OnGhostStunEnded follows once the ghost recovers.
        public static Action<float> OnGhostStunned;
        public static Action OnGhostStunEnded;
    }
}