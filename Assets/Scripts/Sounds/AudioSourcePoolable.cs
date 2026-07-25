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

        _audioSource.clip = null;
        _audioSource.pitch = 1f;
        _audioSource.volume = 1f;
        _audioSource.loop = false;
        _audioSource.playOnAwake = false;

        // Reset 2D/3D settings.
        _audioSource.spatialBlend = 0f;
        _audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        _audioSource.minDistance = 1f;
        _audioSource.maxDistance = 500f;
        _audioSource.dopplerLevel = 0f;
        _audioSource.spread = 0f;
        _audioSource.panStereo = 0f;

        if (AudioPool.Instance != null)
        {
            transform.SetParent(AudioPool.Instance.transform);
        }

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    private void OnDestroy()
    {
        if (_audioSource != null)
        {
            _audioSource.DOKill();
        }
    }
}