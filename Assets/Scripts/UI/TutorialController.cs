using System.Collections;
using General;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The last screen before the run. It loads the game scene underneath itself,
/// slides out of the way to reveal it, and then throws the whole menu scene
/// away: from that point the run owns the screen, and its own UI - the HUD,
/// the pause menu and the Game Over screen - lives in the game scene with it.
/// </summary>
public class TutorialController : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button flashlightButton;

    [Header("Tutorial Animation")]
    [SerializeField] private RectTransform tutorialPanel;
    [SerializeField] private float slideDuration = 1.2f;
    [SerializeField] private float extraSlideDistance = 100f;

    [Header("Screens Scene Camera - Optional")]
    [SerializeField] private Camera screensCamera;

    private const string GameSceneName = GameScenes.Game;
    private const string ScreensSceneName = GameScenes.Menu;

    private bool isStartingGame;

    private void OnEnable()
    {
        if (flashlightButton == null)
        {
            Debug.LogError(
                "TutorialController: Flashlight Button is not assigned.",
                this
            );

            return;
        }

        flashlightButton.onClick.AddListener(StartGame);
    }

    private void OnDisable()
    {
        if (flashlightButton != null)
        {
            flashlightButton.onClick.RemoveListener(StartGame);
        }
    }

    private void StartGame()
    {
        if (isStartingGame)
        {
            return;
        }

        if (tutorialPanel == null)
        {
            Debug.LogError(
                "TutorialController: Tutorial Panel is not assigned.",
                this
            );

            return;
        }

        isStartingGame = true;
        flashlightButton.interactable = false;

        StartCoroutine(StartGameSequence());
    }

    private IEnumerator StartGameSequence()
    {
        Time.timeScale = 1f;

        
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(
            GameSceneName,
            LoadSceneMode.Additive
        );

        if (loadOperation == null)
        {
            Debug.LogError(
                $"TutorialController: Could not start loading {GameSceneName}.",
                this
            );

            isStartingGame = false;
            flashlightButton.interactable = true;
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        Scene gameScene = SceneManager.GetSceneByName(GameSceneName);

        if (!gameScene.IsValid() || !gameScene.isLoaded)
        {
            Debug.LogError(
                $"TutorialController: Scene {GameSceneName} was not loaded.",
                this
            );

            isStartingGame = false;
            flashlightButton.interactable = true;
            yield break;
        }

        
        SceneManager.SetActiveScene(gameScene);

      
        if (screensCamera != null)
        {
            screensCamera.gameObject.SetActive(false);
        }

        yield return SlideTutorialUp();

        
        Scene screensScene = SceneManager.GetSceneByName(ScreensSceneName);

        if (screensScene.IsValid() && screensScene.isLoaded)
        {
            SceneManager.UnloadSceneAsync(screensScene);
        }
    }

    private IEnumerator SlideTutorialUp()
    {
        Vector2 startPosition = tutorialPanel.anchoredPosition;

        float slideDistance =
            tutorialPanel.rect.height + extraSlideDistance;


        slideDistance = Mathf.Max(
            slideDistance,
            Screen.height + extraSlideDistance
        );

        Vector2 endPosition =
            startPosition + Vector2.up * slideDistance;

        float elapsedTime = 0f;

        while (elapsedTime < slideDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / slideDuration
            );

            float smoothProgress = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            tutorialPanel.anchoredPosition = Vector2.Lerp(
                startPosition,
                endPosition,
                smoothProgress
            );

            yield return null;
        }

        tutorialPanel.anchoredPosition = endPosition;
    }
}