using System;
using UnityEngine;

/// <summary>
/// A battery lying on the floor. Picking it up tops up the flashlight and the
/// player's breath, and takes it out of the world.
///
/// It knows nothing about where the next one comes from: it announces that it
/// has been taken through <see cref="Collected"/> and leaves. The
/// <see cref="BatterySpawner"/> listens and decides what appears next and
/// where. That is what lets several kinds of battery exist - they differ only
/// in how many bars they are worth - without any of them knowing that the
/// others exist.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BatteryPickup : MonoBehaviour, IInteractable
{
    [Header("Battery Recharge")]
    [Tooltip(
        "How many bars this battery is worth. This is what tells one kind of " +
        "battery from another."
    )]
    [SerializeField, Min(1)] private int barsToRecharge = 1;

    [Header("Sound")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField, Range(0f, 1f)] private float pickupVolume = 1f;

    /// <summary>
    /// Raised once, the moment this battery is taken. It is destroyed straight
    /// afterwards, so a listener must not hold on to it.
    /// </summary>
    public event Action<BatteryPickup> Collected;

    public int BarsToRecharge => barsToRecharge;

    private BatteryManager batteryManager;
    private PlayerStamina playerStamina;

    // A battery can only be taken once, however many times the key is pressed
    // in the frame before it is gone.
    private bool wasCollected;

    private void Awake()
    {
        batteryManager =
            FindFirstObjectByType<BatteryManager>();

        playerStamina =
            FindFirstObjectByType<PlayerStamina>();
    }

    /*
     * מאפשר איסוף אם:
     * - סוללת הפנס לא מלאה
     * או
     * - הסטמינה לא מלאה
     *
     * אחרת אין טעם לאסוף את הסוללה.
     *
     * זה גם מה שקובע אם ה-UI של הלחיצה על E מוצג, כך שהשחקן לא מתבקש
     * ללחוץ על כפתור שלא יעשה כלום.
     */
    public bool CanInteract
    {
        get
        {
            if (wasCollected || batteryManager == null)
            {
                return false;
            }

            bool flashlightBatteryIsFull =
                batteryManager.CurrentCharge >=
                batteryManager.MaxCharge;

            bool staminaIsFull =
                playerStamina == null ||
                playerStamina.CurrentStamina >=
                playerStamina.MaxStamina;

            return !flashlightBatteryIsFull ||
                   !staminaIsFull;
        }
    }

    public void Interact()
    {
        if (batteryManager == null)
        {
            Debug.LogError(
                "BatteryPickup could not find a BatteryManager in the scene.",
                this
            );

            return;
        }

        if (!CanInteract)
        {
            return;
        }

        wasCollected = true;

        batteryManager.RechargeBars(
            barsToRecharge
        );

        if (playerStamina != null)
        {
            playerStamina.RefillStamina();
        }
        else
        {
            Debug.LogWarning(
                "BatteryPickup could not find PlayerStamina. " +
                "The flashlight battery was recharged, but stamina was not refilled.",
                this
            );
        }

        /*
         * The sound is played through the SoundManager rather than from an
         * AudioSource here, so it carries on after this battery is gone.
         */
        if (
            pickupSound != null &&
            SoundManager.Instance != null
        )
        {
            SoundManager.Instance.PlaySfx(
                pickupSound,
                pickupVolume
            );
        }

        Collected?.Invoke(this);

        Destroy(gameObject);
    }
}
