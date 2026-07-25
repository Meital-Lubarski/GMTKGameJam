using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class GhostAudioController : MonoBehaviour
{
    public static GhostAudioController Instance
    {
        get;
        private set;
    }

    [Header("Ghost References")]
    [SerializeField]
    private Transform ghostTransform;

    [SerializeField]
    private NavMeshAgent ghostAgent;

    [Header("Ghost Static Loop")]
    [Tooltip(
        "A continuous 3D static sound. " +
        "It is loud when the ghost is close and quiet when far away."
    )]
    [SerializeField]
    private AudioClip ghostStaticLoopClip;

    [SerializeField, Range(0f, 1f)]
    private float ghostStaticVolume = 0.8f;

    [SerializeField, Min(0.01f)]
    private float staticMinDistance = 2f;

    [SerializeField, Min(0.01f)]
    private float staticMaxDistance = 35f;

    [Header("Ghost Footsteps")]
    [SerializeField]
    private AudioClip[] footstepClips;

    [SerializeField, Min(0f)]
    private float minimumMovementSpeed = 0.1f;

    [SerializeField, Min(0.05f)]
    private float footstepInterval = 0.6f;

    [SerializeField, Range(0f, 1f)]
    private float footstepVolume = 0.9f;

    [SerializeField, Range(0f, 0.3f)]
    private float footstepPitchVariation = 0.04f;

    [Header("Frequent Ghost Voices")]
    [SerializeField]
    private AudioClip[] frequentVoiceClips;

    [SerializeField]
    private bool playFrequentVoices = true;

    [SerializeField, Min(0.1f)]
    private float minimumVoiceInterval = 2.5f;

    [SerializeField, Min(0.1f)]
    private float maximumVoiceInterval = 5f;

    [SerializeField, Range(0f, 1f)]
    private float voiceVolume = 0.9f;

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

    [Header("General 3D Sound Distance")]
    [SerializeField, Min(0.01f)]
    private float minDistance = 2f;

    [SerializeField, Min(0.01f)]
    private float maxDistance = 35f;

    private AudioSourcePoolable ghostStaticLoop;
    private Coroutine voiceCoroutine;

    private Vector3 previousPosition;

    private float footstepTimer;
    private int previousFootstepIndex = -1;

    private bool isStunned;
    private bool hasStarted;

    public bool IsStunned => isStunned;

    private void Awake()
    {
        Instance = this;

        /*
         * The AudioManager prefab cannot permanently reference
         * scene objects. When a Ghost Agent is assigned in the
         * scene, its Transform is used automatically.
         *
         * There is intentionally no fallback to this.transform,
         * because this component may be on the AudioManager.
         */
        if (ghostTransform == null &&
            ghostAgent != null)
        {
            ghostTransform = ghostAgent.transform;
        }
    }

    private void Start()
    {
        hasStarted = true;

        StartGhostAudio();
    }

    private void OnEnable()
    {
        /*
         * Start is called only once. This restarts the audio
         * if this component is disabled and enabled later.
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

        previousPosition = ghostTransform.position;
        footstepTimer = 0f;

        StartGhostStaticLoop();

        if (playFrequentVoices &&
            !isStunned)
        {
            StartFrequentVoices();
        }
    }

    private void Update()
    {
        if (ghostTransform == null ||
            isStunned)
        {
            return;
        }

        UpdateFootsteps();
    }

    #region Ghost Static

    public void StartGhostStaticLoop()
    {
        if (ghostStaticLoop != null ||
            ghostStaticLoopClip == null ||
            ghostTransform == null ||
            SoundManager.Instance == null)
        {
            return;
        }

        /*
         * PlayLoopAtPosition parents the pooled AudioSource
         * to the ghost, so it moves together with it.
         */
        ghostStaticLoop =
            SoundManager.Instance.PlayLoopAtPosition(
                ghostStaticLoopClip,
                ghostTransform,
                ghostStaticVolume
            );

        if (ghostStaticLoop == null)
        {
            return;
        }

        AudioSource source = ghostStaticLoop.Source;

        /*
         * Spatial Blend 1 makes this a fully 3D sound.
         * Unity changes the volume automatically according
         * to the distance from the Audio Listener.
         */
        source.spatialBlend = 1f;

        source.rolloffMode =
            AudioRolloffMode.Logarithmic;

        source.minDistance =
            Mathf.Max(
                0.01f,
                staticMinDistance
            );

        source.maxDistance =
            Mathf.Max(
                source.minDistance,
                staticMaxDistance
            );

        source.dopplerLevel = 0f;
    }

    public void StopGhostStaticLoop()
    {
        if (ghostStaticLoop == null)
        {
            return;
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopLoop(
                ghostStaticLoop
            );
        }

        ghostStaticLoop = null;
    }

    #endregion

    #region Footsteps

    private void UpdateFootsteps()
    {
        float speed = GetGhostSpeed();

        if (speed < minimumMovementSpeed)
        {
            footstepTimer = 0f;
            return;
        }

        footstepTimer -= Time.deltaTime;

        if (footstepTimer > 0f)
        {
            return;
        }

        PlayFootstep();

        footstepTimer = footstepInterval;
    }

    private float GetGhostSpeed()
    {
        if (ghostAgent != null &&
            ghostAgent.enabled &&
            ghostAgent.isOnNavMesh)
        {
            Vector3 velocity =
                ghostAgent.velocity;

            velocity.y = 0f;

            return velocity.magnitude;
        }

        if (ghostTransform == null ||
            Time.deltaTime <= 0f)
        {
            return 0f;
        }

        Vector3 currentPosition =
            ghostTransform.position;

        Vector3 movement =
            currentPosition - previousPosition;

        movement.y = 0f;

        previousPosition = currentPosition;

        return movement.magnitude /
               Time.deltaTime;
    }

    private void PlayFootstep()
    {
        AudioClip clip =
            GetRandomFootstepClip();

        if (clip == null)
        {
            return;
        }

        float randomPitch = Random.Range(
            1f - footstepPitchVariation,
            1f + footstepPitchVariation
        );

        /*
         * Footsteps remain at the position where the step
         * happened instead of following the ghost afterward.
         */
        PlayGhostSound(
            clip,
            footstepVolume,
            randomPitch,
            false
        );
    }

    private AudioClip GetRandomFootstepClip()
    {
        if (footstepClips == null ||
            footstepClips.Length == 0)
        {
            return null;
        }

        if (footstepClips.Length == 1)
        {
            previousFootstepIndex = 0;

            return footstepClips[0];
        }

        int randomIndex;

        do
        {
            randomIndex = Random.Range(
                0,
                footstepClips.Length
            );
        }
        while (
            randomIndex ==
            previousFootstepIndex
        );

        previousFootstepIndex =
            randomIndex;

        return footstepClips[randomIndex];
    }

    #endregion

    #region Frequent Voices

    public void StartFrequentVoices()
    {
        if (voiceCoroutine != null ||
            ghostTransform == null ||
            isStunned ||
            !playFrequentVoices ||
            !HasValidClip(frequentVoiceClips))
        {
            return;
        }

        voiceCoroutine = StartCoroutine(
            FrequentVoiceRoutine()
        );
    }

    public void StopFrequentVoices()
    {
        if (voiceCoroutine == null)
        {
            return;
        }

        StopCoroutine(voiceCoroutine);

        voiceCoroutine = null;
    }

    private IEnumerator FrequentVoiceRoutine()
    {
        while (true)
        {
            float smallestInterval =
                Mathf.Min(
                    minimumVoiceInterval,
                    maximumVoiceInterval
                );

            float largestInterval =
                Mathf.Max(
                    minimumVoiceInterval,
                    maximumVoiceInterval
                );

            float delay = Random.Range(
                smallestInterval,
                largestInterval
            );

            yield return new WaitForSeconds(
                Mathf.Max(0.1f, delay)
            );

            if (!isStunned &&
                ghostTransform != null)
            {
                PlayRandomVoice();
            }
        }
    }

    public void PlayRandomVoice()
    {
        if (ghostTransform == null ||
            isStunned)
        {
            return;
        }

        AudioClip clip =
            GetRandomClip(
                frequentVoiceClips
            );

        /*
         * The voice follows the ghost while the clip plays.
         */
        PlayGhostSound(
            clip,
            voiceVolume,
            1f,
            true
        );
    }

    #endregion

    #region Attack

    public void PlayAttackStarted()
    {
        if (ghostTransform == null ||
            isStunned)
        {
            return;
        }

        PlayGhostSound(
            attackStartedClip,
            attackVolume,
            1f,
            true
        );
    }

    public void PlayGhostChoking()
    {
        if (ghostTransform == null ||
            isStunned)
        {
            return;
        }

        PlayGhostSound(
            ghostChokingClip,
            chokingVolume,
            1f,
            true
        );
    }

    #endregion

    #region Stun

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
        if (ghostTransform == null ||
            isStunned == stunned)
        {
            return;
        }

        isStunned = stunned;
        footstepTimer = 0f;

        /*
         * Prevents a false footstep after the ghost exits
         * stun or is moved while stunned.
         */
        previousPosition =
            ghostTransform.position;

        if (isStunned)
        {
            StopFrequentVoices();

            PlayGhostSound(
                stunInClip,
                stunInVolume,
                1f,
                true
            );
        }
        else
        {
            PlayGhostSound(
                stunOutClip,
                stunOutVolume,
                1f,
                true
            );

            if (playFrequentVoices)
            {
                StartFrequentVoices();
            }
        }
    }

    #endregion

    #region Runtime Reference Assignment

    /*
     * Use this only if the ghost is spawned during gameplay.
     * It allows another script to connect the spawned ghost
     * without editing this component manually.
     */
    public void SetGhostReferences(
        Transform newGhostTransform,
        NavMeshAgent newGhostAgent)
    {
        StopFrequentVoices();
        StopGhostStaticLoop();

        ghostAgent = newGhostAgent;

        if (newGhostTransform != null)
        {
            ghostTransform = newGhostTransform;
        }
        else if (newGhostAgent != null)
        {
            ghostTransform =
                newGhostAgent.transform;
        }
        else
        {
            ghostTransform = null;
        }

        if (ghostTransform != null &&
            isActiveAndEnabled)
        {
            StartGhostAudio();
        }
    }

    #endregion

    #region Shared Audio

    private void PlayGhostSound(
        AudioClip clip,
        float volume,
        float pitch,
        bool followGhost)
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
                volume,
                pitch
            );

        if (audio == null)
        {
            return;
        }

        AudioSource source = audio.Source;

        source.spatialBlend = 1f;

        source.rolloffMode =
            AudioRolloffMode.Logarithmic;

        source.minDistance =
            Mathf.Max(
                0.01f,
                minDistance
            );

        source.maxDistance =
            Mathf.Max(
                source.minDistance,
                maxDistance
            );

        source.dopplerLevel = 0f;

        if (followGhost)
        {
            audio.transform.SetParent(
                ghostTransform
            );

            audio.transform.localPosition =
                Vector3.zero;
        }
    }

    private AudioClip GetRandomClip(
        AudioClip[] clips)
    {
        if (clips == null ||
            clips.Length == 0)
        {
            return null;
        }

        int attempts = clips.Length;

        while (attempts > 0)
        {
            AudioClip selectedClip =
                clips[
                    Random.Range(
                        0,
                        clips.Length
                    )
                ];

            if (selectedClip != null)
            {
                return selectedClip;
            }

            attempts--;
        }

        return null;
    }

    private bool HasValidClip(
        AudioClip[] clips)
    {
        if (clips == null)
        {
            return false;
        }

        foreach (AudioClip clip in clips)
        {
            if (clip != null)
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    private void OnDisable()
    {
        StopFrequentVoices();
        StopGhostStaticLoop();
    }

    private void OnDestroy()
    {
        StopFrequentVoices();
        StopGhostStaticLoop();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnValidate()
    {
        staticMinDistance =
            Mathf.Max(
                0.01f,
                staticMinDistance
            );

        staticMaxDistance =
            Mathf.Max(
                staticMinDistance,
                staticMaxDistance
            );

        minDistance =
            Mathf.Max(
                0.01f,
                minDistance
            );

        maxDistance =
            Mathf.Max(
                minDistance,
                maxDistance
            );

        maximumVoiceInterval =
            Mathf.Max(
                minimumVoiceInterval,
                maximumVoiceInterval
            );
    }
}