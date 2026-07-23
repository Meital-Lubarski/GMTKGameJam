using System.Collections;
using System.Collections.Generic;
using General;
using UnityEngine;

public class SoundManager : MonoSingleton<SoundManager>
{
    [Header("Default Volumes")]
    [SerializeField, Range(0f, 1f)] private float defaultSfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float defaultLoopVolume = 1f;

    private readonly HashSet<AudioSourcePoolable> activeLoops = new();

    public AudioSourcePoolable PlaySfx(AudioClip clip)
    {
        return PlaySfx(clip, defaultSfxVolume, 1f);
    }

    public AudioSourcePoolable PlaySfx(
        AudioClip clip,
        float volume,
        float pitch = 1f)
    {
        if (!CanPlay(clip))
        {
            return null;
        }

        AudioSourcePoolable pooledAudio = AudioPool.Instance.Get();
        AudioSource source = pooledAudio.Source;

        ConfigureSource(
            pooledAudio,
            clip,
            volume,
            pitch,
            false,
            0f
        );

        source.Play();

        StartCoroutine(ReturnAfterPlaying(pooledAudio));

        return pooledAudio;
    }

    public AudioSourcePoolable PlaySfxAtPosition(
        AudioClip clip,
        Vector3 position,
        float volume = 1f,
        float pitch = 1f)
    {
        if (!CanPlay(clip))
        {
            return null;
        }

        AudioSourcePoolable pooledAudio = AudioPool.Instance.Get();
        AudioSource source = pooledAudio.Source;

        pooledAudio.transform.position = position;

        ConfigureSource(
            pooledAudio,
            clip,
            volume,
            pitch,
            false,
            1f
        );

        source.Play();

        StartCoroutine(ReturnAfterPlaying(pooledAudio));

        return pooledAudio;
    }

    public AudioSourcePoolable PlayLoop(AudioClip clip)
    {
        return PlayLoop(clip, defaultLoopVolume, 1f);
    }

    public AudioSourcePoolable PlayLoop(
        AudioClip clip,
        float volume,
        float pitch = 1f)
    {
        if (!CanPlay(clip))
        {
            return null;
        }

        AudioSourcePoolable pooledAudio = AudioPool.Instance.Get();

        ConfigureSource(
            pooledAudio,
            clip,
            volume,
            pitch,
            true,
            0f
        );

        pooledAudio.Source.Play();
        activeLoops.Add(pooledAudio);

        return pooledAudio;
    }

    public AudioSourcePoolable PlayLoopAtPosition(
        AudioClip clip,
        Transform followTarget,
        float volume = 1f,
        float pitch = 1f)
    {
        if (!CanPlay(clip))
        {
            return null;
        }

        AudioSourcePoolable pooledAudio = AudioPool.Instance.Get();

        if (followTarget != null)
        {
            pooledAudio.transform.SetParent(followTarget);
            pooledAudio.transform.localPosition = Vector3.zero;
        }

        ConfigureSource(
            pooledAudio,
            clip,
            volume,
            pitch,
            true,
            1f
        );

        pooledAudio.Source.Play();
        activeLoops.Add(pooledAudio);

        return pooledAudio;
    }

    public void StopLoop(AudioSourcePoolable loopAudio)
    {
        if (loopAudio == null)
        {
            return;
        }

        if (!activeLoops.Remove(loopAudio))
        {
            return;
        }

        ReturnAudioToPool(loopAudio);
    }

    public void StopAllLoops()
    {
        AudioSourcePoolable[] loops =
            new AudioSourcePoolable[activeLoops.Count];

        activeLoops.CopyTo(loops);
        activeLoops.Clear();

        foreach (AudioSourcePoolable loopAudio in loops)
        {
            if (loopAudio != null)
            {
                ReturnAudioToPool(loopAudio);
            }
        }
    }

    public void SetLoopVolume(
        AudioSourcePoolable loopAudio,
        float volume)
    {
        if (loopAudio == null)
        {
            return;
        }

        loopAudio.Source.volume = Mathf.Clamp01(volume);
    }

    public void SetLoopPitch(
        AudioSourcePoolable loopAudio,
        float pitch)
    {
        if (loopAudio == null)
        {
            return;
        }

        loopAudio.Source.pitch = Mathf.Clamp(pitch, -3f, 3f);
    }

    private void ConfigureSource(
        AudioSourcePoolable pooledAudio,
        AudioClip clip,
        float volume,
        float pitch,
        bool loop,
        float spatialBlend)
    {
        AudioSource source = pooledAudio.Source;

        source.clip = clip;
        source.volume = Mathf.Clamp01(volume);
        source.pitch = Mathf.Clamp(pitch, -3f, 3f);
        source.loop = loop;
        source.spatialBlend = Mathf.Clamp01(spatialBlend);
        source.playOnAwake = false;
    }

    private IEnumerator ReturnAfterPlaying(
        AudioSourcePoolable pooledAudio)
    {
        AudioSource source = pooledAudio.Source;

        while (source != null && source.isPlaying)
        {
            yield return null;
        }

        if (pooledAudio != null &&
            pooledAudio.gameObject.activeSelf)
        {
            ReturnAudioToPool(pooledAudio);
        }
    }

    private void ReturnAudioToPool(
        AudioSourcePoolable pooledAudio)
    {
        if (pooledAudio == null)
        {
            return;
        }

        pooledAudio.Source.Stop();
        pooledAudio.transform.SetParent(AudioPool.Instance.transform);
        pooledAudio.transform.localPosition = Vector3.zero;

        AudioPool.Instance.Return(pooledAudio);
    }

    private bool CanPlay(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning(
                "SoundManager received a null AudioClip."
            );

            return false;
        }

        if (AudioPool.Instance == null)
        {
            Debug.LogError(
                "No AudioPool exists in the current scene."
            );

            return false;
        }

        return true;
    }

    private void OnDestroy()
    {
        StopAllLoops();
    }
}