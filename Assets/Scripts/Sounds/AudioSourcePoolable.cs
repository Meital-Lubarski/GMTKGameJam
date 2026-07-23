using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioSourcePoolable : MonoBehaviour, IPoolable
{
    private AudioSource _audioSource;

    public AudioSource Source => _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f;
    }

    public void Reset()
    {
        _audioSource.DOKill();
        _audioSource.Stop();

        _audioSource.pitch = 1f;
        _audioSource.volume = 1f;
        _audioSource.loop = false;
        _audioSource.clip = null;
        _audioSource.spatialBlend = 0f;

        if (AudioPool.Instance != null)
        {
            transform.SetParent(AudioPool.Instance.transform);
        }

        transform.localPosition = Vector3.zero;
    }

    private void OnDestroy()
    {
        if (_audioSource != null)
        {
            _audioSource.DOKill();
        }
    }
}