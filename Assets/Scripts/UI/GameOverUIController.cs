using System.Collections;
using General;
using TMPro;
using UnityEngine;

/// <summary>
/// The Game Over screen. It belongs in the game scene, next to the run it is
/// reporting on: the catch happens there, so the screen that answers it is
/// there too and nothing has to reach across a scene boundary to raise it.
///
/// Restarting and going back to the menu are plain scene loads, so every run
/// begins from the same clean slate no matter which one the player picks.
/// </summary>
public class GameOverUIController : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("The Game Over panel. It is hidden on Awake, so it can be left visible while editing.")]
    [SerializeField] private GameObject gameOverPanel;

    [Header("Survived Time")]
    [Tooltip("The label that reports how long the player lasted.")]
    [SerializeField] private TMP_Text survivedTimeText;

    [Tooltip("How the time is written. {0} is replaced by the run's time.")]
    [SerializeField] private string survivedTimeFormat = "TIME: {0}";

    [Header("Timing")]
    [Tooltip(
        "How long to hold after the kill animation has played out, before the " +
        "screen comes up. A short beat here lets the last frame land."
    )]
    [SerializeField, Min(0f)] private float showDelay = 0.6f;

    [Tooltip(
        "How long to wait for the kill animation at the very most. It is what " +
        "answers an animation that never reports back - a ghost destroyed " +
        "mid-catch, say - so the player is never left without a screen."
    )]
    [SerializeField, Min(0f)] private float maxWaitForAnimation = 6f;

    private bool gameOverWasTriggered;

    /*
     * Set by the ghost when she has finished with him. It is never put back
     * once the catch has happened: both listen to the same catch, and whoever
     * hears it second must not undo what the first already reported. A new run
     * is a new scene, and a new one of these along with it.
     */
    private bool killAnimationFinished;

    private void Awake()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        EventManager.OnPlayerCaught += HandlePlayerCaught;
        EventManager.OnCaughtAnimationFinished += HandleKillAnimationFinished;
    }

    private void OnDisable()
    {
        EventManager.OnPlayerCaught -= HandlePlayerCaught;
        EventManager.OnCaughtAnimationFinished -= HandleKillAnimationFinished;
    }

    private void HandlePlayerCaught()
    {
        // The run is already over, so a second catch changes nothing.
        if (gameOverWasTriggered)
        {
            return;
        }

        gameOverWasTriggered = true;

        StartCoroutine(ShowGameOverRoutine());
    }

    private void HandleKillAnimationFinished()
    {
        killAnimationFinished = true;
    }

    private IEnumerator ShowGameOverRoutine()
    {
        yield return WaitForKillAnimation();

        /*
         * Real seconds rather than game ones, so the wait is the same length
         * whatever the game clock is doing while the catch plays out.
         */
        if (showDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(showDelay);
        }

        if (gameOverPanel == null)
        {
            Debug.LogError(
                "GameOverUIController has no Game Over Panel assigned.",
                this
            );

            yield break;
        }

        ShowSurvivedTime();

        gameOverPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Everything behind the screen stops where it is.
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Holds until the ghost has finished with the player, so the screen never
    /// lands on top of the kill. It gives up after
    /// <see cref="maxWaitForAnimation"/>: an animation that never reports back
    /// should cost the player a beat, not the whole Game Over screen.
    /// </summary>
    private IEnumerator WaitForKillAnimation()
    {
        float giveUpTime = Time.unscaledTime + maxWaitForAnimation;

        while (
            !killAnimationFinished &&
            Time.unscaledTime < giveUpTime
        )
        {
            yield return null;
        }

        if (!killAnimationFinished)
        {
            Debug.LogWarning(
                "The kill animation never reported that it had finished, so " +
                "Game Over is being shown anyway. Check the kill state's Tag " +
                "on the ghost's Animator.",
                this
            );
        }
    }

    /// <summary>
    /// Reads the run's time off <see cref="RunStats"/>, which the timer filled
    /// in the moment the catch landed. Read here rather than at the catch, so
    /// the label is written from the finished run and never from a timer that
    /// is a frame behind.
    /// </summary>
    private void ShowSurvivedTime()
    {
        if (survivedTimeText == null)
        {
            return;
        }

        survivedTimeText.text = string.Format(
            survivedTimeFormat,
            RunStats.LastRunFormatted
        );
    }

    /*
     * Hooked up on the buttons of the Game Over panel through their On Click.
     * The game clock is let go before any of them leave, or the next scene
     * would open frozen.
     */

    public void RestartGame()
    {
        GameFlow.RestartRun();
    }

    public void ReturnToMainMenu()
    {
        GameFlow.ReturnToMenu();
    }

    public void QuitGame()
    {
        GameFlow.QuitGame();
    }
}
