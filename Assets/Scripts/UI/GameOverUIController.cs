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
        "How long to wait after the player is caught before showing Game Over. " +
        "This is the beat where the view swings onto the ghost, so keep it long " +
        "enough for the player to see what got him."
    )]
    [SerializeField, Min(0f)] private float showDelay = 0.6f;

    private bool gameOverWasTriggered;

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
    }

    private void OnDisable()
    {
        EventManager.OnPlayerCaught -= HandlePlayerCaught;
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

    private IEnumerator ShowGameOverRoutine()
    {
        /*
         * Real seconds rather than game ones, so the wait is the same length
         * whatever the game clock is doing while the catch plays out.
         */
        yield return new WaitForSecondsRealtime(showDelay);

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
