using General;
using UnityEngine;

public class BatteryManager : MonoBehaviour
{
    private const int TotalBatteryBars = 4;

    [Header("Battery")]
    [Tooltip("How many seconds of flashlight use each of the four bars lasts.")]
    [SerializeField, Min(0.1f)] private float secondsPerBar = 5f;
    [SerializeField] private bool startWithFullBattery = true;

    private float currentCharge;

    public float CurrentCharge => currentCharge;

    // The battery is measured in seconds of flashlight use:
    // four bars of secondsPerBar each.
    public float MaxCharge => secondsPerBar * TotalBatteryBars;

    public float ChargeNormalized =>
        MaxCharge <= 0f
            ? 0f
            : currentCharge / MaxCharge;

    public int CurrentBars => CalculateCurrentBars();
    public bool IsEmpty => currentCharge <= 0f;

    /// <summary>
    /// Seconds of flashlight use left before the current bar is lost.
    /// Used to warn the player just before a bar disappears.
    /// </summary>
    public float SecondsUntilBarLost =>
        IsEmpty
            ? 0f
            : currentCharge - (CurrentBars - 1) * secondsPerBar;

    private void Awake()
    {
        currentCharge = startWithFullBattery
            ? MaxCharge
            : 0f;
    }

    /// <summary>
    /// Removes battery charge.
    /// The amount should already include Time.deltaTime
    /// when this is called continuously.
    /// </summary>
    public void Drain(float amount)
    {
        if (amount <= 0f || IsEmpty)
        {
            return;
        }

        int previousBars = CurrentBars;
        bool wasEmpty = IsEmpty;

        currentCharge = Mathf.Max(
            0f,
            currentCharge - amount
        );

        if (previousBars != CurrentBars)
        {
            EventManager.OnBarsChanged?.Invoke(CurrentBars);
        }

        EventManager.OnChargeChanged?.Invoke(currentCharge);

        if (!wasEmpty && IsEmpty)
        {
            EventManager.OnBatteryEmpty?.Invoke();
        }
    }

    /// <summary>
    /// Adds a specific amount of charge, measured in seconds
    /// of flashlight use.
    /// </summary>
    public void AddCharge(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        int previousBars = CurrentBars;

        currentCharge = Mathf.Min(
            MaxCharge,
            currentCharge + amount
        );

        if (previousBars != CurrentBars)
        {
            EventManager.OnBarsChanged?.Invoke(CurrentBars);
        }

        EventManager.OnChargeChanged?.Invoke(currentCharge);
    }

    /// <summary>
    /// Adds complete battery bars.
    /// RechargeBars(1) adds one bar.
    /// </summary>
    public void RechargeBars(int numberOfBars)
    {
        if (numberOfBars <= 0)
        {
            return;
        }

        AddCharge(secondsPerBar * numberOfBars);
    }

    public void RefillBattery()
    {
        int previousBars = CurrentBars;

        currentCharge = MaxCharge;

        if (previousBars != CurrentBars)
        {
            EventManager.OnBarsChanged?.Invoke(CurrentBars);
        }

        EventManager.OnChargeChanged?.Invoke(currentCharge);
    }

    public float GetStunDuration()
    {
        switch (CurrentBars)
        {
            case 4:
                return 10f;

            case 3:
                return 7f;

            case 2:
                return 5f;

            case 1:
                return 3f;

            default:
                return 0f;
        }
    }

    private int CalculateCurrentBars()
    {
        if (currentCharge <= 0f)
        {
            return 0;
        }

        float normalizedCharge =
            currentCharge / MaxCharge;

        if (normalizedCharge > 0.75f)
        {
            return 4;
        }

        if (normalizedCharge > 0.5f)
        {
            return 3;
        }

        if (normalizedCharge > 0.25f)
        {
            return 2;
        }

        return 1;
    }

}