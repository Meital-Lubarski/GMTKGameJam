using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;

    [Header("Manager")]
    [SerializeField] private MenuUIManager menuUIManager;

    private void OnEnable()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(HandleStartClicked);
        }

        if (creditsButton != null)
        {
            creditsButton.onClick.AddListener(HandleCreditsClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(HandleQuitClicked);
        }
    }

    private void OnDisable()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(HandleStartClicked);
        }

        if (creditsButton != null)
        {
            creditsButton.onClick.RemoveListener(HandleCreditsClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(HandleQuitClicked);
        }
    }

    private void HandleStartClicked()
    {
        if (menuUIManager == null)
        {
            Debug.LogError(
                "MainMenuController has no MenuUIManager assigned.",
                this
            );

            return;
        }

        menuUIManager.ShowTutorial();
    }

    private void HandleCreditsClicked()
    {
        if (menuUIManager == null)
        {
            Debug.LogError(
                "MainMenuController has no MenuUIManager assigned.",
                this
            );

            return;
        }

        menuUIManager.ShowCredits();
    }

    private void HandleQuitClicked()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}