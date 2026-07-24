using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(CharacterController))]
public class PlayerStamina : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FirstPersonController firstPersonController;
    [SerializeField] private CharacterController characterController;

    [Tooltip("The Global Volume that contains the Vignette override.")]
    [SerializeField] private Volume postProcessingVolume;

    [Header("Stamina")]
    [SerializeField, Min(1f)]
    private float maxStamina = 100f;

    [Tooltip("Stamina lost every second, even while walking or standing.")]
    [SerializeField, Min(0f)]
    private float passiveDrainPerSecond = 3f;

    [Tooltip("Additional stamina lost every second while sprinting.")]
    [SerializeField, Min(0f)]
    private float extraSprintDrainPerSecond = 17f;

    [Tooltip("The player must be moving this fast for Shift to count as sprinting.")]
    [SerializeField, Min(0f)]
    private float minimumMovementSpeed = 0.1f;

    [Header("Vignette")]
    [SerializeField, Range(0f, 1f)]
    private float restedVignetteIntensity = 0.15f;

    [SerializeField, Range(0f, 1f)]
    private float exhaustedVignetteIntensity = 0.65f;

    [SerializeField, Min(0f)]
    private float vignetteSmoothSpeed = 5f;

    [Header("Exhaustion Sound")]
    [SerializeField]
    private AudioSource exhaustionAudioSource;

    [Tooltip("Exhaustion amount at which the sound begins. 0.6 means 60% tired.")]
    [SerializeField, Range(0f, 1f)]
    private float soundStartThreshold = 0.6f;

    [SerializeField, Range(0f, 1f)]
    private float maximumExhaustionVolume = 1f;

    [SerializeField, Min(0f)]
    private float soundSmoothSpeed = 4f;

    private Vignette vignette;

    private float currentStamina;
    private float normalSprintSpeed;

    private bool isExhausted;
    private bool isCurrentlySprinting;

    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;

    public float StaminaNormalized =>
        maxStamina <= 0f
            ? 0f
            : currentStamina / maxStamina;

    public bool IsExhausted => isExhausted;
    public bool IsSprinting => isCurrentlySprinting;

    private void Awake()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (firstPersonController == null)
        {
            firstPersonController = GetComponent<FirstPersonController>();
        }

        if (characterController == null)
        {
            Debug.LogError(
                "PlayerStamina could not find CharacterController.",
                this
            );
        }

        if (firstPersonController == null)
        {
            Debug.LogError(
                "PlayerStamina could not find FirstPersonController.",
                this
            );
        }
        else
        {
            normalSprintSpeed = firstPersonController.SprintSpeed;
        }

        currentStamina = maxStamina;

        FindVignette();

        if (exhaustionAudioSource != null)
        {
            exhaustionAudioSource.loop = true;
            exhaustionAudioSource.playOnAwake = false;
            exhaustionAudioSource.volume = 0f;
        }
    }

    private void Start()
    {
        UpdateVignetteImmediately();
    }

    private void Update()
    {
        CheckSprintState();
        DrainStamina();
        UpdateSprintAvailability();
        UpdateVignette();
        UpdateExhaustionSound();
    }

    private void CheckSprintState()
    {
        if (
            characterController == null ||
            firstPersonController == null ||
            isExhausted
        )
        {
            isCurrentlySprinting = false;
            return;
        }

        bool shiftIsPressed =
            Keyboard.current != null &&
            (
                Keyboard.current.leftShiftKey.isPressed ||
                Keyboard.current.rightShiftKey.isPressed
            );

        Vector3 horizontalVelocity =
            characterController.velocity;

        horizontalVelocity.y = 0f;

        bool playerIsMoving =
            horizontalVelocity.magnitude >
            minimumMovementSpeed;

        isCurrentlySprinting =
            shiftIsPressed &&
            playerIsMoving &&
            currentStamina > 0f;
    }

    private void DrainStamina()
    {
        if (currentStamina <= 0f)
        {
            currentStamina = 0f;
            isExhausted = true;
            isCurrentlySprinting = false;
            return;
        }

        float drainThisSecond =
            passiveDrainPerSecond;

        if (isCurrentlySprinting)
        {
            drainThisSecond +=
                extraSprintDrainPerSecond;
        }

        currentStamina -=
            drainThisSecond * Time.deltaTime;

        currentStamina = Mathf.Max(
            0f,
            currentStamina
        );

        if (currentStamina <= 0f)
        {
            isExhausted = true;
            isCurrentlySprinting = false;
        }
    }

    private void UpdateSprintAvailability()
    {
        if (firstPersonController == null)
        {
            return;
        }

        if (isExhausted || currentStamina <= 0f)
        {
            firstPersonController.SprintSpeed =
                firstPersonController.MoveSpeed;
        }
        else
        {
            firstPersonController.SprintSpeed =
                normalSprintSpeed;
        }
    }

    /// <summary>
    /// Completely refills stamina.
    /// Called when the player collects a battery.
    /// </summary>
    public void RefillStamina()
    {
        currentStamina = maxStamina;
        isExhausted = false;
        isCurrentlySprinting = false;

        UpdateSprintAvailability();
        UpdateVignetteImmediately();

        if (exhaustionAudioSource != null)
        {
            exhaustionAudioSource.Stop();
            exhaustionAudioSource.volume = 0f;
        }
    }

    /// <summary>
    /// Adds a specific amount of stamina.
    /// </summary>
    public void AddStamina(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        currentStamina = Mathf.Min(
            maxStamina,
            currentStamina + amount
        );

        if (currentStamina > 0f)
        {
            isExhausted = false;
        }

        UpdateSprintAvailability();
    }

    private void FindVignette()
    {
        if (postProcessingVolume == null)
        {
            Debug.LogError(
                "PlayerStamina has no Post Processing Volume assigned.",
                this
            );

            return;
        }

        if (postProcessingVolume.profile == null)
        {
            Debug.LogError(
                "The assigned Volume does not have a Volume Profile.",
                postProcessingVolume
            );

            return;
        }

        if (!postProcessingVolume.profile.TryGet(out vignette))
        {
            Debug.LogError(
                "The assigned Volume Profile does not contain a Vignette override.",
                postProcessingVolume
            );

            return;
        }

        vignette.active = true;
        vignette.intensity.overrideState = true;
        vignette.color.overrideState = true;
        vignette.color.value = Color.black;
    }

    private void UpdateVignette()
    {
        if (vignette == null)
        {
            return;
        }

        float exhaustionAmount =
            1f - StaminaNormalized;

        float targetIntensity = Mathf.Lerp(
            restedVignetteIntensity,
            exhaustedVignetteIntensity,
            exhaustionAmount
        );

        vignette.intensity.value = Mathf.MoveTowards(
            vignette.intensity.value,
            targetIntensity,
            vignetteSmoothSpeed * Time.deltaTime
        );
    }

    private void UpdateVignetteImmediately()
    {
        if (vignette == null)
        {
            return;
        }

        float exhaustionAmount =
            1f - StaminaNormalized;

        vignette.intensity.value = Mathf.Lerp(
            restedVignetteIntensity,
            exhaustedVignetteIntensity,
            exhaustionAmount
        );
    }

    private void UpdateExhaustionSound()
    {
        if (exhaustionAudioSource == null)
        {
            return;
        }

        float exhaustionAmount =
            1f - StaminaNormalized;

        float targetVolume = 0f;

        if (exhaustionAmount >= soundStartThreshold)
        {
            float soundAmount = Mathf.InverseLerp(
                soundStartThreshold,
                1f,
                exhaustionAmount
            );

            targetVolume =
                soundAmount * maximumExhaustionVolume;
        }

        exhaustionAudioSource.volume = Mathf.MoveTowards(
            exhaustionAudioSource.volume,
            targetVolume,
            soundSmoothSpeed * Time.deltaTime
        );

        if (
            targetVolume > 0f &&
            !exhaustionAudioSource.isPlaying
        )
        {
            exhaustionAudioSource.Play();
        }

        if (
            targetVolume <= 0f &&
            exhaustionAudioSource.isPlaying &&
            exhaustionAudioSource.volume <= 0.001f
        )
        {
            exhaustionAudioSource.Stop();
        }
    }

    private void OnDisable()
    {
        if (firstPersonController != null)
        {
            firstPersonController.SprintSpeed =
                normalSprintSpeed;
        }

        if (exhaustionAudioSource != null)
        {
            exhaustionAudioSource.Stop();
            exhaustionAudioSource.volume = 0f;
        }
    }

    private void OnValidate()
    {
        if (
            exhaustedVignetteIntensity <
            restedVignetteIntensity
        )
        {
            exhaustedVignetteIntensity =
                restedVignetteIntensity;
        }
    }
}