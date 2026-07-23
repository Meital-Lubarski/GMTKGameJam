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
    }
}