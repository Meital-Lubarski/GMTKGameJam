using UnityEngine;

public class GameplayAudioController : MonoBehaviour
{
    public static GameplayAudioController Instance
    {
        get;
        private set;
    }

    [Header("Battery Pickup")]
    [SerializeField]
    private AudioClip batteryPickupClip;

    [SerializeField, Range(0f, 1f)]
    private float batteryPickupVolume = 0.8f;

    [Header("UI Buttons")]
    [SerializeField]
    private AudioClip buttonHighlightClip;

    [SerializeField]
    private AudioClip buttonClickClip;

    [SerializeField, Range(0f, 1f)]
    private float buttonHighlightVolume = 0.6f;

    [SerializeField, Range(0f, 1f)]
    private float buttonClickVolume = 0.8f;

    [Header("Battery 3D Sound")]
    [SerializeField]
    private float pickupMinDistance = 1f;

    [SerializeField]
    private float pickupMaxDistance = 10f;

    private float lastButtonHighlightTime = -10f;

    private void Awake()
    {
        Instance = this;
    }

    public void PlayBatteryPickup()
    {
        PlayInterfaceSound(
            batteryPickupClip,
            batteryPickupVolume
        );
    }

    public void PlayBatteryPickup(
        Vector3 pickupPosition)
    {
        PlayWorldSound(
            batteryPickupClip,
            pickupPosition,
            batteryPickupVolume,
            pickupMinDistance,
            pickupMaxDistance
        );
    }

    public void PlayBatteryPickupAtTransform(
        Transform pickupTransform)
    {
        if (pickupTransform == null)
        {
            return;
        }

        PlayBatteryPickup(
            pickupTransform.position
        );
    }

    public void PlayButtonHighlight()
    {
        if (Time.unscaledTime -
            lastButtonHighlightTime < 0.05f)
        {
            return;
        }

        lastButtonHighlightTime =
            Time.unscaledTime;

        PlayInterfaceSound(
            buttonHighlightClip,
            buttonHighlightVolume
        );
    }

    public void PlayButtonClick()
    {
        PlayInterfaceSound(
            buttonClickClip,
            buttonClickVolume
        );
    }

    private void PlayInterfaceSound(
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

    private void PlayWorldSound(
        AudioClip clip,
        Vector3 position,
        float volume,
        float minimumDistance,
        float maximumDistance)
    {
        if (clip == null ||
            SoundManager.Instance == null)
        {
            return;
        }

        AudioSourcePoolable audio =
            SoundManager.Instance.PlaySfxAtPosition(
                clip,
                position,
                volume
            );

        if (audio == null)
        {
            return;
        }

        AudioSource source = audio.Source;

        source.rolloffMode =
            AudioRolloffMode.Logarithmic;

        source.minDistance =
            minimumDistance;

        source.maxDistance =
            maximumDistance;

        source.dopplerLevel = 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}