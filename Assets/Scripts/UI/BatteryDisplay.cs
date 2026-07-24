using General;
using UnityEngine;

/// <summary>
/// Swaps a SpriteRenderer's sprite to match the remaining battery level.
/// Put this on the flashlight hand sprite so the held flashlight itself
/// shows how much battery is left.
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

    private SpriteRenderer spriteRenderer;
    private int lastDisplayedBars = -1;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (batteryManager == null)
        {
            batteryManager = FindFirstObjectByType<BatteryManager>();
        }
    }

    private void OnEnable()
    {
        EventManager.OnBarsChanged += ShowBars;
    }

    private void OnDisable()
    {
        EventManager.OnBarsChanged -= ShowBars;
    }

    /*
     * The battery only raises OnBarsChanged when a bar is actually lost or
     * gained, so the starting level is read once here. Start runs after every
     * Awake, which means the battery has already been filled by now.
     */
    private void Start()
    {
        if (batteryManager == null)
        {
            Debug.LogError(
                "BatteryDisplay could not find a BatteryManager in the scene.",
                this
            );

            return;
        }

        ShowBars(batteryManager.CurrentBars);
    }

    private void ShowBars(int bars)
    {
        if (bars == lastDisplayedBars)
        {
            return;
        }

        lastDisplayedBars = bars;

        Sprite barSprite = GetSpriteForBars(bars);

        spriteRenderer.sprite = barSprite;

        // Nothing to draw for this level, for example an empty battery
        // that has no sprite assigned.
        spriteRenderer.enabled = barSprite != null;
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
