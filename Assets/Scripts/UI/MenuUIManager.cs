using UnityEngine;

public class MenuUIManager : MonoBehaviour
{
    [Header("Menu Screens")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private GameObject creditsPanel;

    private void Awake()
    {
        /*
         * The menu can be arrived at from a paused game or a frozen Game Over
         * screen, and both leave the clock stopped and the mouse taken away.
         * It takes them back rather than trusting whoever left.
         */
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        SetPanelStates(
            showMainMenu: true,
            showTutorial: false,
            showCredits: false
        );
    }

    public void ShowTutorial()
    {
        SetPanelStates(
            showMainMenu: false,
            showTutorial: true,
            showCredits: false
        );
    }

    public void ShowCredits()
    {
        SetPanelStates(
            showMainMenu: false,
            showTutorial: false,
            showCredits: true
        );
    }

    private void SetPanelStates(
        bool showMainMenu,
        bool showTutorial,
        bool showCredits
    )
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(showMainMenu);
        }

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(showTutorial);
        }

        if (creditsPanel != null)
        {
            creditsPanel.SetActive(showCredits);
        }
    }
}