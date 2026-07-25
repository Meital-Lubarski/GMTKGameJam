using System.Collections;
using General;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Game Scene")]
    [SerializeField] private string gameSceneName = "MeitalFullScene";

    [Header("Timing")]
    [Tooltip(
        "How long to wait after the player is caught before showing Game Over."
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
        if (gameOverWasTriggered)
        {
            return;
        }

        gameOverWasTriggered = true;

        Debug.Log("GAME OVER EVENT RECEIVED");

        StartCoroutine(ShowGameOverRoutine());
    }

    private IEnumerator ShowGameOverRoutine()
    {
        yield return new WaitForSecondsRealtime(showDelay);

        if (gameOverPanel == null)
        {
            Debug.LogError(
                "Game Over Panel is not assigned.",
                this
            );

            yield break;
        }

        gameOverPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        StartCoroutine(RestartGameRoutine());
    }

    private IEnumerator RestartGameRoutine()
    {
        Time.timeScale = 1f;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        Scene gameScene =
            SceneManager.GetSceneByName(gameSceneName);

        if (gameScene.IsValid() && gameScene.isLoaded)
        {
            AsyncOperation unloadOperation =
                SceneManager.UnloadSceneAsync(gameScene);

            if (unloadOperation != null)
            {
                yield return unloadOperation;
            }
        }

        AsyncOperation loadOperation =
            SceneManager.LoadSceneAsync(
                gameSceneName,
                LoadSceneMode.Additive
            );

        if (loadOperation == null)
        {
            Debug.LogError(
                $"Could not load game scene: {gameSceneName}",
                this
            );

            yield break;
        }

        yield return loadOperation;

        Scene loadedGameScene =
            SceneManager.GetSceneByName(gameSceneName);

        if (loadedGameScene.IsValid() &&
            loadedGameScene.isLoaded)
        {
            SceneManager.SetActiveScene(loadedGameScene);
        }

        gameOverWasTriggered = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ReturnToMainMenu()
    {
        StartCoroutine(ReturnToMainMenuRoutine());
    }

    private IEnumerator ReturnToMainMenuRoutine()
    {
        Time.timeScale = 1f;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        Scene gameScene =
            SceneManager.GetSceneByName(gameSceneName);

        if (gameScene.IsValid() && gameScene.isLoaded)
        {
            AsyncOperation unloadOperation =
                SceneManager.UnloadSceneAsync(gameScene);

            if (unloadOperation != null)
            {
                yield return unloadOperation;
            }
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

        gameOverWasTriggered = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}