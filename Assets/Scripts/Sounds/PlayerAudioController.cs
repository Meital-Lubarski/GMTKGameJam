using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAudioController : MonoBehaviour
{
    private enum BreathingState
    {
        None,
        Walking,
        Running
    }

    public static PlayerAudioController Instance
    {
        get;
        private set;
    }

    [Header("Player References")]
    [SerializeField]
    private CharacterController characterController;

    [SerializeField]
    private InputActionReference sprintAction;

    [Header("Flashlight Reference")]
    [SerializeField]
    private Light flashlight;

    [Header("Footsteps")]
    [SerializeField]
    private AudioClip[] footstepClips;

    [SerializeField]
    private float minimumMovementSpeed = 0.15f;

    [SerializeField]
    private float runningSpeed = 5f;

    [SerializeField]
    private float walkingStepInterval = 0.55f;

    [SerializeField]
    private float runningStepInterval = 0.32f;

    [SerializeField, Range(0f, 1f)]
    private float footstepVolume = 0.7f;

    [SerializeField, Range(0f, 0.3f)]
    private float footstepPitchVariation = 0.05f;

    [Header("Breathing")]
    [SerializeField]
    private AudioClip walkingBreathingLoop;

    [SerializeField]
    private AudioClip runningBreathingLoop;

    [SerializeField]
    private AudioClip exhaustedClip;

    [SerializeField, Range(0f, 1f)]
    private float walkingBreathingVolume = 0.2f;

    [SerializeField, Range(0f, 1f)]
    private float runningBreathingVolume = 0.5f;

    [SerializeField, Range(0f, 1f)]
    private float exhaustedVolume = 0.8f;

    [Header("Flashlight Sounds")]
    [SerializeField]
    private AudioClip flashlightOnClip;

    [SerializeField]
    private AudioClip flashlightOffClip;

    [SerializeField]
    private AudioClip flashlightStaticLoop;

    [SerializeField, Range(0f, 1f)]
    private float flashlightToggleVolume = 0.8f;

    [SerializeField, Range(0f, 1f)]
    private float flashlightStaticVolume = 0.25f;

    [Header("Death Sounds")]
    [SerializeField]
    private AudioClip playerChokingClip;

    [SerializeField]
    private AudioClip playerDeathClip;

    [SerializeField, Range(0f, 1f)]
    private float chokingVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float deathVolume = 1f;

    private AudioSourcePoolable breathingLoop;
    private AudioSourcePoolable flashlightLoop;

    private BreathingState currentBreathingState;

    private float footstepTimer;
    private bool previousFlashlightState;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        previousFlashlightState = IsFlashlightOn();

        if (previousFlashlightState)
        {
            StartFlashlightStatic();
        }
    }

    private void Update()
    {
        UpdateMovementAudio();
        UpdateFlashlightAudio();
    }

    private void UpdateMovementAudio()
    {
        if (characterController == null)
        {
            return;
        }

        Vector3 velocity = characterController.velocity;
        velocity.y = 0f;

        float speed = velocity.magnitude;

        bool isMoving =
            speed >= minimumMovementSpeed;

        bool isGrounded =
            characterController.isGrounded;

        bool sprintPressed =
            sprintAction != null &&
            sprintAction.action != null &&
            sprintAction.action.IsPressed();

        bool isRunning =
            isMoving &&
            (sprintPressed || speed >= runningSpeed);

        UpdateFootsteps(
            isMoving,
            isGrounded,
            isRunning
        );

        UpdateBreathing(
            isMoving,
            isRunning
        );
    }

    private void UpdateFootsteps(
        bool isMoving,
        bool isGrounded,
        bool isRunning)
    {
        if (!isMoving || !isGrounded)
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

        footstepTimer = isRunning
            ? runningStepInterval
            : walkingStepInterval;
    }

    private void PlayFootstep()
    {
        AudioClip clip = GetRandomClip(footstepClips);

        if (clip == null ||
            SoundManager.Instance == null)
        {
            return;
        }

        float pitch = Random.Range(
            1f - footstepPitchVariation,
            1f + footstepPitchVariation
        );

        SoundManager.Instance.PlaySfx(
            clip,
            footstepVolume,
            pitch
        );
    }

    private void UpdateBreathing(
        bool isMoving,
        bool isRunning)
    {
        BreathingState wantedState;

        if (!isMoving)
        {
            wantedState = BreathingState.None;
        }
        else if (isRunning)
        {
            wantedState = BreathingState.Running;
        }
        else
        {
            wantedState = BreathingState.Walking;
        }

        if (wantedState == currentBreathingState)
        {
            return;
        }

        ChangeBreathingState(wantedState);
    }

    private void ChangeBreathingState(
        BreathingState newState)
    {
        StopBreathingLoop();

        currentBreathingState = newState;

        if (SoundManager.Instance == null)
        {
            return;
        }

        switch (newState)
        {
            case BreathingState.Walking:
                if (walkingBreathingLoop != null)
                {
                    breathingLoop =
                        SoundManager.Instance.PlayLoop(
                            walkingBreathingLoop,
                            walkingBreathingVolume
                        );
                }

                break;

            case BreathingState.Running:
                if (runningBreathingLoop != null)
                {
                    breathingLoop =
                        SoundManager.Instance.PlayLoop(
                            runningBreathingLoop,
                            runningBreathingVolume
                        );
                }

                break;
        }
    }

    private void UpdateFlashlightAudio()
    {
        bool currentFlashlightState =
            IsFlashlightOn();

        if (currentFlashlightState ==
            previousFlashlightState)
        {
            return;
        }

        if (currentFlashlightState)
        {
            PlayPlayerSound(
                flashlightOnClip,
                flashlightToggleVolume
            );

            StartFlashlightStatic();
        }
        else
        {
            PlayPlayerSound(
                flashlightOffClip,
                flashlightToggleVolume
            );

            StopFlashlightStatic();
        }

        previousFlashlightState =
            currentFlashlightState;
    }

    private bool IsFlashlightOn()
    {
        return flashlight != null &&
               flashlight.enabled &&
               flashlight.intensity > 0.01f;
    }

    private void StartFlashlightStatic()
    {
        if (flashlightLoop != null ||
            flashlightStaticLoop == null ||
            SoundManager.Instance == null)
        {
            return;
        }

        flashlightLoop =
            SoundManager.Instance.PlayLoop(
                flashlightStaticLoop,
                flashlightStaticVolume
            );
    }

    private void StopFlashlightStatic()
    {
        if (flashlightLoop == null)
        {
            return;
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopLoop(
                flashlightLoop
            );
        }

        flashlightLoop = null;
    }

    public void PlayExhausted()
    {
        PlayPlayerSound(
            exhaustedClip,
            exhaustedVolume
        );
    }

    public void PlayPlayerChoking()
    {
        StopBreathingLoop();

        PlayPlayerSound(
            playerChokingClip,
            chokingVolume
        );
    }

    public void PlayPlayerDeath()
    {
        StopBreathingLoop();
        StopFlashlightStatic();

        PlayPlayerSound(
            playerDeathClip,
            deathVolume
        );
    }

    private void PlayPlayerSound(
        AudioClip clip,
        float volume)
    {
        if (clip == null ||
            SoundManager.Instance == null)
        {
            return;
        }

        SoundManager.Instance.PlaySfx(
            clip,
            volume
        );
    }

    private void StopBreathingLoop()
    {
        if (breathingLoop == null)
        {
            return;
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopLoop(
                breathingLoop
            );
        }

        breathingLoop = null;
    }

    private AudioClip GetRandomClip(
        AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
        {
            return null;
        }

        return clips[
            Random.Range(0, clips.Length)
        ];
    }

    private void OnDisable()
    {
        StopBreathingLoop();
        StopFlashlightStatic();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}