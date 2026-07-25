using UnityEngine;

/// <summary>
/// Swaps a SpriteRenderer's sprite to match the remaining battery level.
/// Put this on the flashlight hand sprite so the held flashlight itself
/// shows how much battery is left.
///
/// Just before a bar runs out the sprite flickers between the current level
/// and the next lower one, so the bar looks like it is about to go out.
/// The flicker happens during the final seconds of that bar's own lifetime,
/// so it never makes the battery last longer.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BatteryDisplay : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Leave empty to find the BatteryManager in the scene automatically.")]
    [SerializeField] private BatteryManager batteryManager;

    [Header("Bar Sprites")]
    [Tooltip(
        "Indexed by the number of bars left, so element 0 is the empty " +
        "flashlight and element 4 is the full one."
    )]
    [SerializeField] private Sprite[] barSprites = new Sprite[5];

    [Header("Low Bar Warning")]
    [Tooltip(
        "How many seconds before a bar is lost the flicker starts. " +
        "This time is part of the bar's own lifetime, not added to it, " +
        "so keep it smaller than Seconds Per Bar."
    )]
    [SerializeField, Min(0f)] private float blinkDuration = 2f;

    [Tooltip("Seconds between flickers. Smaller values flicker faster.")]
    [SerializeField, Min(0.01f)] private float blinkInterval = 0.15f;

    private SpriteRenderer spriteRenderer;
    private int lastDisplayedBars = -1;
    private float previousCharge;
    private float blinkTimer;

    // While flickering, the sprite alternates to the level below the current one.
    private bool isShowingNextLevel;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (batteryManager == null)
        {
            batteryManager = FindFirstObjectByType<BatteryManager>();
        }
    }

    /*
     * Start rather than OnEnable, so the battery has already been filled
     * by its own Awake before the first level is read.
     */
    private void Start()
    {
        if (batteryManager == null)
        {
            Debug.LogError(
                "BatteryDisplay could not find a BatteryManager in the scene.",
                this
            );

            enabled = false;
            return;
        }

        previousCharge = batteryManager.CurrentCharge;

        RefreshLevel(batteryManager.CurrentBars);
    }

    private void Update()
    {
        float currentCharge = batteryManager.CurrentCharge;

        /*
         * The charge only falls while the flashlight is switched on, so this
         * also keeps the warning from flickering while the light is off or
         * while a collected battery is topping the charge back up.
         */
        bool isDraining = currentCharge < previousCharge;

        previousCharge = currentCharge;

        RefreshLevel(batteryManager.CurrentBars);
        UpdateFlicker(isDraining);
    }

    private void RefreshLevel(int bars)
    {
        if (bars == lastDisplayedBars)
        {
            return;
        }

        lastDisplayedBars = bars;

        // A newly reached level always starts on its own sprite.
        blinkTimer = 0f;
        isShowingNextLevel = false;

        ApplySprite();
    }

    private void UpdateFlicker(bool isDraining)
    {
        bool shouldFlicker =
            isDraining &&
            blinkDuration > 0f &&
            batteryManager.SecondsUntilBarLost <= blinkDuration;

        if (!shouldFlicker)
        {
            StopFlicker();
            return;
        }

        blinkTimer += Time.deltaTime;

        if (blinkTimer < blinkInterval)
        {
            return;
        }

        blinkTimer = 0f;
        isShowingNextLevel = !isShowingNextLevel;

        ApplySprite();
    }

    private void StopFlicker()
    {
        blinkTimer = 0f;

        if (!isShowingNextLevel)
        {
            return;
        }

        // Snap back to the real level so the flicker never leaves the
        // wrong sprite showing.
        isShowingNextLevel = false;

        ApplySprite();
    }

    private void ApplySprite()
    {
        int barsToShow = isShowingNextLevel
            ? lastDisplayedBars - 1
            : lastDisplayedBars;

        spriteRenderer.sprite = GetSpriteForBars(barsToShow);
    }

    private Sprite GetSpriteForBars(int bars)
    {
        if (barSprites == null || barSprites.Length == 0)
        {
            return null;
        }

        int index = Mathf.Clamp(bars, 0, barSprites.Length - 1);

        return barSprites[index];
    }
}
