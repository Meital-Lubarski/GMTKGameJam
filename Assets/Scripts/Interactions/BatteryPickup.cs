using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BatteryPickup : MonoBehaviour, IInteractable
{
    [Header("Battery Recharge")]
    [SerializeField, Min(1)] private int barsToRecharge = 1;

    [Header("Sound")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField, Range(0f, 1f)] private float pickupVolume = 1f;

    private BatteryManager batteryManager;
    private bool wasCollected;

    private void Awake()
    {
        batteryManager = FindFirstObjectByType<BatteryManager>();
    }

    public void Interact()
    {
        if (wasCollected)
        {
            return;
        }

        if (batteryManager == null)
        {
            Debug.LogError(
                "BatteryPickup could not find a BatteryManager in the scene.",
                this
            );

            return;
        }

        if (batteryManager.CurrentCharge >= batteryManager.MaxCharge)
        {
            return;
        }

        wasCollected = true;

        batteryManager.RechargeBars(barsToRecharge);

        if (pickupSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySfx(
                pickupSound,
                pickupVolume
            );
        }

        Destroy(gameObject);
    }
}