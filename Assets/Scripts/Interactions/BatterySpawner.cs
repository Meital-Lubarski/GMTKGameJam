using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps a set number of batteries in the level at all times, and tightens
/// that number as the player collects: every so many pickups the level starts
/// holding one fewer, down to a floor, so power gets scarcer the longer the
/// run goes.
///
/// It holds the kinds of battery that can turn up and the places they can turn
/// up in, and puts one of each together every time one is taken: a battery
/// picked at random from the list, at a spot picked at random from the points -
/// never a spot that already has a battery on it, and never the spot the last
/// one was taken from, so the player always has somewhere new to walk to.
/// </summary>
public class BatterySpawner : MonoBehaviour
{
    // How long to wait before trying again after a spawn could not be placed.
    private const float RespawnRetryDelay = 1f;

    [Header("Batteries")]
    [Tooltip(
        "The kinds of battery that can appear. One is picked at random each " +
        "time. They differ in how many bars they are worth, so a longer list " +
        "with more weak batteries in it makes weak ones more likely.\n\n" +
        "These must be prefab assets dragged from the Project window. An " +
        "object that is already in the scene must never be used: the player " +
        "can collect it, collecting destroys it, and this entry goes empty " +
        "along with it."
    )]
    [SerializeField] private BatteryPickup[] batteryPrefabs;

    [Tooltip(
        "How many batteries lie in the level at the same time. Each one that " +
        "is taken is replaced on its own, so this many are out there whenever " +
        "the player is not inside a respawn delay. Cannot go above the number " +
        "of spawn points, since two batteries are never put in one spot."
    )]
    [SerializeField, Min(1)] private int batteriesInLevel = 1;

    [Header("Escalation")]
    [Tooltip(
        "Every this many batteries the player collects, the level keeps one " +
        "fewer out from then on, so power gets scarcer the longer the run " +
        "goes. Set to 0 to hold at Batteries In Level for the whole run."
    )]
    [SerializeField, Min(0)] private int batteriesCollectedPerDecrease = 2;

    [Tooltip(
        "The fewest batteries the level will ever go down to, however many " +
        "the player has collected. Below one the player would be left with " +
        "no way to recharge at all."
    )]
    [SerializeField, Min(1)] private int minBatteriesInLevel = 1;

    [Header("Spawn Points")]
    [Tooltip(
        "Where a battery can appear. Left empty, every child of this object " +
        "is used, so the points can simply be parented under it."
    )]
    [SerializeField] private Transform[] spawnPoints;

    [Tooltip(
        "Never put the next battery where the last one was taken from. " +
        "Ignored when it would leave nowhere else to go."
    )]
    [SerializeField] private bool avoidLastSpawnPoint = true;

    [Header("Timing")]
    [Tooltip("How long the level goes short a battery after one is taken.")]
    [SerializeField, Min(0f)] private float respawnDelay = 3f;

    // The batteries lying in the level right now, and the spawn point each one
    // came from. The two lists are kept in step: entry i of one belongs with
    // entry i of the other.
    private readonly List<BatteryPickup> liveBatteries =
        new List<BatteryPickup>();

    private readonly List<int> occupiedSpawnPointIndices =
        new List<int>();

    // The entries of batteryPrefabs that were actually filled in, so an empty
    // slot costs nothing at run time beyond one warning at startup.
    private readonly List<BatteryPickup> usableBatteryPrefabs =
        new List<BatteryPickup>();

    // Reused by the spawn point search so picking a spot does not allocate.
    private readonly List<int> freeSpawnPointIndices = new List<int>();

    // Where the battery that was taken most recently came from, so the next
    // one can be sent somewhere else.
    private int lastSpawnPointIndex = -1;

    // The list going empty mid-run is worth saying once, not every retry.
    private bool hasReportedEmptyPrefabList;

    /*
     * How many batteries the level is currently trying to hold. It starts at
     * batteriesInLevel and climbs as the player collects, which is why the
     * serialized field is never used directly after Start.
     */
    private int targetBatteriesInLevel;

    // Counted across the whole run, not reset when one is replaced.
    private int batteriesCollectedCount;

    /// <summary>
    /// The batteries lying in the level right now. Shrinks as they are taken
    /// and grows back as they are replaced.
    /// </summary>
    public IReadOnlyList<BatteryPickup> LiveBatteries => liveBatteries;

    /// <summary>
    /// How many batteries the level is holding out for at this point in the
    /// run. Starts at Batteries In Level and steps up as they are collected.
    /// </summary>
    public int TargetBatteriesInLevel => targetBatteriesInLevel;

    /// <summary>
    /// How many batteries the player has collected so far this run.
    /// </summary>
    public int BatteriesCollectedCount => batteriesCollectedCount;

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

        targetBatteriesInLevel = batteriesInLevel;

        for (int i = 0; i < targetBatteriesInLevel; i++)
        {
            // Nothing should stop the opening batteries from being placed, but
            // one that fails is queued rather than quietly dropped.
            if (!TrySpawnBattery())
            {
                StartCoroutine(TopUpAfterDelay());
            }
        }
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
        CollectUsableBatteryPrefabs();

        if (usableBatteryPrefabs.Count == 0)
        {
            Debug.LogError(
                "BatterySpawner has no battery prefabs filled in, so no " +
                "battery will ever appear.",
                this
            );

            return false;
        }

        int usablePointCount = CountUsableSpawnPoints();

        if (usablePointCount == 0)
        {
            Debug.LogError(
                "BatterySpawner has no spawn points and no children to use " +
                "as spawn points.",
                this
            );

            return false;
        }

        /*
         * Two batteries are never put in the same spot, so asking for more
         * batteries than there are places to put them can never be met. The
         * ask is lowered rather than refused: a level with fewer batteries
         * than intended still plays, one that throws on startup does not.
         */
        if (batteriesInLevel > usablePointCount)
        {
            Debug.LogWarning(
                $"BatterySpawner was asked for {batteriesInLevel} batteries " +
                $"but only has {usablePointCount} usable spawn points. " +
                $"Keeping {usablePointCount} in the level instead.",
                this
            );

            batteriesInLevel = usablePointCount;
        }

        /*
         * A floor above the opening count would mean the level starts below
         * its own lower limit, and the count could never come down at all.
         */
        if (minBatteriesInLevel > batteriesInLevel)
        {
            minBatteriesInLevel = batteriesInLevel;
        }

        return true;
    }

    /// <summary>
    /// Sorts the filled slots from the empty ones, and says which slots were
    /// left empty so they can be found in the Inspector.
    /// </summary>
    private void CollectUsableBatteryPrefabs()
    {
        usableBatteryPrefabs.Clear();

        if (batteryPrefabs == null)
        {
            return;
        }

        List<int> emptySlotIndices = null;

        for (int i = 0; i < batteryPrefabs.Length; i++)
        {
            if (batteryPrefabs[i] == null)
            {
                emptySlotIndices ??= new List<int>();
                emptySlotIndices.Add(i);

                continue;
            }

            usableBatteryPrefabs.Add(batteryPrefabs[i]);
        }

        if (emptySlotIndices != null)
        {
            Debug.LogWarning(
                "BatterySpawner has empty battery prefab slots at index " +
                string.Join(", ", emptySlotIndices) +
                ". They are skipped. An entry that was filled in the Editor " +
                "and is empty here was a scene object rather than a prefab " +
                "asset.",
                this
            );
        }
    }

    private int CountUsableSpawnPoints()
    {
        if (spawnPoints == null)
        {
            return 0;
        }

        int count = 0;

        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint != null)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Places one battery. False when there was nothing to place or nowhere to
    /// place it, which the caller is expected to come back from later rather
    /// than treat as the end of the matter.
    /// </summary>
    private bool TrySpawnBattery()
    {
        BatteryPickup batteryPrefab = ChooseBatteryPrefab();

        if (batteryPrefab == null)
        {
            if (!hasReportedEmptyPrefabList)
            {
                hasReportedEmptyPrefabList = true;

                Debug.LogError(
                    "BatterySpawner has run out of battery prefabs to pick " +
                    "from, so no further battery can appear. Every entry it " +
                    "was given is empty or has been destroyed.",
                    this
                );
            }

            return false;
        }

        int spawnPointIndex = ChooseSpawnPointIndex();

        if (spawnPointIndex < 0)
        {
            // Every point is taken at this instant. One frees up as soon as a
            // battery is collected, so this is worth retrying, not reporting.
            return false;
        }

        Transform spawnPoint = spawnPoints[spawnPointIndex];

        /*
         * Left unparented rather than put under the spawn point, so a point
         * that is moved, turned or scaled later cannot drag the battery that
         * is already lying there along with it.
         */
        BatteryPickup battery = Instantiate(
            batteryPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        battery.Collected += HandleBatteryCollected;

        liveBatteries.Add(battery);
        occupiedSpawnPointIndices.Add(spawnPointIndex);

        return true;
    }

    /// <summary>
    /// A free spot, or -1 when every point already holds a battery.
    /// </summary>
    private int ChooseSpawnPointIndex()
    {
        freeSpawnPointIndices.Clear();

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null)
            {
                continue;
            }

            if (occupiedSpawnPointIndices.Contains(i))
            {
                continue;
            }

            freeSpawnPointIndices.Add(i);
        }

        if (freeSpawnPointIndices.Count == 0)
        {
            return -1;
        }

        /*
         * Sending the replacement straight back to the spot the player just
         * emptied would undo the walk they were meant to make. It is only a
         * preference: when that spot is the last one free, it is used anyway.
         */
        if (
            avoidLastSpawnPoint &&
            lastSpawnPointIndex >= 0 &&
            freeSpawnPointIndices.Count > 1
        )
        {
            freeSpawnPointIndices.Remove(lastSpawnPointIndex);
        }

        return freeSpawnPointIndices[
            Random.Range(0, freeSpawnPointIndices.Count)
        ];
    }

    private BatteryPickup ChooseBatteryPrefab()
    {
        /*
         * An entry can go empty part way through a run when a scene object was
         * assigned instead of a prefab asset: collecting it destroys it, and
         * the reference dies with it. Dropping it here keeps one bad entry
         * from taking every later spawn down with it.
         */
        for (int i = usableBatteryPrefabs.Count - 1; i >= 0; i--)
        {
            if (usableBatteryPrefabs[i] == null)
            {
                usableBatteryPrefabs.RemoveAt(i);
            }
        }

        if (usableBatteryPrefabs.Count == 0)
        {
            return null;
        }

        return usableBatteryPrefabs[
            Random.Range(0, usableBatteryPrefabs.Count)
        ];
    }

    private void HandleBatteryCollected(BatteryPickup battery)
    {
        // It destroys itself the moment it says this, so it is let go of here.
        battery.Collected -= HandleBatteryCollected;

        int liveIndex = liveBatteries.IndexOf(battery);

        if (liveIndex >= 0)
        {
            lastSpawnPointIndex = occupiedSpawnPointIndices[liveIndex];

            liveBatteries.RemoveAt(liveIndex);
            occupiedSpawnPointIndices.RemoveAt(liveIndex);
        }

        batteriesCollectedCount++;

        LowerTargetIfDue();

        StartCoroutine(TopUpAfterDelay());
    }

    /// <summary>
    /// Steps the level down to holding one fewer battery once the player has
    /// collected enough of them, down to the floor.
    ///
    /// Called before the top up, so the collection that triggers a step down
    /// simply goes unreplaced rather than being replaced and then removed.
    /// </summary>
    private void LowerTargetIfDue()
    {
        if (batteriesCollectedPerDecrease <= 0)
        {
            return;
        }

        if (batteriesCollectedCount % batteriesCollectedPerDecrease != 0)
        {
            return;
        }

        if (targetBatteriesInLevel <= minBatteriesInLevel)
        {
            return;
        }

        targetBatteriesInLevel--;
    }

    /// <summary>
    /// Fills the level back up to whatever it is currently holding out for.
    /// Usually that is one battery - the replacement for the one just taken -
    /// but on the collection that triggers a step down it is none at all, and
    /// the level quietly comes out of that pickup one battery poorer.
    /// </summary>
    private IEnumerator TopUpAfterDelay()
    {
        if (respawnDelay > 0f)
        {
            yield return new WaitForSeconds(respawnDelay);
        }

        /*
         * Kept at until they land. A single failed attempt used to leave the
         * level one battery short for the rest of the run, with nothing but a
         * line in the console to say why.
         *
         * Two of these can be in flight at once when batteries are taken in
         * quick succession. That is safe: the count is re-read every pass, so
         * whichever one gets there first simply ends the other one's work.
         */
        while (liveBatteries.Count < targetBatteriesInLevel)
        {
            if (TrySpawnBattery())
            {
                continue;
            }

            if (usableBatteryPrefabs.Count == 0)
            {
                // Nothing left to place. Retrying would only spin.
                yield break;
            }

            yield return new WaitForSeconds(RespawnRetryDelay);
        }
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
