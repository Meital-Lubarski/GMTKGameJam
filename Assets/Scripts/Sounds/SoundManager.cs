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

        AudioSourcePoolable pooledAudio = TakePooledAudio();

        if (pooledAudio == null)
        {
            return null;
        }

        ConfigureSource(
            pooledAudio,
            clip,
            volume,
            pitch,
            false,
            0f
        );

        pooledAudio.Source.Play();

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

        AudioSourcePoolable pooledAudio = TakePooledAudio();

        if (pooledAudio == null)
        {
            return null;
        }

        pooledAudio.transform.position = position;

        ConfigureSource(
            pooledAudio,
            clip,
            volume,
            pitch,
            false,
            1f
        );

        pooledAudio.Source.Play();

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

        AudioSourcePoolable pooledAudio = TakePooledAudio();

        if (pooledAudio == null)
        {
            return null;
        }

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

        AudioSourcePoolable pooledAudio = TakePooledAudio();

        if (pooledAudio == null)
        {
            return null;
        }

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

        if (pooledAudio.Source != null)
        {
            pooledAudio.Source.Stop();
        }

        AudioPool pool = AudioPool.Instance;

        /*
         * There is nowhere to put it back. This is the game shutting down, or
         * the pool having been taken away, and neither is worth crashing over:
         * the sound is stopped and the object is simply let go of.
         */
        if (pool == null)
        {
            return;
        }

        pooledAudio.transform.SetParent(pool.transform);
        pooledAudio.transform.localPosition = Vector3.zero;

        pool.Return(pooledAudio);
    }

    /// <summary>
    /// One free audio source, or null when there is no pool to take it from.
    /// Every way of playing a sound goes through here, so none of them has to
    /// find out the hard way that the pool is gone.
    /// </summary>
    private AudioSourcePoolable TakePooledAudio()
    {
        AudioPool pool = AudioPool.Instance;

        if (pool == null)
        {
            Debug.LogError(
                "No AudioPool exists, so nothing can be played. The " +
                "AudioManager holding it is missing from the game."
            );

            return null;
        }

        AudioSourcePoolable pooledAudio = pool.Get();

        if (pooledAudio == null || pooledAudio.Source == null)
        {
            return null;
        }

        return pooledAudio;
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

        return true;
    }

    private void OnDestroy()
    {
        StopAllLoops();
    }
}