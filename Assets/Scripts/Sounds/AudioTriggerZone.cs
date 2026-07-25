using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AudioTriggerZone : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField]
    private AudioClip loopClip;

    [SerializeField, Range(0f, 1f)]
    private float targetVolume = 0.7f;

    [SerializeField]
    private Transform soundOrigin;

    [Header("Fade")]
    [SerializeField]
    private float fadeInDuration = 1f;

    [SerializeField]
    private float fadeOutDuration = 1f;

    [Header("3D Sound")]
    [SerializeField]
    private float minDistance = 1f;

    [SerializeField]
    private float maxDistance = 15f;

    [Header("Player")]
    [SerializeField]
    private string playerTag = "Player";

    private AudioSourcePoolable currentLoop;
    private Coroutine fadeCoroutine;

    private int playerCollidersInside;

    private void Awake()
    {
        if (soundOrigin == null)
        {
            soundOrigin = transform;
        }

        Collider zoneCollider =
            GetComponent<Collider>();

        if (!zoneCollider.isTrigger)
        {
            Debug.LogWarning(
                $"{name}: AudioTriggerZone Collider " +
                "should have Is Trigger enabled."
            );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        playerCollidersInside++;

        if (playerCollidersInside == 1)
        {
            FadeIn();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        playerCollidersInside = Mathf.Max(
            0,
            playerCollidersInside - 1
        );

        if (playerCollidersInside == 0)
        {
            FadeOut();
        }
    }

    private bool IsPlayer(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            return true;
        }

        Transform root = other.transform.root;

        return root != null &&
               root.CompareTag(playerTag);
    }

    private void FadeIn()
    {
        if (loopClip == null ||
            SoundManager.Instance == null)
        {
            return;
        }

        if (currentLoop == null)
        {
            currentLoop =
                SoundManager.Instance
                    .PlayLoopAtPosition(
                        loopClip,
                        soundOrigin,
                        0f
                    );

            Configure3DSound();
        }

        StartFade(
            targetVolume,
            fadeInDuration,
            false
        );
    }

    private void FadeOut()
    {
        if (currentLoop == null)
        {
            return;
        }

        StartFade(
            0f,
            fadeOutDuration,
            true
        );
    }

    private void StartFade(
        float wantedVolume,
        float duration,
        bool stopWhenFinished)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(
            FadeRoutine(
                wantedVolume,
                duration,
                stopWhenFinished
            )
        );
    }

    private IEnumerator FadeRoutine(
        float wantedVolume,
        float duration,
        bool stopWhenFinished)
    {
        if (currentLoop == null)
        {
            yield break;
        }

        float startingVolume =
            currentLoop.Source.volume;

        float elapsed = 0f;

        if (duration <= 0f)
        {
            currentLoop.Source.volume =
                wantedVolume;
        }
        else
        {
            while (elapsed < duration &&
                   currentLoop != null)
            {
                elapsed += Time.deltaTime;

                float progress = Mathf.Clamp01(
                    elapsed / duration
                );

                currentLoop.Source.volume =
                    Mathf.Lerp(
                        startingVolume,
                        wantedVolume,
                        progress
                    );

                yield return null;
            }
        }

        if (currentLoop != null)
        {
            currentLoop.Source.volume =
                wantedVolume;
        }

        if (stopWhenFinished)
        {
            StopCurrentLoop();
        }

        fadeCoroutine = null;
    }

    private void Configure3DSound()
    {
        if (currentLoop == null)
        {
            return;
        }

        AudioSource source =
            currentLoop.Source;

        source.rolloffMode =
            AudioRolloffMode.Logarithmic;

        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.dopplerLevel = 0f;
    }

    private void StopCurrentLoop()
    {
        if (currentLoop == null)
        {
            return;
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopLoop(
                currentLoop
            );
        }

        currentLoop = null;
    }

    private void OnDisable()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        StopCurrentLoop();
    }
}