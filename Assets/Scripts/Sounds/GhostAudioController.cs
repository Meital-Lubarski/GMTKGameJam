using UnityEngine;

public class GhostAudioController : MonoBehaviour
{
    public static GhostAudioController Instance
    {
        get;
        private set;
    }

    [Header("Ghost Reference")]
    [Tooltip(
        "Drag the main Ghost object here. " +
        "The proximity sound will follow this Transform."
    )]
    [SerializeField]
    private Transform ghostTransform;

    [Header("Ghost Proximity Loop")]
    [Tooltip(
        "Continuous looping sound that is loud when the ghost " +
        "is close and quiet when the ghost is far away."
    )]
    [SerializeField]
    private AudioClip ghostProximityLoopClip;

    [SerializeField, Range(0f, 1f)]
    private float ghostProximityVolume = 0.8f;

    [Tooltip(
        "Inside this distance, the proximity sound is heard " +
        "at its full configured volume."
    )]
    [SerializeField, Min(0.01f)]
    private float proximityMinDistance = 2f;

    [Tooltip(
        "At this distance and beyond, the proximity sound " +
        "is heard at its quietest level."
    )]
    [SerializeField, Min(0.01f)]
    private float proximityMaxDistance = 35f;

    [Header("Attack Sounds")]
    [SerializeField]
    private AudioClip attackStartedClip;

    [SerializeField]
    private AudioClip ghostChokingClip;

    [SerializeField, Range(0f, 1f)]
    private float attackVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float chokingVolume = 1f;

    [Header("Stun Sounds")]
    [SerializeField]
    private AudioClip stunInClip;

    [SerializeField]
    private AudioClip stunOutClip;

    [SerializeField, Range(0f, 1f)]
    private float stunInVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float stunOutVolume = 1f;

    [Header("Event Sound Distance")]
    [Tooltip(
        "3D distance settings used by attack, choking " +
        "and stun sounds."
    )]
    [SerializeField, Min(0.01f)]
    private float eventMinDistance = 2f;

    [SerializeField, Min(0.01f)]
    private float eventMaxDistance = 35f;

    private AudioSourcePoolable ghostProximityLoop;

    private bool isStunned;
    private bool hasStarted;

    public bool IsStunned => isStunned;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Debug.LogWarning(
                "More than one GhostAudioController exists.",
                this
            );

            return;
        }

        Instance = this;
    }

    private void Start()
    {
        hasStarted = true;

        StartGhostAudio();
    }

    private void OnEnable()
    {
        /*
         * Start runs only once. This restarts the loop if
         * the component is disabled and enabled afterward.
         */
        if (hasStarted)
        {
            StartGhostAudio();
        }
    }

    private void StartGhostAudio()
    {
        if (ghostTransform == null)
        {
            return;
        }

        StartGhostProximityLoop();
    }

    #region Proximity Loop

    public void StartGhostProximityLoop()
    {
        if (ghostProximityLoop != null ||
            ghostProximityLoopClip == null ||
            ghostTransform == null ||
            SoundManager.Instance == null)
        {
            return;
        }

        /*
         * PlayLoopAtPosition makes the pooled AudioSource
         * a child of the ghost Transform.
         *
         * Therefore, when the AI moves the ghost,
         * the AudioSource moves together with it.
         */
        ghostProximityLoop =
            SoundManager.Instance.PlayLoopAtPosition(
                ghostProximityLoopClip,
                ghostTransform,
                ghostProximityVolume
            );

        if (ghostProximityLoop == null)
        {
            return;
        }

        Configure3DAudioSource(
            ghostProximityLoop.Source,
            proximityMinDistance,
            proximityMaxDistance
        );
    }

    public void StopGhostProximityLoop()
    {
        if (ghostProximityLoop == null)
        {
            return;
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopLoop(
                ghostProximityLoop
            );
        }

        ghostProximityLoop = null;
    }

    #endregion

    #region Attack Sounds

    public void PlayAttackStarted()
    {
        if (isStunned)
        {
            return;
        }

        PlayGhostEventSound(
            attackStartedClip,
            attackVolume
        );
    }

    public void PlayGhostChoking()
    {
        if (isStunned)
        {
            return;
        }

        PlayGhostEventSound(
            ghostChokingClip,
            chokingVolume
        );
    }

    #endregion

    #region Stun Sounds

    public void EnterStun()
    {
        SetStunned(true);
    }

    public void ExitStun()
    {
        SetStunned(false);
    }

    public void SetStunned(bool stunned)
    {
        if (isStunned == stunned)
        {
            return;
        }

        isStunned = stunned;

        if (isStunned)
        {
            PlayGhostEventSound(
                stunInClip,
                stunInVolume
            );
        }
        else
        {
            PlayGhostEventSound(
                stunOutClip,
                stunOutVolume
            );
        }
    }

    #endregion

    #region Runtime Ghost Assignment

    /// <summary>
    /// Use this when the ghost is spawned during gameplay
    /// instead of already existing in the scene.
    /// </summary>
    public void SetGhostTransform(
        Transform newGhostTransform)
    {
        StopGhostProximityLoop();

        ghostTransform = newGhostTransform;

        if (ghostTransform != null &&
            isActiveAndEnabled)
        {
            StartGhostProximityLoop();
        }
    }

    #endregion

    #region Shared Audio

    private void PlayGhostEventSound(
        AudioClip clip,
        float volume)
    {
        if (clip == null ||
            ghostTransform == null ||
            SoundManager.Instance == null)
        {
            return;
        }

        AudioSourcePoolable audio =
            SoundManager.Instance.PlaySfxAtPosition(
                clip,
                ghostTransform.position,
                volume
            );

        if (audio == null)
        {
            return;
        }

        /*
         * Attach the event sound to the ghost so that
         * it follows the ghost while the clip is playing.
         */
        audio.transform.SetParent(
            ghostTransform
        );

        audio.transform.localPosition =
            Vector3.zero;

        Configure3DAudioSource(
            audio.Source,
            eventMinDistance,
            eventMaxDistance
        );
    }

    private void Configure3DAudioSource(
        AudioSource source,
        float minimumDistance,
        float maximumDistance)
    {
        if (source == null)
        {
            return;
        }

        float validMinimumDistance =
            Mathf.Max(
                0.01f,
                minimumDistance
            );

        float validMaximumDistance =
            Mathf.Max(
                validMinimumDistance,
                maximumDistance
            );

        /*
         * Fully 3D audio.
         * Unity calculates the distance from this AudioSource
         * to the AudioListener on the player's camera.
         */
        source.spatialBlend = 1f;

        source.rolloffMode =
            AudioRolloffMode.Logarithmic;

        source.minDistance =
            validMinimumDistance;

        source.maxDistance =
            validMaximumDistance;

        source.dopplerLevel = 0f;
    }

    #endregion

    private void OnDisable()
    {
        StopGhostProximityLoop();
    }

    private void OnDestroy()
    {
        StopGhostProximityLoop();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnValidate()
    {
        proximityMinDistance =
            Mathf.Max(
                0.01f,
                proximityMinDistance
            );

        proximityMaxDistance =
            Mathf.Max(
                proximityMinDistance,
                proximityMaxDistance
            );

        eventMinDistance =
            Mathf.Max(
                0.01f,
                eventMinDistance
            );

        eventMaxDistance =
            Mathf.Max(
                eventMinDistance,
                eventMaxDistance
            );
    }
}