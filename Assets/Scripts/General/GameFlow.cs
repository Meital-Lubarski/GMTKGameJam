using UnityEngine;
using UnityEngine.SceneManagement;

namespace General
{
    /// <summary>
    /// Leaving one scene for another, in one place.
    ///
    /// The pause menu and the Game Over screen both offer the same three ways
    /// out, and both used to spell them out for themselves. They share these
    /// instead, so restarting from the middle of a run and restarting from the
    /// Game Over screen cannot drift apart into two different restarts.
    /// </summary>
    public static class GameFlow
    {
        /// <summary>
        /// Loads the game scene over everything currently loaded. A single load
        /// throws the old scene away wholesale, so the level, the player, the
        /// ghost and the batteries all come back exactly as they were authored
        /// rather than as the last run left them.
        /// </summary>
        public static void RestartRun()
        {
            LeaveForGood();

            SceneManager.LoadScene(
                GameScenes.Game,
                LoadSceneMode.Single
            );
        }

        public static void ReturnToMenu()
        {
            LeaveForGood();

            SceneManager.LoadScene(
                GameScenes.Menu,
                LoadSceneMode.Single
            );
        }

        public static void QuitGame()
        {
            Time.timeScale = 1f;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// Everything that outlives a scene, put back the way a fresh run
        /// expects to find it. The clock above all: it is left stopped by both
        /// the pause menu and the Game Over screen, and a scene loaded under a
        /// stopped clock opens frozen.
        /// </summary>
        private static void LeaveForGood()
        {
            Time.timeScale = 1f;

            EventManager.ClearAllListeners();

            RunStats.Clear();
        }
    }
}
