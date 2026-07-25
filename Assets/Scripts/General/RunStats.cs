namespace General
{
    /// <summary>
    /// What the last run left behind: how long the player survived it.
    ///
    /// It is static, so it outlives the scene it was made in. That is the whole
    /// point: the timer counts in the game scene and dies with it, while the
    /// screen that reports the time may well be somewhere else entirely, and
    /// neither has to know where the other one lives.
    /// </summary>
    public static class RunStats
    {
        /// <summary>
        /// How long the player survived his last run, in seconds.
        /// </summary>
        public static float LastRunSeconds { get; private set; }

        /// <summary>
        /// Whether a run has finished at all yet. Lets a screen tell a real
        /// time of zero apart from never having played.
        /// </summary>
        public static bool HasLastRun { get; private set; }

        /// <summary>
        /// The last run's time, ready to be put on a label.
        /// </summary>
        public static string LastRunFormatted => FormatTime(LastRunSeconds);

        public static void RecordRun(float survivedSeconds)
        {
            LastRunSeconds = survivedSeconds;
            HasLastRun = true;
        }

        public static void Clear()
        {
            LastRunSeconds = 0f;
            HasLastRun = false;
        }

        /// <summary>
        /// mm:ss. Shared, so the clock in the corner of the screen and the time
        /// on the Game Over screen can never disagree about how to write it.
        /// </summary>
        public static string FormatTime(float seconds)
        {
            int totalSeconds = (int)seconds;
            int minutes = totalSeconds / 60;
            int remainingSeconds = totalSeconds % 60;

            return $"{minutes:00}:{remainingSeconds:00}";
        }
    }
}
