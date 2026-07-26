using System.Collections;
using UnityEngine;

/// <summary>
/// Keeps one battery in the level at all times.
///
/// It holds the kinds of battery that can turn up and the places they can turn
/// up in, and puts one of each together every time the last one is taken: a
/// battery picked at random from the list, at a spot picked at random from the
/// points - never the spot it was just taken from, so the player always has
/// somewhere new to walk to.
/// </summary>
public class BatterySpawner : MonoBehaviour
{
    [Header("Batteries")]
    [Tooltip(
        "The kinds of battery that can appear. One is picked at random each " +
        "time. They differ in how many bars they are worth, so a longer list " +
        "with more weak batteries in it makes weak ones more likely."
    )]
    [SerializeField] private BatteryPickup[] batteryPrefabs;

    [Header("Spawn Points")]
    [Tooltip(
        "Where a battery can appear. Left empty, every child of this object " +
        "is used, so the points can simply be parented under it."
    )]
    [SerializeField] private Transform[] spawnPoints;

    [Tooltip(
        "Never put the next battery where the last one was taken from. " +
        "Ignored when there is only one point to choose from."
    )]
    [SerializeField] private bool avoidLastSpawnPoint = true;

    [Header("Timing")]
    [Tooltip("How long the level goes without a battery after one is taken.")]
    [SerializeField, Min(0f)] private float respawnDelay = 3f;

    // Where the battery that is out there now came from, so the next one can
    // be sent somewhere else.
    private int lastSpawnPointIndex = -1;

    private BatteryPickup currentBattery;

    /// <summary>
    /// The battery lying in the level right now, or null during the gap
    /// between one being taken and the next appearing.
    /// </summary>
    public BatteryPickup CurrentBattery => currentBattery;

    private void Awake()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            spawnPoints = CollectChildSpawnPoints();
        }
    }

    private void Start()
    {
        if (!HasEverythingItNeeds())
        {
            enabled = false;
            return;
        }

        SpawnBattery();
    }

    /// <summary>
    /// Every child, in the order they sit in the hierarchy, so the points can
    /// be arranged in the scene rather than listed by hand.
    /// </summary>
    private Transform[] CollectChildSpawnPoints()
    {
        Transform[] childPoints = new Transform[transform.childCount];

        for (int i = 0; i < transform.childCount; i++)
        {
            childPoints[i] = transform.GetChild(i);
        }

        return childPoints;
    }

    private bool HasEverythingItNeeds()
    {
        if (batteryPrefabs == null || batteryPrefabs.Length == 0)
        {
            Debug.LogError(
                "BatterySpawner has no battery prefabs, so no battery will " +
                "ever appear.",
                this
            );

            return false;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError(
                "BatterySpawner has no spawn points and no children to use " +
                "as spawn points.",
                this
            );

            return false;
        }

        return true;
    }

    private void SpawnBattery()
    {
        int spawnPointIndex = ChooseSpawnPointIndex();

        Transform spawnPoint = spawnPoints[spawnPointIndex];

        if (spawnPoint == null)
        {
            Debug.LogError(
                $"BatterySpawner's spawn point at index {spawnPointIndex} is " +
                "empty, so no battery could be placed.",
                this
            );

            return;
        }

        BatteryPickup batteryPrefab = ChooseBatteryPrefab();

        if (batteryPrefab == null)
        {
            Debug.LogError(
                "BatterySpawner picked an empty battery prefab. Check the " +
                "list for missing entries.",
                this
            );

            return;
        }

        /*
         * Left unparented rather than put under the spawn point, so a point
         * that is moved, turned or scaled later cannot drag the battery that
         * is already lying there along with it.
         */
        currentBattery = Instantiate(
            batteryPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        currentBattery.Collected += HandleBatteryCollected;

        lastSpawnPointIndex = spawnPointIndex;
    }

    private int ChooseSpawnPointIndex()
    {
        if (spawnPoints.Length == 1)
        {
            return 0;
        }

        if (!avoidLastSpawnPoint || lastSpawnPointIndex < 0)
        {
            return Random.Range(0, spawnPoints.Length);
        }

        /*
         * Drawn from the points other than the last one and then stepped past
         * it, rather than drawn again until it comes up different. One draw,
         * every remaining point equally likely, and no chance of a run of bad
         * luck taking a while.
         */
        int index = Random.Range(0, spawnPoints.Length - 1);

        if (index >= lastSpawnPointIndex)
        {
            index++;
        }

        return index;
    }

    private BatteryPickup ChooseBatteryPrefab()
    {
        return batteryPrefabs[
            Random.Range(0, batteryPrefabs.Length)
        ];
    }

    private void HandleBatteryCollected(BatteryPickup battery)
    {
        // It destroys itself the moment it says this, so it is let go of here.
        battery.Collected -= HandleBatteryCollected;

        currentBattery = null;

        StartCoroutine(SpawnAfterDelay());
    }

    private IEnumerator SpawnAfterDelay()
    {
        if (respawnDelay > 0f)
        {
            yield return new WaitForSeconds(respawnDelay);
        }

        SpawnBattery();
    }

    private void OnDrawGizmosSelected()
    {
        Transform[] points =
            spawnPoints != null && spawnPoints.Length > 0
                ? spawnPoints
                : CollectChildSpawnPoints();

        Gizmos.color = Color.cyan;

        foreach (Transform point in points)
        {
            if (point != null)
            {
                Gizmos.DrawWireSphere(point.position, 0.25f);
            }
        }
    }
}
