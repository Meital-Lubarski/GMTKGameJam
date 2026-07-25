using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BatteryPickup : MonoBehaviour, IInteractable
{
    [Header("Battery Recharge")]
    [SerializeField, Min(1)] private int barsToRecharge = 1;

    [Header("Respawn")]
    [Tooltip("Possible positions where this battery can reappear.")]
    [SerializeField] private Transform[] spawnPoints;

    [Tooltip("How long the battery stays hidden after collection.")]
    [SerializeField, Min(0f)] private float respawnDelay = 3f;

    [Tooltip("Avoid respawning at the same point when possible.")]
    [SerializeField] private bool avoidCurrentSpawnPoint = true;

    [Header("Sound")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField, Range(0f, 1f)] private float pickupVolume = 1f;

    private BatteryManager batteryManager;
    private PlayerStamina playerStamina;

    private Collider pickupCollider;
    private Renderer[] pickupRenderers;

    private bool isRespawning;
    private int currentSpawnPointIndex = -1;

    private void Awake()
    {
        batteryManager =
            FindFirstObjectByType<BatteryManager>();

        playerStamina =
            FindFirstObjectByType<PlayerStamina>();

        pickupCollider =
            GetComponent<Collider>();

        pickupRenderers =
            GetComponentsInChildren<Renderer>(true);

        currentSpawnPointIndex =
            FindClosestSpawnPointIndex();
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
            if (isRespawning || batteryManager == null)
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

        StartCoroutine(
            RespawnRoutine()
        );
    }

    private IEnumerator RespawnRoutine()
    {
        isRespawning = true;

        SetPickupVisible(false);

        if (respawnDelay > 0f)
        {
            yield return new WaitForSeconds(
                respawnDelay
            );
        }

        MoveToRandomSpawnPoint();

        SetPickupVisible(true);

        isRespawning = false;
    }

    private void MoveToRandomSpawnPoint()
    {
        if (
            spawnPoints == null ||
            spawnPoints.Length == 0
        )
        {
            Debug.LogWarning(
                "BatteryPickup has no spawn points assigned.",
                this
            );

            return;
        }

        int nextIndex =
            GetRandomSpawnPointIndex();

        Transform nextSpawnPoint =
            spawnPoints[nextIndex];

        if (nextSpawnPoint == null)
        {
            Debug.LogWarning(
                $"Spawn point at index {nextIndex} is null.",
                this
            );

            return;
        }

        transform.SetPositionAndRotation(
            nextSpawnPoint.position,
            nextSpawnPoint.rotation
        );

        currentSpawnPointIndex =
            nextIndex;
    }

    private int GetRandomSpawnPointIndex()
    {
        if (spawnPoints.Length == 1)
        {
            return 0;
        }

        int nextIndex;

        do
        {
            nextIndex = Random.Range(
                0,
                spawnPoints.Length
            );
        }
        while (
            avoidCurrentSpawnPoint &&
            nextIndex ==
            currentSpawnPointIndex
        );

        return nextIndex;
    }

    private int FindClosestSpawnPointIndex()
    {
        if (
            spawnPoints == null ||
            spawnPoints.Length == 0
        )
        {
            return -1;
        }

        int closestIndex = -1;
        float closestDistance =
            float.MaxValue;

        for (
            int i = 0;
            i < spawnPoints.Length;
            i++
        )
        {
            if (spawnPoints[i] == null)
            {
                continue;
            }

            float distance =
                Vector3.SqrMagnitude(
                    transform.position -
                    spawnPoints[i].position
                );

            if (
                distance <
                closestDistance
            )
            {
                closestDistance =
                    distance;

                closestIndex =
                    i;
            }
        }

        return closestIndex;
    }

    private void SetPickupVisible(
        bool isVisible
    )
    {
        if (pickupCollider != null)
        {
            pickupCollider.enabled =
                isVisible;
        }

        foreach (
            Renderer pickupRenderer
            in pickupRenderers
        )
        {
            if (pickupRenderer != null)
            {
                pickupRenderer.enabled =
                    isVisible;
            }
        }
    }
}