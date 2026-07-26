using System.Collections;
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
        "Drag the main active Ghost object here, " +
        "not GhostVisuals or another child object."
    )]
    [SerializeField]
    private Transform ghostTransform;

    [Header("Ghost Proximity Loop")]
    [SerializeField]
    private AudioClip ghostProximityLoopClip;

    [SerializeField, Range(0f, 1f)]
    private float ghostProximityVolume = 1f;

    [SerializeField, Min(0.01f)]
    private float proximityMinDistance = 3f;

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
    [SerializeField, Min(0.01f)]
    private float eventMinDistance = 3f;

    [SerializeField, Min(0.01f)]
    private float eventMaxDistance = 35f;

    private AudioSourcePoolable ghostProximityLoop;

    private Coroutine initializationCoroutine;

    private bool isStunned;

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
        }

        Instance = this;
    }

    private void OnEnable()
    {
        initializationCoroutine =
            StartCoroutine(InitializeAudioRoutine());
    }

    private IEnumerator InitializeAudioRoutine()
    {
        const int maximumFramesToWait = 180;
        int waitedFrames = 0;

        while (
            (
                SoundManager.Instance == null ||
                AudioPool.Instance == null
            ) &&
            waitedFrames < maximumFramesToWait
        )
        {
            waitedFrames++;
            yield return null;
        }

        initializationCoroutine = null;

        if (SoundManager.Instance == null)
        {
            Debug.LogError(
                "GhostAudioController could not find SoundManager.",
                this
            );

            yield break;
        }

        if (AudioPool.Instance == null)
        {
            Debug.LogError(
                "GhostAudioController could not find AudioPool.",
                this
            );

            yield break;
        }

        if (ghostTransform == null)
        {
            Debug.LogError(
                "GhostAudioController has no Ghost Transform assigned.",
                this
            );

            yield break;
        }

        if (!ghostTransform.gameObject.activeInHierarchy)
        {
            Debug.LogError(
                "The assigned Ghost Transform is inactive. " +
                "Assign the main active Ghost object instead.",
                ghostTransform
            );

            yield break;
        }

        StartGhostProximityLoop();
    }

    public void StartGhostProximityLoop()
    {
        if (ghostProximityLoop != null)
        {
            return;
        }

        if (ghostProximityLoopClip == null)
        {
            Debug.LogError(
                "No Ghost Proximity Loop Clip is assigned.",
                this
            );

            return;
        }

        if (ghostTransform == null)
        {
            Debug.LogError(
                "No Ghost Transform is assigned.",
                this
            );

            return;
        }

        if (SoundManager.Instance == null ||
            AudioPool.Instance == null)
        {
            Debug.LogError(
                "The audio system is not ready.",
                this
            );

            return;
        }

        ghostProximityLoop =
            SoundManager.Instance.PlayLoopAtPosition(
                ghostProximityLoopClip,
                ghostTransform,
                ghostProximityVolume
            );

        if (ghostProximityLoop == null)
        {
            Debug.LogError(
                "The Ghost Proximity Loop could not be created.",
                this
            );

            return;
        }

        Configure3DAudioSource(
            ghostProximityLoop.Source,
            proximityMinDistance,
            proximityMaxDistance
        );

        Debug.Log(
            "Ghost proximity loop started successfully.",
            ghostTransform
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

    public void SetGhostTransform(
        Transform newGhostTransform)
    {
        StopGhostProximityLoop();

        ghostTransform = newGhostTransform;

        if (ghostTransform != null &&
            isActiveAndEnabled)
        {
            if (initializationCoroutine != null)
            {
                StopCoroutine(
                    initializationCoroutine
                );
            }

            initializationCoroutine =
                StartCoroutine(
                    InitializeAudioRoutine()
                );
        }
    }

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

        source.spatialBlend = 1f;

        source.rolloffMode =
            AudioRolloffMode.Logarithmic;

        source.minDistance =
            validMinimumDistance;

        source.maxDistance =
            validMaximumDistance;

        source.dopplerLevel = 0f;
    }

    private void OnDisable()
    {
        if (initializationCoroutine != null)
        {
            StopCoroutine(
                initializationCoroutine
            );

            initializationCoroutine = null;
        }

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