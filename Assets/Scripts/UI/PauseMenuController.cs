using General;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The pause menu. It belongs in the game scene: Escape only means anything
/// while a run is going.
///
/// It announces the pause through <see cref="EventManager.OnGamePaused"/> and
/// stops there. Freezing the clock does not stop everything - looking around
/// is driven by raw mouse movement, and so is every key the player presses -
/// but what has to be stopped is spread across the level, and each of those
/// listens for itself rather than being reached for from here.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;

    [Tooltip(
        "The in-game copy of the tutorial, opened from the pause menu. It must " +
        "not be the one from the menu scene: that one starts the game when its " +
        "button is pressed."
    )]
    [SerializeField] private GameObject tutorialPanel;

    [SerializeField] private InputAction pauseAction;

    private bool isPaused;
    private bool tutorialIsOpen;

    // Once the run is over the Game Over screen owns the view, and Escape has
    // nothing left to pause.
    private bool runIsOver;

    private void Awake()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }

        /*
         * The run always opens unpaused and with the mouse out of the way, so
         * arriving here from a frozen Game Over screen or a paused menu still
         * starts the game properly.
         */
        Time.timeScale = 1f;

        SetCursorFreed(false);

    }

    private void OnEnable()
    {
        pauseAction.performed += OnPausePressed;
        pauseAction.Enable();

        EventManager.OnPlayerCaught += HandlePlayerCaught;
    }

    private void OnDisable()
    {
        pauseAction.performed -= OnPausePressed;
        pauseAction.Disable();

        EventManager.OnPlayerCaught -= HandlePlayerCaught;
    }

    private void OnDestroy()
    {
        pauseAction.Dispose();
    }

    private void HandlePlayerCaught()
    {
        runIsOver = true;
    }

    private void OnPausePressed(InputAction.CallbackContext context)
    {
        if (runIsOver)
        {
            return;
        }

        if (tutorialIsOpen)
        {
            CloseTutorial();
            return;
        }

        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    private void PauseGame()
    {
        if (pausePanel == null)
        {
            Debug.LogError(
                "PauseMenuController has no Pause Panel assigned.",
                this
            );

            return;
        }

        isPaused = true;
        tutorialIsOpen = false;

        Time.timeScale = 0f;

        pausePanel.SetActive(true);

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }

        SetCursorFreed(true);

        EventManager.OnGamePaused?.Invoke(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        tutorialIsOpen = false;

        Time.timeScale = 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }

        SetCursorFreed(false);

        EventManager.OnGamePaused?.Invoke(false);
    }

    public void OpenTutorial()
    {
        tutorialIsOpen = true;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }
    }

    public void CloseTutorial()
    {
        tutorialIsOpen = false;

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
    }

    /// <summary>
    /// Starts the run again from the beginning. The scene is loaded fresh
    /// rather than tidied up, so nothing the last run did to it can survive.
    /// </summary>
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

    private void SetCursorFreed(bool isFreed)
    {
        Cursor.lockState =
            isFreed
                ? CursorLockMode.None
                : CursorLockMode.Locked;

        Cursor.visible = isFreed;
    }
}
