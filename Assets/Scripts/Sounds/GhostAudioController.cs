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

    [Header("Ghost Footsteps")]
    [SerializeField]
    private AudioClip[] footstepClips;

    [SerializeField]
    private float minimumMovementSpeed = 0.1f;

    [SerializeField]
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

    [SerializeField]
    private float minimumVoiceInterval = 2.5f;

    [SerializeField]
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

    [Header("3D Sound Distance")]
    [SerializeField]
    private float minDistance = 2f;

    [SerializeField]
    private float maxDistance = 35f;

    private Coroutine voiceCoroutine;

    private Vector3 previousPosition;
    private float footstepTimer;

    private bool isStunned;

    public bool IsStunned => isStunned;

    private void Awake()
    {
        Instance = this;

        if (ghostTransform == null)
        {
            if (ghostAgent != null)
            {
                ghostTransform = ghostAgent.transform;
            }
            else
            {
                ghostTransform = transform;
            }
        }
    }

    private void OnEnable()
    {
        if (ghostTransform != null)
        {
            previousPosition = ghostTransform.position;
        }

        if (playFrequentVoices && !isStunned)
        {
            StartFrequentVoices();
        }
    }

    private void Update()
    {
        if (isStunned)
        {
            return;
        }

        UpdateFootsteps();
    }

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
            Vector3 velocity = ghostAgent.velocity;
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

        return movement.magnitude / Time.deltaTime;
    }

    private void PlayFootstep()
    {
        AudioClip clip = GetRandomClip(
            footstepClips
        );

        if (clip == null)
        {
            return;
        }

        float pitch = Random.Range(
            1f - footstepPitchVariation,
            1f + footstepPitchVariation
        );

        PlayGhostSound(
            clip,
            footstepVolume,
            pitch,
            false
        );
    }

    public void StartFrequentVoices()
    {
        if (voiceCoroutine != null ||
            isStunned ||
            !playFrequentVoices)
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
            float minimumInterval =
                Mathf.Min(
                    minimumVoiceInterval,
                    maximumVoiceInterval
                );

            float maximumInterval =
                Mathf.Max(
                    minimumVoiceInterval,
                    maximumVoiceInterval
                );

            float delay = Random.Range(
                minimumInterval,
                maximumInterval
            );

            yield return new WaitForSeconds(
                Mathf.Max(0.1f, delay)
            );

            if (!isStunned)
            {
                PlayRandomVoice();
            }
        }
    }

    public void PlayRandomVoice()
    {
        if (isStunned)
        {
            return;
        }

        AudioClip clip = GetRandomClip(
            frequentVoiceClips
        );

        PlayGhostSound(
            clip,
            voiceVolume,
            1f,
            true
        );
    }

    public void PlayAttackStarted()
    {
        if (isStunned)
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
        if (isStunned)
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
        footstepTimer = 0f;

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

        source.rolloffMode =
            AudioRolloffMode.Logarithmic;

        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
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

        return clips[
            Random.Range(0, clips.Length)
        ];
    }

    private void OnDisable()
    {
        StopFrequentVoices();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}