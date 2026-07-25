using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject tutorialPanel;

    private InputAction pauseAction;

    private bool isPaused;
    private bool tutorialIsOpen;

    private void Awake()
    {
        Time.timeScale = 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
        
        pauseAction = new InputAction(
            name: "Pause",
            type: InputActionType.Button,
            binding: "<Keyboard>/escape"
        );
    }

    private void OnEnable()
    {
        pauseAction.performed += OnPausePressed;
        pauseAction.Enable();
    }

    private void OnDisable()
    {
        pauseAction.performed -= OnPausePressed;
        pauseAction.Disable();
    }

    private void OnDestroy()
    {
        pauseAction.Dispose();
    }

    private void OnPausePressed(InputAction.CallbackContext context)
    {
        Debug.Log("ESCAPE RECEIVED");

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
                "Pause Panel is not assigned!",
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

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene("Screens");
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}