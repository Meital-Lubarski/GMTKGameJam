using General;
using TMPro;
using UnityEngine;

/// <summary>
/// Tracks how long the player survives the run and shows it on a UI label.
/// Starts when the run begins and stops automatically when the player is caught.
/// </summary>
public class SurvivalTimer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text timerText;

    [Header("Behaviour")]
    [Tooltip("Start counting automatically as soon as this component is enabled.")]
    [SerializeField] private bool startOnEnable = true;

    private float _elapsedSeconds;
    private bool _isRunning;
    private int _lastDisplayedSecond = -1;

    // Final survival time, e.g. for the ending scene to display.
    public float ElapsedSeconds => _elapsedSeconds;
    public string FormattedTime => FormatTime(_elapsedSeconds);

    private void OnEnable()
    {
        EventManager.OnPlayerCaught += StopTimer;

        if (startOnEnable) StartTimer();
    }

    private void OnDisable()
    {
        EventManager.OnPlayerCaught -= StopTimer;
    }

    private void Update()
    {
        if (!_isRunning) return;

        _elapsedSeconds += Time.deltaTime;
        RefreshText();
    }

    public void StartTimer()
    {
        _elapsedSeconds = 0f;
        _lastDisplayedSecond = -1;
        _isRunning = true;
        RefreshText();
    }

    public void StopTimer()
    {
        _isRunning = false;
    }

    // Only rebuild the string when the displayed second changes to avoid
    // allocating a new string every frame.
    private void RefreshText()
    {
        if (timerText == null) return;

        var totalSeconds = (int)_elapsedSeconds;
        if (totalSeconds == _lastDisplayedSecond) return;

        _lastDisplayedSecond = totalSeconds;
        timerText.text = FormatTime(_elapsedSeconds);
    }

    private static string FormatTime(float seconds)
    {
        var totalSeconds = (int)seconds;
        var minutes = totalSeconds / 60;
        var remainingSeconds = totalSeconds % 60;

        return $"{minutes:00}:{remainingSeconds:00}";
    }
}
