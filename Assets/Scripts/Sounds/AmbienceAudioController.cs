using System.Collections;
using UnityEngine;

public class AmbienceAudioController : MonoBehaviour
{
    [Header("Gameplay Ambience")]
    [SerializeField] private AudioClip backgroundStaticClip;

    [Header("Clock")]
    [SerializeField] private AudioClip clockTickClip;
    [SerializeField] private float clockInterval = 1f;

    [Header("End Screen")]
    [SerializeField] private AudioClip endScreenAmbienceClip;

    [Header("Volumes")]
    [SerializeField, Range(0f, 1f)]
    private float staticVolume = 0.15f;

    [SerializeField, Range(0f, 1f)]
    private float clockVolume = 0.5f;

    [SerializeField, Range(0f, 1f)]
    private float endScreenVolume = 0.5f;

    [Header("Start Automatically")]
    [SerializeField] private bool playAmbienceOnStart = true;
    [SerializeField] private bool playClockOnStart = true;

    private AudioSourcePoolable staticLoop;
    private AudioSourcePoolable endScreenLoop;

    private Coroutine clockCoroutine;

    private void Start()
    {
        if (playAmbienceOnStart)
        {
            StartGameplayAmbience();
        }

        if (playClockOnStart)
        {
            StartClock();
        }
    }

    public void StartGameplayAmbience()
    {
        StopEndScreenAmbience();

        StartLoopIfNeeded(
            ref staticLoop,
            backgroundStaticClip,
            staticVolume
        );
    }

    public void StopGameplayAmbience()
    {
        StopLoop(ref staticLoop);
    }

    public void StartEndScreenAmbience()
    {
        StopGameplayAmbience();
        StopClock();

        StartLoopIfNeeded(
            ref endScreenLoop,
            endScreenAmbienceClip,
            endScreenVolume
        );
    }

    public void StopEndScreenAmbience()
    {
        StopLoop(ref endScreenLoop);
    }

    public void StartClock()
    {
        if (clockCoroutine != null ||
            clockTickClip == null)
        {
            return;
        }

        clockCoroutine = StartCoroutine(ClockRoutine());
    }

    public void StopClock()
    {
        if (clockCoroutine == null)
        {
            return;
        }

        StopCoroutine(clockCoroutine);
        clockCoroutine = null;
    }

    private IEnumerator ClockRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(
                Mathf.Max(0.05f, clockInterval)
            );

            if (SoundManager.Instance != null &&
                clockTickClip != null)
            {
                SoundManager.Instance.PlaySfx(
                    clockTickClip,
                    clockVolume
                );
            }
        }
    }

    private void StartLoopIfNeeded(
        ref AudioSourcePoolable loopAudio,
        AudioClip clip,
        float volume)
    {
        if (loopAudio != null ||
            clip == null ||
            SoundManager.Instance == null)
        {
            return;
        }

        loopAudio = SoundManager.Instance.PlayLoop(
            clip,
            volume
        );
    }

    private void StopLoop(
        ref AudioSourcePoolable loopAudio)
    {
        if (loopAudio == null)
        {
            return;
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopLoop(loopAudio);
        }

        loopAudio = null;
    }

    private void OnDisable()
    {
        StopGameplayAmbience();
        StopEndScreenAmbience();
        StopClock();
    }
}