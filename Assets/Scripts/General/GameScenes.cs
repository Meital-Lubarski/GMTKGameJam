namespace General
{
    /// <summary>
    /// The names of the scenes the game moves between, in one place, so a
    /// renamed scene breaks the build rather than only breaking at runtime in
    /// whichever script still held the old spelling.
    /// </summary>
    public static class GameScenes
    {
        /// <summary>
        /// The menu shell: main menu, tutorial and credits.
        /// </summary>
        public const string Menu = "Screens";

        /// <summary>
        /// The run itself: the level, the player, the ghost and the in-game UI.
        /// </summary>
        public const string Game = "MayScene";
    }
}
