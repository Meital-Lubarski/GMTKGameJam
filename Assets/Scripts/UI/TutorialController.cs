using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialController : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button flashlightButton;

    [Header("Game Scene")]
    [SerializeField] private string gameSceneName = "MayScene";

    private void OnEnable()
    {
        if (flashlightButton != null)
        {
            flashlightButton.onClick.AddListener(StartGame);
        }
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
        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogError(
                "TutorialController has no game scene name assigned.",
                this
            );

            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }
}