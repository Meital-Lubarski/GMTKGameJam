using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// The player's own noises. Like the ghost's, they are made from the
/// AudioManager, which outlives every scene, while the player it listens to is
/// built again with each run - so he is looked for again every time a scene
/// is loaded rather than held onto across one.
/// </summary>
public class PlayerAudioController : MonoBehaviour
{
    public static PlayerAudioController Instance
    {
        get;
        private set;
    }

    [Header("Player References")]
    [Tooltip(
        "The player this listens to. Leave it empty when the AudioManager " +
        "and the player are in different scenes: he is found in whichever " +
        "scene is loaded, which is the only thing that survives a scene change."
    )]
    [SerializeField]
    private CharacterController characterController;

    [SerializeField]
    private InputActionReference sprintAction;

    [Header("Walking Footsteps")]
    [SerializeField]
    private AudioClip[] walkingFootstepClips;

    [SerializeField]
    private float minimumMovementSpeed = 0.15f;

    [SerializeField]
    private float stepInterval = 0.55f;

    [SerializeField, Range(0f, 1f)]
    private float footstepVolume = 0.7f;

    [SerializeField, Range(0f, 0.3f)]
    private float footstepPitchVariation = 0.05f;

    [Header("Running Panting")]
    [SerializeField]
    private AudioClip runningPantingLoop;

    [SerializeField, Range(0f, 1f)]
    private float runningPantingVolume = 0.5f;

    [Header("Low Stamina")]
    [SerializeField]
    private AudioClip lowStaminaLoopClip;

    [SerializeField, Range(0f, 1f)]
    private float lowStaminaVolume = 0.7f;

    [Tooltip(
        "The Low Stamina sound starts when stamina " +
        "falls below this percentage."
    )]
    [SerializeField, Range(0.01f, 1f)]
    private float lowStaminaThreshold = 0.25f;

    [Header("Choking And Death")]
    [SerializeField]
    private AudioClip playerChokingClip;

    [SerializeField]
    private AudioClip playerDeathClip;

    [SerializeField, Range(0f, 1f)]
    private float chokingVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float deathVolume = 1f;

    [SerializeField]
    private float deathSoundDelay = 1.5f;

    private float stepTimer;
    private int previousClipIndex = -1;

    private AudioSourcePoolable pantingLoop;
    private AudioSourcePoolable lowStaminaLoop;

    private Coroutine chokingAndDeathCoroutine;

    private bool isLowStamina;
    private bool isDead;

    /*
     * A second one leaves the first alone rather than taking its place: the
     * duplicate is on its way out, and pointing everyone at it would leave
     * them holding something that is about to be destroyed.
     */
    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;

        ResolveCharacterController();
    }

    /// <summary>
    /// A new scene means a new player, and the one this was listening to is
    /// gone. The run also starts over, so nothing is carried in from the last.
    /// </summary>
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        characterController = null;
        isDead = false;

        ResolveCharacterController();
    }

    /// <summary>
    /// The player who is in the game right now. One dragged in by hand is used
    /// as it stands; otherwise he is looked for, which is what lets this sit
    /// in one scene and speak for a player in another.
    /// </summary>
    private void ResolveCharacterController()
    {
        if (characterController != null)
        {
            return;
        }

        characterController =
            FindFirstObjectByType<CharacterController>();
    }

    private void Update()
    {
        if (characterController == null ||
            isDead)
        {
            return;
        }

        Vector3 horizontalVelocity =
            characterController.velocity;

        horizontalVelocity.y = 0f;

        bool isMoving =
            horizontalVelocity.magnitude >=
            minimumMovementSpeed;

        bool isGrounded =
            characterController.isGrounded;

        bool sprintPressed =
            sprintAction != null &&
            sprintAction.action != null &&
            sprintAction.action.IsPressed();

        bool isRunning =
            isMoving &&
            isGrounded &&
            sprintPressed;

        UpdateFootsteps(
            isMoving,
            isGrounded
        );

        /*
         * Low Stamina overrides the regular
         * running panting sound.
         */
        UpdateRunningPanting(
            isRunning && !isLowStamina
        );
    }

    private void UpdateFootsteps(
        bool isMoving,
        bool isGrounded)
    {
        if (!isMoving || !isGrounded)
        {
            stepTimer = 0f;
            return;
        }

        stepTimer -= Time.deltaTime;

        if (stepTimer > 0f)
        {
            return;
        }

        PlayRandomFootstep();

        stepTimer = stepInterval;
    }

    private void PlayRandomFootstep()
    {
        if (walkingFootstepClips == null ||
            walkingFootstepClips.Length == 0 ||
            SoundManager.Instance == null)
        {
            return;
        }

        int clipIndex = GetRandomClipIndex();

        AudioClip selectedClip =
            walkingFootstepClips[clipIndex];

        if (selectedClip == null)
        {
            return;
        }

        previousClipIndex = clipIndex;

        float randomPitch = Random.Range(
            1f - footstepPitchVariation,
            1f + footstepPitchVariation
        );

        SoundManager.Instance.PlaySfx(
            selectedClip,
            footstepVolume,
            randomPitch
        );
    }

    private int GetRandomClipIndex()
    {
        if (walkingFootstepClips.Length == 1)
        {
            return 0;
        }

        int randomIndex;

        do
        {
            randomIndex = Random.Range(
                0,
                walkingFootstepClips.Length
            );
        }
        while (randomIndex == previousClipIndex);

        return randomIndex;
    }

    private void UpdateRunningPanting(
        bool shouldPant)
    {
        if (shouldPant)
        {
            StartPanting();
        }
        else
        {
            StopPanting();
        }
    }

    private void StartPanting()
    {
        if (pantingLoop != null ||
            runningPantingLoop == null ||
            SoundManager.Instance == null)
        {
            return;
        }

        pantingLoop =
            SoundManager.Instance.PlayLoop(
                runningPantingLoop,
                runningPantingVolume
            );
    }

    private void StopPanting()
    {
        if (pantingLoop == null)
        {
            return;
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopLoop(
                pantingLoop
            );
        }

        pantingLoop = null;
    }

    public void UpdateStaminaAudio(
        float currentStamina,
        float maximumStamina)
    {
        if (maximumStamina <= 0f)
        {
            SetLowStamina(false);
            return;
        }

        float normalizedStamina =
            Mathf.Clamp01(
                currentStamina / maximumStamina
            );

        UpdateStaminaNormalized(
            normalizedStamina
        );
    }

    public void UpdateStaminaNormalized(
        float normalizedStamina)
    {
        normalizedStamina =
            Mathf.Clamp01(normalizedStamina);

        bool shouldBeLow =
            !isDead &&
            normalizedStamina <= lowStaminaThreshold;

        SetLowStamina(shouldBeLow);
    }

    public void SetLowStamina(
        bool lowStamina)
    {
        if (isLowStamina == lowStamina)
        {
            return;
        }

        isLowStamina = lowStamina;

        if (isLowStamina)
        {
            StopPanting();
            StartLowStaminaSound();
        }
        else
        {
            StopLowStaminaSound();
        }
    }

    private void StartLowStaminaSound()
    {
        if (lowStaminaLoop != null ||
            lowStaminaLoopClip == null ||
            SoundManager.Instance == null)
        {
            return;
        }

        lowStaminaLoop =
            SoundManager.Instance.PlayLoop(
                lowStaminaLoopClip,
                lowStaminaVolume
            );
    }

    private void StopLowStaminaSound()
    {
        if (lowStaminaLoop == null)
        {
            return;
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopLoop(
                lowStaminaLoop
            );
        }

        lowStaminaLoop = null;
    }

    public void PlayPlayerChoking()
    {
        StopMovementLoops();

        PlayPlayerSound(
            playerChokingClip,
            chokingVolume
        );
    }

    public void PlayPlayerDeath()
    {
        isDead = true;

        StopMovementLoops();

        PlayPlayerSound(
            playerDeathClip,
            deathVolume
        );
    }

    public void PlayChokingAndDeath()
    {
        if (chokingAndDeathCoroutine != null)
        {
            return;
        }

        chokingAndDeathCoroutine =
            StartCoroutine(
                ChokingAndDeathRoutine()
            );
    }

    private IEnumerator ChokingAndDeathRoutine()
    {
        isDead = true;

        StopMovementLoops();

        PlayPlayerSound(
            playerChokingClip,
            chokingVolume
        );

        yield return new WaitForSeconds(
            Mathf.Max(0f, deathSoundDelay)
        );

        PlayPlayerSound(
            playerDeathClip,
            deathVolume
        );

        chokingAndDeathCoroutine = null;
    }

    private void StopMovementLoops()
    {
        StopPanting();
        StopLowStaminaSound();

        isLowStamina = false;
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

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        StopMovementLoops();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}