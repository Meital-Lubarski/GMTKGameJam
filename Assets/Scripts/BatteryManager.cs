using General;
using UnityEngine;
using UnityEngine.UI;

public class BatteryManager : MonoBehaviour
{
    private const int TotalBatteryBars = 4;

    [Header("Battery")]
    [SerializeField, Min(1f)] private float maxCharge = 100f;
    [SerializeField] private bool startWithFullBattery = true;

    [Header("Battery UI")]
    [Tooltip("Drag the four battery bar Images here from left to right.")]
    [SerializeField] private Image[] batteryBars =
        new Image[TotalBatteryBars];

    private float currentCharge;

    public float CurrentCharge => currentCharge;
    public float MaxCharge => maxCharge;

    public float ChargeNormalized =>
        maxCharge <= 0f
            ? 0f
            : currentCharge / maxCharge;

    public int CurrentBars => CalculateCurrentBars();
    public bool IsEmpty => currentCharge <= 0f;

    private void Awake()
    {
        currentCharge = startWithFullBattery
            ? maxCharge
            : 0f;

        RefreshBatteryState();
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

        RefreshBatteryState();

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
    /// Adds a specific amount of charge.
    /// For example, 25 adds one quarter when maxCharge is 100.
    /// </summary>
    public void AddCharge(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        int previousBars = CurrentBars;

        currentCharge = Mathf.Min(
            maxCharge,
            currentCharge + amount
        );

        RefreshBatteryState();

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

        float chargePerBar =
            maxCharge / TotalBatteryBars;

        AddCharge(chargePerBar * numberOfBars);
    }

    public void RefillBattery()
    {
        int previousBars = CurrentBars;

        currentCharge = maxCharge;

        RefreshBatteryState();

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
            currentCharge / maxCharge;

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

    private void RefreshBatteryState()
    {
        UpdateBatteryUI();
    }

    private void UpdateBatteryUI()
    {
        if (batteryBars == null)
        {
            return;
        }

        int activeBars = CurrentBars;

        for (int i = 0; i < batteryBars.Length; i++)
        {
            if (batteryBars[i] == null)
            {
                continue;
            }

            batteryBars[i].enabled =
                i < activeBars;
        }
    }
}